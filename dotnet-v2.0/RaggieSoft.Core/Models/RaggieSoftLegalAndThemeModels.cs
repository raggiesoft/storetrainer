// ================================================================================
// FILE: RaggieSoftLegalAndThemeModels.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file holds four shared building blocks used across every Windows app
//     in the RaggieSoft desktop suite (`BookReader`, `Backstage`, `ListeningRoom`,
//     and `RaggieSoftLauncher`):
//       1. `RaggieSoftLegalCopy`: Warm, everyday-English Work of Fiction & Creative
//          Commons licensing notices (with zero technical jargon).
//       2. `RaggieSoftTitleNormalizer`: Cleans up Era, Chapter, and Part titles so
//          sidebar trees never show stuttered labels like `"Part 1: Part 1: ..."`.
//       3. `RichTextSpan` & `RichTextSpanKind`: The inline rich-text tokens that
//          represent bold/italic/links AND custom Ocean View Archives narrative tags
//          (`<aac>` speech-tablet output, `<pa>` public-address announcements,
//          `<led>` amber transit destination signs, `<sms>` text messages, `<asl>`
//          American Sign Language glosses, `<sgn>` gestures, and `<lang>` phrases).
//       4. `NarrativeThemeColors`, `NarrativeThemePalette`, & `NarrativeThemeCssParser`:
//          Reads canonical web stylesheets (`/raggiesoft-books/css/themes/<slug>.css`)
//          and translates their `--rs-*` CSS variables into Light, Sepia, and Dark
//          color palettes for native Windows WPF controls!
//
//   Why does this file exist?
//     On the web (`books.raggiesoft.com`), the browser natively understands CSS
//     variables (`--rs-bg`, `--rs-primary`) and custom HTML tags (`<led>`, `<aac>`).
//     Windows WPF (`FlowDocument`) is NOT a web browser—it paints native DirectX
//     vector typography using C# `Brush` and `Inline` objects. By parsing the exact
//     same `.css` theme files and custom tags into strongly typed C# models here,
//     authors write a chapter ONCE in Markdown + CSS, and both the website and the
//     native Windows app render the exact same colors, transit signs, and badges!
// ================================================================================

using System.Text.RegularExpressions; // Gives us `Regex` and `Match` (like `preg_match` / `preg_replace` in PHP).

namespace RaggieSoft.Core.Models;

/// <summary>
/// Shared consumer-friendly legal and licensing copy used across RaggieSoft Book Reader,
/// RaggieSoft Backstage, The Listening Room, and RaggieSoft Launcher.
/// Written in warm, everyday prose with zero technical jargon.
/// </summary>
// `static class` means this class holds ONLY `const` / `static` members—you never write
// `new RaggieSoftLegalCopy()`. Instead, any dialog or footer simply reads
// `RaggieSoftLegalCopy.FictitiousDisclaimerBody`.
public static class RaggieSoftLegalCopy
{
    /// <summary>
    /// Heading displayed on first-run acknowledgement dialogs, scrollable footers, and About views.
    /// </summary>
    public const string FictitiousDisclaimerTitle = "Work of Fiction Notice";

    /// <summary>
    /// Mandatory Fictitious Persons &amp; Entities Disclaimer covering all narrative characters,
    /// universities, corporations, transit agencies, in-universe LLCs, and events.
    /// </summary>
    // In C#, `+` between string literals is concatenated by the compiler at build time into
    // one single string in the compiled `.dll`, with zero runtime performance cost.
    public const string FictitiousDisclaimerBody =
        "All narrative characters, bands, biographies, universities (including Commonwealth Polytechnic Institute / CPI, " +
        "Virginia Dominion University / VDU, Community College of Hampton Roads / CCHR, The Forgers, and The Crucible), " +
        "corporations (including OmniCorp, Quantum Corporation, Apex Records, Prairie Coachworks, and Sainte-Claire), " +
        "transit agencies (Blue Ridge Transit / BRT and Coastal Virginia Transit Authority / CVTA), in-universe correspondence, " +
        "in-universe legal filings (including Engine Room Records, LLC, The O'Connell Family, LLC, and Stardust Properties, LLC), " +
        "and events across RaggieSoft Media, Ocean View Archives, Engine Room Records, and RaggieSoft Backstage are works of fiction. " +
        "Any resemblance to actual persons, living or dead, or actual businesses, institutions, or events is purely coincidental.";

