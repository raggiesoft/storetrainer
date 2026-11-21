// ================================================================================
// FILE: BookDataService.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file is the **Ocean View Archives Publication & Markdown-to-AST Compiler**
//     for **RaggieSoft Book Reader** (`BookReader.exe`) on Windows (.NET 10).
//
//   What files on `https://assets.raggiesoft.com` (or `%LOCALAPPDATA%` cache) feed into it?
//     1. `/raggiesoft-books/books/catalog.json`
//        -> Master list of all published Ocean View Archives & Story Crossroads books
//           (`LoadCatalogAsync`).
//     2. `/raggiesoft-books/books/<slug>/toc.json`
//        -> Authoritative 4-level Table of Contents (`Era -> Chapter -> Part -> Scene`)
//           and interactive Story Crossroads segment graphs (`LoadBookTocAsync`).
//     3. `/raggiesoft-books/css/themes/<slug>.css`
//        -> Custom per-book / per-chapter color stylesheets (`LoadNarrativeThemePaletteAsync`).
//     4. `/raggiesoft-books/books/<slug>/eXXX/cXXX/pXXX.md`
//        -> Raw Markdown chapter installments with YAML frontmatter (`LoadAndParsePartAsync`).
//
//   How does `ParseMarkdownPart` turn a raw `.md` file into a rich desktop reading view?
//     Think of `BookDataService` as a two-stage compiler—just like how our PHP CMS
//     parses Markdown into HTML on the web, except here we parse Markdown into
//     strongly typed C# objects (`ParsedChapterPart` -> `BookContentBlock` -> `RichTextSpan`)
//     that `RaggieSoftFlowDocumentBuilder` renders natively in WPF:
//       - **Stage 1 (`ParseYamlFrontmatter`)**: Reads the `--- ... ---` metadata header
//         at the top of `pXXX.md` (`title`, `subtitle`, `date`, `stardate`, `start_time`,
//         `end_time`, `timezone`, `location`, `pov`, `characters`, `theme`, `audio`,
//         `playlist`, `audio_start`, `image`/`images`, `image_alt`/`image_alts`, and
//         `backstage` bonus dossier slugs).
//       - **Stage 2 (`ParseMarkdownBodyIntoBlocks` + `ParseInlineSpans`)**: Scans the
//         prose body line-by-line to recognize 12 block structures (carousels, Zoë AI
//         kernel logs, `<figure>` plates, fenced code logs, `***` scene dividers,
//         `#`/`##`/`###` headings, standalone images, Markdown tables, GitHub callout
//         alerts `> [!NOTE]`, corporate email blockquotes `> **From:**`, lists, and
//         paragraphs) and 13 inline formatting / custom OVA narrative tags (`<aac>`,
//         `<pa>`, `<led>`, `<sms>`, `<asl>`, `<sgn>`, `<lang>`, `[credential ...]`,
//         links, code, bold-italic, bold, and italic)!
// ================================================================================

using System.Globalization;           // `NumberStyles` & `CultureInfo.InvariantCulture` for parsing `audio_start` seconds.
using System.Net;                     // `WebUtility.HtmlDecode` for decoding HTML entities (`&amp;`, `&mdash;`, etc.).
using System.Text.Json;               // `JsonSerializer` & `JsonSerializerOptions` for `catalog.json` and `toc.json`.
using System.Text.RegularExpressions; // `Regex` & `Match` for Markdown block and inline OVA tag tokenization.
using RaggieSoft.Core.Models;         // All book, TOC, AST block, inline span, and theme models.

namespace RaggieSoft.Core.Services;

/// <summary>
/// Loads Ocean View Archives book catalogs, Tables of Contents, custom narrative theme palettes,
/// and <c>pXXX.md</c> chapter installments, compiling YAML frontmatter and Markdown + custom OVA
/// tags into native <see cref="ParsedChapterPart"/> AST models.
/// </summary>
public sealed class BookDataService
{
    private readonly RaggieSoftWorkspaceLocator _locator;

    // Caches parsed `NarrativeThemePalette` objects in memory by theme slug (case-insensitively)
    // so flipping between chapters that share the same theme stylesheet requires zero re-parsing!
    private readonly Dictionary<string, NarrativeThemePalette> _themePaletteCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // --- Pre-Compiled Markdown & HTML Helper Regexes ---

    // Matches a line containing ONLY a Markdown image `![alt caption](path/to/image.jpg#inline)`.
    private static readonly Regex StandaloneMarkdownImageRegex = new(
        @"^\s*!\[(?<alt>[^\]]*)\]\((?<src>[^\)]+)\)\s*$",
        RegexOptions.Compiled);

    // Matches any Markdown image `![alt](src)` inside a ` ````carousel ` slide block.
    private static readonly Regex AnyMarkdownImageRegex = new(
        @"!\[(?<alt>[^\]]*)\]\((?<src>[^\)]+)\)",
        RegexOptions.Compiled);

