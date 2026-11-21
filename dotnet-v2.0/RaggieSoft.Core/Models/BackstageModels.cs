// ================================================================================
// FILE: BackstageModels.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file defines the C# data blueprints ("models") for **RaggieSoft Backstage**
//     (`Backstage.exe` and `https://backstage.raggiesoft.com`) as well as the
//     in-chapter "Read Dossier" popup inside **RaggieSoft Book Reader**.
//
//   Why does this file exist?
//     In PHP, calling `json_decode($json, true)` gives you a loose associative array
//     like `$data['counts']['total']` where typos aren't caught until runtime.
//     In C#, we define strongly typed classes whose properties match the JSON fields
//     served by our static CDN (`https://assets.raggiesoft.com`):
//       1. `/raggiesoft-backstage/api/v1/index.json`
//          -> Deserialized into `BackstageIndexResponse` (collection counts +
//             summary cards `BackstageSummaryItem` for every behind-the-scenes
//             dossier, in-universe character email, and worldbuilding guide).
//       2. `/raggiesoft-backstage/api/v1/<slug>.json`
//          -> Deserialized into `BackstageRecordDetail` (the full dossier metadata
//             plus pre-rendered HTML body).
//
//   What visible features does it power?
//     - The left-hand collection filter sidebar in RaggieSoft Backstage (`The Rachel
//       Narrative`, `Katie & Nathaniel`, `The Caretakers`, `The Eleventh Child`,
//       and `Campus & Music Worldbuilding Guides`).
//     - Searchable dossier summary cards with category/timestamp badges.
//     - Native WPF rendering (`BackstageContentBlock` + `BackstageBlockType`) of
//       headings, in-universe corporate emails/memos (`From:` / `To:` / `Subject:`),
//       console logs, comparison tables, 16:9 illustrations, and custom Ocean View
//       Archives narrative tags—without needing an embedded web browser!
// ================================================================================

using System.Text.Json;               // Core JSON reader/writer (`Utf8JsonReader`, `JsonSerializer`).
using System.Text.Json.Serialization; // Attributes like `[JsonPropertyName]`, `[JsonIgnore]`, and `JsonConverter<T>`.

namespace RaggieSoft.Core.Models;

