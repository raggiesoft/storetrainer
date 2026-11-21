// ================================================================================
// FILE: BookModels.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file defines every C# data model for **RaggieSoft Book Reader**
//     (`BookReader.exe`, the Windows desktop edition of `https://books.raggiesoft.com`).
//
//   What JSON and Markdown files feed into these models?
//     Our editorial build tool Paige (`paige.php`) compiles our Markdown novels into
//     static JSON manifests served from `https://assets.raggiesoft.com` (and cached
//     offline in `%LOCALAPPDATA%\RaggieSoft\Cache`):
//       1. `/raggiesoft-books/books/catalog.json`
//          -> `BookCatalogItem` (every novel on the library bookshelf, cover art,
//             word counts, reading hours, and `.7z` / `.epub` / `.pdf` sizes).
//       2. `/raggiesoft-books/books/<slug>/toc.json`
//          -> `BookToc` (the complete 4-level reading hierarchy:
//             Level 1 `BookEra` [e001] -> Level 2 `BookChapter` [c001] ->
//             Level 3 `BookPart` [p001.md] -> Level 4 `BookSceneAnchor` [`##` headings],
//             plus **RaggieSoft Story Crossroads** interactive branching routes!).
//       3. `/raggiesoft-books/json/story-arcs.json`
//          -> `StoryArcsCatalogEnvelope`, `BookStoryArc`, & `BookStoryArcStop`
//             (curated thematic reading playlists that weave novel chapters and
//             Backstage bonus dossiers together in chronological order).
//       4. `/raggiesoft-books/books/<slug>/eXXX/cXXX/pXXX.md`
//          -> `ParsedChapterPart` & `BookContentBlock` (the parsed YAML frontmatter,
//             16:9 Hero illustrations/carousels, soundtrack cues, and structured body
//             blocks rendered natively in WPF's `FlowDocument`).
//
//   What visible features does it power?
//     - The Book Reader library grid, offline badge indicators, and word-count stats.
//     - The left-hand Table of Contents tree (including smart collapsing of single-part
//       chapters and interactive Story Crossroads path switching).
//     - Multi-stop Story Arc guided reading mode, "Scenes in This Part" jump links,
//       and clickable cross-links to songs (`ListeningRoom.exe`) and dossiers (`Backstage.exe`).
// ================================================================================

using System.Text.Json;               // Low-level `Utf8JsonReader`, `JsonSerializer`, and `JsonElement`.
using System.Text.Json.Serialization; // `[JsonPropertyName]`, `[JsonConverter]`, `[JsonIgnore]`, and `[JsonExtensionData]`.

namespace RaggieSoft.Core.Models;

/// <summary>
/// Tolerant JSON converter that reads either a single JSON string OR an array of JSON strings
/// into <c>List&lt;string&gt;</c> (used for <c>toc.json</c> <c>parts[].audio</c>, where a chapter
/// may specify either one soundtrack MP3 string or a list of multiple MP3 strings).
/// </summary>
public sealed class FlexibleStringListConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Case 1: `"audio": "the-stardust-engine/1997-hard-reset/01-hard-reset.mp3"` (single string)
        if (reader.TokenType == JsonTokenType.String)
        {
            var val = reader.GetString();
            // `[val]` creates a 1-element `List<string>` containing `val`; `[]` creates an empty list.
            return string.IsNullOrWhiteSpace(val) ? [] : [val];
        }

        // Case 2: `"audio": [ "track1.mp3", "track2.mp3" ]` (JSON array of strings)
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            // Advance token by token until we hit the closing `]` (`JsonTokenType.EndArray`).
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var s = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(s);
                    }
                }
                else
                {
                    // Safely skip any unexpected non-string element inside the array.
                    reader.Skip();
                }
            }
            return list;
        }

        // Case 3: `null` or unexpected JSON token -> skip and return an empty list.
        reader.Skip();
        return [];
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// Represents a single slide image + accessible alt caption (used in Hero Carousels,
/// inline <c>````carousel</c> blocks, and <c>toc.json</c> <c>parts[].images</c>).
/// </summary>
public sealed class BookSlideImage
{
    // Image file path or resolved URI (`Src`) and screen-reader / caption description (`Alt`).
    [JsonPropertyName("src")]
    public string Src { get; set; } = string.Empty;

    [JsonPropertyName("alt")]
    public string Alt { get; set; } = string.Empty;
}