    /// <summary>
    /// Clarifies real-world unincorporated creative brands and open-culture / commercial licensing.
    /// </summary>
    public const string CreativeLicenseSummary =
        "RaggieSoft, RaggieSoft Media, Ocean View Archives, and Engine Room Records are creative imprints of Michael P. Ragsdale. " +
        "Stories, illustrations, dossiers, and free music previews are shared under the Creative Commons Attribution-ShareAlike 4.0 " +
        "International License (CC BY-SA 4.0), and application source code is licensed under the MIT License.";
}

/// <summary>
/// Appearance modes supported by RaggieSoft reading surfaces.
/// <see cref="System"/> automatically follows the user's Windows Light/Dark theme setting
/// (just like <c>prefers-color-scheme</c> in CSS).
/// </summary>
public enum ReaderAppearanceMode
{
    System, // Follow Windows Settings -> Personalization -> Colors (Light or Dark)
    Light,  // Warm ivory paper background with high-contrast charcoal ink
    Sepia,  // Soft vintage book-paper warmth for low-glare daytime/evening reading
    Dark    // Deep slate night canvas with crisp light typography
}

/// <summary>
/// Line-spacing presets for comfortable long-form literary reading.
/// </summary>
public enum ReaderLineSpacingPreset
{
    Compact,     // 1.48x line height (fits more prose on smaller laptop screens)
    Comfortable, // 1.68x line height (default literary paperback proportion)
    Relaxed      // 1.92x line height (extra breathing room; great with OpenDyslexic)
}