/// <summary>
/// Tolerant JSON converter for <see cref="BackstageIndexResponse.Items"/> that accepts EITHER
/// the canonical dictionary-by-slug object (<c>{ "books/rachel/...": { ... } }</c>)
/// OR a JSON array (<c>[ { ... } ]</c>) without throwing a <see cref="JsonException"/>.
/// </summary>
// WHY DO WE NEED A CUSTOM `JsonConverter` HERE?
//   In PHP, arrays do double duty: `['a' => $obj1, 'b' => $obj2]` serializes to a JSON
//   OBJECT `{ "a": {...}, "b": {...} }`, while `[$obj1, $obj2]` serializes to a JSON
//   ARRAY `[ {...}, {...} ]`.
//   C#'s `List<BackstageSummaryItem>` normally ONLY accepts `[ ... ]` and would throw an
//   error if `index.json` stores `"items"` as a `{ "slug": { ... } }` dictionary!
//   By inheriting from `JsonConverter<List<BackstageSummaryItem>>`, we teach `System.Text.Json`
//   how to inspect the first token (`[` vs `{`) and convert EITHER JSON shape into one clean
//   `List<BackstageSummaryItem>` automatically.
public sealed class BackstageItemsDictionaryOrListConverter : JsonConverter<List<BackstageSummaryItem>>
{
    // `override` replaces the base `JsonConverter.Read` method with our custom reading logic.
    // `ref Utf8JsonReader reader` passes the low-level, zero-allocation JSON cursor by reference
    // (`ref`) so advancing the reader inside this method advances the caller's stream position too.
    public override List<BackstageSummaryItem> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // Case 1: `"items": [ { ... }, { ... } ]` (standard JSON array `[`)
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Deserialize directly as a `List<BackstageSummaryItem>`.
            // `?? []` is the **null-coalescing operator** (`??` in PHP too!) combined with C#'s
            // empty-list expression `[]`: if `Deserialize` returns `null`, return an empty list instead.
            return JsonSerializer.Deserialize<List<BackstageSummaryItem>>(ref reader, options) ?? [];
        }

        // Case 2: `"items": { "books/rachel/email-01": { ... }, ... }` (JSON object `{` keyed by slug)
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // `Dictionary<string, BackstageSummaryItem>` is C#'s hash map / associative array
            // mapping each string key (the dossier slug) to its `BackstageSummaryItem` value.
            var dict = JsonSerializer.Deserialize<Dictionary<string, BackstageSummaryItem>>(ref reader, options);
            if (dict is null)
            {
                return [];
            }

            // Pre-size the `List` to `dict.Count` so .NET allocates the exact array capacity once.
            var list = new List<BackstageSummaryItem>(dict.Count);

            // Tuple deconstruction (`var (slugKey, item)`): just like `foreach ($dict as $slugKey => $item)`
            // in PHP! If the inner JSON object omitted `"slug"`, we copy the dictionary key into `item.Slug`.
            foreach (var (slugKey, item) in dict)
            {
                if (string.IsNullOrWhiteSpace(item.Slug))
                {
                    item.Slug = slugKey;
                }
                list.Add(item);
            }
            return list;
        }

        // Case 3: Unexpected JSON token (e.g. `null` or a primitive). Skip past it cleanly so the
        // rest of `index.json` can still finish parsing without crashing.
        reader.Skip();
        return [];
    }

    // `Write` handles the reverse direction (C# -> JSON) if we ever serialize this object back out.
    public override void Write(
        Utf8JsonWriter writer,
        List<BackstageSummaryItem> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// Sidebar collection filter options for RaggieSoft Backstage (e.g., "All Records",
/// "The Rachel Narrative", "Katie &amp; Nathaniel", "Worldbuilding Guides").
/// </summary>
public sealed class BackstageCollectionOption
{
    // Notice `{ get; init; }` vs `{ get; set; }`:
    //   - `init` (introduced in C# 9) lets you set `Key`, `Title`, and `Subtitle` ONCE when creating
    //     the object (`new BackstageCollectionOption { Key = "rachel", Title = "..." }`), and then
    //     locks them as read-only forever!
    //   - `set` on `Count` allows the UI to update the record count later when `index.json` refreshes.
    public string Key { get; init; } = "all";
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public int Count { get; set; }

    // `=>` defines a read-only **computed property** (expression-bodied property): every time
    // WPF data-binding reads `DisplayBadge`, C# evaluates `$"..."` (string interpolation, like
    // `"{$Count} Records"` in PHP) on the fly!
    public string DisplayBadge => $"{Count} Records";
}

/// <summary>
/// Aggregate collection counts inside <c>/raggiesoft-backstage/api/v1/index.json</c>.
/// </summary>
public sealed class BackstageCounts
{
    // `[JsonPropertyName("total")]` is a C# **Attribute** (metadata tag in square brackets,
    // similar to PHP 8 `#[...]` attributes). In C#, public properties use `PascalCase` (`Total`,
    // `RachelDossiers`) by convention, while JSON APIs use `camelCase` (`"total"`, `"rachelDossiers"`).
    // `[JsonPropertyName]` bridges that naming difference explicitly!
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("rachel")]
    public int Rachel { get; set; }

    [JsonPropertyName("rachelDossiers")]
    public int RachelDossiers { get; set; }

    [JsonPropertyName("katie")]
    public int Katie { get; set; }

    [JsonPropertyName("katieDossiers")]
    public int KatieDossiers { get; set; }

    [JsonPropertyName("casey")]
    public int Casey { get; set; }

    [JsonPropertyName("caseyDossiers")]
    public int CaseyDossiers { get; set; }

    [JsonPropertyName("aubrie")]
    public int Aubrie { get; set; }

    [JsonPropertyName("aubrieDossiers")]
    public int AubrieDossiers { get; set; }

    [JsonPropertyName("lore")]
    public int Lore { get; set; }

    [JsonPropertyName("hubs")]
    public int Hubs { get; set; }
}

/// <summary>
/// Root response envelope for <c>/raggiesoft-backstage/api/v1/index.json</c>.
/// </summary>
public sealed class BackstageIndexResponse
{
    // Schema version string from `index.json` (defaults to `"1.0.0"`).
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    // Pre-computed per-collection totals (`= new()` ensures `Counts` is never `null` even on an empty response).
    [JsonPropertyName("counts")]
    public BackstageCounts Counts { get; set; } = new();

    // `[JsonConverter(...)]` plugs in our `BackstageItemsDictionaryOrListConverter` above so `"items"`
    // deserializes cleanly into a `List<BackstageSummaryItem>` whether JSON used `{...}` or `[...]`.
    [JsonPropertyName("items")]
    [JsonConverter(typeof(BackstageItemsDictionaryOrListConverter))]
    public List<BackstageSummaryItem> Items { get; set; } = [];
}

/// <summary>
/// Summary metadata entry for a single Backstage dossier, correspondence record,
/// or worldbuilding guide inside <c>index.json</c>. Powers the searchable dossier list
/// in <c>Backstage.exe</c>.
/// </summary>
public sealed class BackstageSummaryItem
{
    // Unique URL path key for this dossier, e.g. `"books/rachel/delores-intake-memo"`.
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    // Human-readable title and subtitle shown on the dossier card.
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    // Record classification (e.g. `"Correspondence"`, `"Dossier"`, `"Architecture Guide"`).
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    // Parent series/collection name (e.g. `"The Rachel Narrative"`).
    [JsonPropertyName("collection")]
    public string Collection { get; set; } = string.Empty;