/// <summary>
/// Tolerant JSON converter for <c>toc.json</c> <c>parts[].images</c> that accepts either
/// <c>[{ "src": "...", "alt": "..." }]</c> objects OR plain string URLs <c>["..."]</c>.
/// </summary>
public sealed class BookSlideImageListConverter : JsonConverter<List<BookSlideImage>>
{
    public override List<BookSlideImage> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<BookSlideImage>();
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return list;
        }

        // Step through each element of the JSON array (`["a.jpg"]` or `[{"src":"a.jpg","alt":"..."}]`).
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var src = reader.GetString();
                if (!string.IsNullOrWhiteSpace(src))
                {
                    list.Add(new BookSlideImage { Src = src });
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                var obj = JsonSerializer.Deserialize<BookSlideImage>(ref reader, options);
                if (obj is not null && !string.IsNullOrWhiteSpace(obj.Src))
                {
                    list.Add(obj);
                }
            }
            else
            {
                reader.Skip();
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<BookSlideImage> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// Represents a single novel entry from <c>/raggiesoft-books/books/catalog.json</c>,
/// powering each book cover card on the RaggieSoft Book Reader library screen.
/// </summary>
public sealed class BookCatalogItem
{
    // Unique folder/URL identifier for the novel, e.g. `"rachel"`, `"katie"`, or `"stardust-engine"`.
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    // Display title, one-line card hook (`ShortDescription`), and full jacket blurb (`Description`).
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // Relative cover art path (`Image`), opening chapter route (`FirstRoute`), and folder name.
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("first_route")]
    public string FirstRoute { get; set; } = string.Empty;

    [JsonPropertyName("folder")]
    public string Folder { get; set; } = string.Empty;

    // When `true`, `BookDataService.LoadCatalogAsync` hides draft/unreleased volumes from the shelf.
    [JsonPropertyName("hide")]
    public bool Hide { get; set; }

    // Total full-color scene illustrations and curated Story Arc playlists available in this novel.
    [JsonPropertyName("illustration_count")]
    public int IllustrationCount { get; set; }

    [JsonPropertyName("story_arc_count")]
    public int StoryArcCount { get; set; }

    // Nested objects (`?` means they may be `null` if omitted in older catalog entries).
    [JsonPropertyName("ebook_sizes")]
    public BookEbookSizes? EbookSizes { get; set; }

    [JsonPropertyName("word_stats")]
    public BookWordStats? WordStats { get; set; }

    // --- Runtime-Populated UI Properties (`[JsonIgnore]`) ---

    // Local cached path or CDN URL for the book's cover image, populated by `BookDataService`.
    [JsonIgnore]
    public string ResolvedCoverUri { get; set; } = string.Empty;

    // `true` when the user has clicked "Save Book Offline" and extracted `<slug>-package.7z` locally.
    [JsonIgnore]
    public bool IsInstalledOffline { get; set; }

    // Builds the consumer-friendly summary line underneath each book cover card, e.g.:
    // `"Epic Novel • 48 Illustrations • 3 Story Arcs • 42.5 MB • ✅ Saved Offline"`.
    // Notice C#'s **null-conditional operator** `?.` (`EbookSizes?.Package7z`, just like `$sizes?->package7z`
    // in PHP 8!): if `EbookSizes` is `null`, the whole expression evaluates cleanly to `null` instead of crashing.
    [JsonIgnore]
    public string MetadataBadge
    {
        get
        {
            var pkgSize = !string.IsNullOrWhiteSpace(EbookSizes?.Package7z)
                ? EbookSizes.Package7z
                : (EbookSizes?.Epub ?? "Ready");
            var offlineTag = IsInstalledOffline ? " • ✅ Saved Offline" : string.Empty;
            var arcTag = StoryArcCount > 0 ? $" • {StoryArcCount} Story {(StoryArcCount == 1 ? "Arc" : "Arcs")}" : string.Empty;
            var scaleBadge = !string.IsNullOrWhiteSpace(WordStats?.ScaleBadge) ? $"{WordStats.ScaleBadge} • " : string.Empty;
            return IllustrationCount > 0
                ? $"{scaleBadge}{IllustrationCount} Illustrations{arcTag} • {pkgSize}{offlineTag}"
                : $"{scaleBadge}Complete Edition{arcTag} • {pkgSize}{offlineTag}";
        }
    }
}