/// <summary>
/// Normalizes structural titles across Era/Book, Chapter, and Part levels so UI trees
/// never duplicate prefixes (e.g., preventing <c>"Part 1: Part 1: ..."</c> or <c>"Chapter 1: Chapter 1: ..."</c>).
/// </summary>
public static class RaggieSoftTitleNormalizer
{
    // `@"..."` is a C# **verbatim string literal**: backslashes like `\b` are passed straight to
    // the Regex engine without needing double-backslashes `"\\b"` like in PHP strings!
    // `RegexOptions.IgnoreCase | RegexOptions.Compiled` combines two flags with bitwise OR (`|`):
    //   - `IgnoreCase`: matches `"Part"`, `"part"`, or `"PART"`
    //   - `Compiled`: compiles the regex pattern into fast native .NET instructions once at startup
    private static readonly Regex ExistingStructuralWordRegex = new(
        @"^(?:part|chapter|episode|book|era|act|volume|movement|prologue|epilogue|interlude)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Collapses accidental repeated prefixes and prepends the structural prefix at most once.
    /// </summary>
    /// <param name="rawTitle">The raw title string from <c>toc.json</c> or YAML frontmatter (may be <c>null</c>).</param>
    /// <param name="structuralWord">The hierarchy level word, such as <c>"Episode"</c>, <c>"Chapter"</c>, or <c>"Part"</c>.</param>
    /// <param name="number">The 1-based Episode, Chapter, or Part number.</param>
    /// <param name="includeNumberPrefix">When <c>true</c>, returns <c>"Part 1: Title"</c>; when <c>false</c>, strips <c>"Part 1:"</c> to leave just <c>"Title"</c>.</param>
    public static string NormalizeStructuralTitle(
        string? rawTitle,
        string structuralWord,
        int number,
        bool includeNumberPrefix)
    {
        // `(rawTitle ?? string.Empty).Trim()` safely turns `null` into `""` and trims outer spaces.
        var cleaned = (rawTitle ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            // Fallback when a Part or Chapter had no custom title: e.g. `"Part 1"` or `"Part"`.
            return includeNumberPrefix ? $"{structuralWord} {number}" : structuralWord;
        }

        // Build a regex that matches one or more leading `"Part 1: "` / `"Chapter 2 — "` prefixes
        // (`Regex.Escape` safely escapes any special characters in `structuralWord`).
        // Collapse repeated prefixes like "Part 1: Part 1: Title" or "Chapter 2 — Chapter 2: Title"
        var escapedWord = Regex.Escape(structuralWord);
        var duplicatePrefixRegex = new Regex(
            $@"^(?:{escapedWord}\s+\d+\s*[:\-—–]\s*)+",
            RegexOptions.IgnoreCase);

        var match = duplicatePrefixRegex.Match(cleaned);
        if (match.Success)
        {
            // `cleaned[match.Length..]` slices everything AFTER the matched prefix(es)!
            var remainder = cleaned[match.Length..].Trim();
            if (includeNumberPrefix)
            {
                return string.IsNullOrWhiteSpace(remainder)
                    ? $"{structuralWord} {number}"
                    : $"{structuralWord} {number}: {remainder}";
            }

            return string.IsNullOrWhiteSpace(remainder) ? cleaned : remainder;
        }

        // If the caller only wants the bare title (for example, the big H1 chapter heading when an
        // eyebrow above it already shows the structural location), return `cleaned` as-is.
        if (!includeNumberPrefix)
        {
            return cleaned;
        }

        // If the title already begins with a structural label ("Book 1:", "Episode 4:", "Prologue:", etc.), keep it intact
        // so we never produce awkward titles like `"Chapter 1: Prologue: The Storm"`.
        if (ExistingStructuralWordRegex.IsMatch(cleaned))
        {
            return cleaned;
        }

        // Otherwise prepend `"Episode 1: "` / `"Chapter 2: "` / `"Part 3: "` cleanly once.
        return $"{structuralWord} {number}: {cleaned}";
    }
}

/// <summary>
/// Semantic inline token types for literary prose and custom Ocean View Archives tags.
/// </summary>
public enum RichTextSpanKind
{
    Plain,           // Unstyled roman body text
    Bold,            // `**bold**` or `<strong>`
    Italic,          // `*italic*` or `<em>` (vocal emphasis, internal thought, book/album titles)
    BoldItalic,      // `***bold italic***`
    Code,            // `` `code` `` or `<code>` inline monospace badge
    Link,            // `[label](url)` or `<a href="url">` clickable hyperlink
    AacSpeech,       // `<aac>...</aac>` Augmentative & Alternative Communication speech-tablet pill
    PaAnnouncement,  // `<pa>...</pa>` Transit / campus / shipboard public-address speaker pill
    LedSign,         // `<led>...</led>` Amber dot-matrix transit bus destination sign
    SmsBubble,       // `<sms>...</sms>` Inline text-message bubble pill
    AslSign,         // `<asl>...</asl>` American Sign Language gloss badge
    GestureTag,      // `<sgn>...</sgn>` Non-verbal / tactile grounding gesture badge
    LanguageSpan,    // `<lang>...</lang>` Non-English spoken phrase rendered in literary italics
    CredentialBadge  // `[credential ...]` In-universe ID / access badge pill
}

/// <summary>
/// Represents a single styled inline run inside a paragraph, list item, blockquote, or table cell.
/// Think of a <see cref="Paragraph"/> as an HTML <c>&lt;p&gt;</c> tag, and a list of
/// <see cref="RichTextSpan"/> objects as the <c>Text</c>, <c>&lt;em&gt;</c>, <c>&lt;a&gt;</c>,
/// and custom <c>&lt;led&gt;</c> nodes inside that paragraph!
/// </summary>
public sealed class RichTextSpan
{
    public RichTextSpanKind Kind { get; set; } = RichTextSpanKind.Plain;
    public string Text { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string MetadataTag { get; set; } = string.Empty;

    /// <summary>
    /// Optional full-height transit route number for <see cref="RichTextSpanKind.LedSign"/>
    /// (e.g., <c>"913"</c> in <c>&lt;led&gt;913 | VA BEACH | SHORE &amp; PLEAS HSE&lt;/led&gt;</c>).
    /// </summary>
    public string LedRoute { get; set; } = string.Empty;

    /// <summary>
    /// Primary (top or single) destination line for <see cref="RichTextSpanKind.LedSign"/>.
    /// </summary>
    public string LedLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Optional secondary (bottom stacked) destination line for <see cref="RichTextSpanKind.LedSign"/>.
    /// </summary>
    public string LedLine2 { get; set; } = string.Empty;

    /// <summary>
    /// Parses a <c>&lt;led ...&gt;...&lt;/led&gt;</c> tag's attributes and inner body into a
    /// structured <see cref="RichTextSpan"/> supporting single-line headers, two-line stacked
    /// matrices, and full-height left route numbers beside stacked destination lines.
    /// </summary>
    public static RichTextSpan ParseLedSign(string attrString, string rawInner)
    {
        // Step 1: Parse any HTML-style attributes inside the opening `<led ...>` tag, such as
        // `route="913" line1="VA BEACH" line2="SHORE &amp; PLEAS HSE"`.
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(attrString))
        {
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                attrString,
                @"([a-zA-Z0-9_-]+)\s*=\s*(?:""([^""]*)""|'([^']*)')"))
            {
                // Group 1 = attribute name; Group 2 = double-quoted value; Group 3 = single-quoted value.
                var key = m.Groups[1].Value.Trim();
                var val = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                // `HtmlDecode` converts `&amp;` back into `&` (like `html_entity_decode()` in PHP).
                attrs[key] = System.Net.WebUtility.HtmlDecode(val).Trim();
            }
        }

