// ================================================================================
// FILE: RaggieSoftWpfDialogs.cs
// PROJECT: SharedWpf (Linked into BookReader, Backstage, ListeningRoom, Launcher)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file builds four clean, accessible popup dialog windows (`System.Windows.Window`)
//     shared across all four RaggieSoft Windows desktop applications (`BookReader.exe`,
//     `Backstage.exe`, `ListeningRoom.exe`, and `RaggieSoftLauncher.exe`):
//       1. `EnsureFirstRunFictionNoticeAcknowledged`:
//          Shows a friendly one-time "Welcome to RaggieSoft Media — Work of Fiction
//          Notice" dialog the very first time a user opens any RaggieSoft desktop app,
//          then remembers their acknowledgement in `%APPDATA%\RaggieSoft\legal-acknowledgement.json`
//          so reading and listening windows stay clutter-free!
//       2. `ShowAboutAndLegalDialog`:
//          Opens the "About & Legal Notice" popup whenever the user clicks *About* in
//          the top toolbar, with one-click buttons to visit `raggiesoft.com`,
//          `books.raggiesoft.com`, `music.raggiesoft.com`, and `backstage.raggiesoft.com`.
//       3. `ShowTrackLyricsDialog`:
//          Lets a Book Reader user inspect full song lyrics for a chapter's soundtrack
//          inside a resizable popup without losing their scroll position in the novel!
//       4. `ShowBackstageQuickReadDialog`:
//          Renders a complete behind-the-scenes Backstage dossier (including images,
//          correspondence headers, and tables) right inside Book Reader via WPF's
//          `FlowDocumentScrollViewer`, with an optional button to hand off into the
//          full `Backstage.exe` app.
//
//   Mental Bridge from HTML/CSS to Windows Presentation Foundation (WPF):
//     If you know HTML and CSS, WPF UI layout controls map straight across:
//       - `Window` + `.ShowDialog()`  <-->  HTML `<dialog>` opened with `.showModal()`
//       - `StackPanel`                <-->  CSS `display: flex; flex-direction: column;`
//       - `WrapPanel`                 <-->  CSS `display: flex; flex-wrap: wrap;`
//       - `Grid` (`Auto` / `1*`)      <-->  CSS `display: grid; grid-template-rows: auto 1fr auto;`
//       - `DockPanel`                 <-->  Edge-docked header/footer bar (`Dock.Left` / `Dock.Right`)
//       - `TextBlock`                 <-->  HTML `<p>` / `<h2>` static text element
//       - `TextBox { IsReadOnly }`    <-->  HTML `<textarea readonly>` (selectable & copyable text)
//       - `AutomationProperties.Name` <-->  HTML `aria-label` for Windows Narrator, JAWS, and NVDA!
// ================================================================================

using System.Diagnostics;        // `Process.Start` for opening external `https://` links in the user's browser.
using System.Windows;            // Core WPF types: `Window`, `Thickness`, `FontWeights`, `SizeToContent`, `GridLength`.
using System.Windows.Automation; // `AutomationProperties` exposing accessibility labels to Windows `UIAutomation`.
using System.Windows.Controls;   // WPF UI widgets: `StackPanel`, `Grid`, `WrapPanel`, `DockPanel`, `TextBlock`, `TextBox`, `Button`, `FlowDocumentScrollViewer`.
using System.Windows.Media;      // `FontFamily` typography configuration.
using RaggieSoft.Core.Models;    // Shared legal copy (`RaggieSoftLegalCopy`), `BackstageRecordDetail`, and theme models.
using RaggieSoft.Core.Services;  // `RaggieSoftWorkspaceLocator` for checking/saving first-run notice state.

namespace RaggieSoft.SharedWpf;