/// <summary>
/// Represents pre-calculated eBook and offline download sizes from <c>catalog.json</c> and <c>toc.json</c>.
/// </summary>
public sealed class BookEbookSizes
{
    [JsonPropertyName("epub")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Epub { get; set; } = string.Empty;

    [JsonPropertyName("pdf")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Pdf { get; set; } = string.Empty;

    [JsonPropertyName("text_epub")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string TextEpub { get; set; } = string.Empty;

    [JsonPropertyName("text_pdf")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string TextPdf { get; set; } = string.Empty;

    [JsonPropertyName("package_7z")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Package7z { get; set; } = string.Empty;

    // `long` is a 64-bit integer (`Int64`), used in .NET for byte counts so files over 2 GB never overflow!
    [JsonPropertyName("package_7z_bytes")]
    public long Package7zBytes { get; set; }

    // `[JsonExtensionData]` is a handy forward-compatibility feature in `System.Text.Json`:
    // if `paige.php` adds new size fields to `ebook_sizes` in the future (e.g. `"audiobook_zip"`),
    // they are captured harmlessly in `ExtraFields` without failing or getting dropped!
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}

/// <summary>
/// Word count and estimated reading duration statistics from <c>toc.json</c> / <c>catalog.json</c>.
/// </summary>
public sealed class BookWordStats
{
    [JsonPropertyName("story_word_count")]
    public int StoryWordCount { get; set; }

    [JsonPropertyName("combined_word_count")]
    public int CombinedWordCount { get; set; }

    // `double` is a 64-bit floating-point decimal number (like `float` in PHP), e.g. `14.5` hours.
    [JsonPropertyName("reading_hours")]
    public double ReadingHours { get; set; }

    [JsonPropertyName("reading_minutes")]
    public int ReadingMinutes { get; set; }

    [JsonPropertyName("part_count")]
    public int PartCount { get; set; }

    // Consumer-friendly length label such as `"Novella"`, `"Full-Length Novel"`, or `"Epic Series"`.
    [JsonPropertyName("scale_badge")]
    public string ScaleBadge { get; set; } = string.Empty;
}

/// <summary>
/// Represents a novel's authoritative Table of Contents from <c>/raggiesoft-books/books/&lt;slug&gt;/toc.json</c>,
/// supporting both linear Ocean View Archives novels and multi-branch **RaggieSoft Story Crossroads**
/// interactive novels.
/// </summary>
public sealed class BookToc
{
    [JsonPropertyName("series_title")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("series_slug")]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonPropertyName("series_short_description")]
    public string SeriesShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("series_description")]
    public string SeriesDescription { get; set; } = string.Empty;

    [JsonPropertyName("series_image")]
    public string SeriesImage { get; set; } = string.Empty;

    [JsonPropertyName("illustration_count")]
    public int IllustrationCount { get; set; }

    [JsonPropertyName("ebook_sizes")]
    public BookEbookSizes? EbookSizes { get; set; }

    [JsonPropertyName("word_stats")]
    public BookWordStats? WordStats { get; set; }

    // Curated Story Arc playlists specific to this novel.
    [JsonPropertyName("story_arcs")]
    public List<BookStoryArc> StoryArcs { get; set; } = [];

    // Standard linear Era -> Chapter -> Part tree (used directly when `Crossroads` is empty).
    [JsonPropertyName("eras")]
    public List<BookEra> Eras { get; set; } = [];

    // --- RaggieSoft Story Crossroads Interactive Branching Graph ---
    // In a branching Story Crossroads book, the story starts at `RootSegmentId` (`"__root__"`).
    // Each segment in `Segments` contains its own `Eras` plus an optional `NextCrossroadsId`
    // pointing to a decision point in `Crossroads`, where the reader picks a `BookCrossroadsOption`
    // whose `Id` becomes the next segment key in `Segments`!
    [JsonPropertyName("crossroads")]
    public List<BookCrossroads> Crossroads { get; set; } = [];

    [JsonPropertyName("root_segment_id")]
    public string RootSegmentId { get; set; } = "__root__";

    [JsonPropertyName("segments")]
    public Dictionary<string, BookStoryPath> Segments { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("trunk_eras")]
    public List<BookEra> TrunkEras { get; set; } = [];

    [JsonPropertyName("paths")]
    public Dictionary<string, BookStoryPath> Paths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Clones <paramref name="baseEras"/> and appends <paramref name="additionalEras"/> without
    /// mutating the cached <c>toc.json</c> objects in memory. If a Story Crossroads route continues
    /// inside an existing Era and Chapter number, its Parts are seamlessly stitched onto the end
    /// of that existing Chapter!
    /// </summary>
    private static List<BookEra> MergeEras(IReadOnlyList<BookEra> baseEras, IReadOnlyList<BookEra> additionalEras)
    {
        // Deep-clone `baseEras` using LINQ `.Select(...)` (like `array_map` in PHP).
        // Notice `[.. c.Parts]`: C#'s spread element `..` copies all elements of `c.Parts` into a
        // brand-new `List<BookPart>` so `AddRange` below never mutates the original segment!
        var merged = baseEras
            .Select(e => new BookEra
            {
                EraNum = e.EraNum,
                EraTitle = e.EraTitle,
                EraUrl = e.EraUrl,
                PathId = e.PathId,
                IllustrationCount = e.IllustrationCount,
                Chapters = e.Chapters
                    .Select(c => new BookChapter
                    {
                        ChapNum = c.ChapNum,
                        ChapTitle = c.ChapTitle,
                        ChapUrl = c.ChapUrl,
                        PathId = c.PathId,
                        IllustrationCount = c.IllustrationCount,
                        Parts = [.. c.Parts]
                    })
                    .ToList()
            })
            .ToList();

        // Merge each Era from the newly unlocked Story Crossroads segment into `merged`.
        foreach (var pEra in additionalEras)
        {
            var existingEra = merged.FirstOrDefault(m =>
                m.EraNum == pEra.EraNum &&
                string.Equals(m.EraUrl, pEra.EraUrl, StringComparison.OrdinalIgnoreCase));

            if (existingEra is null)
            {
                merged.Add(pEra);
            }
            else
            {
                foreach (var pChap in pEra.Chapters)
                {
                    var existingChap = existingEra.Chapters.FirstOrDefault(mc => mc.ChapNum == pChap.ChapNum);
                    if (existingChap is null)
                    {
                        existingEra.Chapters.Add(pChap);
                    }
                    else
                    {
                        existingChap.Parts.AddRange(pChap.Parts);
                        existingChap.IllustrationCount += pChap.IllustrationCount;
                    }
                }
                existingEra.IllustrationCount += pEra.IllustrationCount;
            }
        }

        return merged;
    }

    /// <summary>
    /// Walks the multi-Crossroads segment graph from <see cref="RootSegmentId"/> using the reader's per-book
    /// choices map (<c>crossroadsId -&gt; optionId</c>), supporting any number of sequential or nested Crossroads.
    /// </summary>
    public ResolvedCrossroadsWalk WalkCrossroads(IReadOnlyDictionary<string, string>? choices)
    {
        // Fast path for standard linear novels: return `Eras` directly with zero Crossroads overhead.
        if (Crossroads.Count == 0 || Segments.Count == 0)
        {
            return new ResolvedCrossroadsWalk(Eras, [], null);
        }

        // Build a fast dictionary lookup from `crossroads.Id` -> `BookCrossroads` object.
        var cxLookup = Crossroads
            .Where(c => !string.IsNullOrWhiteSpace(c.Id))
            .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var accEras = new List<BookEra>();
        var decisions = new List<ActiveCrossroadsDecision>();
        BookCrossroads? frontier = null;
        var cursor = string.IsNullOrWhiteSpace(RootSegmentId) ? "__root__" : RootSegmentId;

        // `HashSet<string>` tracks visited segment IDs in O(1) time (`visited.Add(cursor)` returns `false`
        // if `cursor` was already visited), guaranteeing a malformed JSON cycle could never loop forever!
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrWhiteSpace(cursor) &&
               Segments.TryGetValue(cursor, out var seg) &&
               visited.Add(cursor))
        {
            // Append this segment's Eras/Chapters/Parts onto our active Table of Contents.
            accEras = MergeEras(accEras, seg.Eras);

            // If this segment reaches the end of the story (no `NextCrossroadsId`), stop walking.
            if (string.IsNullOrWhiteSpace(seg.NextCrossroadsId) ||
                !cxLookup.TryGetValue(seg.NextCrossroadsId, out var cxObj))
            {
                break;
            }

            // Check whether the reader has already saved an option choice for `seg.NextCrossroadsId`.
            string? chosenOptId = null;
            if (choices is not null && choices.TryGetValue(seg.NextCrossroadsId, out var foundOpt))
            {
                chosenOptId = foundOpt;
            }

            var matchedOpt = !string.IsNullOrWhiteSpace(chosenOptId)
                ? cxObj.Options.FirstOrDefault(o => string.Equals(o.Id, chosenOptId, StringComparison.OrdinalIgnoreCase))
                : null;

            // If the reader hasn't chosen an option at this Crossroads yet, mark it as the `frontier`
            // (so Book Reader hides future unchosen chapters in the TOC until the reader reaches
            // the end of the trigger chapter and makes their choice!).
            if (matchedOpt is null || string.IsNullOrWhiteSpace(chosenOptId) || !Segments.ContainsKey(chosenOptId))
            {
                frontier = cxObj;
                break;
            }

            // Record the active decision and advance `cursor` into the chosen route's segment!
            decisions.Add(new ActiveCrossroadsDecision(cxObj, matchedOpt));
            cursor = chosenOptId;
        }

        return new ResolvedCrossroadsWalk(accEras, decisions, frontier);
    }

    /// <summary>
    /// Flattens the reader's currently unlocked <c>Era -&gt; Chapter -&gt; Part</c> tree into a single
    /// sequential list of <see cref="BookPart"/> installments for "Previous Part" / "Next Part" navigation.
    /// </summary>
    public List<BookPart> GetOrderedPartsForChoices(IReadOnlyDictionary<string, string>? choices)
    {
        // LINQ `.SelectMany(...)` flattens nested collections (turning a list of Eras containing
        // Chapters containing Parts into one flat `List<BookPart>`).
        return WalkCrossroads(choices)
            .Eras
            .SelectMany(e => e.Chapters.SelectMany(c => c.Parts))
            .ToList();
    }

    /// <summary>
    /// Collects every unique <see cref="BookPart"/> across the trunk and all branching Story Crossroads
    /// routes (used when pre-caching custom theme stylesheets or resolving direct deep-links).
    /// </summary>
    public List<BookPart> GetAllPartsAcrossAllPaths()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var all = new List<BookPart>();

        foreach (var part in Eras.SelectMany(e => e.Chapters.SelectMany(c => c.Parts)))
        {
            if (seen.Add(part.FilePath))
            {
                all.Add(part);
            }
        }

        foreach (var kv in Paths.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var part in kv.Value.Eras.SelectMany(e => e.Chapters.SelectMany(c => c.Parts)))
            {
                if (seen.Add(part.FilePath))
                {
                    all.Add(part);
                }
            }
        }

        return all;
    }
}