        // `TryGetValue("route", out var rVal)` checks whether `"route"` exists in `attrs` and, if so,
        // assigns its value to `rVal` in one fast lookup (without throwing a `KeyNotFoundException`).
        var route = attrs.TryGetValue("route", out var rVal) ? rVal : string.Empty;
        var lines = new List<string>();

        // Step 2A: If explicit `line1="..."` / `line2="..."` attributes were provided, use them directly.
        if (attrs.ContainsKey("line1") || attrs.ContainsKey("line2"))
        {
            if (attrs.TryGetValue("line1", out var l1) && !string.IsNullOrWhiteSpace(l1))
            {
                lines.Add(l1);
            }
            if (attrs.TryGetValue("line2", out var l2) && !string.IsNullOrWhiteSpace(l2))
            {
                lines.Add(l2);
            }
        }
        else
        {
            // Step 2B: Otherwise parse pipe-delimited or `<br>`-delimited inner text such as:
            //   - 3 parts: `<led>913 | VA BEACH | SHORE & PLEAS HSE</led>` -> Route + Line 1 + Line 2
            //   - 2 parts: `<led>OUT OF SERVICE | GARAGE</led>`            -> Line 1 + Line 2
            //   - 1 part:  `<led>112 NNTC</led>`                           -> Single-line header
            var normalized = System.Text.RegularExpressions.Regex.Replace(
                rawInner ?? string.Empty,
                @"<br\s*/?>|\r?\n",
                "|",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"<[^>]+>", string.Empty);
            normalized = System.Net.WebUtility.HtmlDecode(normalized);

            var parts = normalized
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (string.IsNullOrEmpty(route) && parts.Count >= 3)
            {
                route = parts[0];
                lines.Add(parts[1]);
                lines.Add(string.Join(" ", parts.Skip(2)));
            }
            else if (parts.Count >= 2)
            {
                lines.Add(parts[0]);
                lines.Add(string.Join(" ", parts.Skip(1)));
            }
            else if (parts.Count == 1)
            {
                lines.Add(parts[0]);
            }
        }

        // Step 3: Build a clean plain-text summary (`fallbackText`, e.g. `"913 | VA BEACH / SHORE & PLEAS HSE"`)
        // used both for plain-text extraction and for Windows Narrator / JAWS / NVDA screen-reader labels!
        var line1 = lines.Count > 0 ? lines[0] : string.Empty;
        var line2 = lines.Count > 1 ? lines[1] : string.Empty;
        var joinedLines = string.Join(" / ", lines);
        var fallbackText = (!string.IsNullOrEmpty(route) && !string.IsNullOrEmpty(joinedLines))
            ? $"{route} | {joinedLines}"
            : (!string.IsNullOrEmpty(route) ? route : joinedLines);

