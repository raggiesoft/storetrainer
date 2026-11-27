// ================================================================================
// FILE: RaggieSoftFlowDocumentBuilder.cs
// PROJECT: SharedWpf (Linked into BookReader, Backstage, ListeningRoom, Launcher)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file is the **Native WPF Rich Typography, Theme, Layout, and Accessibility
//     Rendering Engine** shared across all four RaggieSoft Windows desktop apps
//     (`BookReader.exe`, `Backstage.exe`, `ListeningRoom.exe`, and `RaggieSoftLauncher.exe`).
//
//   Why does this file exist?
//     Once `BookDataService` (for `.md` chapters) or `BackstageDataService` (for `.json`
//     dossiers) parses raw content into C# `ParsedChapterPart` or `BackstageRecordDetail`
//     AST objects, something needs to paint those chapters, illustrations, carousels,
//     transit LED signs, correspondence cards, Story Crossroads choices, and tables
//     onto the user's Windows screen—without using a web browser!
//
//   Mental Bridge: HTML DOM vs. Windows Presentation Foundation (`FlowDocument`)
//     If you know HTML and CSS, WPF's `FlowDocument` is Microsoft's native desktop
//     equivalent of a styled `<article>` element, rendered directly on the GPU via
//     DirectX with ClearType sub-pixel font smoothing:
//
//       HTML / CSS Concept                  WPF `FlowDocument` Equivalent
//       ----------------------------------  -----------------------------------------------
//       `<article style="max-width:880px">` `FlowDocument { MaxPageWidth = 880, ... }`
//       `<p>`, `<h1>`, `<h2>`, `<blockquote>` `Paragraph` (added to `doc.Blocks`)
//       `<ul>` / `<ol>` + `<li>`            `List` (`Disc` / `Decimal`) + `ListItem`
//       `<table>`, `<tr>`, `<th>`, `<td>`   `Table`, `TableRowGroup`, `TableRow`, `TableCell`
//       `<span>` text node                  `Run` (added to `paragraph.Inlines`)
//       `<a href="...">`                    `Hyperlink` (added to `paragraph.Inlines`)
//       `<br>`                              `LineBreak` (added to `paragraph.Inlines`)
//       Block `<div>` widget card           `BlockUIContainer` (embeds any WPF `Border`,
//                                           `Grid`, `StackPanel`, `Image`, or `Button`!)
//       Inline `<span class="pill">`        `InlineUIContainer` (embeds a rounded badge or
//                                           `<led>` transit sign right inside a sentence!)
//       `alt="..."` / `aria-label="..."`    `AutomationProperties.SetName(element, "...")`
//                                           (exposes labels to Windows Narrator, JAWS, NVDA!)
//       `@media (prefers-color-scheme)`     `IsSystemDarkThemeActive()` + `ResolveActiveColors()`
//
//   What visible features does this file power?
//     - Full chapter rendering in `BookReader.exe` and dossier rendering in `Backstage.exe`
//       (plus Book Reader's inline Backstage Quick-Read modal).
//     - Automatic System / Light / Sepia / Dark color adaptation honoring per-chapter
//       custom narrative stylesheets (`/raggiesoft-books/css/themes/<slug>.css`).
//     - 7 reader-selectable typefaces (including bundled **OpenDyslexic** and **Open Sans**)
//       and Compact / Comfortable / Relaxed line-height presets.
//     - Single-plate 16:9 Hero illustrations, interactive Prev/Next multi-slide carousels,
//       "Scenes in This Part" smooth-scroll jump cards, custom Ocean View Archives tags
//       (`<aac>`, `<pa>`, `<led>`, `<sms>`, `<asl>`, `<sgn>`, `<lang>`, `[credential]`),
//       Zoë AI kernel log cards, corporate email cards, Story Crossroads route pickers,
//       and the scrollable bottom Work of Fiction & Creative Commons footer.
// ================================================================================

using System.IO;                 // `File.Exists` check when loading cached local bitmap illustrations.
using System.Windows;            // Core WPF primitives: `Application`, `Thickness`, `CornerRadius`, `FontWeights`, `FontStyles`, `Visibility`.
using System.Windows.Automation; // `AutomationProperties` for Windows `UIAutomation` screen-reader accessibility (Narrator, JAWS, NVDA).
using System.Windows.Controls;   // Interactive WPF widgets: `StackPanel`, `WrapPanel`, `Grid`, `Border`, `Button`, `Image`, `TextBlock`.
using System.Windows.Documents;  // Rich document tree types: `FlowDocument`, `Paragraph`, `Run`, `Hyperlink`, `LineBreak`, `List`, `ListItem`, `Table`, `BlockUIContainer`, `InlineUIContainer`.
using System.Windows.Input;      // `Cursors.Hand` pointer cursor for clickable buttons and zoomable illustrations.
using System.Windows.Media;      // `Brush`, `SolidColorBrush`, `Color`, `ColorConverter`, `FontFamily`, and `Stretch`.
using System.Windows.Media.Imaging; // `BitmapImage` & `BitmapCacheOption` for decoding PNG/JPG/WebP scene plates.
using Microsoft.Win32;           // `Registry.CurrentUser` for reading Windows' Light/Dark app theme preference.
using RaggieSoft.Core.Models;    // Shared AST models (`ParsedChapterPart`, `BackstageRecordDetail`, `RichTextSpan`, `NarrativeThemeColors`, etc.).
using RaggieSoft.Core.Services;  // `RaggieSoftWorkspaceLocator` for resolving and caching illustration files.

namespace RaggieSoft.SharedWpf;

