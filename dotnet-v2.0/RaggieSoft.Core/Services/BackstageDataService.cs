// ================================================================================
// FILE: BackstageDataService.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file is the **Data Loader, Offline Archive Downloader, and HTML-to-AST
//     Parser** for **RaggieSoft Backstage** (`Backstage.exe`) and the inline
//     "Read Dossier" popup inside **RaggieSoft Book Reader** (`BookReader.exe`).
//
//   What JSON endpoints feed into this service?
//     Via `RaggieSoftWorkspaceLocator` (which checks `%LOCALAPPDATA%\RaggieSoft\Cache`
//     first and falls back to `https://assets.raggiesoft.com`), this service reads:
//       1. `/raggiesoft-backstage/api/v1/index.json`
//          -> Master list of all behind-the-scenes dossiers, character emails,
//             and worldbuilding guides (`LoadIndexAsync`).
//       2. `/raggiesoft-backstage/api/v1/<slug>.json`
//          -> Full record metadata + pre-rendered HTML body (`LoadAndParseRecordAsync`).
//       3. `/raggiesoft-books/json/story-arcs.json`
//          (with fallback to `/raggiesoft-backstage/api/v1/story-arcs.json`)
//          -> Curated Story Arc playlists (`LoadStoryArcsAsync`).
//
//   Why do we parse HTML into C# `BackstageContentBlock` objects instead of using a browser?
//     Each `<slug>.json` file on our CDN stores its article body as semantic HTML
//     (`<h2>`, `<p>`, `<blockquote>`, `<pre>`, `<table>`, `<figure>`, plus custom
//     Ocean View Archives tags like `<aac>`, `<led>`, and `<asl>`).
//     Embedding a full Chromium browser (`WebView2`) inside a desktop app would use
//     hundreds of megabytes of RAM and wouldn't automatically match Book Reader's
//     custom fonts (like OpenDyslexic) or user line-spacing settings.
//     Instead, `ParseHtmlIntoBlocks` converts that clean semantic HTML into native
//     C# `BackstageContentBlock` and `RichTextSpan` objects so WPF's `FlowDocument`
//     paints razor-sharp native DirectX typography in milliseconds!
// ================================================================================

using System.Net;                     // `WebUtility.HtmlDecode` (converts `&amp;`, `&mdash;`, `&quot;` to plain characters).
using System.Text.Json;               // `JsonSerializer` & `JsonSerializerOptions` for JSON deserialization.
using System.Text.RegularExpressions; // `Regex` & `Match` for tokenizing semantic HTML blocks and inline spans.
using RaggieSoft.Core.Models;         // `BackstageIndexResponse`, `BackstageRecordDetail`, `BackstageContentBlock`, etc.

namespace RaggieSoft.Core.Services;

/// <summary>
/// Loads RaggieSoft Backstage indexes, Story Arcs, and individual dossiers from the
/// local cache or CDN, synchronizes offline archives, and parses HTML bodies into
/// native <see cref="BackstageContentBlock"/> trees.
/// </summary>
public sealed class BackstageDataService
{
    // Injected file/CDN locator shared across the running application.
    private readonly RaggieSoftWorkspaceLocator _locator;

    // Configures `System.Text.Json` to match JSON property names case-insensitively
    // as an extra safety net alongside our explicit `[JsonPropertyName]` attributes.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // --- Pre-Compiled Regular Expressions (`static readonly` + `RegexOptions.Compiled`) ---
    // Compiling these regexes once when the class first loads makes parsing an entire dossier
    // take less than 1 millisecond!