        return new RichTextSpan
        {
            Kind = RichTextSpanKind.LedSign,
            Text = fallbackText,
            LedRoute = route,
            LedLine1 = line1,
            LedLine2 = line2
        };
    }
}

/// <summary>
/// Represents a structured Markdown (<c>| Col A | Col B |</c>) or HTML comparison table.
/// </summary>
public sealed class MarkdownTableModel
{
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
}

/// <summary>
/// Single-polarity hex color token set (WCAG 2.1 AA / Section 508 contrast verified, &gt;= 4.5:1).
/// Stores <c>#RRGGBB</c> hex strings so <c>RaggieSoft.Core</c> stays 100% platform-independent
/// (reusable across Windows WPF, tests, or CLI tools without depending on WPF's <c>System.Windows.Media</c>).
/// </summary>
public sealed class NarrativeThemeColors
{
    public string BackgroundHex { get; set; } = "#FAF8F5";      // Main page reading canvas (`--rs-bg`)
    public string SurfaceHex { get; set; } = "#FFFFFF";         // Elevated card / table row surface (`--rs-surface`)
    public string CardBackgroundHex { get; set; } = "#F4F1EA";  // Callout / quote / caption card tint (`--rs-card-bg`)
    public string TextHex { get; set; } = "#1C1917";            // Primary body prose ink (`--rs-text`)
    public string MutedTextHex { get; set; } = "#57534E";       // Datelines, captions, and subtle labels (`--rs-text-muted`)
    public string HeadingHex { get; set; } = "#111827";         // Chapter & scene titles (`--rs-heading`)
    public string PrimaryAccentHex { get; set; } = "#1D4ED8";   // Links, eyebrows, and active borders (`--rs-primary`)
    public string OnPrimaryHex { get; set; } = "#FFFFFF";       // Text drawn atop PrimaryAccentHex (`--rs-on-primary`)
    public string SecondaryAccentHex { get; set; } = "#0F766E"; // Secondary pills like SMS/PA/Tips (`--rs-secondary`)
    public string BorderHex { get; set; } = "#D6D3D1";          // Subtle card and table borders (`--rs-border`)
    public string AslBadgeHex { get; set; } = "#0369A1";        // `<asl>` badge accent (`--rs-asl`)
    public string GestureBadgeHex { get; set; } = "#4338CA";    // `<sgn>` gesture badge accent (`--rs-gesture`)

    /// <summary>
    /// Creates an independent shallow copy of this color set so mutating one palette never
    /// accidentally alters another.
    /// </summary>
    public NarrativeThemeColors Clone() => new()
    {
        BackgroundHex = BackgroundHex,
        SurfaceHex = SurfaceHex,
        CardBackgroundHex = CardBackgroundHex,
        TextHex = TextHex,
        MutedTextHex = MutedTextHex,
        HeadingHex = HeadingHex,
        PrimaryAccentHex = PrimaryAccentHex,
        OnPrimaryHex = OnPrimaryHex,
        SecondaryAccentHex = SecondaryAccentHex,
        BorderHex = BorderHex,
        AslBadgeHex = AslBadgeHex,
        GestureBadgeHex = GestureBadgeHex
    };
}

/// <summary>
/// Multi-polarity (Light + Sepia + Dark) palette resolved from a custom narrative theme
/// stylesheet (<c>/raggiesoft-books/css/themes/&lt;slug&gt;.css</c>) or standard reader defaults.
/// </summary>
public sealed class NarrativeThemePalette
{
    public string ThemeSlug { get; set; } = "default";
    public NarrativeThemeColors Light { get; set; } = CreateDefaultLight();
    public NarrativeThemeColors Sepia { get; set; } = CreateDefaultSepia();
    public NarrativeThemeColors Dark { get; set; } = CreateDefaultDark();