/// <summary>
/// Pairs a <see cref="BookCrossroads"/> decision point with the <see cref="BookCrossroadsOption"/>
/// currently selected by the reader.
/// </summary>
// C# 9+ `record`: a concise, immutable value-style data carrier! In one single line, the compiler
// generates `public BookCrossroads Crossroads { get; init; }`, `public BookCrossroadsOption ChosenOption { get; init; }`,
// a constructor, `Equals`/`GetHashCode`, and `ToString()`.
public sealed record ActiveCrossroadsDecision(BookCrossroads Crossroads, BookCrossroadsOption ChosenOption);

/// <summary>
/// Result returned by <see cref="BookToc.WalkCrossroads"/>: the merged <see cref="Eras"/> visible on
/// the reader's current route, all <see cref="ActiveDecisions"/> made so far, and the next unanswered
/// <see cref="FrontierCrossroads"/> (if any).
/// </summary>
public sealed record ResolvedCrossroadsWalk(
    List<BookEra> Eras,
    List<ActiveCrossroadsDecision> ActiveDecisions,
    BookCrossroads? FrontierCrossroads);

/// <summary>
/// One selectable route option presented at a RaggieSoft Story Crossroads decision card.
/// </summary>
public sealed class BookCrossroadsOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("crossroads_id")]
    public string CrossroadsId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    // First Markdown `pXXX.md` part file opened when the reader chooses this Story Crossroads option.
    [JsonPropertyName("entry_part")]
    public string EntryPart { get; set; } = string.Empty;

    [JsonPropertyName("entry_url")]
    public string EntryUrl { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("part_count")]
    public int PartCount { get; set; }

    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    [JsonPropertyName("reading_minutes")]
    public int ReadingMinutes { get; set; }

    [JsonPropertyName("required_choices")]
    public Dictionary<string, string> RequiredChoices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Formats route length (e.g. `"8 Chapters • ~1.4 hr read"`) for option cards.
    [JsonIgnore]
    public string StatsLabel
    {
        get
        {
            if (PartCount <= 0) return string.Empty;
            var partsLabel = $"{PartCount} {(PartCount == 1 ? "Chapter" : "Chapters")}";
            if (ReadingMinutes <= 0) return partsLabel;
            var timeLabel = ReadingMinutes >= 60
                ? $"~{Math.Round(ReadingMinutes / 60.0, 1):0.#} hr read"
                : $"~{ReadingMinutes} min read";
            return $"{partsLabel} • {timeLabel}";
        }
    }
}