/// <summary>
/// Shared consumer-friendly modal dialogs across all RaggieSoft Windows desktop applications.
/// </summary>
public static class RaggieSoftWpfDialogs
{
    /// <summary>
    /// Displays the one-time Work of Fiction Notice dialog on first launch across
    /// the RaggieSoft desktop suite, then persists acknowledgement in user settings.
    /// </summary>
    /// <param name="owner">The main application window that owns (and centers) this modal dialog.</param>
    /// <param name="locator">Workspace locator used to check and persist <c>legal-acknowledgement.json</c>.</param>
    public static void EnsureFirstRunFictionNoticeAcknowledged(
        Window owner,
        RaggieSoftWorkspaceLocator locator)
    {
        // Fast exit on every launch after the user's very first run!
        if (locator.HasAcknowledgedFictionNotice())
        {
            return;
        }

        // Create a fixed-width (`Width = 600`) WPF modal dialog window whose height automatically
        // expands to fit all wrapped text paragraphs (`SizeToContent.Height`), centered directly
        // over `owner` (`WindowStartupLocation.CenterOwner`).
        var dlg = new Window
        {
            Title = "Welcome to RaggieSoft — Work of Fiction Notice",
            Owner = owner,
            Width = 600,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        // WINDOWS ACCESSIBILITY (`UIAutomation`):
        //   Just like `aria-label="Work of Fiction Notice"` on an HTML `<dialog>`, calling
        //   `AutomationProperties.SetName(...)` sets the accessible name announced by Windows
        //   Narrator, JAWS, and NVDA the instant focus enters this dialog window.
        AutomationProperties.SetName(dlg, "Work of Fiction Notice");

        // `Thickness(left, top, right, bottom)` works like CSS `margin` / `padding` (note: WPF uses
        // Left, Top, Right, Bottom order rather than CSS's clockwise Top, Right, Bottom, Left!).
        var root = new StackPanel { Margin = new Thickness(24, 22, 24, 20) };

        // 1. Warm welcome heading
        root.Children.Add(new TextBlock
        {
            Text = "Welcome to RaggieSoft Media",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        // 2. Work of Fiction Notice subtitle
        root.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.FictitiousDisclaimerTitle,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Opacity = 0.85,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // 3. Full Fictitious Persons & Entities Disclaimer body (`TextWrapping.Wrap` reflows lines cleanly)
        root.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.FictitiousDisclaimerBody,
            FontSize = 13.5,
            LineHeight = 21,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // 4. CC BY-SA 4.0 & MIT open-culture licensing summary
        root.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.CreativeLicenseSummary,
            FontSize = 12.5,
            Opacity = 0.85,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18)
        });

        // 5. Primary confirmation button:
        //    Setting `IsDefault = true` means keyboard users can simply press `Enter` to confirm!
        var continueBtn = new Button
        {
            Content = "I Understand — Continue",
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(18, 8, 18, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true
        };
        // `+=` subscribes an event-handler lambda (`(sender, eventArgs) => ...`, where `_` discards
        // unused parameters) to the button's `Click` event—just like `addEventListener('click', ...)` in JS!
        continueBtn.Click += (_, _) =>
        {
            locator.MarkFictionNoticeAcknowledged();
            dlg.DialogResult = true;
            dlg.Close();
        };
        root.Children.Add(continueBtn);

        // Attach the `StackPanel` as the window's visual root and open it modally (`ShowDialog()` pauses
        // here without freezing the Windows message pump until the user closes or confirms the dialog).
        dlg.Content = root;
        _ = dlg.ShowDialog();
        locator.MarkFictionNoticeAcknowledged();
    }

    /// <summary>
    /// Displays the clean About &amp; Legal Notice dialog from any RaggieSoft app's toolbar or menu.
    /// </summary>
    public static void ShowAboutAndLegalDialog(
        Window owner,
        string appTitle,
        string imprintLabel,
        string version = "1.0.0")
    {
        var dlg = new Window
        {
            Title = $"About {appTitle}",
            Owner = owner,
            Width = 620,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        // Expose a clear accessible dialog title to Windows screen readers via UIAutomation.
        AutomationProperties.SetName(dlg, $"About {appTitle} and Legal Notice");

        var root = new StackPanel { Margin = new Thickness(24, 22, 24, 20) };

        // Imprint eyebrow (e.g., `"OCEAN VIEW ARCHIVES"` or `"ENGINE ROOM RECORDS"`)
        root.Children.Add(new TextBlock
        {
            Text = imprintLabel.ToUpperInvariant(),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Opacity = 0.78,
            Margin = new Thickness(0, 0, 0, 2)
        });

        // Application name and semantic version number
        root.Children.Add(new TextBlock
        {
            Text = $"{appTitle} (v{version})",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 14)
        });

        // Work of Fiction heading + body + Creative Commons / MIT license summary
        root.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.FictitiousDisclaimerTitle,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        root.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.FictitiousDisclaimerBody,
            FontSize = 13,
            LineHeight = 20,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        root.Children.Add(new TextBlock
        {
            Text = RaggieSoftLegalCopy.CreativeLicenseSummary,
            FontSize = 12.5,
            Opacity = 0.88,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Horizontal wrapping row of companion website buttons (`WrapPanel` wraps cleanly on narrow screens)
        var webRow = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        AddWebButton(webRow, "🌐 RaggieSoft.com", "https://raggiesoft.com");
        AddWebButton(webRow, "📖 Ocean View Archives", "https://books.raggiesoft.com");
        AddWebButton(webRow, "🎵 The Listening Room", "https://music.raggiesoft.com");
        AddWebButton(webRow, "🎬 RaggieSoft Backstage", "https://backstage.raggiesoft.com");
        AddWebButton(webRow, "🕹 StoreTrainer", "https://storetrainer.raggiesoft.com");
        root.Children.Add(webRow);

        // Bottom bar: 1-Click "Check for Updates..." button (left) + "Close" button (right)
        var footer = new DockPanel { LastChildFill = false };

        var updateBtn = new Button
        {
            Content = "🔄 Check for Updates...",
            Padding = new Thickness(14, 7, 14, 7)
        };
        DockPanel.SetDock(updateBtn, Dock.Left);
        updateBtn.Click += async (_, _) =>
        {
            var appId = InferAppIdFromTitle(appTitle);
            if (RaggieSoftUpdaterLauncher.TryLaunchInteractiveWizard(appId))
            {
                dlg.Close();
                return;
            }

            // Graceful fallback on developer builds if RaggieSoftUpdate.exe hasn't been compiled yet
            updateBtn.IsEnabled = false;
            updateBtn.Content = "⏳ Checking...";
            try
            {
                var mgr = new RaggieSoftSuiteManager(new RaggieSoftWorkspaceLocator());
                var apps = await mgr.LoadSuiteAppsAsync(checkRemoteManifest: true);
                var updates = apps.Where(a => a.IsUpdateAvailable).ToList();
                var summary = updates.Count == 0
                    ? $"You are running the latest available version of {appTitle} (v{version})."
                    : $"Updates are available for: {string.Join(", ", updates.Select(u => $"{u.Title} (v{u.LatestVersion})"))}!\n\nOpen RaggieSoft Launcher to install updates.";
                MessageBox.Show(dlg, summary, "RaggieSoft Software Update", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                OpenExternalWebUrl("https://raggiesoft.com");
            }
            finally
            {
                updateBtn.Content = "🔄 Check for Updates...";
                updateBtn.IsEnabled = true;
            }
        };
        footer.Children.Add(updateBtn);

        // Close button (`IsDefault = true` handles `Enter`; `IsCancel = true` handles `Esc` automatically!)
        var closeBtn = new Button
        {
            Content = "Close",
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(18, 7, 18, 7),
            IsDefault = true,
            IsCancel = true
        };
        DockPanel.SetDock(closeBtn, Dock.Right);
        closeBtn.Click += (_, _) => dlg.Close();
        footer.Children.Add(closeBtn);
        root.Children.Add(footer);

        dlg.Content = root;
        dlg.ShowDialog();
    }

    // Maps any RaggieSoft desktop app's display title to its manifest ID for RaggieSoftUpdate.exe.
    private static string InferAppIdFromTitle(string appTitle)
    {
        if (appTitle.Contains("Book", StringComparison.OrdinalIgnoreCase)) return "bookreader";
        if (appTitle.Contains("Listening", StringComparison.OrdinalIgnoreCase)) return "listeningroom";
        if (appTitle.Contains("Backstage", StringComparison.OrdinalIgnoreCase)) return "backstage";
        if (appTitle.Contains("StoreTrainer", StringComparison.OrdinalIgnoreCase)) return "storetrainer";
        return "launcher";
    }

    /// <summary>
    /// Displays song lyrics directly inside RaggieSoft Book Reader without losing reading scroll position.
    /// </summary>
    public static void ShowTrackLyricsDialog(
        Window owner,
        string trackTitle,
        string subtitle,
        string lyricsText)
    {
        // Unlike the fixed-size About dialog, the Lyrics dialog is user-resizable (`Width`/`Height`
        // with `MinWidth`/`MinHeight` guardrails) so readers can stretch tall multi-verse ballads.
        var dlg = new Window
        {
            Title = $"Lyrics — {trackTitle}",
            Owner = owner,
            Width = 580,
            Height = 620,
            MinWidth = 420,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        // 3-Row WPF `Grid` layout (identical to CSS `grid-template-rows: auto 1fr auto`):
        //   - Row 0 (`GridLength.Auto`): Song title & artist/album header (shrink-wraps content)
        //   - Row 1 (`1*` / `GridUnitType.Star`): Scrollable lyrics box (fills 100% of remaining height!)
        //   - Row 2 (`GridLength.Auto`): Bottom "Done" button
        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new TextBlock
        {
            Text = trackTitle,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            header.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 13,
                Opacity = 0.82,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        // `Grid.SetRow(element, rowIndex)` is a WPF **attached property** telling the parent `Grid`
        // which row slot (`0`, `1`, or `2`) this child belongs in.
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // Read-only `TextBox` with automatic vertical scrollbar so keyboard/screen-reader users
        // can arrow through lines and select/copy lyrics freely.
        var lyricsBox = new TextBox
        {
            Text = lyricsText,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Georgia, Segoe UI"),
            FontSize = 15,
            Padding = new Thickness(14)
        };
        Grid.SetRow(lyricsBox, 1);
        grid.Children.Add(lyricsBox);

        var closeBtn = new Button
        {
            Content = "Done",
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(16, 6, 16, 6),
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true,
            IsCancel = true
        };
        closeBtn.Click += (_, _) => dlg.Close();
        Grid.SetRow(closeBtn, 2);
        grid.Children.Add(closeBtn);

        dlg.Content = grid;
        dlg.ShowDialog();
    }

    /// <summary>
    /// Displays an inline Backstage Bonus Feature Dossier modal directly inside RaggieSoft Book Reader,
    /// styled with the reader's active theme palette, font family, font size, and line spacing.
    /// </summary>
    public static void ShowBackstageQuickReadDialog(
        Window owner,
        BackstageRecordDetail detail,
        RaggieSoftWorkspaceLocator locator,
        NarrativeThemeColors colors,
        string fontKey,
        double fontSize,
        ReaderLineSpacingPreset lineSpacing,
        Func<string, string, Task>? onLinkClicked,
        Func<Task>? onOpenInBackstageApp)
    {
        var dlg = new Window
        {
            Title = $"RaggieSoft Backstage — {detail.Title}",
            Owner = owner,
            Width = 860,
            Height = 720,
            MinWidth = 560,
            MinHeight = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        // 2-Row `Grid`: Row 0 (`1*`) holds the rich `FlowDocumentScrollViewer`; Row 1 (`Auto`) holds action buttons.
        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // `FlowDocumentScrollViewer` is WPF's continuous-scroll rich document surface—and notice how it
        // reuses the exact same `RaggieSoftFlowDocumentBuilder.BuildBackstageFlowDocument(...)` engine
        // as `Backstage.exe`!
        var viewer = new FlowDocumentScrollViewer
        {
            IsToolBarVisible = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Document = RaggieSoftFlowDocumentBuilder.BuildBackstageFlowDocument(
                detail,
                locator,
                colors,
                fontKey,
                fontSize,
                lineSpacing,
                onImageClicked: null,
                onLinkClicked: onLinkClicked)
        };
        Grid.SetRow(viewer, 0);
        grid.Children.Add(viewer);

        // Footer bar using `DockPanel`: docks `"Back to Chapter"` on the far right (`Dock.Right`)
        // and places `"🎬 Open in RaggieSoft Backstage"` on the left.
        var footer = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        var closeBtn = new Button
        {
            Content = "Back to Chapter",
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(16, 7, 16, 7),
            IsDefault = true,
            IsCancel = true
        };
        DockPanel.SetDock(closeBtn, Dock.Right);
        closeBtn.Click += (_, _) => dlg.Close();
        footer.Children.Add(closeBtn);

        if (onOpenInBackstageApp is not null)
        {
            var openFullBtn = new Button
            {
                Content = "🎬 Open in RaggieSoft Backstage",
                Padding = new Thickness(14, 7, 14, 7),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            // `async (_, _) => ...` lets a WPF button click handler `await` our asynchronous IPC handoff
            // (`SendOrLaunchSiblingAsync`) and then close the quick-read popup cleanly!
            openFullBtn.Click += async (_, _) =>
            {
                await onOpenInBackstageApp();
                dlg.Close();
            };
            footer.Children.Add(openFullBtn);
        }

        Grid.SetRow(footer, 1);
        grid.Children.Add(footer);

        dlg.Content = grid;
        dlg.ShowDialog();
    }

    /// <summary>
    /// Opens an external <c>https://</c> web URL in the user's default Windows web browser.
    /// </summary>
    public static void OpenExternalWebUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            // On modern .NET, setting `UseShellExecute = true` tells Windows Explorer / Shell to open
            // `https://...` URLs in the user's default browser (Edge, Chrome, Firefox, etc.) rather
            // than trying to execute the URL string as an `.exe` program!
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore shell launch error
        }
    }

    // Small helper that creates a styled hyperlink button inside the About dialog's `WrapPanel`.
    private static void AddWebButton(Panel parent, string label, string url)
    {
        var btn = new Button
        {
            Content = label,
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 8, 6)
        };
        btn.Click += (_, _) => OpenExternalWebUrl(url);
        parent.Children.Add(btn);
    }
}