    // Matches top-level semantic HTML blocks (`<h2>...</h2>`, `<p>...</p>`, `<table>...</table>`, etc.)
    // OR self-closing `<hr>` dividers.
    // Notice `\k<tag>`: that regex backreference requires the closing `</...>` tag to match the
    // EXACT opening tag captured in `(?<tag>...)` (e.g. `<h2>` closes at `</h2>`, not `</p>`)!
    private static readonly Regex BlockRegex = new(
        @"<(?<tag>h2|h3|h4|p|blockquote|pre|li|table|figure)\b[^>]*>(?<inner>[\s\S]*?)</\k<tag>>|<hr\b[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Strips structural container wrappers (`<ul>`, `<ol>`, `<div>`, `<section>`, `<article>`)
    // before running `BlockRegex` so nested `<li>` and `<p>` elements sit at top level cleanly.
    private static readonly Regex WrapperTagRegex = new(
        @"</?(?:ul|ol|div|section|article)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Extracts `<a href="https://...">Label</a>` links inside HTML fragments.
    private static readonly Regex AnchorTagRegex = new(
        @"<a\b[^>]*\bhref=""(?<url>https?://[^""]+)""[^>]*>(?<label>[\s\S]*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Extracts `src="..."`, `alt="..."`, and `<figcaption>...</figcaption>` from `<figure>` / `<img>` tags.
    private static readonly Regex ImgSrcRegex = new(
        @"<img\b[^>]*\bsrc=""(?<src>[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImgAltRegex = new(
        @"<img\b[^>]*\balt=""(?<alt>[^""]*)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FigCaptionRegex = new(
        @"<figcaption\b[^>]*>(?<cap>[\s\S]*?)</figcaption>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Extracts `<tr>...</tr>` table rows and `<th>` / `<td>` table cells inside `<table>`.
    private static readonly Regex TableRowRegex = new(
        @"<tr\b[^>]*>(?<row>[\s\S]*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TableCellRegex = new(
        @"<(?<celltag>th|td)\b[^>]*>(?<val>[\s\S]*?)</\k<celltag>>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Multi-branch alternation (`|`) matching every supported inline HTML / custom OVA tag inside
    // a paragraph, list item, or blockquote. Each branch uses a distinct named capture group
    // (`(?<url>...)`, `(?<bold>...)`, `(?<aac>...)`, `(?<led>...)`, etc.) so `ParseHtmlFragmentToRichSpans`
    // can immediately tell which inline tag matched!
    private static readonly Regex InlineHtmlTokenRegex = new(
        @"<a\b[^>]*\bhref=""(?<url>https?://[^""]+)""[^>]*>(?<alabel>[\s\S]*?)</a>" +
        @"|<(?:strong|b)\b[^>]*>(?<bold>[\s\S]*?)</(?:strong|b)>" +
        @"|<(?:em|i)\b[^>]*>(?<italic>[\s\S]*?)</(?:em|i)>" +
        @"|<code\b[^>]*>(?<code>[\s\S]*?)</code>" +
        @"|<aac\b[^>]*>(?<aac>[\s\S]*?)</aac>" +
        @"|<pa\b[^>]*>(?<pa>[\s\S]*?)</pa>" +
        @"|<led\b(?<ledattrs>[^>]*?)(?:/>|>(?<led>[\s\S]*?)</led>)" +
        @"|<sms\b[^>]*>(?<sms>[\s\S]*?)</sms>" +
        @"|<asl\b[^>]*>(?<asl>[\s\S]*?)</asl>" +
        @"|<sgn\b[^>]*>(?<sgn>[\s\S]*?)</sgn>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Strips any remaining HTML markup tags (`<span>`, etc.) and collapses repeated spaces/tabs.
    private static readonly Regex AnyHtmlTagRegex = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Regex MultiSpaceRegex = new(
        @"[ \t]+",
        RegexOptions.Compiled);

    /// <summary>
    /// Creates a new <see cref="BackstageDataService"/> backed by the shared <see cref="RaggieSoftWorkspaceLocator"/>.
    /// </summary>
    public BackstageDataService(RaggieSoftWorkspaceLocator locator)
    {
        _locator = locator;
    }

    /// <summary>
    /// Loads the master RaggieSoft Backstage index (counts + all readable dossiers &amp; guides).
    /// </summary>
    public async Task<BackstageIndexResponse> LoadIndexAsync()
    {
        var json = await _locator.ReadAssetTextAsync("raggiesoft-backstage/api/v1/index.json");
        var response = JsonSerializer.Deserialize<BackstageIndexResponse>(json, JsonOptions) ?? new BackstageIndexResponse();

        // Filter out landing hub overview cards ("index", "books/rachel", "books/katie", "books/quantum-directive-3-casey", "books/aubrie")
        // so only readable dossiers, correspondence records, and worldbuilding guides appear in the collection list.
        // Notice the LINQ pipeline (`.Where(...)` -> `.OrderByDescending(...)` -> `.ThenBy(...)` -> `.ToList()`):
        //   1. `.Where(...)` keeps only items where `!item.IsCollectionHub` is true (like `array_filter` in PHP)
        //   2. `.OrderByDescending(...)` sorts newest `Date` first
        //   3. `.ThenBy(...)` breaks date ties alphabetically by `Title`
        //   4. `.ToList()` materializes the lazy LINQ query into a concrete `List<BackstageSummaryItem>`.
        response.Items = response.Items
            .Where(item => !item.IsCollectionHub)
            .OrderByDescending(item => item.Date, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return response;
    }

    /// <summary>
    /// Loads all curated Story Arc playlists (reading <c>raggiesoft-books/json/story-arcs.json</c>
    /// with fallback to <c>raggiesoft-backstage/api/v1/story-arcs.json</c>).
    /// </summary>
    public async Task<List<BookStoryArc>> LoadStoryArcsAsync()
    {
        string json = string.Empty;
        try
        {
            json = await _locator.ReadAssetTextAsync("raggiesoft-books/json/story-arcs.json");
        }
        catch
        {
            // Fallback endpoint if only the Backstage API mirror was cached offline.
            try
            {
                json = await _locator.ReadAssetTextAsync("raggiesoft-backstage/api/v1/story-arcs.json");
            }
            catch
            {
                return [];
            }
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var envelope = JsonSerializer.Deserialize<StoryArcsCatalogEnvelope>(json, JsonOptions);
        var arcs = envelope?.Arcs ?? [];

        // Pre-resolve every Story Arc's hero artwork to a local cache path or HTTPS CDN URL.
        foreach (var arc in arcs)
        {
            if (!string.IsNullOrWhiteSpace(arc.Image))
            {
                arc.ResolvedHeroImageUri = _locator.ResolveAssetUri(arc.Image);
            }
        }

        return arcs;
    }

    /// <summary>
    /// Loads and parses a full Backstage dossier or worldbuilding guide by slug.
    /// </summary>
    public async Task<BackstageRecordDetail> LoadAndParseRecordAsync(string slug)
    {
        // Trim any accidental leading/trailing `/` or spaces so `"books/rachel/memo"` builds a clean path.
        var cleanSlug = slug.Trim('/', ' ');
        var json = await _locator.ReadAssetTextAsync($"raggiesoft-backstage/api/v1/{cleanSlug}.json");
        var detail = JsonSerializer.Deserialize<BackstageRecordDetail>(json, JsonOptions)
                     ?? new BackstageRecordDetail { Slug = cleanSlug };

        // Prefer uncropped 16:9 scene plate ("image") over social card ("ogImage").
        var heroRaw = !string.IsNullOrWhiteSpace(detail.Image) ? detail.Image : detail.OgImage;
        if (!string.IsNullOrWhiteSpace(heroRaw))
        {
            detail.ResolvedHeroImageUri = _locator.ResolveAssetUri(heroRaw);
        }

        // Convert `detail.Html` into native `detail.Blocks` + `detail.MusicReferences`.
        ParseHtmlIntoBlocks(detail);
        return detail;
    }

    /// <summary>
    /// Downloads and caches the entire RaggieSoft Backstage archive (all JSON dossiers
    /// plus hero and inline illustrations) into <c>%LOCALAPPDATA%\RaggieSoft\Cache</c>
    /// for 100% offline reading.
    /// </summary>
    public async Task DownloadCompleteArchiveOfflineAsync(
        IReadOnlyList<BackstageSummaryItem> items,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Refreshing Backstage archive index...");
        _ = await _locator.ReadAssetTextAsync("raggiesoft-backstage/api/v1/index.json");

        for (var i = 0; i < items.Count; i++)
        {
            // Check whether the user clicked Cancel or closed the window mid-download; if so,
            // `ThrowIfCancellationRequested()` stops immediately with `OperationCanceledException`.
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[i];
            progress?.Report($"Saving dossier {i + 1} of {items.Count}: {item.Title}");

            // Cache the dossier's `<slug>.json` file on disk.
            var cleanSlug = item.Slug.Trim('/', ' ');
            var detailJson = await _locator.ReadAssetTextAsync($"raggiesoft-backstage/api/v1/{cleanSlug}.json");

            // Cache the dossier's hero illustration on disk.
            var heroImg = !string.IsNullOrWhiteSpace(item.Image) ? item.Image : item.OgImage;
            if (!string.IsNullOrWhiteSpace(heroImg))
            {
                try
                {
                    await _locator.EnsureAssetCachedLocallyAsync(heroImg, cancellationToken);
                }
                catch
                {
                    // Continue caching remaining dossiers if an optional illustration cannot be fetched
                }
            }

            // Also scan the dossier JSON/HTML for any inline `<img src="...">` plates and cache those too!
            foreach (Match imgMatch in ImgSrcRegex.Matches(detailJson))
            {
                var inlineSrc = WebUtility.HtmlDecode(imgMatch.Groups["src"].Value);
                if (!string.IsNullOrWhiteSpace(inlineSrc))
                {
                    try
                    {
                        await _locator.EnsureAssetCachedLocallyAsync(inlineSrc, cancellationToken);
                    }
                    catch
                    {
                        // Ignore optional inline image failure
                    }
                }
            }
        }

        // Record the `.offline_backstage_installed` sentinel file so the UI shows "✅ Saved Offline".
        var markerDir = Path.Combine(_locator.LocalCacheDirectory, "raggiesoft-backstage");
        Directory.CreateDirectory(markerDir);
        var markerPath = Path.Combine(markerDir, ".offline_backstage_installed");
        await File.WriteAllTextAsync(markerPath, DateTime.UtcNow.ToString("O"), cancellationToken);
    }

    /// <summary>
    /// Parses a <c>https://books.raggiesoft.com/&lt;bookSlug&gt;/&lt;era&gt;/&lt;chapter&gt;/&lt;part&gt;</c> companion URL
    /// into its component <c>bookSlug</c> and chapter <c>routePath</c> for RaggieSoft Book Reader IPC handoff.
    /// </summary>
    // C# FEATURE SPOTLIGHT — Named ValueTuples `(string BookSlug, string RoutePath)?`:
    //   Instead of creating a whole separate 2-property class just to return two strings (or returning
    //   a vague `['rachel', 'e001/c001/p001']` indexed array like in PHP), C# lets a method return a
    //   strongly typed **ValueTuple** with named fields (`result.Value.BookSlug` and `result.Value.RoutePath`),
    //   or `null` when the URL isn't a `books.raggiesoft.com` chapter link!
    public static (string BookSlug, string RoutePath)? ParseCompanionBookUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl) || !Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Host, "books.raggiesoft.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var bookSlug = segments[0];
        var routePath = segments.Length > 1 ? string.Join("/", segments.Skip(1)) : string.Empty;
        return (bookSlug, routePath);
    }

    /// <summary>
    /// Extracts the optional <c>?arc=&lt;slug&gt;</c> Story Arc query parameter from a <c>books.raggiesoft.com</c> URL.
    /// </summary>
    public static string ExtractStoryArcQuerySlug(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl) || !Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        // Strip leading `?`, split `a=1&arc=my-arc` on `&`, and split each pair on `=` (max 2 parts).
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], "arc", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(kv[1]).Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Converts pre-rendered Backstage HTML into native structured blocks with rich inline spans.
    /// </summary>
    private void ParseHtmlIntoBlocks(BackstageRecordDetail detail)
    {
        detail.Blocks.Clear();
        detail.MusicReferences.Clear();

        if (string.IsNullOrWhiteSpace(detail.Html))
        {
            return;
        }

        // Normalize Windows CRLF (`\r\n`) to `\n` and strip outer `<div>` / `<section>` / `<ul>` wrappers
        // so `BlockRegex` visits every semantic block in exact reading order.
        var normalized = WrapperTagRegex.Replace(detail.Html.Replace("\r\n", "\n"), "\n");
        foreach (Match match in BlockRegex.Matches(normalized))
        {
            var rawMatch = match.Value;

            // 1. `<hr>` horizontal divider -> centered `•   •   •` break
            if (rawMatch.StartsWith("<hr", StringComparison.OrdinalIgnoreCase))
            {
                detail.Blocks.Add(new BackstageContentBlock { BlockType = BackstageBlockType.Divider });
                continue;
            }

            var tag = match.Groups["tag"].Value.ToLowerInvariant();
            var inner = match.Groups["inner"].Value;

            switch (tag)
            {
                // 2. Major section heading (`<h2>`)
                case "h2":
                {
                    var text = StripTagsAndDecode(inner);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        detail.Blocks.Add(new BackstageContentBlock
                        {
                            BlockType = BackstageBlockType.Heading2,
                            Text = text
                        });
                    }
                    break;
                }

                // 3. Subsection heading (`<h3>` or `<h4>`)
                case "h3":
                case "h4":
                {
                    var text = StripTagsAndDecode(inner);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        detail.Blocks.Add(new BackstageContentBlock
                        {
                            BlockType = BackstageBlockType.Heading3,
                            Text = text
                        });
                    }
                    break;
                }

                // 4. Preformatted console / system log (`<pre>`)
                case "pre":
                {
                    var text = WebUtility.HtmlDecode(AnyHtmlTagRegex.Replace(inner, string.Empty)).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        detail.Blocks.Add(new BackstageContentBlock
                        {
                            BlockType = BackstageBlockType.ConsoleBlock,
                            Text = text
                        });
                    }
                    break;
                }

                // 5. `<figure><img src="..." alt="..."><figcaption>...</figcaption></figure>`
                case "figure":
                {
                    var srcMatch = ImgSrcRegex.Match(inner);
                    if (srcMatch.Success)
                    {
                        var rawSrc = WebUtility.HtmlDecode(srcMatch.Groups["src"].Value);
                        var resolvedUri = _locator.ResolveAssetUri(rawSrc);
                        var capMatch = FigCaptionRegex.Match(inner);
                        var altMatch = ImgAltRegex.Match(inner);
                        var caption = capMatch.Success
                            ? StripTagsAndDecode(capMatch.Groups["cap"].Value)
                            : (altMatch.Success ? WebUtility.HtmlDecode(altMatch.Groups["alt"].Value).Trim() : string.Empty);

                        // Skip if this `<figure>` is the exact same image already rendered as the top Hero Plate!
                        if (!string.Equals(resolvedUri, detail.ResolvedHeroImageUri, StringComparison.OrdinalIgnoreCase))
                        {
                            detail.Blocks.Add(new BackstageContentBlock
                            {
                                BlockType = BackstageBlockType.Illustration,
                                ImageUri = resolvedUri,
                                ImageAlt = caption,
                                Text = caption
                            });
                        }
                    }
                    break;
                }

                // 6. Multi-column HTML `<table>`
                case "table":
                {
                    var tableModel = ParseHtmlTable(inner);
                    if (tableModel is not null)
                    {
                        detail.Blocks.Add(new BackstageContentBlock
                        {
                            BlockType = BackstageBlockType.Table,
                            Table = tableModel
                        });
                    }
                    break;
                }

                // 7. `<blockquote>`, `<li>`, or `<p>` rich prose blocks
                case "blockquote":
                case "li":
                case "p":
                {
                    // Special case: if a `<p>` contains ONLY a standalone `<img ...>` with no prose text,
                    // promote it to a proper `BackstageBlockType.Illustration` card!
                    if (tag == "p" && ImgSrcRegex.IsMatch(inner) && string.IsNullOrWhiteSpace(StripTagsAndDecode(inner)))
                    {
                        var imgMatch = ImgSrcRegex.Match(inner);
                        var rawSrc = WebUtility.HtmlDecode(imgMatch.Groups["src"].Value);
                        var resolvedUri = _locator.ResolveAssetUri(rawSrc);
                        var altMatch = ImgAltRegex.Match(inner);
                        var alt = altMatch.Success ? WebUtility.HtmlDecode(altMatch.Groups["alt"].Value).Trim() : string.Empty;
                        if (!string.Equals(resolvedUri, detail.ResolvedHeroImageUri, StringComparison.OrdinalIgnoreCase))
                        {
                            detail.Blocks.Add(new BackstageContentBlock
                            {
                                BlockType = BackstageBlockType.Illustration,
                                ImageUri = resolvedUri,
                                ImageAlt = alt,
                                Text = alt
                            });
                        }
                        break;
                    }

                    // Collect inline links (and any `music.raggiesoft.com` song references) inside this block.
                    var links = ExtractInlineLinksAndMusic(inner, detail.MusicReferences);
                    var prepared = tag == "blockquote"
                        ? Regex.Replace(inner, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase)
                            .Replace("</p>", "\n\n", StringComparison.OrdinalIgnoreCase)
                            .Replace("<p>", string.Empty, StringComparison.OrdinalIgnoreCase)
                        : Regex.Replace(inner, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

                    var plainText = StripTagsAndDecode(prepared);
                    if (string.IsNullOrWhiteSpace(plainText))
                    {
                        break;
                    }

                    // Detect in-universe email / memo blockquotes (`From:` / `To:` / `Subject:`)
                    // and upgrade them into styled `CorrespondenceCard` blocks!
                    if (tag == "blockquote" && TryExtractCorrespondenceBlock(plainText, out var headers, out var emailBody))
                    {
                        detail.Blocks.Add(new BackstageContentBlock
                        {
                            BlockType = BackstageBlockType.CorrespondenceCard,
                            EmailHeaders = headers,
                            Text = emailBody,
                            Links = links
                        });
                        break;
                    }

                    // Tokenize inline HTML tags (`<strong>`, `<em>`, `<a>`, `<aac>`, `<led>`, etc.) into `RichTextSpan` runs.
                    var spans = ParseHtmlFragmentToRichSpans(prepared);
                    var blockType = tag switch
                    {
                        "blockquote" => BackstageBlockType.Blockquote,
                        "li" => BackstageBlockType.BulletItem,
                        _ => BackstageBlockType.Paragraph
                    };

                    detail.Blocks.Add(new BackstageContentBlock
                    {
                        BlockType = blockType,
                        Text = plainText,
                        Spans = spans,
                        Links = links
                    });
                    break;
                }
            }
        }
    }

    // C# `out` parameter pattern (`TryExtract...`):
    //   Returns `true` if `plainText` begins with at least 2 correspondence headers (including `From:` or `To:`),
    //   while simultaneously filling `out headers` and `out bodyText` for the caller!
    private static bool TryExtractCorrespondenceBlock(
        string plainText,
        out Dictionary<string, string> headers,
        out string bodyText)
    {
        headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        bodyText = string.Empty;

        var lines = plainText.Split('\n');
        var bodyStartIndex = 0;
        var foundFromOrTo = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (headers.Count > 0)
                {
                    bodyStartIndex = i + 1;
                    break;
                }
                continue;
            }

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx > 0 && colonIdx <= 18)
            {
                var key = trimmed[..colonIdx].Trim();
                var val = trimmed[(colonIdx + 1)..].Trim();
                if (key.Equals("From", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("To", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Cc", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Date", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Subject", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("Re", StringComparison.OrdinalIgnoreCase))
                {
                    headers[key] = val;
                    if (key.Equals("From", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("To", StringComparison.OrdinalIgnoreCase))
                    {
                        foundFromOrTo = true;
                    }
                    bodyStartIndex = i + 1;
                    continue;
                }
            }

            break;
        }

        if (!foundFromOrTo || headers.Count < 2)
        {
            return false;
        }

        bodyText = string.Join("\n", lines.Skip(bodyStartIndex)).Trim();
        return true;
    }

    // Sliding-cursor inline HTML tokenizer:
    //   Walks `htmlFragment` left-to-right using `cursor`. Whenever `InlineHtmlTokenRegex` finds a
    //   styled tag at `match.Index`, any unstyled text between `cursor` and `match.Index` becomes a
    //   `RichTextSpanKind.Plain` run, the matched tag becomes its rich `RichTextSpanKind` run, and
    //   `cursor` advances to `match.Index + match.Length`!
    private static List<RichTextSpan> ParseHtmlFragmentToRichSpans(string htmlFragment)
    {
        var spans = new List<RichTextSpan>();
        var cursor = 0;

        foreach (Match match in InlineHtmlTokenRegex.Matches(htmlFragment))
        {
            // Emit any plain text preceding this inline tag.
            if (match.Index > cursor)
            {
                var plainChunk = DecodeInlineHtmlText(htmlFragment[cursor..match.Index]);
                if (plainChunk.Length > 0)
                {
                    spans.Add(new RichTextSpan { Kind = RichTextSpanKind.Plain, Text = plainChunk });
                }
            }

            // Classify the matched inline tag by checking which named regex capture group succeeded.
            if (match.Groups["url"].Success)
            {
                var url = WebUtility.HtmlDecode(match.Groups["url"].Value).Trim();
                var label = StripTagsAndDecode(match.Groups["alabel"].Value).Replace("↗", string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(label))
                {
                    spans.Add(new RichTextSpan
                    {
                        Kind = RichTextSpanKind.Link,
                        Text = label,
                        LinkUrl = url
                    });
                }
            }
            else if (match.Groups["bold"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Bold,
                    Text = DecodeInlineHtmlText(match.Groups["bold"].Value)
                });
            }
            else if (match.Groups["italic"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Italic,
                    Text = DecodeInlineHtmlText(match.Groups["italic"].Value)
                });
            }
            else if (match.Groups["code"].Success)
            {
                spans.Add(new RichTextSpan
                {
                    Kind = RichTextSpanKind.Code,
                    Text = WebUtility.HtmlDecode(AnyHtmlTagRegex.Replace(match.Groups["code"].Value, string.Empty))
                });
            }
            else if (match.Groups["aac"].Success)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.AacSpeech, Text = DecodeInlineHtmlText(match.Groups["aac"].Value) });
            }
            else if (match.Groups["pa"].Success)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.PaAnnouncement, Text = DecodeInlineHtmlText(match.Groups["pa"].Value) });
            }
            else if (match.Groups["led"].Success || match.Groups["ledattrs"].Success)
            {
                spans.Add(RichTextSpan.ParseLedSign(
                    match.Groups["ledattrs"].Value,
                    match.Groups["led"].Value));
            }
            else if (match.Groups["sms"].Success)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.SmsBubble, Text = DecodeInlineHtmlText(match.Groups["sms"].Value) });
            }
            else if (match.Groups["asl"].Success)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.AslSign, Text = DecodeInlineHtmlText(match.Groups["asl"].Value) });
            }
            else if (match.Groups["sgn"].Success)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.GestureTag, Text = DecodeInlineHtmlText(match.Groups["sgn"].Value) });
            }

            cursor = match.Index + match.Length;
        }

        // Emit any trailing plain text after the final inline tag.
        if (cursor < htmlFragment.Length)
        {
            var tail = DecodeInlineHtmlText(htmlFragment[cursor..]);
            if (tail.Length > 0)
            {
                spans.Add(new RichTextSpan { Kind = RichTextSpanKind.Plain, Text = tail });
            }
        }

        return spans;
    }

    // Strips residual HTML tags, decodes HTML entities (`&amp;` -> `&`), and collapses repeated horizontal spaces.
    private static string DecodeInlineHtmlText(string raw)
    {
        var withoutTags = AnyHtmlTagRegex.Replace(raw, string.Empty);
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return MultiSpaceRegex.Replace(decoded, " ");
    }

    // Scans an HTML fragment for `<a href="...">` links and registers any `music.raggiesoft.com`
    // soundtrack links into `musicAccumulator` (deduplicated by URL via `!musicAccumulator.Any(...)`).
    private static List<BookInlineLink> ExtractInlineLinksAndMusic(
        string htmlFragment,
        List<BookMusicReference> musicAccumulator)
    {
        var links = new List<BookInlineLink>();

        foreach (Match anchor in AnchorTagRegex.Matches(htmlFragment))
        {
            var url = WebUtility.HtmlDecode(anchor.Groups["url"].Value).Trim();
            var label = StripTagsAndDecode(anchor.Groups["label"].Value)
                .Replace("↗", string.Empty)
                .Trim();

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            links.Add(new BookInlineLink
            {
                Label = label,
                Url = url
            });

            if (url.Contains("music.raggiesoft.com", StringComparison.OrdinalIgnoreCase))
            {
                var musicRef = BookDataService.ParseMusicUrl(label, url);
                if (musicRef is not null &&
                    !musicAccumulator.Any(m => string.Equals(m.Url, musicRef.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    musicAccumulator.Add(musicRef);
                }
            }
        }

        return links;
    }

    // Parses an HTML `<table>` inner body into column `Headers` (`<th>`) and data `Rows` (`<td>`).
    private static BackstageTableModel? ParseHtmlTable(string tableInner)
    {
        var headers = new List<string>();
        var rows = new List<List<string>>();

        foreach (Match rowMatch in TableRowRegex.Matches(tableInner))
        {
            var rowHtml = rowMatch.Groups["row"].Value;
            var cellMatches = TableCellRegex.Matches(rowHtml);
            if (cellMatches.Count == 0)
            {
                continue;
            }

            var isHeader = false;
            var cells = new List<string>();
            foreach (Match cell in cellMatches)
            {
                if (string.Equals(cell.Groups["celltag"].Value, "th", StringComparison.OrdinalIgnoreCase))
                {
                    isHeader = true;
                }
                cells.Add(StripTagsAndDecode(cell.Groups["val"].Value));
            }

            if (isHeader && headers.Count == 0)
            {
                headers = cells;
            }
            else if (cells.Count > 0)
            {
                rows.Add(cells);
            }
        }

        return headers.Count > 0 || rows.Count > 0
            ? new BackstageTableModel { Headers = headers, Rows = rows }
            : null;
    }

    // Strips all HTML tags, decodes HTML entities, and trims whitespace line-by-line while preserving `\n` breaks.
    private static string StripTagsAndDecode(string rawHtml)
    {
        var withoutTags = AnyHtmlTagRegex.Replace(rawHtml, string.Empty);
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var lines = decoded
            .Split('\n')
            .Select(line => MultiSpaceRegex.Replace(line, " ").Trim());
        return string.Join("\n", lines).Trim();
    }
}