/// <summary>
/// Represents a RaggieSoft Story Crossroads branching decision point triggered at the end
/// of <see cref="TriggerPart"/>.
/// </summary>
public sealed class BookCrossroads
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Relative `pXXX.md` path at whose conclusion the Story Crossroads choice cards appear.
    [JsonPropertyName("trigger_part")]
    public string TriggerPart { get; set; } = string.Empty;

    [JsonPropertyName("trigger_url")]
    public string TriggerUrl { get; set; } = string.Empty;

    [JsonPropertyName("kicker")]
    public string Kicker { get; set; } = "Story Crossroads";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Choose How the Story Continues";

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    // Coequal branching route options available at this Crossroads.
    [JsonPropertyName("options")]
    public List<BookCrossroadsOption> Options { get; set; } = [];
}

/// <summary>
/// A narrative segment inside <see cref="BookToc.Segments"/> or <see cref="BookToc.Paths"/>
/// containing the <see cref="Eras"/> belonging to a trunk or Story Crossroads route.
/// </summary>
public sealed class BookStoryPath
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("crossroads_id")]
    public string CrossroadsId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("entry_part")]
    public string EntryPart { get; set; } = string.Empty;

    [JsonPropertyName("entry_url")]
    public string EntryUrl { get; set; } = string.Empty;

    // ID of the next `BookCrossroads` reached at the end of this segment (or `null` if terminal).
    [JsonPropertyName("next_crossroads_id")]
    public string? NextCrossroadsId { get; set; }

    [JsonPropertyName("required_choices")]
    public Dictionary<string, string> RequiredChoices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("eras")]
    public List<BookEra> Eras { get; set; } = [];
}