    /// <summary>
    /// Standard high-readability warm ivory Light Mode palette.
    /// </summary>
    public static NarrativeThemeColors CreateDefaultLight() => new()
    {
        BackgroundHex = "#FAF8F5",
        SurfaceHex = "#FFFFFF",
        CardBackgroundHex = "#F3EFE8",
        TextHex = "#1C1917",
        MutedTextHex = "#57534E",
        HeadingHex = "#0F172A",
        PrimaryAccentHex = "#1D4ED8",
        OnPrimaryHex = "#FFFFFF",
        SecondaryAccentHex = "#0F766E",
        BorderHex = "#D6D3D1",
        AslBadgeHex = "#0369A1",
        GestureBadgeHex = "#4338CA"
    };

    /// <summary>
    /// Warm book-paper Sepia Mode palette designed to reduce blue-light eye strain.
    /// </summary>
    public static NarrativeThemeColors CreateDefaultSepia() => new()
    {
        BackgroundHex = "#F4ECD8",
        SurfaceHex = "#FAF3E3",
        CardBackgroundHex = "#EFE4CA",
        TextHex = "#2C2218",
        MutedTextHex = "#5C4B3B",
        HeadingHex = "#1F160E",
        PrimaryAccentHex = "#854D0E",
        OnPrimaryHex = "#FFFBEB",
        SecondaryAccentHex = "#115E59",
        BorderHex = "#D4C3A3",
        AslBadgeHex = "#0369A1",
        GestureBadgeHex = "#4338CA"
    };

    /// <summary>
    /// Deep-charcoal Dark Mode palette engineered for comfortable night reading without harsh pure-black halation.
    /// </summary>
    public static NarrativeThemeColors CreateDefaultDark() => new()
    {
        BackgroundHex = "#121318",
        SurfaceHex = "#1A1C23",
        CardBackgroundHex = "#20232C",
        TextHex = "#F3F4F6",
        MutedTextHex = "#9CA3AF",
        HeadingHex = "#F9FAFB",
        PrimaryAccentHex = "#60A5FA",
        OnPrimaryHex = "#0B1120",
        SecondaryAccentHex = "#5EEAD4",
        BorderHex = "#2E3340",
        AslBadgeHex = "#38BDF8",
        GestureBadgeHex = "#A5B4FC"
    };
}

/// <summary>
/// Extracts CSS custom properties (<c>--rs-bg</c>, <c>--rs-text</c>, <c>--rs-primary</c>, etc.) from canonical
/// Stardust Engine / Ocean View Archives theme stylesheets (<c>/raggiesoft-books/css/themes/*.css</c>).
/// </summary>
public static class NarrativeThemeCssParser
{
    // Matches individual CSS rule blocks like `:root, .theme-light { --rs-bg: #FAF8F5; ... }`,
    // capturing everything before `{` into `(?<selector>...)` and everything inside `{...}` into `(?<body>...)`.
    private static readonly Regex CssRuleBlockRegex = new(
        @"(?<selector>[^{}]+)\{(?<body>[^{}]+)\}",
        RegexOptions.Compiled);