    // Extracts `src="..."`, `alt="..."`, and `<figcaption>...</figcaption>` from raw HTML `<figure>` blocks.
    private static readonly Regex HtmlImgSrcRegex = new(
        @"<img\b[^>]*\bsrc=""(?<src>[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlImgAltRegex = new(
        @"<img\b[^>]*\balt=""(?<alt>[^""]*)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlFigCaptionRegex = new(
        @"<figcaption\b[^>]*>(?<cap>[\s\S]*?)</figcaption>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches unordered bullet list lines (`- item`, `* item`, `+ item`) and numbered list lines (`1. item`).
    private static readonly Regex BulletLineRegex = new(
        @"^\s*[-*+]\s+(?<item>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex NumberedLineRegex = new(
        @"^\s*\d+\.\s+(?<item>.+)$",
        RegexOptions.Compiled);

    // Matches the second line of a GitHub-Flavored Markdown table (`| --- | :---: | ---: |`).
    private static readonly Regex TableSeparatorRowRegex = new(
        @"^\s*\|?(?:\s*:?-{3,}:?\s*\|)+\s*:?-{3,}:?\s*\|?\s*$",
        RegexOptions.Compiled);

    // Matches GitHub-style blockquote alert headers (`> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`, `> [!CAUTION]`).
    private static readonly Regex GitHubAlertHeaderRegex = new(
        @"^\s*\[!(?<kind>NOTE|TIP|IMPORTANT|WARNING|CAUTION)\]\s*(?<rest>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Master 13-branch inline tokenizer matching custom Ocean View Archives narrative tags AND
    // standard Markdown inline formatting in a single pass!
    // Teaching tips on two subtle regex tricks here:
    //   1. `(?<!!)\[(?<linklabel>...)\]\((?<linkurl>...)\)`: `(?<!!)` is a **negative lookbehind**
    //      meaning "match `[label](url)` ONLY if the character right before `[` is NOT `!`"—so
    //      Markdown images `![alt](src)` aren't accidentally treated as text hyperlinks!
    //   2. `\*\*\*(?<bolditalic>...)\*\*\*` comes BEFORE `\*\*` and `\*` because regex alternation (`|`)
    //      tries branches left-to-right; checking 3 stars first prevents `***word***` from being
    //      eaten prematurely by the 2-star `**bold**` rule!
    private static readonly Regex InlineMarkdownAndOvaTokenRegex = new(
        @"<aac\b[^>]*>(?<aac>[\s\S]*?)</aac>" +
        @"|<pa\b[^>]*>(?<pa>[\s\S]*?)</pa>" +
        @"|<led\b(?<ledattrs>[^>]*?)(?:/>|>(?<led>[\s\S]*?)</led>)" +
        @"|<sms\b[^>]*>(?<sms>[\s\S]*?)</sms>" +
        @"|<asl\b[^>]*>(?<asl>[\s\S]*?)</asl>" +
        @"|<sgn\b[^>]*>(?<sgn>[\s\S]*?)</sgn>" +
        @"|<lang\b[^>]*>(?<lang>[\s\S]*?)</lang>" +
        @"|\[credential(?<cred>[^\]]*)\]" +
        @"|(?<!!)\[(?<linklabel>[^\]]+)\]\((?<linkurl>[^\)]+)\)" +
        @"|`(?<code>[^`]+)`" +
        @"|\*\*\*(?<bolditalic>[^*]+)\*\*\*" +
        @"|\*\*(?<bold>[^*]+)\*\*" +
        @"|\*(?<italic>[^*\n]+)\*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public BookDataService(RaggieSoftWorkspaceLocator locator)
    {
        _locator = locator;
    }

    /// <summary>
    /// Loads the complete Ocean View Archives master book catalog (<c>/raggiesoft-books/books/catalog.json</c>),
    /// resolving cover image URIs and local offline installation status for each visible novel.
    /// </summary>
    public async Task<List<BookCatalogItem>> LoadCatalogAsync()
    {
        var json = await _locator.ReadAssetTextAsync("raggiesoft-books/books/catalog.json");
        var items = JsonSerializer.Deserialize<List<BookCatalogItem>>(json, JsonOptions) ?? [];

        foreach (var item in items)
        {
            item.ResolvedCoverUri = _locator.ResolveAssetUri(item.Image);
            item.IsInstalledOffline = _locator.IsBookPackageInstalledOffline(item.Slug);
        }

        // Filter out hidden/unpublished volumes (`!i.Hide`) and sort alphabetically by `Title`.
        return items.Where(i => !i.Hide).OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Loads the hierarchical Table of Contents (<c>Eras -&gt; Chapters -&gt; Parts</c>) for a novel
    /// and normalizes all structural labels once during loading.
    /// </summary>
    public async Task<BookToc> LoadBookTocAsync(string bookSlug)
    {
        var json = await _locator.ReadAssetTextAsync($"raggiesoft-books/books/{bookSlug}/toc.json");
        var toc = JsonSerializer.Deserialize<BookToc>(json, JsonOptions) ?? new BookToc { SeriesSlug = bookSlug };

        // C# local function (`static void EnrichEraList(...)`): a private helper scoped right inside
        // `LoadBookTocAsync` (`static` guarantees it doesn't accidentally capture outer variables).
        // It stamps every `BookPart` with its parent `SeriesSlug`, `EraTitle`, and `ChapterTitle` so
        // `RaggieSoftFlowDocumentBuilder` can render the `"Episode 1  •  Chapter 2"` eyebrow above every part!
        static void EnrichEraList(IEnumerable<BookEra> eraList, string slug)
        {
            foreach (var era in eraList)
            {
                var normalizedEraTitle = era.DisplayHeader;
                foreach (var chap in era.Chapters)
                {
                    var normalizedChapTitle = chap.DisplayHeader;
                    foreach (var part in chap.Parts)
                    {
                        part.SeriesSlug = slug;
                        part.EraTitle = normalizedEraTitle;
                        part.ChapterTitle = normalizedChapTitle;
                    }
                }
            }
        }

        // Enrich both standard linear Eras and any branching Story Crossroads segments/paths.
        EnrichEraList(toc.Eras, bookSlug);
        EnrichEraList(toc.TrunkEras, bookSlug);
        foreach (var seg in toc.Segments.Values)
        {
            EnrichEraList(seg.Eras, bookSlug);
        }
        foreach (var path in toc.Paths.Values)
        {
            EnrichEraList(path.Eras, bookSlug);
        }

        return toc;
    }

    /// <summary>
    /// Loads and caches the custom narrative theme stylesheet (<c>/raggiesoft-books/css/themes/&lt;slug&gt;.css</c>)
    /// declared in a chapter's YAML frontmatter (<c>theme:</c>), falling back cleanly to the default reader palette.
    /// </summary>
    public async Task<NarrativeThemePalette> LoadNarrativeThemePaletteAsync(string? themeSlug)
    {
        // No `theme:` in YAML -> return standard Light / Sepia / Dark reader colors.
        if (string.IsNullOrWhiteSpace(themeSlug))
        {
            return new NarrativeThemePalette();
        }

        var cleanSlug = themeSlug.Trim();
        if (cleanSlug.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
        {
            // `cleanSlug[..^4]` strips the last 4 characters (`".css"`).
            cleanSlug = cleanSlug[..^4];
        }

        // Fast cache hit if we already parsed this stylesheet earlier in the session.
        if (_themePaletteCache.TryGetValue(cleanSlug, out var cached))
        {
            return cached;
        }

        try
        {
            var cssText = await _locator.ReadAssetTextAsync($"raggiesoft-books/css/themes/{cleanSlug}.css");
            var parsed = NarrativeThemeCssParser.Parse(cleanSlug, cssText);
            _themePaletteCache[cleanSlug] = parsed;
            return parsed;
        }
        catch
        {
            // Cache the default fallback under `cleanSlug` too so offline reading never repeatedly retries a missing CSS file.
            var fallback = new NarrativeThemePalette { ThemeSlug = cleanSlug };
            _themePaletteCache[cleanSlug] = fallback;
            return fallback;
        }
    }

    /// <summary>
    /// Loads and parses a specific Part (<c>pXXX.md</c>) from local cache or Production CDN.
    /// </summary>
    public async Task<ParsedChapterPart> LoadAndParsePartAsync(string bookSlug, string relativeFilePath)
    {
        var cleanFile = relativeFilePath.TrimStart('/');
        var rawMarkdown = await _locator.ReadAssetTextAsync(
            $"raggiesoft-books/books/{bookSlug}/{cleanFile}",
            $"books/{bookSlug}/{cleanFile}");

        return ParseMarkdownPart(bookSlug, cleanFile, rawMarkdown);
    }

    /// <summary>
    /// Parses complete YAML frontmatter, Hero slide(s), block-level Markdown, custom OVA tags,
    /// soundtrack cues, and Backstage dossier slugs from a <c>pXXX.md</c> string.
    /// </summary>
    public ParsedChapterPart ParseMarkdownPart(string bookSlug, string filePath, string rawMarkdown)
    {
        var result = new ParsedChapterPart
        {
            SeriesSlug = bookSlug,
            FilePath = filePath
        };

        // Normalize Windows `\r\n` line endings to Unix `\n` so every string slice and `\n` split
        // behaves identically regardless of which OS saved the `.md` file.
        var normalized = rawMarkdown.Replace("\r\n", "\n");
        var body = normalized;

        // 1. Extract YAML frontmatter block between leading `---` delimiters:
        //    ---
        //    title: "..."
        //    date: "October 14, 2016"
        //    ---
        //    Prose starts here...
        if (normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            var endIdx = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
            if (endIdx > 0)
            {
                var frontmatter = normalized[4..endIdx];
                body = normalized[(endIdx + 5)..];
                ParseYamlFrontmatter(frontmatter, result);
            }
        }

        result.RawMarkdownBody = body.Trim();

        // 2. Parse block-level Markdown into semantic AST blocks (`result.Blocks`).
        ParseMarkdownBodyIntoBlocks(result);

        return result;
    }

    // Lightweight, zero-dependency YAML frontmatter parser tailored to the Ocean View Archives
    // chapter contract—supporting scalar `key: value` pairs, multi-line `- item` lists, AND
    // inline bracketed `[item1, item2]` lists!
    private void ParseYamlFrontmatter(string yaml, ParsedChapterPart target)
    {
        // Remembers the active YAML key when a line like `images:` or `characters:` introduces
        // indented `- value` bullet items on the lines immediately below it.
        string? currentListKey = null;
        var rawImages = new List<string>();
        var rawAlts = new List<string>();

        foreach (var rawLine in yaml.Split('\n'))
        {
            var trimmed = rawLine.Trim();
            // Skip blank lines and `# ...` YAML comments.
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            // Multi-line YAML list item (`- value`) belonging to `currentListKey`
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) && currentListKey is not null)
            {
                var itemVal = UnquoteYamlScalar(trimmed[2..]);
                if (!string.IsNullOrWhiteSpace(itemVal))
                {
                    AppendYamlListValue(currentListKey, itemVal, target, rawImages, rawAlts);
                }
                continue;
            }

            // Every other YAML line has the shape `key: value` (split at the FIRST `:` only, so
            // values containing colons like `start_time: "9:15 AM"` stay intact!).
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx <= 0)
            {
                continue;
            }

            var key = trimmed[..colonIdx].Trim().ToLowerInvariant();
            var rawVal = trimmed[(colonIdx + 1)..].Trim();

            // Empty value after `:` (e.g. `images:`) starts a multi-line YAML list on subsequent `- ` lines.
            if (string.IsNullOrWhiteSpace(rawVal) || rawVal is "|" or ">")
            {
                currentListKey = key;
                continue;
            }

            currentListKey = null;

            // Inline YAML bracketed array: `characters: ["Rachel", "Isabel", "Eleanor"]`
            if (rawVal.StartsWith('[') && rawVal.EndsWith(']'))
            {
                // `rawVal[1..^1]` slices off `[` at index 0 and `]` at the end!
                var innerList = rawVal[1..^1];
                foreach (var piece in SplitInlineYamlArray(innerList))
                {
                    var cleanedPiece = UnquoteYamlScalar(piece);
                    if (!string.IsNullOrWhiteSpace(cleanedPiece))
                    {
                        AppendYamlListValue(key, cleanedPiece, target, rawImages, rawAlts);
                    }
                }
                continue;
            }

            // Standard scalar `key: "value"` assignment across all supported Ocean View Archives YAML fields:
            var val = UnquoteYamlScalar(rawVal);
            switch (key)
            {
                // Primary installment heading & optional subtitle
                case "title":
                    target.Title = val;
                    break;
                case "subtitle":
                    target.Subtitle = val;
                    break;
                // Earth calendar date (`date:`) or fantasy/in-universe date (`realm_time:`)
                case "date":
                case "realm_time":
                    if (string.IsNullOrWhiteSpace(target.Date))
                    {
                        target.Date = val;
                    }
                    break;
                // Deep-space stardate (`books/ad-astra`) and scene clock window (`start_time`–`end_time`, `timezone`)
                case "stardate":
                    target.Stardate = val;
                    break;
                case "start_time":
                    target.StartTime = val;
                    break;
                case "end_time":
                    target.EndTime = val;
                    break;
                case "timezone":
                    target.Timezone = val;
                    break;
                // Physical setting & active Point-of-View character (`pov:`)
                case "location":
                    target.Location = val;
                    break;
                case "pov":
                    target.Pov = val;
                    break;
                // Comma-separated character cast (`characters: "Rachel, Isabel, Eleanor"`)
                case "characters":
                    foreach (var ch in val.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var cleanCh = ch.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanCh))
                        {
                            target.Characters.Add(cleanCh);
                        }
                    }
                    break;
                // Chapter summary / OpenGraph description & custom CSS color theme (`theme:`)
                case "summary":
                case "description":
                    if (string.IsNullOrWhiteSpace(target.Summary))
                    {
                        target.Summary = val;
                    }
                    break;
                case "theme":
                    target.Theme = val;
                    break;
                // Soundtrack track(s), curated `playlist:`, custom banner title, and `audio_start:` seek offset
                case "audio":
                    target.AudioSpecs.Add(val);
                    if (string.IsNullOrWhiteSpace(target.AudioRelativePath))
                    {
                        target.AudioRelativePath = val;
                    }
                    break;
                case "playlist":
                    target.Playlist = val;
                    break;
                case "audio_playlist_title":
                    target.AudioPlaylistTitle = val;
                    break;
                case "audio_start":
                    target.AudioStartRaw = val;
                    target.AudioStartSeconds = ParseAudioStartToSeconds(val);
                    break;
                // Hero scene plate(s) (`image:` / `images:`), `og_image:` fallback, alt captions, and `backstage:` slugs
                case "image":
                case "images":
                    rawImages.Add(val);
                    break;
                case "og_image":
                    // Use `og_image` only if no primary 16:9 `image` / `images` plate was specified.
                    if (rawImages.Count == 0)
                    {
                        rawImages.Add(val);
                    }
                    break;
                case "image_alt":
                case "image_alts":
                    rawAlts.Add(val);
                    break;
                case "backstage":
                    target.BackstageSlugs.Add(val.Trim('/'));
                    break;
            }
        }

        // Pair up `rawImages[i]` with its corresponding `rawAlts[i]` caption into `target.HeroSlides`.
        for (var i = 0; i < rawImages.Count; i++)
        {
            var resolvedImg = _locator.ResolveAssetUri(rawImages[i]);
            var alt = i < rawAlts.Count ? rawAlts[i] : (rawAlts.Count > 0 ? rawAlts[0] : string.Empty);
            if (!string.IsNullOrWhiteSpace(resolvedImg))
            {
                target.HeroSlides.Add(new BookSlideImage
                {
                    Src = resolvedImg,
                    Alt = alt
                });
            }
        }

        if (target.HeroSlides.Count > 0)
        {
            target.HeroImageUri = target.HeroSlides[0].Src;
            target.HeroImageAlt = target.HeroSlides[0].Alt;
        }
    }