/// <summary>
/// Root envelope for <c>/raggiesoft-books/json/story-arcs.json</c> and <c>/raggiesoft-backstage/api/v1/story-arcs.json</c>.
/// </summary>
public sealed class StoryArcsCatalogEnvelope
{
    [JsonPropertyName("arcs")]
    public List<BookStoryArc> Arcs { get; set; } = [];
}

/// <summary>
/// Curated multi-arc reading playlist compiled from <c>books/&lt;slug&gt;/arcs.json</c> by <c>paige.php</c>.
/// Lets readers follow a specific character thread or mystery across novel chapters and Backstage dossiers.
/// </summary>
public sealed class BookStoryArc
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("series_slug")]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonPropertyName("series_title")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("backstage_playlist_slug")]
    public string BackstagePlaylistSlug { get; set; } = string.Empty;

    [JsonPropertyName("backstage_playlist_url")]
    public string BackstagePlaylistUrl { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("start_url")]
    public string StartUrl { get; set; } = string.Empty;

    [JsonPropertyName("part_count")]
    public int PartCount { get; set; }

    [JsonPropertyName("backstage_count")]
    public int BackstageCount { get; set; }

    [JsonPropertyName("stop_count")]
    public int StopCount { get; set; }

    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    [JsonPropertyName("reading_minutes")]
    public int ReadingMinutes { get; set; }

    [JsonPropertyName("reading_hours")]
    public double ReadingHours { get; set; }

    // Ordered sequence of chapters (`kind: "part"`) and dossiers (`kind: "backstage"`) in this Story Arc.
    [JsonPropertyName("stops")]
    public List<BookStoryArcStop> Stops { get; set; } = [];

    [JsonIgnore]
    public string ResolvedHeroImageUri { get; set; } = string.Empty;

    [JsonIgnore]
    public string ReadingDurationLabel =>
        ReadingHours >= 1.0 ? $"~{ReadingHours:0.0} hrs" : $"~{Math.Max(1, ReadingMinutes)} min";
}