    // Matches hex-valued RaggieSoft CSS custom properties such as `--rs-primary: #1D4ED8`.
    private static readonly Regex CssVarRegex = new(
        @"(?<name>--rs-[a-z0-9\-]+)\s*:\s*(?<val>#[0-9a-fA-F]{3,8})\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a chapter's <c>.css</c> stylesheet text into a <see cref="NarrativeThemePalette"/>,
    /// falling back to clean reader defaults if <paramref name="cssContent"/> is empty or offline.
    /// </summary>
    public static NarrativeThemePalette Parse(string themeSlug, string? cssContent)
    {
        // Start with full Light, Sepia, and Dark defaults so even if a `.css` file only overrides
        // 3 accent variables, every other color token still has a safe, accessible default!
        var palette = new NarrativeThemePalette
        {
            ThemeSlug = string.IsNullOrWhiteSpace(themeSlug) ? "default" : themeSlug
        };

        if (string.IsNullOrWhiteSpace(cssContent))
        {
            return palette;
        }

        // Remove CSS comments (`/* ... */`) first so commented-out example tokens are ignored.
        var stripped = Regex.Replace(cssContent, @"/\*[\s\S]*?\*/", string.Empty);
        var appliedLight = false;
        var appliedDark = false;

        // Walk each `{ ... }` CSS block in top-to-bottom stylesheet order.
        foreach (Match block in CssRuleBlockRegex.Matches(stripped))
        {
            var selector = block.Groups["selector"].Value;
            var body = block.Groups["body"].Value;

            // Detect whether this CSS block targets Dark Mode (`color-scheme: dark` or `.theme-dark`)
            // vs Light Mode (`color-scheme: light`, `:root`, or `.theme-light`).
            var isDarkBlock =
                body.Contains("color-scheme: dark", StringComparison.OrdinalIgnoreCase) ||
                (selector.Contains(".theme-dark", StringComparison.OrdinalIgnoreCase) &&
                 !selector.Contains(".theme-light", StringComparison.OrdinalIgnoreCase));

            var isLightBlock =
                body.Contains("color-scheme: light", StringComparison.OrdinalIgnoreCase) ||
                selector.Contains(".theme-light", StringComparison.OrdinalIgnoreCase);

            // Collect all `--rs-*` hex variables declared inside this block.
            var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match vm in CssVarRegex.Matches(body))
            {
                vars[vm.Groups["name"].Value] = vm.Groups["val"].Value;
            }

            if (vars.Count == 0)
            {
                continue;
            }

            if (isDarkBlock && !appliedDark)
            {
                ApplyCssVars(palette.Dark, vars);
                appliedDark = true;
            }
            else if ((isLightBlock || !isDarkBlock) && !appliedLight)
            {
                ApplyCssVars(palette.Light, vars);
                // Keep Sepia warm paper canvas while adopting the chapter's bespoke accent colors
                if (vars.TryGetValue("--rs-primary", out var pri)) palette.Sepia.PrimaryAccentHex = pri;
                if (vars.TryGetValue("--rs-on-primary", out var onPri)) palette.Sepia.OnPrimaryHex = onPri;
                if (vars.TryGetValue("--rs-secondary", out var sec)) palette.Sepia.SecondaryAccentHex = sec;
                appliedLight = true;
            }
        }

        return palette;
    }

    // Copies any recognized `--rs-*` CSS custom properties from `vars` onto the target C# color model.
    private static void ApplyCssVars(NarrativeThemeColors target, Dictionary<string, string> vars)
    {
        if (vars.TryGetValue("--rs-bg", out var bg)) target.BackgroundHex = bg;
        if (vars.TryGetValue("--rs-surface", out var surf)) target.SurfaceHex = surf;
        if (vars.TryGetValue("--rs-card-bg", out var card)) target.CardBackgroundHex = card;
        else if (vars.TryGetValue("--rs-bg-alt", out var bgAlt)) target.CardBackgroundHex = bgAlt;
        if (vars.TryGetValue("--rs-text", out var txt)) target.TextHex = txt;
        if (vars.TryGetValue("--rs-text-muted", out var muted)) target.MutedTextHex = muted;
        if (vars.TryGetValue("--rs-heading", out var hdg)) target.HeadingHex = hdg;
        if (vars.TryGetValue("--rs-primary", out var pri)) target.PrimaryAccentHex = pri;
        if (vars.TryGetValue("--rs-on-primary", out var onPri)) target.OnPrimaryHex = onPri;
        if (vars.TryGetValue("--rs-secondary", out var sec)) target.SecondaryAccentHex = sec;
        if (vars.TryGetValue("--rs-border", out var bdr)) target.BorderHex = bdr;
        if (vars.TryGetValue("--rs-asl", out var asl)) target.AslBadgeHex = asl;
        if (vars.TryGetValue("--rs-gesture", out var gest)) target.GestureBadgeHex = gest;
    }
}