    // Routes an individual YAML list item (`images`, `image_alts`, `audio`, `backstage`, `characters`)
    // into the matching list on `target`.
    private static void AppendYamlListValue(
        string key,
        string itemVal,
        ParsedChapterPart target,
        List<string> rawImages,
        List<string> rawAlts)
    {
        switch (key)
        {
            case "image":
            case "images":
                rawImages.Add(itemVal);
                break;
            case "image_alt":
            case "image_alts":
                rawAlts.Add(itemVal);
                break;
            case "audio":
                target.AudioSpecs.Add(itemVal);
                if (string.IsNullOrWhiteSpace(target.AudioRelativePath))
                {
                    target.AudioRelativePath = itemVal;
                }
                break;
            case "backstage":
                target.BackstageSlugs.Add(itemVal.Trim('/'));
                break;
            case "characters":
                target.Characters.Add(itemVal);
                break;
        }
    }

    // Splits an inline YAML array (`" Norfolk, VA ", "Virginia Beach, VA"`) on commas `,`,
    // while smartly ignoring commas that appear INSIDE `'...'` or `"..."` quotes!
    // (`StringBuilder` is C#'s mutable string buffer so appending character-by-character doesn't
    // allocate temporary string copies on every keystroke.)
    private static IEnumerable<string> SplitInlineYamlArray(string raw)
    {
        var items = new List<string>();
        var current = new System.Text.StringBuilder();
        var inSingle = false;
        var inDouble = false;

        foreach (var ch in raw)
        {
            if (ch == '\'' && !inDouble)
            {
                inSingle = !inSingle;
                current.Append(ch);
            }
            else if (ch == '"' && !inSingle)
            {
                inDouble = !inDouble;
                current.Append(ch);
            }
            else if (ch == ',' && !inSingle && !inDouble)
            {
                items.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            items.Add(current.ToString());
        }

        return items;
    }

    // Strips outer `"..."` or `'...'` YAML quotes and unescapes `\"` -> `"`.
    private static string UnquoteYamlScalar(string raw)
    {
        var t = raw.Trim();
        if (t.Length >= 2 &&
            ((t.StartsWith('"') && t.EndsWith('"')) || (t.StartsWith('\'') && t.EndsWith('\''))))
        {
            return t[1..^1].Replace("\\\"", "\"");
        }
        return t;
    }

    /// <summary>
    /// Converts a YAML <c>audio_start:</c> cue value (such as <c>"42.5"</c>, <c>"01:32"</c>, or <c>"00:02:15.5"</c>)
    /// into non-negative seconds for <c>MediaPlayer</c> seeking.
    /// </summary>
    public static double ParseAudioStartToSeconds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0;
        }