/// <summary>
/// Single numbered stop inside a <see cref="BookStoryArc"/> (<c>"part"</c> chapter or <c>"backstage"</c> bonus scene).
/// </summary>
public sealed class BookStoryArcStop
{
    [JsonPropertyName("stop_num")]
    public int StopNum { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "part";

    [JsonPropertyName("series_slug")]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonPropertyName("series_title")]
    public string SeriesTitle { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("backstage_slug")]
    public string BackstageSlug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("era_title")]
    public string EraTitle { get; set; } = string.Empty;

    [JsonPropertyName("chap_title")]
    public string ChapTitle { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("arc_url")]
    public string ArcUrl { get; set; } = string.Empty;

    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    [JsonPropertyName("reading_minutes")]
    public int ReadingMinutes { get; set; }

    // `true` when this stop opens a Backstage dossier rather than a standard `pXXX.md` novel part.
    [JsonIgnore]
    public bool IsBackstage => string.Equals(Kind, "backstage", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Level 1 hierarchy container (Episode / Era / Book, e.g. <c>e001</c>).
/// </summary>
public sealed class BookEra
{
    [JsonPropertyName("era_num")]
    public int EraNum { get; set; }

    [JsonPropertyName("era_title")]
    public string EraTitle { get; set; } = string.Empty;

    [JsonPropertyName("era_url")]
    public string EraUrl { get; set; } = string.Empty;

    [JsonPropertyName("path_id")]
    public string PathId { get; set; } = string.Empty;

    [JsonPropertyName("illustration_count")]
    public int IllustrationCount { get; set; }

    [JsonPropertyName("chapters")]
    public List<BookChapter> Chapters { get; set; } = [];

    // Deduplicated structural header for the sidebar tree (e.g. `"Episode 1: The Arrival"`).
    [JsonIgnore]
    public string DisplayHeader =>
        RaggieSoftTitleNormalizer.NormalizeStructuralTitle(EraTitle, "Episode", EraNum, includeNumberPrefix: true);
}

/// <summary>
/// Level 2 hierarchy container (Chapter, e.g. <c>c001</c>).
/// When a Chapter has exactly 1 Part, <see cref="IsSinglePartChapter"/> is <c>true</c> so UI trees can
/// collapse redundant <c>"Part 1"</c> children into a clean single selectable node.
/// </summary>
public sealed class BookChapter
{
    [JsonPropertyName("chap_num")]
    public int ChapNum { get; set; }

    [JsonPropertyName("chap_title")]
    public string ChapTitle { get; set; } = string.Empty;

    [JsonPropertyName("chap_url")]
    public string ChapUrl { get; set; } = string.Empty;

    [JsonPropertyName("path_id")]
    public string PathId { get; set; } = string.Empty;

    [JsonPropertyName("illustration_count")]
    public int IllustrationCount { get; set; }

    [JsonPropertyName("parts")]
    public List<BookPart> Parts { get; set; } = [];

    [JsonIgnore]
    public bool IsSinglePartChapter => Parts.Count == 1;

    [JsonIgnore]
    public BookPart? SinglePart => Parts.Count == 1 ? Parts[0] : null;

    [JsonIgnore]
    public string DisplayHeader =>
        RaggieSoftTitleNormalizer.NormalizeStructuralTitle(ChapTitle, "Chapter", ChapNum, includeNumberPrefix: true);
}

/// <summary>
/// Level 3 reading installment (Part, e.g. <c>e001/c001/p001.md</c>).
/// Normalizes <see cref="PartTitle"/> so <c>"Part 1: Part 1: ..."</c> never occurs.
/// </summary>
public sealed class BookPart
{
    [JsonPropertyName("part_num")]
    public int PartNum { get; set; }

    [JsonPropertyName("part_title")]
    public string PartTitle { get; set; } = string.Empty;

    // Relative Markdown path inside the novel folder, e.g. `"e001/c001/p001.md"`.
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("image_alt")]
    public string ImageAlt { get; set; } = string.Empty;

    [JsonPropertyName("image_count")]
    public int ImageCount { get; set; }

    [JsonPropertyName("images")]
    [JsonConverter(typeof(BookSlideImageListConverter))]
    public List<BookSlideImage> Images { get; set; } = [];

    [JsonPropertyName("audio")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public List<string> AudioSpecs { get; set; } = [];

    [JsonPropertyName("playlist")]
    public string Playlist { get; set; } = string.Empty;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = string.Empty;

    [JsonPropertyName("path_id")]
    public string PathId { get; set; } = string.Empty;

    [JsonPropertyName("required_choices")]
    public Dictionary<string, string> RequiredChoices { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    [JsonPropertyName("reading_minutes")]
    public int ReadingMinutes { get; set; }

    // --- Parent Context Backfilled by `BookDataService.LoadBookTocAsync` (`[JsonIgnore]`) ---
    [JsonIgnore]
    public string SeriesSlug { get; set; } = string.Empty;

    [JsonIgnore]
    public string EraTitle { get; set; } = string.Empty;

    [JsonIgnore]
    public string ChapterTitle { get; set; } = string.Empty;

    // Lets the TOC sidebar show a small musical note indicator next to chapters that have a soundtrack!
    [JsonIgnore]
    public bool HasEmbeddedAudio => AudioSpecs.Count > 0 || !string.IsNullOrWhiteSpace(Playlist);

    [JsonIgnore]
    public string CleanTitle =>
        RaggieSoftTitleNormalizer.NormalizeStructuralTitle(PartTitle, "Part", PartNum, includeNumberPrefix: true);

    [JsonIgnore]
    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Date)
            ? CleanTitle
            : $"{CleanTitle} ({Date})";
}

/// <summary>
/// Level 4 internal Scene anchor extracted from Markdown <c>##</c> headings inside a Part.
/// Powers the toolbar "Scenes" jump menu and the inline "Scenes in This Part" Mini-TOC card.
/// </summary>
public sealed class BookSceneAnchor
{
    public int SceneIndex { get; set; }
    public string AnchorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Represents a fully parsed <c>pXXX.md</c> Markdown reading installment ready for native rendering.
/// Preserves the complete Ocean View Archives YAML frontmatter contract, multi-slide Hero
/// carousels, custom OVA inline tags, structured tables, callout alerts, correspondence cards,
/// embedded soundtrack specs, and Backstage bonus dossier links.
/// </summary>
public sealed class ParsedChapterPart
{
    public string SeriesSlug { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    // --- YAML Frontmatter Metadata Fields ---
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Stardate { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Pov { get; set; } = string.Empty;
    public List<string> Characters { get; set; } = [];
    public string Summary { get; set; } = string.Empty;

    // Custom CSS theme slug (e.g. `"ad-astra"` -> `/raggiesoft-books/css/themes/ad-astra.css`).
    public string Theme { get; set; } = string.Empty;

    // Soundtrack & playlist cues from YAML (`audio:`, `playlist:`, `audio_playlist_title:`, `audio_start:`).
    public string AudioRelativePath { get; set; } = string.Empty;
    public List<string> AudioSpecs { get; set; } = [];
    public string Playlist { get; set; } = string.Empty;
    public string AudioPlaylistTitle { get; set; } = string.Empty;
    public string AudioStartRaw { get; set; } = string.Empty;
    public double AudioStartSeconds { get; set; }

    // Resolved 16:9 Hero illustration(s) and Backstage bonus feature dossier slugs (`backstage:`).
    public string HeroImageUri { get; set; } = string.Empty;
    public string HeroImageAlt { get; set; } = string.Empty;
    public List<BookSlideImage> HeroSlides { get; set; } = [];
    public List<string> BackstageSlugs { get; set; } = [];
    public string RawMarkdownBody { get; set; } = string.Empty;

    /// <summary>
    /// Internal <c>##</c> Scene headings extracted for the Scenes jump menu and Mini-TOC.
    /// </summary>
    public List<string> SceneHeadings { get; set; } = [];

    /// <summary>
    /// Structured Level-4 Scene anchors corresponding to <c>##</c> headings in <see cref="Blocks"/>.
    /// </summary>
    public List<BookSceneAnchor> SceneAnchors { get; set; } = [];

    /// <summary>
    /// Structured semantic blocks for native <c>FlowDocument</c> rendering.
    /// </summary>
    public List<BookContentBlock> Blocks { get; set; } = [];

    /// <summary>
    /// All Engine Room Records song/album links referenced in prose.
    /// </summary>
    public List<BookMusicReference> MusicReferences { get; set; } = [];
}

/// <summary>
/// Semantic block types for native WPF <c>FlowDocument</c> rendering of an Ocean View Archives Part.
/// </summary>
public enum BookBlockType
{
    Heading1,           // `# Heading` (skipped automatically if it merely repeats the YAML Part title)
    SceneHeading,       // `## Scene Heading` (anchored for instant scene jumping)
    Subheading,         // `###` or `####` minor subheading
    Paragraph,          // Standard literary narrative paragraph
    BulletList,         // Unordered `-` / `*` list (used in lore volumes and reference notes)
    NumberedList,       // Ordered `1.` list
    Blockquote,         // Literary blockquote (`> ...`)
    CalloutAlert,       // GitHub-flavored alert box (`> [!NOTE]`, `> [!IMPORTANT]`, etc.)
    CorrespondenceCard, // In-universe email or memo card (`> **From:** ... <br> > **To:** ...`)
    CodeOrKernelLog,    // Fenced code block or `<div class="zoe-kernel-log">` AI telemetry readout
    Table,              // Multi-column Markdown pipe table (`| Col A | Col B |`)
    Illustration,       // Standalone `#fullwidth` or `#inline` scene illustration + caption
    ImageCarousel,      // Interactive multi-slide ` ````carousel ` gallery with Prev/Next buttons
    SceneDivider        // Thematic scene break (`***`, `---`, `<hr>`) rendered as `•   •   •`
}

/// <summary>
/// A single semantic content block inside a <see cref="ParsedChapterPart"/>.
/// </summary>
public sealed class BookContentBlock
{
    public BookBlockType BlockType { get; set; } = BookBlockType.Paragraph;
    public string Text { get; set; } = string.Empty;
    public string AnchorId { get; set; } = string.Empty;
    public string AlertKind { get; set; } = string.Empty;
    public bool IsZoeKernelLog { get; set; }
    public bool IsInlineIllustration { get; set; }
    public List<RichTextSpan> Spans { get; set; } = [];
    public List<List<RichTextSpan>> ListItems { get; set; } = [];
    public Dictionary<string, string> EmailHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public MarkdownTableModel? Table { get; set; }
    public string ImageUri { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public List<BookSlideImage> CarouselSlides { get; set; } = [];
    public List<BookInlineLink> Links { get; set; } = [];
}

/// <summary>
/// Represents an inline hyperlink inside a book paragraph or Backstage dossier, with helper
/// booleans classifying RaggieSoft cross-app links (Music, Books, and Backstage).
/// </summary>
public sealed class BookInlineLink
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    // Lets click handlers intercept ecosystem URLs and route them natively to `ListeningRoom.exe`,
    // `BookReader.exe`, or `Backstage.exe` over Named Pipe IPC before falling back to a web browser!
    public bool IsMusicLink => Url.Contains("music.raggiesoft.com", StringComparison.OrdinalIgnoreCase);
    public bool IsBookLink => Url.Contains("books.raggiesoft.com", StringComparison.OrdinalIgnoreCase);
    public bool IsBackstageLink => Url.Contains("backstage.raggiesoft.com", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Represents a parsed reference to an Engine Room Records artist, album, or track linked inside prose.
/// </summary>
public sealed class BookMusicReference
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ArtistSlug { get; set; } = string.Empty;
    public string AlbumSlug { get; set; } = string.Empty;
    public string TrackSlug { get; set; } = string.Empty;
}