    // In-universe story date, clock time, or formatted timestamp string.
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    // In-universe setting where the document was written or filed.
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    // Optional correspondence header metadata for in-universe emails and memos.
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    // Companion link back to the novel chapter on `https://books.raggiesoft.com/...`
    // (which `Backstage.exe` intercepts to jump directly into `BookReader.exe` via Named Pipe IPC!).
    [JsonPropertyName("relatedBookUrl")]
    public string RelatedBookUrl { get; set; } = string.Empty;

    [JsonPropertyName("relatedBookLabel")]
    public string RelatedBookLabel { get; set; } = string.Empty;

    // Primary 16:9 illustration path (`Image`), OpenGraph social card fallback (`OgImage`),
    // accessible alt-text description (`ImageAlt`), and short card teaser (`Summary`).
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("ogImage")]
    public string OgImage { get; set; } = string.Empty;

    [JsonPropertyName("imageAlt")]
    public string ImageAlt { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    // `[JsonIgnore]` tells `System.Text.Json`: "Do NOT look for these properties in JSON and do NOT
    // write them out when serializing." They are convenience/UI helpers computed at runtime in C#!

    // Optional reference if this record is linked to a curated multi-stop `BookStoryArc`.
    [JsonIgnore]
    public BookStoryArc? AssociatedStoryArc { get; set; }

    // Identifies web-only landing/overview hub pages in `index.json` so `BackstageDataService`
    // can filter them out and display only actual readable dossiers & guides in the desktop app.
    // (`StringComparison.OrdinalIgnoreCase` performs fast, culture-safe case-insensitive matching.)
    [JsonIgnore]
    public bool IsCollectionHub =>
        string.Equals(Slug, "index", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Slug, "books/rachel", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Slug, "books/katie", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Slug, "books/quantum-directive-3-casey", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Slug, "books/aubrie", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Category, "Collection Hub", StringComparison.OrdinalIgnoreCase);

    // Builds a clean one-line eyebrow badge for UI list items, such as
    // `"Correspondence • October 14, 2016 • 9:15 AM"`, skipping any blank pieces cleanly.
    [JsonIgnore]
    public string MetadataBadge
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Category))
            {
                parts.Add(Category);
            }
            if (!string.IsNullOrWhiteSpace(Timestamp))
            {
                parts.Add(Timestamp);
            }
            else if (!string.IsNullOrWhiteSpace(Date))
            {
                parts.Add(string.IsNullOrWhiteSpace(Time) ? Date : $"{Date} • {Time}");
            }
            return string.Join(" • ", parts);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if this summary item belongs to the sidebar collection filter
    /// selected by the user (<c>"all"</c>, <c>"rachel"</c>, <c>"katie"</c>, <c>"casey"</c>,
    /// <c>"aubrie"</c>, or <c>"worldbuilding"</c>).
    /// </summary>
    public bool MatchesCollectionKey(string key)
    {
        // "all" (or empty filter) matches every dossier in the archive.
        if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Novel-specific collections organize their slugs under `books/<series-slug>/...`.
        if (string.Equals(key, "rachel", StringComparison.OrdinalIgnoreCase))
        {
            return Slug.StartsWith("books/rachel/", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(key, "katie", StringComparison.OrdinalIgnoreCase))
        {
            return Slug.StartsWith("books/katie/", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(key, "casey", StringComparison.OrdinalIgnoreCase))
        {
            return Slug.StartsWith("books/quantum-directive-3-casey/", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(key, "aubrie", StringComparison.OrdinalIgnoreCase))
        {
            return Slug.StartsWith("books/aubrie/", StringComparison.OrdinalIgnoreCase);
        }

        // Everything outside `books/...` is a cross-series Campus, Transit, or Music Worldbuilding Guide!
        if (string.Equals(key, "worldbuilding", StringComparison.OrdinalIgnoreCase))
        {
            return !Slug.StartsWith("books/", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}

/// <summary>
/// Complete dossier payload from <c>/raggiesoft-backstage/api/v1/&lt;slug&gt;.json</c>.
/// Contains both top-level metadata and the raw <see cref="Html"/> body that
/// <c>BackstageDataService</c> parses into native WPF <see cref="Blocks"/>.
/// </summary>
public sealed class BackstageRecordDetail
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("collection")]
    public string Collection { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("relatedBookUrl")]
    public string RelatedBookUrl { get; set; } = string.Empty;

    [JsonPropertyName("relatedBookLabel")]
    public string RelatedBookLabel { get; set; } = string.Empty;

    // Public web permalink on `https://backstage.raggiesoft.com/...` (for "Open on Web" / sharing).
    [JsonPropertyName("canonicalUrl")]
    public string CanonicalUrl { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("ogImage")]
    public string OgImage { get; set; } = string.Empty;

    [JsonPropertyName("imageAlt")]
    public string ImageAlt { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    // Pre-rendered semantic HTML body generated by the Backstage static build pipeline.
    // Instead of spinning up a heavy Chromium WebView2 browser just to display text,
    // `BackstageDataService.ParseHtmlIntoBlocks` converts this HTML into native `Blocks` below!
    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    // --- Runtime-Enriched Properties (`[JsonIgnore]`) ---

    // Local `%LOCALAPPDATA%` file path (when cached offline) or `https://assets.raggiesoft.com/...` URL
    // for the dossier's top 16:9 hero illustration.
    [JsonIgnore]
    public string ResolvedHeroImageUri { get; set; } = string.Empty;

    // Ordered list of structured native content blocks rendered by `RaggieSoftFlowDocumentBuilder`.
    [JsonIgnore]
    public List<BackstageContentBlock> Blocks { get; set; } = [];

    // Any Engine Room Records songs (`https://music.raggiesoft.com/...`) linked inside the dossier prose,
    // surfaced as one-click "Play in The Listening Room" buttons!
    [JsonIgnore]
    public List<BookMusicReference> MusicReferences { get; set; } = [];
}

/// <summary>
/// Structured multi-column comparison table parsed from an HTML <c>&lt;table&gt;</c> inside a Backstage dossier.
/// </summary>
public sealed class BackstageTableModel
{
    // Column header labels extracted from `<th>` cells (`<tr><th>...</th></tr>`).
    public List<string> Headers { get; set; } = [];

    // 2D list of body rows and cell strings extracted from `<td>` cells (`Rows[rowIndex][colIndex]`).
    public List<List<string>> Rows { get; set; } = [];
}

/// <summary>
/// Semantic block categories produced when <c>BackstageDataService</c> parses a dossier's HTML body.
/// </summary>
// An `enum` (enumeration) defines a fixed, compiler-checked vocabulary of choices.
// Unlike passing raw strings like `"h2"` or `"email"` in PHP (where `"emial"` would silently fail),
// an `enum` gives us autocomplete and compile-time safety in `switch (block.BlockType)`.
public enum BackstageBlockType
{
    Heading2,           // Major section heading (`<h2>`) with subtle bottom rule
    Heading3,           // Subsection heading (`<h3>` / `<h4>`)
    Paragraph,          // Standard literary/dossier body paragraph (`<p>`)
    Blockquote,         // Quoted excerpt (`<blockquote>`) with left accent border
    CorrespondenceCard, // In-universe email or memo card (`From:`, `To:`, `Subject:` header + body)
    ConsoleBlock,       // Monospace terminal / system log readout (`<pre>`)
    BulletItem,         // Bulleted list item (`<li>`)
    Illustration,       // Standalone `<figure>` or `<img ...>` scene plate with caption
    Table,              // Multi-column comparison table (`<table>`)
    Divider             // Thematic scene break (`<hr>`) rendered as `•   •   •`
}

/// <summary>
/// Native structured block rendered inside <c>FlowDocumentScrollViewer</c> in Backstage
/// and in Book Reader's inline Backstage Quick-Read modal.
/// </summary>
public sealed class BackstageContentBlock
{
    // Tells `RaggieSoftFlowDocumentBuilder` which visual WPF element to build for this block.
    public BackstageBlockType BlockType { get; set; }

    // Plain-text representation of the block (used directly for headings/console logs, or as a fallback).
    public string Text { get; set; } = string.Empty;

    // Rich inline styling runs (bold, italic, links, `<aac>`, `<led>`, `<asl>`, etc.) inside paragraphs/quotes/lists.
    public List<RichTextSpan> Spans { get; set; } = [];

    // Case-insensitive dictionary (`StringComparer.OrdinalIgnoreCase`) holding `From`, `To`, `Cc`,
    // `Date`, `Sent`, and `Subject` lines when `BlockType == BackstageBlockType.CorrespondenceCard`.
    public Dictionary<string, string> EmailHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Resolved image path/URL and accessible alt caption when `BlockType == BackstageBlockType.Illustration`.
    public string ImageUri { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;

    // Populated when `BlockType == BackstageBlockType.Table` (`?` means `null` for non-table blocks).
    public BackstageTableModel? Table { get; set; }

    // Clickable inline hyperlinks found inside this block.
    public List<BookInlineLink> Links { get; set; } = [];
}