        var trimmed = raw.Trim();
        // Plain seconds number (`"42.5"`)
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var directSec))
        {
            return Math.Max(0, directSec);
        }

        // `MM:SS` format (`"01:32"`)
        var parts = trimmed.Split(':');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mm) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var ss))
        {
            return Math.Max(0, mm * 60.0 + ss);
        }

        // `HH:MM:SS` format (`"01:02:15"`)
        if (parts.Length == 3 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hh) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var m2) &&
            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var s2))
        {
            return Math.Max(0, hh * 3600.0 + m2 * 60.0 + s2);
        }

        return 0;
    }

    /// <summary>
    /// Full semantic line-by-line Markdown parser supporting headings, scene anchors,
    /// multi-slide carousels, tables, lists, callout alerts, corporate correspondence
    /// blockquotes, Zoë kernel logs, inline/fullwidth figures, and custom OVA tags.
    /// </summary>
    private void ParseMarkdownBodyIntoBlocks(ParsedChapterPart result)
    {
        var lines = result.RawMarkdownBody.Split('\n');
        var i = 0;
        var sceneCounter = 0;

        // State-machine cursor loop: each recognized Markdown construct consumes 1 or more lines
        // by advancing `i`, appends its typed `BookContentBlock` onto `result.Blocks`, and `continue`s!
        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Skip blank separator lines between blocks.
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                i++;
                continue;
            }

            // 1. Multi-slide ````carousel ... ```` block (four-backtick fenced slide gallery)
            if (trimmed.StartsWith("````carousel", StringComparison.OrdinalIgnoreCase))
            {
                i++;
                var slides = new List<BookSlideImage>();
                while (i < lines.Length && !lines[i].Trim().Equals("````", StringComparison.Ordinal))
                {
                    var slideLine = lines[i].Trim();
                    var m = AnyMarkdownImageRegex.Match(slideLine);
                    if (m.Success)
                    {
                        var resolved = _locator.ResolveAssetUri(m.Groups["src"].Value);
                        if (!string.IsNullOrWhiteSpace(resolved))
                        {
                            slides.Add(new BookSlideImage
                            {
                                Src = resolved,
                                Alt = m.Groups["alt"].Value.Trim()
                            });
                        }
                    }
                    i++;
                }
                if (i < lines.Length && lines[i].Trim().Equals("````", StringComparison.Ordinal))
                {
                    i++;
                }

                if (slides.Count == 1)
                {
                    AddStandaloneIllustrationIfNotDuplicateHero(result, slides[0].Src, slides[0].Alt, isInline: false);
                }
                else if (slides.Count > 1)
                {
                    result.Blocks.Add(new BookContentBlock
                    {
                        BlockType = BookBlockType.ImageCarousel,
                        CarouselSlides = slides
                    });
                }
                continue;
            }

            // Skip stray HTML slide comments (e.g. `<!-- slide -->`)
            if (trimmed.StartsWith("<!--", StringComparison.Ordinal) && trimmed.EndsWith("-->", StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            // 2. Zoë Kernel Log HTML container (`<div class="zoe-kernel-log"> ... </div>`)
            //    Renders Zoë's cognitive/telemetry readouts in a dedicated dark-slate terminal card!
            if (trimmed.Contains("zoe-kernel-log", StringComparison.OrdinalIgnoreCase) &&
                trimmed.StartsWith("<div", StringComparison.OrdinalIgnoreCase))
            {
                var logLines = new List<string>();
                if (!trimmed.EndsWith("</div>", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    while (i < lines.Length && !lines[i].Contains("</div>", StringComparison.OrdinalIgnoreCase))
                    {
                        logLines.Add(WebUtility.HtmlDecode(Regex.Replace(lines[i], "<[^>]+>", string.Empty)));
                        i++;
                    }
                    if (i < lines.Length) i++;
                }
                else
                {
                    logLines.Add(WebUtility.HtmlDecode(Regex.Replace(trimmed, "<[^>]+>", string.Empty)));
                    i++;
                }

                var logBody = string.Join("\n", logLines).Trim();
                if (!string.IsNullOrWhiteSpace(logBody))
                {
                    result.Blocks.Add(new BookContentBlock
                    {
                        BlockType = BookBlockType.CodeOrKernelLog,
                        IsZoeKernelLog = true,
                        Text = logBody
                    });
                }
                continue;
            }

            // 3. Multi-line HTML `<figure> ... </figure>` illustration block
            if (trimmed.StartsWith("<figure", StringComparison.OrdinalIgnoreCase))
            {
                var figBuilder = new System.Text.StringBuilder();
                while (i < lines.Length)
                {
                    figBuilder.AppendLine(lines[i]);
                    if (lines[i].Contains("</figure>", StringComparison.OrdinalIgnoreCase))
                    {
                        i++;
                        break;
                    }
                    i++;
                }

                var figHtml = figBuilder.ToString();
                var srcMatch = HtmlImgSrcRegex.Match(figHtml);
                if (srcMatch.Success)
                {
                    var rawSrc = WebUtility.HtmlDecode(srcMatch.Groups["src"].Value);
                    var isInline = rawSrc.Contains("#inline", StringComparison.OrdinalIgnoreCase) ||
                                   figHtml.Contains("inline", StringComparison.OrdinalIgnoreCase);
                    var resolved = _locator.ResolveAssetUri(rawSrc);
                    var capMatch = HtmlFigCaptionRegex.Match(figHtml);
                    var altMatch = HtmlImgAltRegex.Match(figHtml);
                    var caption = capMatch.Success
                        ? WebUtility.HtmlDecode(Regex.Replace(capMatch.Groups["cap"].Value, "<[^>]+>", string.Empty)).Trim()
                        : (altMatch.Success ? WebUtility.HtmlDecode(altMatch.Groups["alt"].Value).Trim() : string.Empty);

                    AddStandaloneIllustrationIfNotDuplicateHero(result, resolved, caption, isInline);
                }
                continue;
            }

            // 4. Fenced Code / Log Block (``` ... ```)
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var fenceInfo = trimmed[3..].Trim();
                var isKernel = fenceInfo.Contains("zoe", StringComparison.OrdinalIgnoreCase) ||
                               fenceInfo.Contains("kernel", StringComparison.OrdinalIgnoreCase) ||
                               fenceInfo.Contains("log", StringComparison.OrdinalIgnoreCase);
                i++;
                var codeLines = new List<string>();
                while (i < lines.Length && !lines[i].Trim().StartsWith("```", StringComparison.Ordinal))
                {
                    codeLines.Add(lines[i]);
                    i++;
                }
                if (i < lines.Length) i++;

                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.CodeOrKernelLog,
                    IsZoeKernelLog = isKernel,
                    Text = string.Join("\n", codeLines).TrimEnd()
                });
                continue;
            }

            // 5. Scene Divider (`***`, `---`, `___`, `* * *`, or `<hr>`)
            //    (`trimmed is "***" or "---" ...` is C# 9+ disjunctive pattern matching—super readable!)
            if (trimmed is "***" or "---" or "___" or "* * *" ||
                trimmed.StartsWith("<hr", StringComparison.OrdinalIgnoreCase))
            {
                result.Blocks.Add(new BookContentBlock { BlockType = BookBlockType.SceneDivider });
                i++;
                continue;
            }

            // 6. Headings (`#`, `##`, `###`, `####`)
            //    `## ` headings become Level-4 `BookSceneAnchor` stops (`scene-1`, `scene-2`, ...)
            //    powering the toolbar "Scenes" menu and the top-of-chapter Mini-TOC jump card!
            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                var headingText = StripInlineFormattingMarkers(trimmed[3..].Trim());
                sceneCounter++;
                var anchorId = $"scene-{sceneCounter}";
                result.SceneHeadings.Add(headingText);
                result.SceneAnchors.Add(new BookSceneAnchor
                {
                    SceneIndex = sceneCounter,
                    AnchorId = anchorId,
                    Title = headingText
                });
                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.SceneHeading,
                    AnchorId = anchorId,
                    Text = headingText
                });
                i++;
                continue;
            }

            if (trimmed.StartsWith("### ", StringComparison.Ordinal) ||
                trimmed.StartsWith("#### ", StringComparison.Ordinal))
            {
                var hashEnd = trimmed.IndexOf(' ');
                var subText = StripInlineFormattingMarkers(trimmed[(hashEnd + 1)..].Trim());
                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.Subheading,
                    Text = subText
                });
                i++;
                continue;
            }

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                var h1Text = StripInlineFormattingMarkers(trimmed[2..].Trim());
                // Avoid duplicating the YAML Part Title if the Markdown file repeats `# Same Title` on line 1!
                if (!string.Equals(h1Text, result.Title, StringComparison.OrdinalIgnoreCase))
                {
                    result.Blocks.Add(new BookContentBlock
                    {
                        BlockType = BookBlockType.Heading1,
                        Text = h1Text
                    });
                }
                i++;
                continue;
            }

            // 7. Standalone Markdown Illustration (`![alt](src#fullwidth)` or `![alt](src#inline)`)
            var standaloneImg = StandaloneMarkdownImageRegex.Match(trimmed);
            if (standaloneImg.Success)
            {
                var rawSrc = standaloneImg.Groups["src"].Value.Trim();
                var isInline = rawSrc.Contains("#inline", StringComparison.OrdinalIgnoreCase);
                var resolved = _locator.ResolveAssetUri(rawSrc);
                var alt = standaloneImg.Groups["alt"].Value.Trim();
                AddStandaloneIllustrationIfNotDuplicateHero(result, resolved, alt, isInline);
                i++;
                continue;
            }

            // 8. Markdown Pipe Table (`| Col A | Col B |` followed immediately by `| --- | --- |`)
            if (trimmed.StartsWith('|') && i + 1 < lines.Length && TableSeparatorRowRegex.IsMatch(lines[i + 1]))
            {
                var headers = SplitMarkdownTableRow(lines[i]);
                i += 2; // Skip header + separator
                var rows = new List<List<string>>();
                while (i < lines.Length && lines[i].Trim().StartsWith('|'))
                {
                    rows.Add(SplitMarkdownTableRow(lines[i]));
                    i++;
                }

                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.Table,
                    Table = new MarkdownTableModel { Headers = headers, Rows = rows }
                });
                continue;
            }

            // 9. Blockquote (`>`), GitHub Callout Alert (`> [!NOTE]`), or Corporate Email (`> **From:**`)
            if (trimmed.StartsWith('>'))
            {
                var quoteLines = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('>'))
                {
                    var q = lines[i].TrimStart();
                    q = q.Length > 1 && q[1] == ' ' ? q[2..] : q[1..];
                    quoteLines.Add(q);
                    i++;
                }

                BuildQuoteOrEmailOrAlertBlock(quoteLines, result);
                continue;
            }

            // 10. Unordered Bullet List (`- item` / `* item`)
            if (BulletLineRegex.IsMatch(line))
            {
                var items = new List<List<RichTextSpan>>();
                var blockLinks = new List<BookInlineLink>();
                while (i < lines.Length)
                {
                    var m = BulletLineRegex.Match(lines[i]);
                    if (!m.Success) break;
                    items.Add(ParseInlineSpans(m.Groups["item"].Value.Trim(), blockLinks, result.MusicReferences));
                    i++;
                }

                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.BulletList,
                    ListItems = items,
                    Links = blockLinks
                });
                continue;
            }

            // 11. Ordered Numbered List (`1. item`)
            if (NumberedLineRegex.IsMatch(line))
            {
                var items = new List<List<RichTextSpan>>();
                var blockLinks = new List<BookInlineLink>();
                while (i < lines.Length)
                {
                    var m = NumberedLineRegex.Match(lines[i]);
                    if (!m.Success) break;
                    items.Add(ParseInlineSpans(m.Groups["item"].Value.Trim(), blockLinks, result.MusicReferences));
                    i++;
                }

                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.NumberedList,
                    ListItems = items,
                    Links = blockLinks
                });
                continue;
            }

            // 12. Standard Literary Paragraph:
            //     In Markdown, consecutive non-blank prose lines belong to ONE paragraph until a blank
            //     line or block-level marker (`#`, `>`, ```` ``` ````, `<figure>`, `***`, list, or table) appears.
            var paraLines = new List<string>();
            while (i < lines.Length)
            {
                var cur = lines[i];
                var curTrim = cur.Trim();
                if (string.IsNullOrWhiteSpace(curTrim) ||
                    curTrim.StartsWith('#') ||
                    curTrim.StartsWith('>') ||
                    curTrim.StartsWith("```", StringComparison.Ordinal) ||
                    curTrim.StartsWith("<figure", StringComparison.OrdinalIgnoreCase) ||
                    curTrim.StartsWith("<hr", StringComparison.OrdinalIgnoreCase) ||
                    (curTrim.StartsWith("<div", StringComparison.OrdinalIgnoreCase) && curTrim.Contains("zoe-kernel-log", StringComparison.OrdinalIgnoreCase)) ||
                    curTrim is "***" or "---" or "___" or "* * *" ||
                    StandaloneMarkdownImageRegex.IsMatch(curTrim) ||
                    BulletLineRegex.IsMatch(cur) ||
                    NumberedLineRegex.IsMatch(cur) ||
                    (curTrim.StartsWith('|') && i + 1 < lines.Length && TableSeparatorRowRegex.IsMatch(lines[i + 1])))
                {
                    break;
                }

                paraLines.Add(curTrim);
                i++;
            }

            if (paraLines.Count > 0)
            {
                var joined = JoinParagraphLinesPreservingHardBreaks(paraLines);
                var links = new List<BookInlineLink>();
                var spans = ParseInlineSpans(joined, links, result.MusicReferences);
                var plainText = string.Concat(spans.Select(s => s.Text));

                if (!string.IsNullOrWhiteSpace(plainText))
                {
                    result.Blocks.Add(new BookContentBlock
                    {
                        BlockType = BookBlockType.Paragraph,
                        Text = plainText,
                        Spans = spans,
                        Links = links
                    });
                }
            }
        }
    }

    // Appends a standalone scene illustration unless that exact image URI is already displayed
    // at the top of the chapter in `result.HeroSlides`!
    private static void AddStandaloneIllustrationIfNotDuplicateHero(
        ParsedChapterPart result,
        string resolvedUri,
        string alt,
        bool isInline)
    {
        if (string.IsNullOrWhiteSpace(resolvedUri))
        {
            return;
        }

        // Do not render the exact same image twice if it is already the Part's Hero Plate
        if (result.HeroSlides.Any(h => string.Equals(h.Src, resolvedUri, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        result.Blocks.Add(new BookContentBlock
        {
            BlockType = BookBlockType.Illustration,
            ImageUri = resolvedUri,
            ImageAlt = alt,
            Text = alt,
            IsInlineIllustration = isInline
        });
    }

    // Classifies a `>` blockquote block into one of three distinct visual presentations:
    //   1. `CalloutAlert` (`> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, `> [!WARNING]`, `> [!CAUTION]`)
    //   2. `CorrespondenceCard` (`> **From:** ... <br> > **To:** ... <br> > **Subject:** ...`)
    //   3. `Blockquote` (standard literary quotation / epigraph)
    private void BuildQuoteOrEmailOrAlertBlock(List<string> quoteLines, ParsedChapterPart result)
    {
        if (quoteLines.Count == 0)
        {
            return;
        }

        // Normalize `<br>` HTML line breaks inside quote lines into separate logical lines
        // (email headers in Ocean View Archives chapters end each header line with `<br>`).
        var expanded = quoteLines
            .SelectMany(l => Regex.Split(l, @"<br\s*/?>", RegexOptions.IgnoreCase))
            .ToList();

        // Check 1: GitHub Callout Alert (`> [!NOTE]`, `> [!IMPORTANT]`, etc.)
        var firstNonEmptyIdx = expanded.FindIndex(l => !string.IsNullOrWhiteSpace(l));
        if (firstNonEmptyIdx >= 0)
        {
            var alertMatch = GitHubAlertHeaderRegex.Match(expanded[firstNonEmptyIdx]);
            if (alertMatch.Success)
            {
                var kind = alertMatch.Groups["kind"].Value.ToUpperInvariant();
                var bodyLines = new List<string>();
                var restFirst = alertMatch.Groups["rest"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(restFirst))
                {
                    bodyLines.Add(restFirst);
                }
                bodyLines.AddRange(expanded.Skip(firstNonEmptyIdx + 1));

                var joinedAlert = JoinQuoteLines(bodyLines);
                var links = new List<BookInlineLink>();
                var spans = ParseInlineSpans(joinedAlert, links, result.MusicReferences);

                result.Blocks.Add(new BookContentBlock
                {
                    BlockType = BookBlockType.CalloutAlert,
                    AlertKind = kind,
                    Text = string.Concat(spans.Select(s => s.Text)),
                    Spans = spans,
                    Links = links
                });
                return;
            }
        }

        // Check 2: In-Universe Corporate Correspondence Email Card (`> **From:** ... <br> > **To:** ...`):
        //          Walks the leading lines of the blockquote looking for `HeaderKey: Value` pairs (`From`,
        //          `To`, `Cc`, `Bcc`, `Date`, `Sent`, `Subject`, `Re`, `Priority`) until a blank line or
        //          body sentence begins.
        var emailHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bodyStart = 0;
        var hasFromOrTo = false;

        for (var idx = 0; idx < expanded.Count; idx++)
        {
            var line = expanded[idx].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                // First blank line after collecting headers marks the start of the email body!
                if (emailHeaders.Count > 0)
                {
                    bodyStart = idx + 1;
                    break;
                }
                continue;
            }

            // Strip Markdown `**` bold markers (`**From:**` -> `From:`) before splitting at `:`.
            var strippedStars = line.Replace("**", string.Empty);
            var colon = strippedStars.IndexOf(':');
            if (colon > 0 && colon <= 18)
            {
                var headerKey = strippedStars[..colon].Trim();
                var headerVal = strippedStars[(colon + 1)..].Trim();
                if (headerKey.Equals("From", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("To", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Cc", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Bcc", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Date", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Subject", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Re", StringComparison.OrdinalIgnoreCase) ||
                    headerKey.Equals("Priority", StringComparison.OrdinalIgnoreCase))
                {
                    emailHeaders[headerKey] = headerVal;
                    if (headerKey.Equals("From", StringComparison.OrdinalIgnoreCase) ||
                        headerKey.Equals("To", StringComparison.OrdinalIgnoreCase))
                    {
                        hasFromOrTo = true;
                    }
                    bodyStart = idx + 1;
                    continue;
                }
            }
            break;
        }

        // Require at least 2 recognized headers including `From:` or `To:` so ordinary dialogue lines
        // with colons (`> Rachel: ...`) remain standard literary blockquotes.
        if (hasFromOrTo && emailHeaders.Count >= 2)
        {
            var emailBody = JoinQuoteLines(expanded.Skip(bodyStart).ToList());
            var emailLinks = new List<BookInlineLink>();
            var emailSpans = ParseInlineSpans(emailBody, emailLinks, result.MusicReferences);
            result.Blocks.Add(new BookContentBlock
            {
                BlockType = BookBlockType.CorrespondenceCard,
                EmailHeaders = emailHeaders,
                Text = string.Concat(emailSpans.Select(s => s.Text)),
                Spans = emailSpans,
                Links = emailLinks
            });
            return;
        }

        // Check 3: Standard Literary Blockquote
        var quoteText = JoinQuoteLines(expanded);
        var qLinks = new List<BookInlineLink>();
        var qSpans = ParseInlineSpans(quoteText, qLinks, result.MusicReferences);
        result.Blocks.Add(new BookContentBlock
        {
            BlockType = BookBlockType.Blockquote,
            Text = string.Concat(qSpans.Select(s => s.Text)),
            Spans = qSpans,
            Links = qLinks
        });
    }

    // Joins soft-wrapped Markdown paragraph lines with spaces while preserving explicit `<br>` line breaks as `\n`.
    private static string JoinParagraphLinesPreservingHardBreaks(List<string> lines)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = Regex.Replace(lines[i], @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            sb.Append(line);
            if (i < lines.Count - 1)
            {
                sb.Append(line.EndsWith('\n') ? string.Empty : " ");
            }
        }
        return sb.ToString();
    }

    // Groups lines inside a blockquote into `\n\n`-separated sub-paragraphs while keeping intentional line breaks.
    private static string JoinQuoteLines(List<string> lines)
    {
        var paragraphs = new List<string>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current.Count > 0)
                {
                    paragraphs.Add(string.Join("\n", current));
                    current.Clear();
                }
            }
            else
            {
                current.Add(line.Trim());
            }
        }

        if (current.Count > 0)
        {
            paragraphs.Add(string.Join("\n", current));
        }

        return string.Join("\n\n", paragraphs);
    }

    // Splits a Markdown pipe table row (`| Cell 1 | Cell 2 |`) into cleaned cell strings.
    private static List<string> SplitMarkdownTableRow(string rawRow)
    {
        var trimmed = rawRow.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];
        return trimmed
            .Split('|')
            .Select(cell => StripInlineFormattingMarkers(cell.Trim()))
            .ToList();
    }

    // Strips `[label](url)` Markdown links down to `label`, removes `**` / `` ` `` / HTML tags, and decodes HTML entities.
    private static string StripInlineFormattingMarkers(string raw)
    {
        var cleaned = Regex.Replace(raw, @"(?<!!)\[(?<lbl>[^\]]+)\]\([^\)]+\)", "${lbl}");
        cleaned = cleaned.Replace("**", string.Empty).Replace("`", string.Empty);
        cleaned = Regex.Replace(cleaned, "<[^>]+>", string.Empty);
        return WebUtility.HtmlDecode(cleaned).Trim();
    }

    /// <summary>
    /// Tokenizes a paragraph or list item into <see cref="RichTextSpan"/> runs, preserving bold, italic,
    /// code, inline links, and Ocean View Archives custom narrative tags (<c>&lt;aac&gt;</c>, <c>&lt;pa&gt;</c>,
    /// <c>&lt;led&gt;</c>, <c>&lt;sms&gt;</c>, <c>&lt;asl&gt;</c>, <c>&lt;sgn&gt;</c>, <c>&lt;lang&gt;</c>,
    /// and <c>[credential ...]</c>).
    /// </summary>
    public static List<RichTextSpan> ParseInlineSpans(
        string text,
        List<BookInlineLink> blockLinks,
        List<BookMusicReference> musicAccumulator)
    {
        var spans = new List<RichTextSpan>();
        if (string.IsNullOrEmpty(text))
        {
            return spans;
        }

        // Sliding-cursor scan across `text`:
        //   - Plain prose before each regex match becomes `RichTextSpanKind.Plain`
        //   - Each matched tag/marker becomes its semantic `RichTextSpanKind`
        //   - Any remaining prose after the final match becomes a trailing `RichTextSpanKind.Plain`
        var cursor = 0;
        foreach (Match match in InlineMarkdownAndOvaTokenRegex.Matches(text))
        {
            if (match.Index > cursor)
            {
                var plain = WebUtility.HtmlDecode(text[cursor..match.Index]);
                if (plain.Length > 0)
                {
                    spans.Add(new RichTextSpan { Kind = RichTextSpanKind.Plain, Text = plain });
                }
            }

            // Custom OVA `<aac>...</aac>`: AAC tablet speech output
            if (match.Groups["aac"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.AacSpeech,
                    Text = StripInlineFormattingMarkers(match.Groups["aac"].Value)
                });
            }
            // Custom OVA `<pa>...</pa>`: Public-address announcement
            else if (match.Groups["pa"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.PaAnnouncement,
                    Text = StripInlineFormattingMarkers(match.Groups["pa"].Value)
                });
            }
            // Custom OVA `<led ...>...</led>`: Amber dot-matrix transit destination header
            else if (match.Groups["led"].Success || match.Groups["ledattrs"].Success)
            {
                spans.Add(RichTextSpan.ParseLedSign(
                    match.Groups["ledattrs"].Value,
                    match.Groups["led"].Value));
            }
            // Custom OVA `<sms>...</sms>`: Text-message bubble
            else if (match.Groups["sms"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.SmsBubble,
                    Text = StripInlineFormattingMarkers(match.Groups["sms"].Value)
                });
            }
            // Custom OVA `<asl>...</asl>`: American Sign Language gloss pill
            else if (match.Groups["asl"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.AslSign,
                    Text = StripInlineFormattingMarkers(match.Groups["asl"].Value)
                });
            }
            // Custom OVA `<sgn>...</sgn>`: Non-verbal / tactile gesture tag
            else if (match.Groups["sgn"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.GestureTag,
                    Text = StripInlineFormattingMarkers(match.Groups["sgn"].Value)
                });
            }
            // Custom OVA `<lang>...</lang>`: Non-English phrase rendered in italics
            else if (match.Groups["lang"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.LanguageSpan,
                    Text = StripInlineFormattingMarkers(match.Groups["lang"].Value)
                });
            }
            // `[credential ...]`: In-universe credential badge
            else if (match.Groups["cred"].Success)
            {
                var credRaw = match.Groups["cred"].Value.Trim(' ', ':', '"');
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.CredentialBadge,
                    Text = string.IsNullOrWhiteSpace(credRaw) ? "Verified Credential" : credRaw
                });
            }
            // `[label](url)`: Clickable hyperlink (and automatic `music.raggiesoft.com` song extraction!)
            else if (match.Groups["linkurl"].Success)
            {
                var label = StripInlineFormattingMarkers(match.Groups["linklabel"].Value);
                var url = match.Groups["linkurl"].Value.Trim();
                var resolvedUrl = NormalizeBookOrMusicUrl(url);

                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Link,
                    Text = label,
                    LinkUrl = resolvedUrl
                });

                blockLinks.Add(new BookInlineLink
                {
                    Label = label,
                    Url = resolvedUrl
                });

                if (resolvedUrl.Contains("music.raggiesoft.com", StringComparison.OrdinalIgnoreCase))
                {
                    var musicRef = ParseMusicUrl(label, resolvedUrl);
                    if (musicRef is not null &&
                        !musicAccumulator.Any(m => string.Equals(m.Url, musicRef.Url, StringComparison.OrdinalIgnoreCase)))
                    {
                        musicAccumulator.Add(musicRef);
                    }
                }
            }
            // `` `code` ``, `***bolditalic***`, `**bold**`, and `*italic*`
            else if (match.Groups["code"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Code,
                    Text = WebUtility.HtmlDecode(match.Groups["code"].Value)
                });
            }
            else if (match.Groups["bolditalic"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.BoldItalic,
                    Text = WebUtility.HtmlDecode(match.Groups["bolditalic"].Value)
                });
            }
            else if (match.Groups["bold"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Bold,
                    Text = WebUtility.HtmlDecode(match.Groups["bold"].Value)
                });
            }
            else if (match.Groups["italic"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Italic,
                    Text = WebUtility.HtmlDecode(match.Groups["italic"].Value)
                });
            }

            cursor = match.Index + match.Length;
        }

        // Append any trailing plain text after the final inline token.
        if (cursor < text.Length)
        {
            var tail = WebUtility.HtmlDecode(text[cursor..]);
            if (tail.Length > 0)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.Plain, Text = tail });
            }
        }

        return spans;
    }

    // Expands root-relative book cross-links like `"/rachel/e001/c001/p002"` into full
    // `https://books.raggiesoft.com/...` URLs so Book Reader's click router handles them uniformly.
    private static string NormalizeBookOrMusicUrl(string rawUrl)
    {
        if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return rawUrl;
        }

        if (rawUrl.StartsWith('/'))
        {
            return $"https://books.raggiesoft.com{rawUrl}";
        }

        return rawUrl;
    }

    /// <summary>
    /// Parses a <c>https://music.raggiesoft.com/&lt;artist&gt;/&lt;album&gt;?track=&lt;trackSlug&gt;</c> URL
    /// into its component artist, album, and track slugs for IPC handoff to <c>ListeningRoom.exe</c>.
    /// </summary>
    public static BookMusicReference? ParseMusicUrl(string label, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var artistSlug = segments.Length >= 1 ? segments[0] : string.Empty;
        var albumSlug = segments.Length >= 2 ? segments[1] : string.Empty;
        var trackSlug = string.Empty;

        // Check `?track=<trackSlug>` query parameter first; fall back to a 3rd path segment if present.
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], "track", StringComparison.OrdinalIgnoreCase))
            {
                trackSlug = Uri.UnescapeDataString(kv[1]);
            }
        }

        if (string.IsNullOrWhiteSpace(trackSlug) && segments.Length >= 3)
        {
            trackSlug = segments[2];
        }

        return new BookMusicReference
        {
            Label = label.Replace("*", string.Empty).Replace("\"", string.Empty).Trim(),
            Url = url,
            ArtistSlug = artistSlug,
            AlbumSlug = albumSlug,
            TrackSlug = trackSlug
        };
    }
}