/// <summary>
/// Builds native, theme-aware, screen-reader-accessible WPF <see cref="FlowDocument"/> trees
/// for Ocean View Archives chapters and RaggieSoft Backstage dossiers.
/// </summary>
public static class RaggieSoftFlowDocumentBuilder
{
    /// <summary>
    /// Detects whether the user's Windows desktop (or current WPF <see cref="Application"/>)
    /// is currently running in Dark Mode—the native Windows equivalent of CSS
    /// <c>@media (prefers-color-scheme: dark)</c>.
    /// </summary>
    public static bool IsSystemDarkThemeActive()
    {
        // `#pragma warning disable WPF0001` silences the compiler's informational note on .NET 9/10's
        // modern `Application.ThemeMode` API so our build output stays 100% warning-free.
#pragma warning disable WPF0001
        if (Application.Current?.ThemeMode == ThemeMode.Dark)
        {
            return true;
        }
        if (Application.Current?.ThemeMode == ThemeMode.Light)
        {
            return false;
        }
#pragma warning restore WPF0001

        // When `ThemeMode` is `System` (or `None`), query Windows 10/11's personalization Registry key:
        // `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
        // stores `0` when Dark Mode is active and `1` when Light Mode is active.
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int useLight)
                {
                    return useLight == 0;
                }
            }
        }
        catch
        {
            // Ignore registry lookup errors and default safely to Light Mode (`false`).
        }

        return false;
    }

    /// <summary>
    /// Selects the active <see cref="NarrativeThemeColors"/> token set (<c>Light</c>, <c>Sepia</c>,
    /// or <c>Dark</c>) from <paramref name="palette"/> based on the reader's chosen
    /// <see cref="ReaderAppearanceMode"/>.
    /// </summary>
    public static NarrativeThemeColors ResolveActiveColors(
        NarrativeThemePalette? palette,
        ReaderAppearanceMode appearanceMode)
    {
        var activePalette = palette ?? new NarrativeThemePalette();
        return appearanceMode switch
        {
            ReaderAppearanceMode.Light => activePalette.Light,
            ReaderAppearanceMode.Sepia => activePalette.Sepia,
            ReaderAppearanceMode.Dark => activePalette.Dark,
            // `ReaderAppearanceMode.System` (`_`) automatically tracks Windows Dark/Light Mode!
            _ => IsSystemDarkThemeActive() ? activePalette.Dark : activePalette.Light
        };
    }

    /// <summary>
    /// Converts a CSS hex color string (such as <c>"#1D4ED8"</c>) into a frozen, thread-safe
    /// WPF <see cref="SolidColorBrush"/>, falling back to <paramref name="fallback"/> if invalid.
    /// </summary>
    public static SolidColorBrush BrushFromHex(string hex, Color fallback)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex) &&
                ColorConverter.ConvertFromString(hex.Trim()) is Color parsed)
            {
                var brush = new SolidColorBrush(parsed);
                // WPF PERFORMANCE TIP FOR BEGINNERS:
                //   By default, every WPF `Brush` allocates hidden event listeners in case your code
                //   animates its color mid-frame. Calling `brush.Freeze()` locks the brush as read-only,
                //   eliminating that tracking overhead and making long chapters scroll buttery-smooth!
                brush.Freeze();
                return brush;
            }
        }
        catch
        {
            // Fall through to fallback color
        }

        var fallbackBrush = new SolidColorBrush(fallback);
        fallbackBrush.Freeze();
        return fallbackBrush;
    }

    /// <summary>
    /// Maps a user-selected font key (<c>"Georgia"</c>, <c>"Palatino"</c>, <c>"Sitka"</c>,
    /// <c>"SegoeUI"</c>, <c>"OpenSans"</c>, <c>"OpenDyslexic"</c>, or <c>"Consolas"</c>)
    /// to a WPF <see cref="FontFamily"/> with graceful fallback chains.
    /// </summary>
    public static FontFamily ResolveReadingFontFamily(string fontKey)
    {
        // Notice how `"OpenSans"` and `"OpenDyslexic"` use `pack://application:,,,/` + `./Assets/Fonts/#...`:
        // that loads the `.ttf`/`.otf` font files bundled directly inside our app's `Assets/Fonts/` folder,
        // so dyslexic readers get OpenDyslexic immediately WITHOUT needing to install fonts into Windows!
        return fontKey switch
        {
            "Palatino" => new FontFamily("Palatino Linotype, Book Antiqua, Georgia"),
            "Sitka" => new FontFamily("Sitka Text, Cambria, Georgia"),
            "SegoeUI" => new FontFamily("Segoe UI, Arial"),
            "OpenSans" => new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Open Sans, Segoe UI"),
            "OpenDyslexic" => new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#OpenDyslexic, OpenDyslexic, Comic Sans MS, Segoe UI"),
            "Consolas" => new FontFamily("Consolas, Cascadia Mono, Courier New"),
            _ => new FontFamily("Georgia, Cambria, Segoe UI")
        };
    }

    /// <summary>
    /// Converts a <see cref="ReaderLineSpacingPreset"/> into a CSS-style unitless line-height multiplier.
    /// </summary>
    public static double ResolveLineHeightMultiplier(ReaderLineSpacingPreset preset)
    {
        return preset switch
        {
            ReaderLineSpacingPreset.Compact => 1.48,
            ReaderLineSpacingPreset.Relaxed => 1.92,
            _ => 1.68
        };
    }

    /// <summary>
    /// Builds a complete chapter <see cref="FlowDocument"/> for RaggieSoft Book Reader and populates
    /// <paramref name="sceneAnchorParagraphs"/> so the toolbar "Scenes" menu and Mini-TOC jump smoothly
    /// to any <c>##</c> scene heading.
    /// </summary>
    // PARAMETER GUIDE FOR BEGINNERS:
    //   - `parsed` & `partMeta`: The parsed `pXXX.md` AST + `toc.json` structural metadata.
    //   - `colors`, `fontKey`, `fontSize`, `lineSpacing`: Active visual & typography preferences.
    //   - `Action<string, string>? onImageClicked`: Callback `(imageUri, altText) => ...` invoked when
    //     the reader clicks an illustration to open the full-screen zoomable lightbox.
    //   - `Func<string, string, Task>? onLinkClicked`: Async callback `(label, url) => Task` invoked
    //     when the reader clicks an inline link (routing songs to `ListeningRoom.exe`, chapters within
    //     `BookReader.exe`, or dossiers to `Backstage.exe`).
    //   - `Func<string, bool, Task>? onBackstageSlugRequested`: Async callback `(slug, openExternalApp) => Task`
    //     for end-of-chapter Backstage Bonus Feature buttons.
    //   - `out Dictionary<string, Paragraph> sceneAnchorParagraphs`: Filled with `"scene-1" -> Paragraph`
    //     references so calling `paragraph.BringIntoView()` scrolls straight to that scene!
    //   - `crossroads`, `selectedOptionId`, `onCrossroadsOptionSelected`: Optional **RaggieSoft Story
    //     Crossroads** branching decision card rendered at the end of a Crossroads trigger chapter.
    public static FlowDocument BuildChapterFlowDocument(
        ParsedChapterPart parsed,
        BookPart partMeta,
        RaggieSoftWorkspaceLocator locator,
        NarrativeThemeColors colors,
        string fontKey,
        double fontSize,
        ReaderLineSpacingPreset lineSpacing,
        Action<string, string>? onImageClicked,
        Func<string, string, Task>? onLinkClicked,
        Func<string, bool, Task>? onBackstageSlugRequested,
        out Dictionary<string, Paragraph> sceneAnchorParagraphs,
        BookCrossroads? crossroads = null,
        string? selectedOptionId = null,
        Func<BookCrossroadsOption, Task>? onCrossroadsOptionSelected = null)
    {
        // Initialize the `out` dictionary. We also store a local alias `activeAnchors` because C#
        // does not allow anonymous button-click lambdas (`(_, _) => ...`) to capture `out` parameters directly!
        sceneAnchorParagraphs = new Dictionary<string, Paragraph>(StringComparer.OrdinalIgnoreCase);
        var activeAnchors = sceneAnchorParagraphs;

        // Convert every hex token in `colors` into a frozen WPF `SolidColorBrush`.
        var bgBrush = BrushFromHex(colors.BackgroundHex, Color.FromRgb(250, 248, 245));
        var cardBrush = BrushFromHex(colors.CardBackgroundHex, Color.FromRgb(243, 239, 232));
        var surfaceBrush = BrushFromHex(colors.SurfaceHex, Colors.White);
        var textBrush = BrushFromHex(colors.TextHex, Color.FromRgb(28, 25, 23));
        var mutedBrush = BrushFromHex(colors.MutedTextHex, Color.FromRgb(87, 83, 78));
        var headingBrush = BrushFromHex(colors.HeadingHex, Color.FromRgb(15, 23, 42));
        var accentBrush = BrushFromHex(colors.PrimaryAccentHex, Color.FromRgb(29, 78, 216));
        var secondaryBrush = BrushFromHex(colors.SecondaryAccentHex, Color.FromRgb(15, 118, 110));
        var borderBrush = BrushFromHex(colors.BorderHex, Color.FromRgb(214, 211, 209));
        var aslBrush = BrushFromHex(colors.AslBadgeHex, Color.FromRgb(3, 105, 161));
        var gestureBrush = BrushFromHex(colors.GestureBadgeHex, Color.FromRgb(67, 56, 202));

        // Prepare clean UI sans-serif (`uiFont`), code/LED monospace (`monoFont`), and reader-chosen prose (`bodyFont`) families.
        var uiFont = new FontFamily("Segoe UI");
        var monoFont = new FontFamily("Consolas, Cascadia Mono, Courier New");
        var bodyFont = ResolveReadingFontFamily(fontKey);
        var lineHeight = fontSize * ResolveLineHeightMultiplier(lineSpacing);

        // Create the root `FlowDocument` container. `MaxPageWidth = 880` keeps prose lines around
        // 65–75 characters wide even on ultrawide 4K monitors so the eye never loses its place!
        var doc = new FlowDocument
        {
            FontFamily = bodyFont,
            FontSize = fontSize,
            LineHeight = lineHeight,
            Foreground = textBrush,
            Background = bgBrush,
            PagePadding = new Thickness(38, 26, 38, 38),
            MaxPageWidth = 880
        };

        // 1. Normalized Structural Eyebrow (`Episode 1: ...  •  Chapter 2: ...`)
        var eyebrowPieces = new List<string>();
        if (!string.IsNullOrWhiteSpace(partMeta.EraTitle)) eyebrowPieces.Add(partMeta.EraTitle);
        if (!string.IsNullOrWhiteSpace(partMeta.ChapterTitle)) eyebrowPieces.Add(partMeta.ChapterTitle);
        if (eyebrowPieces.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Run(string.Join("  •  ", eyebrowPieces)))
            {
                FontFamily = uiFont,
                FontSize = Math.Max(11.5, fontSize * 0.78),
                FontWeight = FontWeights.SemiBold,
                Foreground = accentBrush,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        // 2. Normalized Part Title & Optional Italic Subtitle
        var rawTitle = !string.IsNullOrWhiteSpace(parsed.Title) ? parsed.Title : partMeta.CleanTitle;
        var cleanHeadingTitle = RaggieSoftTitleNormalizer.NormalizeStructuralTitle(
            rawTitle,
            "Part",
            partMeta.PartNum,
            includeNumberPrefix: false);

        doc.Blocks.Add(new Paragraph(new Run(cleanHeadingTitle))
        {
            FontFamily = bodyFont,
            FontSize = fontSize * 1.68,
            LineHeight = fontSize * 1.68 * 1.24,
            FontWeight = FontWeights.Bold,
            Foreground = headingBrush,
            Margin = new Thickness(0, 0, 0, string.IsNullOrWhiteSpace(parsed.Subtitle) ? 8 : 4)
        });

        if (!string.IsNullOrWhiteSpace(parsed.Subtitle))
        {
            doc.Blocks.Add(new Paragraph(new Run(parsed.Subtitle))
            {
                FontFamily = bodyFont,
                FontSize = fontSize * 1.12,
                FontStyle = FontStyles.Italic,
                Foreground = mutedBrush,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        // 3. Dateline Metadata Bar (`📅 Date`, `🪐 Stardate`, `🕒 Start–End Time (Timezone)`, `📍 Location`, `👁 POV`)
        var metaItems = new List<string>();
        if (!string.IsNullOrWhiteSpace(parsed.Date)) metaItems.Add($"📅 {parsed.Date}");
        if (!string.IsNullOrWhiteSpace(parsed.Stardate)) metaItems.Add($"🪐 Stardate {parsed.Stardate}");
        if (!string.IsNullOrWhiteSpace(parsed.StartTime))
        {
            var timeSpanText = !string.IsNullOrWhiteSpace(parsed.EndTime)
                ? $"{parsed.StartTime} – {parsed.EndTime}"
                : parsed.StartTime;
            if (!string.IsNullOrWhiteSpace(parsed.Timezone))
            {
                timeSpanText = $"{timeSpanText} ({parsed.Timezone})";
            }
            metaItems.Add($"🕒 {timeSpanText}");
        }
        if (!string.IsNullOrWhiteSpace(parsed.Location)) metaItems.Add($"📍 {parsed.Location}");
        if (!string.IsNullOrWhiteSpace(parsed.Pov)) metaItems.Add($"👁 {parsed.Pov}");

        if (metaItems.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Run(string.Join("   •   ", metaItems)))
            {
                FontFamily = uiFont,
                FontSize = Math.Max(12.0, fontSize * 0.78),
                Foreground = mutedBrush,
                Margin = new Thickness(0, 0, 0, 18)
            });
        }

        // 4. Hero Illustration Plate (single 16:9 image) OR Interactive Multi-Slide Hero Carousel
        if (parsed.HeroSlides.Count == 1)
        {
            doc.Blocks.Add(CreateImageFigureContainer(
                parsed.HeroSlides[0].Src,
                parsed.HeroSlides[0].Alt,
                isInline: false,
                locator,
                cardBrush,
                borderBrush,
                mutedBrush,
                uiFont,
                onImageClicked));
        }
        else if (parsed.HeroSlides.Count > 1)
        {
            doc.Blocks.Add(CreateInteractiveCarouselContainer(
                parsed.HeroSlides,
                locator,
                cardBrush,
                surfaceBrush,
                borderBrush,
                textBrush,
                mutedBrush,
                accentBrush,
                uiFont,
                onImageClicked));
        }
        else if (!string.IsNullOrWhiteSpace(partMeta.Image))
        {
            doc.Blocks.Add(CreateImageFigureContainer(
                locator.ResolveAssetUri(partMeta.Image),
                partMeta.ImageAlt,
                isInline: false,
                locator,
                cardBrush,
                borderBrush,
                mutedBrush,
                uiFont,
                onImageClicked));
        }

        // 5. "Scenes in This Part" Mini-TOC Jump Card (rendered when 2 or more `##` scenes exist):
        //    Wraps interactive WPF `Button`s inside a `BlockUIContainer` so clicking a scene pill
        //    calls `targetPara.BringIntoView()`—smoothly scrolling the document right to that heading!
        if (parsed.SceneAnchors.Count > 1)
        {
            var miniTocPanel = new StackPanel();
            miniTocPanel.Children.Add(new TextBlock
            {
                Text = $"SCENES IN THIS PART ({parsed.SceneAnchors.Count})",
                FontFamily = uiFont,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var sceneWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var anchor in parsed.SceneAnchors)
            {
                // Copy `anchor.AnchorId` into a per-iteration local variable `capturedId` so each button's
                // click closure remembers its own scene ID.
                var capturedId = anchor.AnchorId;
                var jumpBtn = new Button
                {
                    Content = $"{anchor.SceneIndex}. {anchor.Title}",
                    FontFamily = uiFont,
                    FontSize = 12.5,
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 2, 6, 4),
                    Cursor = Cursors.Hand
                };
                // Expose clear action description to Windows Narrator / JAWS / NVDA via UIAutomation.
                AutomationProperties.SetName(jumpBtn, $"Jump to scene {anchor.Title}");
                jumpBtn.Click += (_, _) =>
                {
                    if (activeAnchors.TryGetValue(capturedId, out var targetPara))
                    {
                        targetPara.BringIntoView();
                    }
                };
                sceneWrap.Children.Add(jumpBtn);
            }
            miniTocPanel.Children.Add(sceneWrap);

            doc.Blocks.Add(new BlockUIContainer(new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 20),
                Child = miniTocPanel
            }));
        }

        // 6. Semantic Body Blocks: convert each `BookContentBlock` in `parsed.Blocks` into its
        //    corresponding native WPF `Block` (`Paragraph`, `List`, `Table`, or `BlockUIContainer`).
        foreach (var block in parsed.Blocks)
        {
            switch (block.BlockType)
            {
                // Centered three-dot ornamental scene divider (`***` / `---` in Markdown)
                case BookBlockType.SceneDivider:
                    doc.Blocks.Add(new Paragraph(new Run("•   •   •"))
                    {
                        TextAlignment = TextAlignment.Center,
                        Foreground = mutedBrush,
                        Margin = new Thickness(0, 20, 0, 20)
                    });
                    break;

                // Top-level `#` heading inside body prose
                case BookBlockType.Heading1:
                    doc.Blocks.Add(new Paragraph(new Run(block.Text))
                    {
                        FontFamily = bodyFont,
                        FontSize = fontSize * 1.42,
                        FontWeight = FontWeights.Bold,
                        Foreground = headingBrush,
                        Margin = new Thickness(0, 24, 0, 10)
                    });
                    break;

                // Level-4 `##` Scene Heading (with subtle bottom border and registered jump anchor!)
                case BookBlockType.SceneHeading:
                {
                    var scenePara = new Paragraph(new Run(block.Text))
                    {
                        FontFamily = bodyFont,
                        FontSize = fontSize * 1.26,
                        LineHeight = fontSize * 1.26 * 1.3,
                        FontWeight = FontWeights.Bold,
                        Foreground = headingBrush,
                        BorderBrush = borderBrush,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 0, 0, 6),
                        Margin = new Thickness(0, 26, 0, 12)
                    };
                    if (!string.IsNullOrWhiteSpace(block.AnchorId))
                    {
                        sceneAnchorParagraphs[block.AnchorId] = scenePara;
                    }
                    doc.Blocks.Add(scenePara);
                    break;
                }

                // `###` / `####` Subheading
                case BookBlockType.Subheading:
                    doc.Blocks.Add(new Paragraph(new Run(block.Text))
                    {
                        FontFamily = uiFont,
                        FontSize = fontSize * 1.08,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = headingBrush,
                        Margin = new Thickness(0, 18, 0, 8)
                    });
                    break;

                // Standalone `#fullwidth` or `#inline` scene illustration card
                case BookBlockType.Illustration:
                    doc.Blocks.Add(CreateImageFigureContainer(
                        block.ImageUri,
                        block.ImageAlt,
                        block.IsInlineIllustration,
                        locator,
                        cardBrush,
                        borderBrush,
                        mutedBrush,
                        uiFont,
                        onImageClicked));
                    break;

                // Interactive multi-slide ` ````carousel ` gallery
                case BookBlockType.ImageCarousel:
                    doc.Blocks.Add(CreateInteractiveCarouselContainer(
                        block.CarouselSlides,
                        locator,
                        cardBrush,
                        surfaceBrush,
                        borderBrush,
                        textBrush,
                        mutedBrush,
                        accentBrush,
                        uiFont,
                        onImageClicked));
                    break;

                // Native multi-column comparison `Table`
                case BookBlockType.Table:
                    if (block.Table is not null)
                    {
                        doc.Blocks.Add(BuildNativeTable(
                            block.Table.Headers,
                            block.Table.Rows,
                            fontSize,
                            cardBrush,
                            surfaceBrush,
                            borderBrush,
                            headingBrush,
                            textBrush));
                    }
                    break;

                // Monospace terminal or `ZOË.Systems.Logs` cognitive telemetry card
                case BookBlockType.CodeOrKernelLog:
                    doc.Blocks.Add(CreateConsoleOrLogCard(
                        block.Text,
                        block.IsZoeKernelLog,
                        fontSize,
                        monoFont,
                        uiFont));
                    break;

                // In-universe corporate email / memo card (`From:` / `To:` / `Subject:`)
                case BookBlockType.CorrespondenceCard:
                    doc.Blocks.Add(CreateCorrespondenceBlock(
                        block.EmailHeaders,
                        block.Spans,
                        block.Text,
                        fontSize,
                        bodyFont,
                        uiFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        headingBrush,
                        textBrush,
                        mutedBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        monoFont,
                        onLinkClicked));
                    break;

                // GitHub-style `> [!NOTE]` / `> [!IMPORTANT]` callout alert card
                case BookBlockType.CalloutAlert:
                    doc.Blocks.Add(CreateCalloutAlertBlock(
                        block.AlertKind,
                        block.Spans,
                        block.Text,
                        fontSize,
                        bodyFont,
                        uiFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        secondaryBrush,
                        textBrush,
                        aslBrush,
                        gestureBrush,
                        monoFont,
                        onLinkClicked));
                    break;

                // Literary blockquote (`> ...`) with `3px` left accent bar and italic styling
                case BookBlockType.Blockquote:
                {
                    var quotePara = new Paragraph
                    {
                        FontStyle = FontStyles.Italic,
                        Foreground = textBrush,
                        Background = cardBrush,
                        Margin = new Thickness(20, 8, 16, 16),
                        Padding = new Thickness(16, 10, 14, 10),
                        BorderBrush = accentBrush,
                        BorderThickness = new Thickness(3, 0, 0, 0)
                    };
                    AppendRichSpans(
                        quotePara.Inlines,
                        block.Spans,
                        block.Text,
                        fontSize,
                        uiFont,
                        monoFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        onLinkClicked);
                    doc.Blocks.Add(quotePara);
                    break;
                }

                // Bullet (`TextMarkerStyle.Disc`, like `<ul>`) or Numbered (`TextMarkerStyle.Decimal`, like `<ol>`) `List`:
                // In WPF FlowDocuments, each `List` contains `ListItem` nodes (`<li>`), and each `ListItem`
                // wraps a `Paragraph` holding the item's inline `Run`s, `Hyperlink`s, and custom OVA pills.
                case BookBlockType.BulletList:
                case BookBlockType.NumberedList:
                {
                    var list = new List
                    {
                        MarkerStyle = block.BlockType == BookBlockType.NumberedList
                            ? TextMarkerStyle.Decimal
                            : TextMarkerStyle.Disc,
                        Margin = new Thickness(18, 4, 0, 16),
                        Padding = new Thickness(16, 0, 0, 0)
                    };
                    foreach (var itemSpans in block.ListItems)
                    {
                        // Build one `Paragraph` per bullet/numbered item and populate its inline spans.
                        var liPara = new Paragraph { Margin = new Thickness(0, 0, 0, 6) };
                        AppendRichSpans(
                            liPara.Inlines,
                            itemSpans,
                            string.Empty,
                            fontSize,
                            uiFont,
                            monoFont,
                            cardBrush,
                            borderBrush,
                            accentBrush,
                            secondaryBrush,
                            aslBrush,
                            gestureBrush,
                            onLinkClicked);
                        list.ListItems.Add(new ListItem(liPara));
                    }
                    doc.Blocks.Add(list);
                    break;
                }

                // Standard literary paragraph (`<p>`) populated with rich inline `Run`s and custom OVA pills.
                // `Margin = new Thickness(0, 0, 0, 15)` adds 15px of breathing room below each paragraph
                // (just like `p { margin: 0 0 15px 0; }` in CSS!).
                case BookBlockType.Paragraph:
                {
                    var para = new Paragraph
                    {
                        Margin = new Thickness(0, 0, 0, 15)
                    };
                    AppendRichSpans(
                        para.Inlines,
                        block.Spans,
                        block.Text,
                        fontSize,
                        uiFont,
                        monoFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        onLinkClicked);
                    doc.Blocks.Add(para);
                    break;
                }
            }
        }

        // 7. End-of-Chapter Backstage Bonus Feature Callout Cards (rendered when YAML declares `backstage:`):
        //    Offers BOTH an instant in-window Quick-Read modal (`false`) AND a one-click IPC handoff
        //    to open the full `Backstage.exe` desktop app (`true`).
        if (parsed.BackstageSlugs.Count > 0 && onBackstageSlugRequested is not null)
        {
            var bonusStack = new StackPanel();
            bonusStack.Children.Add(new TextBlock
            {
                Text = "🎬 RAGGIESOFT BACKSTAGE — BEHIND-THE-SCENES BONUS FEATURE",
                FontFamily = uiFont,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                Margin = new Thickness(0, 0, 0, 4)
            });
            bonusStack.Children.Add(new TextBlock
            {
                Text = "This chapter includes companion behind-the-scenes correspondence or dossier files.",
                FontFamily = uiFont,
                FontSize = 13.5,
                Foreground = textBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            foreach (var slug in parsed.BackstageSlugs)
            {
                // Copy `slug` into a fresh per-iteration variable `capturedSlug` so each button's async
                // lambda (`async (_, _) => ...`) stays bound to its own dossier slug!
                var capturedSlug = slug;
                var prettyName = HumanizeBackstageSlug(slug);
                var row = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };

                // Button 1 (`openExternalApp = false`): opens the dossier inline right inside Book Reader!
                var quickReadBtn = new Button
                {
                    Content = $"📄 Read Dossier: {prettyName}",
                    FontFamily = uiFont,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 2, 8, 2),
                    Cursor = Cursors.Hand
                };
                quickReadBtn.Click += async (_, _) => await onBackstageSlugRequested(capturedSlug, false);

                // Button 2 (`openExternalApp = true`): sends an `"OPEN_BACKSTAGE"` Named Pipe IPC message
                // to launch/focus the standalone `Backstage.exe` companion app!
                var openAppBtn = new Button
                {
                    Content = "Open in RaggieSoft Backstage ↗",
                    FontFamily = uiFont,
                    FontSize = 12.5,
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 2, 0, 2),
                    Cursor = Cursors.Hand
                };
                openAppBtn.Click += async (_, _) => await onBackstageSlugRequested(capturedSlug, true);

                row.Children.Add(quickReadBtn);
                row.Children.Add(openAppBtn);
                bonusStack.Children.Add(row);
            }

            // Embed the rounded Backstage callout card into the FlowDocument via `BlockUIContainer`.
            doc.Blocks.Add(new BlockUIContainer(new Border
            {
                Background = cardBrush,
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 22, 0, 10),
                Child = bonusStack
            }));
        }

        // 7b. Coequal Story Crossroads Decision Cards (when this chapter part reaches a Story Crossroads):
        //     Renders selectable route cards right at the conclusion of the trigger chapter, highlighting
        //     any route the reader previously chose (`isSaved`) while letting them switch routes anytime!
        if (crossroads is not null && crossroads.Options.Count > 0)
        {
            var cxStack = new StackPanel();
            // Uppercase Story Crossroads kicker badge, main Crossroads prompt heading, and optional description.
            cxStack.Children.Add(new TextBlock
            {
                Text = $"🪧 {crossroads.Kicker.ToUpperInvariant()}",
                FontFamily = uiFont,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                Margin = new Thickness(0, 0, 0, 4)
            });
            cxStack.Children.Add(new TextBlock
            {
                Text = crossroads.Title,
                FontFamily = bodyFont,
                FontSize = Math.Max(17, fontSize * 1.18),
                FontWeight = FontWeights.Bold,
                Foreground = headingBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });
            if (!string.IsNullOrWhiteSpace(crossroads.Prompt))
            {
                cxStack.Children.Add(new TextBlock
                {
                    Text = crossroads.Prompt,
                    FontFamily = uiFont,
                    FontSize = 13.5,
                    Foreground = textBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                });
            }

            // Render one coequal decision card per branching route option (`crossroads.Options`):
            foreach (var opt in crossroads.Options)
            {
                var capturedOpt = opt;
                // Check if the reader previously chose this route in their saved Story Crossroads map.
                var isSaved = !string.IsNullOrWhiteSpace(selectedOptionId) &&
                              string.Equals(selectedOptionId, opt.Id, StringComparison.OrdinalIgnoreCase);

                var optInner = new StackPanel();
                if (isSaved)
                {
                    // Visual indicator badge showing which route is currently active in the TOC sidebar.
                    optInner.Children.Add(new TextBlock
                    {
                        Text = "✓ CURRENTLY SELECTED PATH",
                        FontFamily = uiFont,
                        FontSize = 10.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = accentBrush,
                        Margin = new Thickness(0, 0, 0, 3)
                    });
                }
                optInner.Children.Add(new TextBlock
                {
                    Text = opt.Label,
                    FontFamily = uiFont,
                    FontSize = 14.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = headingBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                if (!string.IsNullOrWhiteSpace(opt.Subtitle))
                {
                    optInner.Children.Add(new TextBlock
                    {
                        Text = opt.Subtitle,
                        FontFamily = uiFont,
                        FontSize = 13,
                        Foreground = mutedBrush,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    });
                }

                // Action button that saves the chosen route option and navigates straight into `opt.EntryPart`.
                var chooseBtn = new Button
                {
                    Content = isSaved ? "Continue This Story Path →" : "Follow This Story Path →",
                    FontFamily = uiFont,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(14, 7, 14, 7),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Cursor = Cursors.Hand
                };
                if (onCrossroadsOptionSelected is not null)
                {
                    chooseBtn.Click += async (_, _) => await onCrossroadsOptionSelected(capturedOpt);
                }
                optInner.Children.Add(chooseBtn);

                // Highlight the currently selected route card with a 2px accent border (`isSaved ? 2 : 1`).
                cxStack.Children.Add(new Border
                {
                    Background = surfaceBrush,
                    BorderBrush = isSaved ? accentBrush : borderBrush,
                    BorderThickness = new Thickness(isSaved ? 2 : 1),
                    CornerRadius = new CornerRadius(9),
                    Padding = new Thickness(14, 12, 14, 12),
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = optInner
                });
            }

            doc.Blocks.Add(new BlockUIContainer(new Border
            {
                Background = cardBrush,
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 16, 18, 12),
                Margin = new Thickness(0, 26, 0, 14),
                Child = cxStack
            }));
        }

        // 8. Scrollable Bottom Work of Fiction & Creative Licensing Footer
        AppendScrollableLegalFooter(doc, uiFont, cardBrush, borderBrush, mutedBrush);

        return doc;
    }

    /// <summary>
    /// Builds a complete <see cref="FlowDocument"/> for RaggieSoft Backstage (and for Book Reader's
    /// in-app Backstage Quick-Read modal) from a parsed <see cref="BackstageRecordDetail"/>.
    /// </summary>
    public static FlowDocument BuildBackstageFlowDocument(
        BackstageRecordDetail detail,
        RaggieSoftWorkspaceLocator locator,
        NarrativeThemeColors colors,
        string fontKey,
        double fontSize,
        ReaderLineSpacingPreset lineSpacing,
        Action<string, string>? onImageClicked,
        Func<string, string, Task>? onLinkClicked)
    {
        // Resolve theme brushes identically to `BuildChapterFlowDocument` so dossiers seamlessly
        // adopt Light, Sepia, or Dark appearance mode.
        var bgBrush = BrushFromHex(colors.BackgroundHex, Color.FromRgb(250, 248, 245));
        var cardBrush = BrushFromHex(colors.CardBackgroundHex, Color.FromRgb(243, 239, 232));
        var surfaceBrush = BrushFromHex(colors.SurfaceHex, Colors.White);
        var textBrush = BrushFromHex(colors.TextHex, Color.FromRgb(28, 25, 23));
        var mutedBrush = BrushFromHex(colors.MutedTextHex, Color.FromRgb(87, 83, 78));
        var headingBrush = BrushFromHex(colors.HeadingHex, Color.FromRgb(15, 23, 42));
        var accentBrush = BrushFromHex(colors.PrimaryAccentHex, Color.FromRgb(29, 78, 216));
        var secondaryBrush = BrushFromHex(colors.SecondaryAccentHex, Color.FromRgb(15, 118, 110));
        var borderBrush = BrushFromHex(colors.BorderHex, Color.FromRgb(214, 211, 209));
        var aslBrush = BrushFromHex(colors.AslBadgeHex, Color.FromRgb(3, 105, 161));
        var gestureBrush = BrushFromHex(colors.GestureBadgeHex, Color.FromRgb(67, 56, 202));

        var uiFont = new FontFamily("Segoe UI");
        var monoFont = new FontFamily("Consolas, Cascadia Mono, Courier New");
        var bodyFont = ResolveReadingFontFamily(fontKey);

        var doc = new FlowDocument
        {
            FontFamily = bodyFont,
            FontSize = fontSize,
            LineHeight = fontSize * ResolveLineHeightMultiplier(lineSpacing),
            Foreground = textBrush,
            Background = bgBrush,
            PagePadding = new Thickness(36, 24, 36, 36),
            MaxPageWidth = 900
        };

        // 1. Category & Story Date / Location Eyebrow
        var eyebrowParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(detail.Category)) eyebrowParts.Add(detail.Category.ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(detail.Timestamp)) eyebrowParts.Add(detail.Timestamp);
        else if (!string.IsNullOrWhiteSpace(detail.Date))
        {
            eyebrowParts.Add(string.IsNullOrWhiteSpace(detail.Time) ? detail.Date : $"{detail.Date} • {detail.Time}");
        }
        if (!string.IsNullOrWhiteSpace(detail.Location)) eyebrowParts.Add($"📍 {detail.Location}");

        if (eyebrowParts.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Run(string.Join("  •  ", eyebrowParts)))
            {
                FontFamily = uiFont,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = accentBrush,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        // 2. Dossier Title & Optional Subtitle
        doc.Blocks.Add(new Paragraph(new Run(detail.Title))
        {
            FontFamily = bodyFont,
            FontSize = fontSize * 1.62,
            LineHeight = fontSize * 1.62 * 1.24,
            FontWeight = FontWeights.Bold,
            Foreground = headingBrush,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (!string.IsNullOrWhiteSpace(detail.Subtitle))
        {
            doc.Blocks.Add(new Paragraph(new Run(detail.Subtitle))
            {
                FontSize = fontSize * 1.08,
                FontStyle = FontStyles.Italic,
                Foreground = mutedBrush,
                Margin = new Thickness(0, 0, 0, 14)
            });
        }

        // 3. In-Universe Correspondence Header Card (when `<slug>.json` top-level metadata provides `sender` / `recipient`)
        if (!string.IsNullOrWhiteSpace(detail.Sender) || !string.IsNullOrWhiteSpace(detail.Recipient))
        {
            var topHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(detail.Sender)) topHeaders["From"] = detail.Sender;
            if (!string.IsNullOrWhiteSpace(detail.Recipient)) topHeaders["To"] = detail.Recipient;
            if (!string.IsNullOrWhiteSpace(detail.Subject)) topHeaders["Subject"] = detail.Subject;
            if (!string.IsNullOrWhiteSpace(detail.Timestamp)) topHeaders["Sent"] = detail.Timestamp;
            else if (!string.IsNullOrWhiteSpace(detail.Date)) topHeaders["Date"] = detail.Date;

            doc.Blocks.Add(CreateCorrespondenceBlock(
                topHeaders,
                [],
                string.Empty,
                fontSize,
                bodyFont,
                uiFont,
                cardBrush,
                borderBrush,
                accentBrush,
                headingBrush,
                textBrush,
                mutedBrush,
                secondaryBrush,
                aslBrush,
                gestureBrush,
                monoFont,
                onLinkClicked));
        }

        // 4. 16:9 Hero Illustration Plate
        if (!string.IsNullOrWhiteSpace(detail.ResolvedHeroImageUri))
        {
            doc.Blocks.Add(CreateImageFigureContainer(
                detail.ResolvedHeroImageUri,
                detail.ImageAlt,
                isInline: false,
                locator,
                cardBrush,
                borderBrush,
                mutedBrush,
                uiFont,
                onImageClicked));
        }

        // 5. Structured Body Blocks parsed from `detail.Html`:
        //    Walk every `BackstageContentBlock` in reading order and translate it into native WPF blocks.
        foreach (var block in detail.Blocks)
        {
            switch (block.BlockType)
            {
                // `<hr>` horizontal divider -> centered ornamental dots
                case BackstageBlockType.Divider:
                    doc.Blocks.Add(new Paragraph(new Run("•   •   •"))
                    {
                        TextAlignment = TextAlignment.Center,
                        Foreground = mutedBrush,
                        Margin = new Thickness(0, 18, 0, 18)
                    });
                    break;

                // `<h2>` section heading with `1px` bottom underline (`BorderThickness(0, 0, 0, 1)` = CSS `border-bottom: 1px solid`)
                case BackstageBlockType.Heading2:
                    doc.Blocks.Add(new Paragraph(new Run(block.Text))
                    {
                        FontSize = fontSize * 1.26,
                        FontWeight = FontWeights.Bold,
                        Foreground = headingBrush,
                        BorderBrush = borderBrush,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(0, 0, 0, 5),
                        Margin = new Thickness(0, 24, 0, 10)
                    });
                    break;

                // `<h3>` / `<h4>` clean sans-serif subsection heading
                case BackstageBlockType.Heading3:
                    doc.Blocks.Add(new Paragraph(new Run(block.Text))
                    {
                        FontFamily = uiFont,
                        FontSize = fontSize * 1.08,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = headingBrush,
                        Margin = new Thickness(0, 18, 0, 8)
                    });
                    break;

                // `<pre>` monospace system/terminal log readout card
                case BackstageBlockType.ConsoleBlock:
                    doc.Blocks.Add(CreateConsoleOrLogCard(block.Text, isZoeLog: false, fontSize, monoFont, uiFont));
                    break;

                // Inline `<figure>` / `<img>` scene illustration with zoomable click support
                case BackstageBlockType.Illustration:
                    doc.Blocks.Add(CreateImageFigureContainer(
                        block.ImageUri,
                        block.ImageAlt,
                        isInline: false,
                        locator,
                        cardBrush,
                        borderBrush,
                        mutedBrush,
                        uiFont,
                        onImageClicked));
                    break;

                // Multi-column HTML `<table>` rendered as a native WPF `Table`
                case BackstageBlockType.Table:
                    if (block.Table is not null)
                    {
                        doc.Blocks.Add(BuildNativeTable(
                            block.Table.Headers,
                            block.Table.Rows,
                            fontSize,
                            cardBrush,
                            surfaceBrush,
                            borderBrush,
                            headingBrush,
                            textBrush));
                    }
                    break;

                // Embedded email/memo blockquote (`From:` / `To:` / `Subject:` + body)
                case BackstageBlockType.CorrespondenceCard:
                    doc.Blocks.Add(CreateCorrespondenceBlock(
                        block.EmailHeaders,
                        block.Spans,
                        block.Text,
                        fontSize,
                        bodyFont,
                        uiFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        headingBrush,
                        textBrush,
                        mutedBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        monoFont,
                        onLinkClicked));
                    break;

                // Standard italic `<blockquote>` with `3px` left accent bar (`BorderThickness(3, 0, 0, 0)`)
                case BackstageBlockType.Blockquote:
                {
                    var quotePara = new Paragraph
                    {
                        FontStyle = FontStyles.Italic,
                        Background = cardBrush,
                        Margin = new Thickness(20, 8, 16, 14),
                        Padding = new Thickness(16, 10, 14, 10),
                        BorderBrush = accentBrush,
                        BorderThickness = new Thickness(3, 0, 0, 0)
                    };
                    AppendRichSpans(
                        quotePara.Inlines,
                        block.Spans,
                        block.Text,
                        fontSize,
                        uiFont,
                        monoFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        onLinkClicked);
                    doc.Blocks.Add(quotePara);
                    break;
                }

                // `<li>` bulleted dossier item prefixed with a bold accent-colored bullet (`•  `)
                case BackstageBlockType.BulletItem:
                {
                    var bulletPara = new Paragraph(new Run("•  ") { FontWeight = FontWeights.Bold, Foreground = accentBrush })
                    {
                        Margin = new Thickness(16, 0, 0, 10)
                    };
                    AppendRichSpans(
                        bulletPara.Inlines,
                        block.Spans,
                        block.Text,
                        fontSize,
                        uiFont,
                        monoFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        onLinkClicked);
                    doc.Blocks.Add(bulletPara);
                    break;
                }

                // `<p>` dossier body paragraph with full rich-span support (`<a>`, `<strong>`, `<aac>`, `<led>`, etc.)
                case BackstageBlockType.Paragraph:
                {
                    var para = new Paragraph { Margin = new Thickness(0, 0, 0, 14) };
                    AppendRichSpans(
                        para.Inlines,
                        block.Spans,
                        block.Text,
                        fontSize,
                        uiFont,
                        monoFont,
                        cardBrush,
                        borderBrush,
                        accentBrush,
                        secondaryBrush,
                        aslBrush,
                        gestureBrush,
                        onLinkClicked);
                    doc.Blocks.Add(para);
                    break;
                }
            }
        }

        // 6. Scrollable Bottom Work of Fiction & Creative Licensing Footer
        AppendScrollableLegalFooter(doc, uiFont, cardBrush, borderBrush, mutedBrush);

        return doc;
    }

    /// <summary>
    /// Appends the mandatory Work of Fiction &amp; Creative Commons / MIT licensing card
    /// at the natural scrollable bottom of any <see cref="FlowDocument"/> (just like a web
    /// page footer, keeping the reading viewport open and spacious).
    /// </summary>
    public static void AppendScrollableLegalFooter(
        FlowDocument doc,
        FontFamily uiFont,
        Brush cardBrush,
        Brush borderBrush,
        Brush mutedBrush)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.FictitiousDisclaimerTitle.ToUpperInvariant(),
            FontFamily = uiFont,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = mutedBrush,
            Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.FictitiousDisclaimerBody,
            FontFamily = uiFont,
            FontSize = 11.5,
            Foreground = mutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6)
        });
        stack.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.CreativeLicenseSummary,
            FontFamily = uiFont,
            FontSize = 11,
            Foreground = mutedBrush,
            Opacity = 0.9,
            TextWrapping = TextWrapping.Wrap
        });

        doc.Blocks.Add(new BlockUIContainer(new Border
        {
            Background = cardBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 28, 0, 8),
            Child = stack
        }));
    }

    // Translates our `List<RichTextSpan>` AST runs into native WPF `Inline` elements (`Run`, `Hyperlink`,
    // `LineBreak`, and `InlineUIContainer` pills/LED signs) appended into `target` (`Paragraph.Inlines`
    // or `TextBlock.Inlines`).
    private static void AppendRichSpans(
        InlineCollection target,
        List<RichTextSpan> spans,
        string fallbackPlainText,
        double fontSize,
        FontFamily uiFont,
        FontFamily monoFont,
        Brush cardBrush,
        Brush borderBrush,
        Brush accentBrush,
        Brush secondaryBrush,
        Brush aslBrush,
        Brush gestureBrush,
        Func<string, string, Task>? onLinkClicked)
    {
        // Fallback when a block provided plain `Text` without pre-tokenized `Spans`.
        if (spans.Count == 0)
        {
            if (!string.IsNullOrEmpty(fallbackPlainText))
            {
                AppendTextWithLineBreaks(target, fallbackPlainText, FontWeights.Normal, FontStyles.Normal);
            }
            return;
        }

        foreach (var span in spans)
        {
            switch (span.Kind)
            {
                case RichTextSpanKind.Bold:
                    AppendTextWithLineBreaks(target, span.Text, FontWeights.Bold, FontStyles.Normal);
                    break;

                // Both standard `*italic*` and `<lang>...</lang>` foreign phrases render in literary italics.
                case RichTextSpanKind.Italic:
                case RichTextSpanKind.LanguageSpan:
                    AppendTextWithLineBreaks(target, span.Text, FontWeights.Normal, FontStyles.Italic);
                    break;

                case RichTextSpanKind.BoldItalic:
                    AppendTextWithLineBreaks(target, span.Text, FontWeights.Bold, FontStyles.Italic);
                    break;

                // Inline monospace badge (`` `code` ``) with subtle card-tinted background
                case RichTextSpanKind.Code:
                    target.Add(new Run($" {span.Text} ")
                    {
                        FontFamily = monoFont,
                        FontSize = Math.Max(12.0, fontSize * 0.88),
                        Background = cardBrush
                    });
                    break;

                // Clickable WPF `Hyperlink` element (`<a href="...">`)
                case RichTextSpanKind.Link:
                {
                    var hyperlink = new Hyperlink(new Run(span.Text))
                    {
                        Foreground = accentBrush,
                        FontWeight = FontWeights.SemiBold,
                        NavigateUri = Uri.TryCreate(span.LinkUrl, UriKind.Absolute, out var u) ? u : null,
                        ToolTip = span.LinkUrl
                    };
                    if (onLinkClicked is not null)
                    {
                        var capturedLabel = span.Text;
                        var capturedUrl = span.LinkUrl;
                        hyperlink.Click += async (_, _) => await onLinkClicked(capturedLabel, capturedUrl);
                    }
                    target.Add(hyperlink);
                    break;
                }

                // Custom Ocean View Archives `<aac>` AAC speech-tablet output pill
                case RichTextSpanKind.AacSpeech:
                    target.Add(CreateInlinePill(
                        $"💬 “{span.Text}”",
                        uiFont,
                        Math.Max(12.5, fontSize * 0.92),
                        cardBrush,
                        accentBrush,
                        accentBrush));
                    break;

                // Custom Ocean View Archives `<sms>` text-message bubble pill
                case RichTextSpanKind.SmsBubble:
                    target.Add(CreateInlinePill(
                        $"📱 “{span.Text}”",
                        uiFont,
                        Math.Max(12.5, fontSize * 0.92),
                        cardBrush,
                        secondaryBrush,
                        secondaryBrush));
                    break;

                // Custom Ocean View Archives `<pa>` public-address speaker announcement pill
                case RichTextSpanKind.PaAnnouncement:
                    target.Add(CreateInlinePill(
                        $"📢 {span.Text}",
                        uiFont,
                        Math.Max(12.0, fontSize * 0.90),
                        cardBrush,
                        borderBrush,
                        secondaryBrush));
                    break;

                // Custom Ocean View Archives `<led>` amber dot-matrix transit destination sign
                case RichTextSpanKind.LedSign:
                    target.Add(CreateInlineLedSign(
                        span,
                        monoFont,
                        fontSize));
                    break;

                // Custom Ocean View Archives `<asl>` American Sign Language gloss badge
                case RichTextSpanKind.AslSign:
                    target.Add(CreateInlinePill(
                        $"🤟 ASL: {span.Text}",
                        uiFont,
                        Math.Max(12.0, fontSize * 0.88),
                        cardBrush,
                        aslBrush,
                        aslBrush));
                    break;

                // Custom Ocean View Archives `<sgn>` tactile/gestural communication badge
                case RichTextSpanKind.GestureTag:
                    target.Add(CreateInlinePill(
                        $"✦ {span.Text}",
                        uiFont,
                        Math.Max(12.0, fontSize * 0.88),
                        cardBrush,
                        gestureBrush,
                        gestureBrush));
                    break;

                // `[credential ...]` badge pill
                case RichTextSpanKind.CredentialBadge:
                    target.Add(CreateInlinePill(
                        $"🪪 {span.Text}",
                        uiFont,
                        Math.Max(11.5, fontSize * 0.84),
                        cardBrush,
                        borderBrush,
                        accentBrush));
                    break;

                default:
                    AppendTextWithLineBreaks(target, span.Text, FontWeights.Normal, FontStyles.Normal);
                    break;
            }
        }
    }

    // Wraps a rounded `Border` + `TextBlock` inside an `InlineUIContainer` (`BaselineAlignment.Center`)
    // so custom OVA pills (`<aac>`, `<sms>`, `<pa>`, `<asl>`, `<sgn>`) sit inline inside prose sentences!
    private static InlineUIContainer CreateInlinePill(
        string text,
        FontFamily font,
        double fontSize,
        Brush bg,
        Brush border,
        Brush fg)
    {
        return new InlineUIContainer(new Border
        {
            Background = bg,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(2, 0, 2, 0),
            Child = new TextBlock
            {
                Text = text,
                FontFamily = font,
                FontSize = fontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = fg,
                TextWrapping = TextWrapping.Wrap
            }
        })
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    /// <summary>
    /// Renders an in-universe Transit Amber LED Destination Matrix Sign (<c>&lt;led&gt;</c>)
    /// inside WPF <see cref="FlowDocument"/> prose, supporting single-line headers, two-line stacked
    /// matrices, and full-height left route numbers beside stacked destination lines.
    /// </summary>
    private static InlineUIContainer CreateInlineLedSign(
        RichTextSpan span,
        FontFamily monoFont,
        double fontSize)
    {
        // Authentic transit bus sign palette: deep matte-black bezel housing (`#0A0C10`), warm amber
        // frame border, translucent vertical matrix separator, and high-contrast amber LEDs (`#FFB300`
        // on `#0A0C10` exceeds 11:1 WCAG AAA contrast in BOTH Light and Dark reader modes!).
        var housingBrush = new SolidColorBrush(Color.FromRgb(10, 12, 16));
        var frameBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6));
        var dividerBrush = new SolidColorBrush(Color.FromArgb(90, 255, 179, 0));
        var amberPrimary = new SolidColorBrush(Color.FromRgb(255, 179, 0));
        var amberSecondary = new SolidColorBrush(Color.FromRgb(255, 194, 51));

        var hasRoute = !string.IsNullOrWhiteSpace(span.LedRoute);
        var hasLine1 = !string.IsNullOrWhiteSpace(span.LedLine1);
        var hasLine2 = !string.IsNullOrWhiteSpace(span.LedLine2);

        UIElement contentElement;

        if (!hasRoute && !hasLine2)
        {
            // Layout A: Simple single-line transit header (`<led>112 NNTC</led>`)
            contentElement = new TextBlock
            {
                Text = hasLine1 ? span.LedLine1 : span.Text,
                FontFamily = monoFont,
                FontSize = Math.Max(12.0, fontSize * 0.88),
                FontWeight = FontWeights.ExtraBold,
                Foreground = amberPrimary,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else
        {
            // Layout B: Multi-zone transit matrix (`<led>913 | VA BEACH | SHORE & PLEAS HSE</led>`)
            // Uses a horizontal WPF `Grid` with:
            //   - Column 0: Oversized full-height bold route number (`913`)
            //   - Column 1: 1-pixel vertical amber separator line
            //   - Column 2: Vertical `StackPanel` stacking `LedLine1` above `LedLine2`!
            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var colIndex = 0;
            if (hasRoute)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var routeBlock = new TextBlock
                {
                    Text = span.LedRoute,
                    FontFamily = monoFont,
                    FontSize = Math.Max(18.0, fontSize * 1.48),
                    FontWeight = FontWeights.Black,
                    Foreground = amberPrimary,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 7, 0)
                };
                Grid.SetColumn(routeBlock, 0);
                grid.Children.Add(routeBlock);

                if (hasLine1 || hasLine2)
                {
                    var divider = new Border
                    {
                        Width = 1,
                        Background = dividerBrush,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Margin = new Thickness(0, 1, 7, 1)
                    };
                    Grid.SetColumn(divider, 1);
                    grid.Children.Add(divider);
                }

                colIndex = 2;
            }

            if (hasLine1 || hasLine2)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var linesStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (hasLine1)
                {
                    linesStack.Children.Add(new TextBlock
                    {
                        Text = span.LedLine1,
                        FontFamily = monoFont,
                        FontSize = hasLine2 ? Math.Max(10.5, fontSize * 0.74) : Math.Max(12.0, fontSize * 0.88),
                        FontWeight = FontWeights.ExtraBold,
                        Foreground = amberPrimary,
                        LineHeight = hasLine2 ? Math.Max(12.0, fontSize * 0.84) : double.NaN,
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                if (hasLine2)
                {
                    linesStack.Children.Add(new TextBlock
                    {
                        Text = span.LedLine2,
                        FontFamily = monoFont,
                        FontSize = Math.Max(10.0, fontSize * 0.70),
                        FontWeight = FontWeights.Bold,
                        Foreground = amberSecondary,
                        LineHeight = Math.Max(11.5, fontSize * 0.80),
                        Margin = new Thickness(0, 1, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    });
                }

                Grid.SetColumn(linesStack, colIndex);
                grid.Children.Add(linesStack);
            }

            contentElement = grid;
        }

        var containerBorder = new Border
        {
            Background = housingBrush,
            BorderBrush = frameBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = (hasRoute || hasLine2) ? new Thickness(8, 3, 8, 3) : new Thickness(7, 2, 7, 2),
            Margin = new Thickness(2, 1, 2, 1),
            Child = contentElement
        };

        // WINDOWS ACCESSIBILITY (`UIAutomation`):
        //   Give the outer LED border a unified screen-reader announcement (`"Transit LED Sign: 913 | VA BEACH / ..."`)
        //   so Windows Narrator, JAWS, and NVDA read the full sign coherently instead of fragmented cells!
        System.Windows.Automation.AutomationProperties.SetName(
            containerBorder,
            $"Transit LED Sign: {span.Text}");

        return new InlineUIContainer(containerBorder)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    // Splits `text` on `\n` and inserts explicit WPF `LineBreak` (`<br>`) elements between `Run`s.
    private static void AppendTextWithLineBreaks(
        InlineCollection target,
        string text,
        FontWeight weight,
        FontStyle style)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 0)
            {
                target.Add(new Run(lines[i])
                {
                    FontWeight = weight,
                    FontStyle = style
                });
            }
            if (i < lines.Length - 1)
            {
                target.Add(new LineBreak());
            }
        }
    }

    // Builds a framed scene illustration card (`#fullwidth` up to 450px tall, or `#inline` centered
    // up to 310px tall x 540px wide) with `Stretch.Uniform` (preserving 100% of the artwork's aspect
    // ratio without cropping), `UIAutomation` alt text, lightbox click zoom, and italic caption.
    private static BlockUIContainer CreateImageFigureContainer(
        string imageUri,
        string altText,
        bool isInline,
        RaggieSoftWorkspaceLocator locator,
        Brush cardBrush,
        Brush borderBrush,
        Brush mutedBrush,
        FontFamily uiFont,
        Action<string, string>? onImageClicked)
    {
        var img = new Image
        {
            MaxHeight = isInline ? 310 : 450,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = Cursors.Hand,
            ToolTip = string.IsNullOrWhiteSpace(altText)
                ? "Click to inspect full-size illustration"
                : $"{altText}\n(Click to inspect full-size illustration)"
        };
        // Equivalent to `<img alt="...">` in HTML for Windows screen readers!
        AutomationProperties.SetName(img, string.IsNullOrWhiteSpace(altText) ? "Scene Illustration" : altText);

        if (onImageClicked is not null)
        {
            img.MouseLeftButtonUp += (_, _) => onImageClicked(imageUri, altText);
        }

        // Kick off asynchronous local-cache / CDN image decoding without blocking document layout!
        _ = LoadBitmapFromCacheAsync(img, imageUri, locator);

        var stack = new StackPanel();
        stack.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Child = img
        });

        if (!string.IsNullOrWhiteSpace(altText))
        {
            stack.Children.Add(new TextBlock
            {
                Text = altText,
                FontFamily = uiFont,
                FontSize = 12.5,
                FontStyle = FontStyles.Italic,
                Foreground = mutedBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 8, 4, 2)
            });
        }

        return new BlockUIContainer(new Border
        {
            Background = cardBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            MaxWidth = isInline ? 540 : double.PositiveInfinity,
            HorizontalAlignment = isInline ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 18),
            Child = stack
        });
    }

    // Builds an interactive multi-slide image carousel (`← Previous Slide` / `Slide X of N` / `Next Slide →`)
    // directly inside the `FlowDocument` using C# closure state (`var currentIndex = 0;`) and a local
    // `SyncSlide()` helper function!
    private static BlockUIContainer CreateInteractiveCarouselContainer(
        List<BookSlideImage> slides,
        RaggieSoftWorkspaceLocator locator,
        Brush cardBrush,
        Brush surfaceBrush,
        Brush borderBrush,
        Brush textBrush,
        Brush mutedBrush,
        Brush accentBrush,
        FontFamily uiFont,
        Action<string, string>? onImageClicked)
    {
        var currentIndex = 0;

        var img = new Image
        {
            MaxHeight = 440,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = Cursors.Hand
        };

        var counterLabel = new TextBlock
        {
            FontFamily = uiFont,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = accentBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0)
        };

        var captionLabel = new TextBlock
        {
            FontFamily = uiFont,
            FontSize = 12.5,
            FontStyle = FontStyles.Italic,
            Foreground = mutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4, 8, 4, 6)
        };

        // Local helper that refreshes the image, slide counter, caption `Visibility` (`Collapsed` hides the
        // element AND reclaims its vertical space, just like CSS `display: none`), and `AutomationProperties.Name`.
        void SyncSlide()
        {
            var slide = slides[currentIndex];
            counterLabel.Text = $"Slide {currentIndex + 1} of {slides.Count}";
            captionLabel.Text = slide.Alt;
            captionLabel.Visibility = string.IsNullOrWhiteSpace(slide.Alt) ? Visibility.Collapsed : Visibility.Visible;
            img.ToolTip = string.IsNullOrWhiteSpace(slide.Alt)
                ? "Click to inspect full-size illustration"
                : $"{slide.Alt}\n(Click to inspect full-size illustration)";
            AutomationProperties.SetName(img, string.IsNullOrWhiteSpace(slide.Alt) ? $"Slide {currentIndex + 1}" : slide.Alt);
            _ = LoadBitmapFromCacheAsync(img, slide.Src, locator);
        }

        if (onImageClicked is not null)
        {
            img.MouseLeftButtonUp += (_, _) =>
            {
                var slide = slides[currentIndex];
                onImageClicked(slide.Src, slide.Alt);
            };
        }

        // Modulo arithmetic `(currentIndex - 1 + slides.Count) % slides.Count` wraps cleanly from Slide 1
        // around to the last slide (and `(currentIndex + 1) % slides.Count` wraps from last back to Slide 1).
        var prevBtn = new Button
        {
            Content = "← Previous Slide",
            FontFamily = uiFont,
            FontSize = 12.5,
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = Cursors.Hand
        };
        prevBtn.Click += (_, _) =>
        {
            currentIndex = (currentIndex - 1 + slides.Count) % slides.Count;
            SyncSlide();
        };

        var nextBtn = new Button
        {
            Content = "Next Slide →",
            FontFamily = uiFont,
            FontSize = 12.5,
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = Cursors.Hand
        };
        nextBtn.Click += (_, _) =>
        {
            currentIndex = (currentIndex + 1) % slides.Count;
            SyncSlide();
        };

        var navBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 2)
        };
        navBar.Children.Add(prevBtn);
        navBar.Children.Add(counterLabel);
        navBar.Children.Add(nextBtn);

        var outerStack = new StackPanel();
        outerStack.Children.Add(new Border
        {
            Background = surfaceBrush,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(4),
            Child = img
        });
        outerStack.Children.Add(captionLabel);
        outerStack.Children.Add(navBar);

        // Load Slide 1 immediately upon building the carousel.
        SyncSlide();

        return new BlockUIContainer(new Border
        {
            Background = cardBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 8, 0, 18),
            Child = outerStack
        });
    }

    // Renders an in-universe corporate email or memo card (`From:`, `To:`, `Cc:`, `Date:`, `Subject:`
    // header block separated by a 1px horizontal divider from the rich-text message body).
    private static BlockUIContainer CreateCorrespondenceBlock(
        Dictionary<string, string> headers,
        List<RichTextSpan> bodySpans,
        string fallbackBodyText,
        double fontSize,
        FontFamily bodyFont,
        FontFamily uiFont,
        Brush cardBrush,
        Brush borderBrush,
        Brush accentBrush,
        Brush headingBrush,
        Brush textBrush,
        Brush mutedBrush,
        Brush secondaryBrush,
        Brush aslBrush,
        Brush gestureBrush,
        FontFamily monoFont,
        Func<string, string, Task>? onLinkClicked)
    {
        var stack = new StackPanel();

        // Top metadata envelope: each `(k, v)` pair (`From: Holly O'Connell`) renders as a single
        // `TextBlock` with two `Run`s—muted bold for the label (`"From: "`) and heading ink for the value!
        var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, string.IsNullOrWhiteSpace(fallbackBodyText) && bodySpans.Count == 0 ? 0 : 10) };
        foreach (var (k, v) in headers)
        {
            var row = new TextBlock
            {
                FontFamily = uiFont,
                FontSize = Math.Max(12.5, fontSize * 0.84),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 2)
            };
            row.Inlines.Add(new Run($"{k}: ") { FontWeight = FontWeights.Bold, Foreground = mutedBrush });
            row.Inlines.Add(new Run(v) { FontWeight = FontWeights.SemiBold, Foreground = headingBrush });
            headerPanel.Children.Add(row);
        }
        stack.Children.Add(headerPanel);

        // If the correspondence block also includes a message body below the headers, draw a crisp
        // 1-pixel horizontal rule (`Border { Height = 1 }`) followed by the rich-span message body.
        if (bodySpans.Count > 0 || !string.IsNullOrWhiteSpace(fallbackBodyText))
        {
            stack.Children.Add(new Border
            {
                Height = 1,
                Background = borderBrush,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var bodyTextBlock = new TextBlock
            {
                FontFamily = bodyFont,
                FontSize = fontSize * 0.96,
                LineHeight = fontSize * 1.55,
                Foreground = textBrush,
                TextWrapping = TextWrapping.Wrap
            };
            AppendRichSpans(
                bodyTextBlock.Inlines,
                bodySpans,
                fallbackBodyText,
                fontSize * 0.96,
                uiFont,
                monoFont,
                cardBrush,
                borderBrush,
                accentBrush,
                secondaryBrush,
                aslBrush,
                gestureBrush,
                onLinkClicked);
            stack.Children.Add(bodyTextBlock);
        }

        // Wrap the complete correspondence envelope + body inside a rounded `Border` card.
        return new BlockUIContainer(new Border
        {
            Background = cardBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 8, 0, 18),
            Child = stack
        });
    }

    // Renders a GitHub-Flavored Markdown alert box (`> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`,
    // `> [!WARNING]`, `> [!CAUTION]`) with an icon eyebrow and `3px` left accent border.
    private static BlockUIContainer CreateCalloutAlertBlock(
        string alertKind,
        List<RichTextSpan> spans,
        string fallbackText,
        double fontSize,
        FontFamily bodyFont,
        FontFamily uiFont,
        Brush cardBrush,
        Brush borderBrush,
        Brush accentBrush,
        Brush secondaryBrush,
        Brush textBrush,
        Brush aslBrush,
        Brush gestureBrush,
        FontFamily monoFont,
        Func<string, string, Task>? onLinkClicked)
    {
        // Tuple deconstruction + switch expression: assigns BOTH `badgeLabel` and `badgeBrush` in one expression!
        var (badgeLabel, badgeBrush) = alertKind.ToUpperInvariant() switch
        {
            "TIP" => ("💡 TIP", secondaryBrush),
            "IMPORTANT" => ("📌 IMPORTANT", accentBrush),
            "WARNING" => ("⚠️ PLEASE NOTE", new SolidColorBrush(Color.FromRgb(180, 83, 9))),
            "CAUTION" => ("🛑 CAUTION", new SolidColorBrush(Color.FromRgb(185, 28, 28))),
            _ => ("ℹ️ NOTE", accentBrush)
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = badgeLabel,
            FontFamily = uiFont,
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = badgeBrush,
            Margin = new Thickness(0, 0, 0, 6)
        });

        // Render the alert body prose using the reader's active body font and inline OVA spans.
        var contentTb = new TextBlock
        {
            FontFamily = bodyFont,
            FontSize = fontSize * 0.95,
            LineHeight = fontSize * 1.55,
            Foreground = textBrush,
            TextWrapping = TextWrapping.Wrap
        };
        AppendRichSpans(
            contentTb.Inlines,
            spans,
            fallbackText,
            fontSize * 0.95,
            uiFont,
            monoFont,
            cardBrush,
            borderBrush,
            accentBrush,
            secondaryBrush,
            aslBrush,
            gestureBrush,
            onLinkClicked);
        stack.Children.Add(contentTb);

        // `BorderThickness = new Thickness(3, 1, 1, 1)` gives a thicker 3px left accent stripe
        // paired with a crisp 1px top/right/bottom border!
        return new BlockUIContainer(new Border
        {
            Background = cardBrush,
            BorderBrush = badgeBrush,
            BorderThickness = new Thickness(3, 1, 1, 1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 12),
            Margin = new Thickness(0, 8, 0, 16),
            Child = stack
        });
    }

    // Renders a dark-slate monospace console readout card (with a glowing cyan `"ZOË.Systems.Logs"`
    // header badge when `isZoeLog` is `true` for Zoë's cognitive logs in Ocean View Archives).
    private static BlockUIContainer CreateConsoleOrLogCard(
        string logText,
        bool isZoeLog,
        double fontSize,
        FontFamily monoFont,
        FontFamily uiFont)
    {
        // High-contrast dark terminal colors (`#0F172A` navy-slate canvas with `#E2E8F0` crisp light text)
        // that stay legible regardless of whether the surrounding page is in Light, Sepia, or Dark mode.
        var consoleBg = new SolidColorBrush(Color.FromRgb(15, 23, 42));
        var consoleBorder = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        var consoleFg = new SolidColorBrush(Color.FromRgb(226, 232, 240));
        var headerFg = new SolidColorBrush(Color.FromRgb(125, 211, 252));

        var stack = new StackPanel();
        if (isZoeLog)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "ZOË.Systems.Logs",
                FontFamily = uiFont,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = headerFg,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        stack.Children.Add(new TextBlock
        {
            Text = logText,
            FontFamily = monoFont,
            FontSize = Math.Max(12.0, fontSize * 0.85),
            LineHeight = Math.Max(18.0, fontSize * 1.35),
            Foreground = consoleFg,
            TextWrapping = TextWrapping.Wrap
        });

        return new BlockUIContainer(new Border
        {
            Background = consoleBg,
            BorderBrush = isZoeLog ? consoleBorder : new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 8, 0, 18),
            Child = stack
        });
    }

    // Builds a native WPF `FlowDocument` `Table` (equivalent to HTML `<table><thead>...</thead><tbody>...</tbody></table>`)
    // with shaded column headers, subtle internal cell borders, and semi-bold first-column row labels.
    private static Table BuildNativeTable(
        List<string> headers,
        List<List<string>> rows,
        double fontSize,
        Brush headerBgBrush,
        Brush rowBgBrush,
        Brush borderBrush,
        Brush headingBrush,
        Brush textBrush)
    {
        // Compute total column count across both `headers` and the widest row in `rows`.
        var colCount = Math.Max(
            headers.Count,
            rows.Count > 0 ? rows.Max(r => r.Count) : 0);

        // `CellSpacing = 0` collapses gaps between cells (just like `border-collapse: collapse;` in CSS!).
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            Background = rowBgBrush,
            Margin = new Thickness(0, 10, 0, 18),
            FontSize = Math.Max(13.0, fontSize - 1.5)
        };

        // Register one proportional `TableColumn` definition per column.
        for (var c = 0; c < colCount; c++)
        {
            table.Columns.Add(new TableColumn());
        }

        // `TableRowGroup` works like `<tbody>` in HTML, holding all `TableRow` (`<tr>`) objects.
        var rowGroup = new TableRowGroup();

        // Optional bold header row (`<th>` equivalent)
        if (headers.Count > 0)
        {
            var headerRow = new TableRow { Background = headerBgBrush };
            for (var c = 0; c < colCount; c++)
            {
                var text = c < headers.Count ? headers[c] : string.Empty;
                // Draw a 1px right divider on every column except the last (`c < colCount - 1 ? 1 : 0`)
                // and a 1px bottom border separating the header row from the data rows.
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(text))
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = headingBrush,
                    Margin = new Thickness(0)
                })
                {
                    Padding = new Thickness(10, 8, 10, 8),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(0, 0, c < colCount - 1 ? 1 : 0, 1)
                });
            }
            rowGroup.Rows.Add(headerRow);
        }

        // Data body rows (`<td>` equivalent): omit the bottom cell border on the final row (`isLast ? 0 : 1`)
        // so it doesn't double-up against the outer `Table.BorderThickness`!
        for (var r = 0; r < rows.Count; r++)
        {
            var vals = rows[r];
            var tr = new TableRow();
            var isLast = r == rows.Count - 1;

            for (var c = 0; c < colCount; c++)
            {
                var text = c < vals.Count ? vals[c] : string.Empty;
                tr.Cells.Add(new TableCell(new Paragraph(new Run(text))
                {
                    FontWeight = c == 0 ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = textBrush,
                    Margin = new Thickness(0)
                })
                {
                    Padding = new Thickness(10, 8, 10, 8),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(0, 0, c < colCount - 1 ? 1 : 0, isLast ? 0 : 1)
                });
            }
            rowGroup.Rows.Add(tr);
        }

        table.RowGroups.Add(rowGroup);
        return table;
    }

    /// <summary>
    /// Ensures <paramref name="imageUri"/> is cached in <c>%LOCALAPPDATA%\RaggieSoft\Cache</c>,
    /// decodes it into a WPF <see cref="BitmapImage"/> with <see cref="BitmapCacheOption.OnLoad"/>
    /// (so Windows never locks the image file on disk), and assigns it to <paramref name="targetImage"/>.
    /// </summary>
    public static async Task LoadBitmapFromCacheAsync(
        Image targetImage,
        string imageUri,
        RaggieSoftWorkspaceLocator locator)
    {
        try
        {
            var localPath = await locator.EnsureAssetCachedLocallyAsync(imageUri);
            if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
            {
                return;
            }

            // `BeginInit()` ... `EndInit()` configures the WPF `BitmapImage` transactionally.
            // Setting `BitmapCacheOption.OnLoad` tells WPF to read all pixels into RAM immediately
            // during `EndInit()` and close the disk file handle right away!
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(localPath, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            targetImage.Source = bmp;
        }
        catch
        {
            // Offline with uncached image -> leave `Source` empty cleanly without crashing.
            targetImage.Source = null;
        }
    }

    // Converts a Backstage slug like `"books/rachel/delores-intake-memo"` into `"Delores Intake Memo"`
    // for end-of-chapter `"📄 Read Dossier: ..."` button labels.
    private static string HumanizeBackstageSlug(string slug)
    {
        var last = slug.Trim('/').Split('/').LastOrDefault() ?? slug;
        var words = last.Split('-', '_')
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..] : string.Empty));
        var res = string.Join(" ", words);
        return string.IsNullOrWhiteSpace(res) ? slug : res;
    }
}
