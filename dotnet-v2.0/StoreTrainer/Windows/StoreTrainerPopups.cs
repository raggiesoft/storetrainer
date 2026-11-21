// ================================================================================
// FILE: StoreTrainerPopups.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Artwork)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is in this file?
//     Instead of scattering five tiny XAML files across the project, this file builds
//     StoreTrainer 2.0's five specialized companion windows and wizards cleanly in C#
//     using WPF's responsive layout panels ('Grid', 'StackPanel', 'DockPanel', 'Border'):
//
//     1. 'ShowStoreFileUpgradeWizardDialog':
//        A 3-step guided wizard (modeled after Visual Basic's classic Upgrade Wizard!)
//        that upgrades classic StoreTrainer 1.0/1.1 '.stf' profiles into modern
//        StoreTrainer 2.0 '.stfx' profiles inside 'Documents\StoreFiles\' while
//        leaving the original 'My StoreFiles\*.stf' files 100% untouched.
//
//     2. 'ShowTrainerZipExtractDialog':
//        Inspects a downloaded '.zip' trainer archive, lets you choose which '.exe'
//        inside the archive is your trainer, and lets you extract either into your
//        'StoreFiles\Trainers\' folder OR directly into the game's folder (requesting
//        Windows administrator permission automatically if the game is in Program Files!).
//
//     3. 'ShowCp437NfoViewerDialog':
//        Renders classic trainer '.nfo', 'file_id.diz', and 'readme.txt' files with
//        authentic IBM PC Code Page 437 ASCII art borders, plus a 1-click button to
//        import detected shortcut keys straight into your profile!
//
//     4. 'ShowHotkeyCheatCardWindow':
//        Opens a compact Always-On-Top floating cheat sheet window you can keep on a
//        second monitor (or beside a borderless game window) while playing!
//
//     5. 'ShowSaveBackupsDialog':
//        Manages rolling pre-launch save-game backup snapshots ("Trainer Insurance")
//        with 1-click snapshot creation and 1-click rollback.
// ================================================================================

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using StoreTrainer.Models;
using StoreTrainer.Services;

namespace StoreTrainer.Windows;

/// <summary>
/// Result returned when the user finishes the 3-step StoreFile Upgrade Wizard.
/// </summary>
public sealed record StoreFileUpgradeWizardOutcome(
    IReadOnlyList<string> CreatedStfxPaths,
    bool LaunchFirstProfileImmediately);

/// <summary>
/// User choices from the Trainer ZIP Archive Extraction dialog.
/// </summary>
public sealed record TrainerZipDialogChoice(
    string SelectedExecutableEntry,
    string DestinationDirectory,
    bool ExtractIntoGameFolder);

public static class StoreTrainerPopups
{
    // =========================================================================
    // 1. THREE-STEP CLASSIC '.STF' -> MODERN '.STFX' UPGRADE WIZARD
    // =========================================================================

    public static StoreFileUpgradeWizardOutcome ShowStoreFileUpgradeWizardDialog(
        Window? owner,
        StoreTrainerService service,
        IReadOnlyList<string> candidateStfFiles,
        string destinationStoreFilesFolder)
    {
        var createdPaths = new List<string>();
        var launchImmediately = false;
        var currentStep = 1;

        var dlg = new Window
        {
            Title = "StoreTrainer — Classic StoreFile (.stf) Upgrade Wizard",
            Owner = owner,
            Width = 680,
            Height = 520,
            MinWidth = 580,
            MinHeight = 440,
            WindowStartupLocation = owner is not null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
        };
        AutomationProperties.SetName(dlg, "Classic StoreFile Upgrade Wizard");

        var root = new Grid { Margin = new Thickness(22, 18, 22, 18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header banner
        var headerStack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        var stepEyebrow = new TextBlock
        {
            Text = "STEP 1 OF 3 • SELECT CLASSIC STOREFILES",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Opacity = 0.75
        };
        var stepTitle = new TextBlock
        {
            Text = "Upgrade Classic StoreFiles (.stf) to StoreTrainer 2.0 (.stfx)",
            FontSize = 18.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 3, 0, 4),
            TextWrapping = TextWrapping.Wrap
        };
        var stepSub = new TextBlock
        {
            Text = "Your original .stf files in My StoreFiles are never altered or deleted — StoreTrainer 2.0 saves upgraded .stfx copies into Documents\\StoreFiles.",
            FontSize = 12.5,
            Opacity = 0.84,
            TextWrapping = TextWrapping.Wrap
        };
        headerStack.Children.Add(stepEyebrow);
        headerStack.Children.Add(stepTitle);
        headerStack.Children.Add(stepSub);
        Grid.SetRow(headerStack, 0);
        root.Children.Add(headerStack);

        // Step 1 panel: Checkboxes for each classic .stf file + "Add Another .stf File..." button
        var step1Grid = new Grid();
        step1Grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        step1Grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var checkBoxes = new List<CheckBox>();
        var filesStack = new StackPanel { Margin = new Thickness(6) };
        var filesScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = filesStack
        };
        var listBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            BorderBrush = Brushes.Gray,
            Padding = new Thickness(8),
            Child = filesScroll
        };
        Grid.SetRow(listBorder, 0);
        step1Grid.Children.Add(listBorder);

        void AddStfCheckBoxRow(string stfPath, bool isChecked)
        {
            if (checkBoxes.Any(c => string.Equals(c.Tag as string, stfPath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            var cb = new CheckBox
            {
                Content = $"{Path.GetFileName(stfPath)}   ({Path.GetDirectoryName(stfPath)})",
                Tag = stfPath,
                IsChecked = isChecked,
                Margin = new Thickness(4, 5, 4, 5)
            };
            checkBoxes.Add(cb);
            filesStack.Children.Add(cb);
        }

        foreach (var path in candidateStfFiles)
        {
            AddStfCheckBoxRow(path, isChecked: true);
        }

        var addMoreBtn = new Button
        {
            Content = "📂 Browse for Another Classic .stf File...",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 10, 0, 0)
        };
        addMoreBtn.Click += (_, _) =>
        {
            var open = new OpenFileDialog
            {
                Title = "Select Classic StoreFile (.stf)",
                Filter = "Classic StoreTrainer Profile (*.stf)|*.stf",
                Multiselect = true,
                InitialDirectory = Directory.Exists(service.GetClassicMyStoreFilesFolder())
                    ? service.GetClassicMyStoreFilesFolder()
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (open.ShowDialog(dlg) == true)
            {
                foreach (var f in open.FileNames)
                {
                    AddStfCheckBoxRow(f, isChecked: true);
                }
            }
        };
        Grid.SetRow(addMoreBtn, 1);
        step1Grid.Children.Add(addMoreBtn);

        // Step 2 panel: Review confirmation memo
        var step2Box = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            FontSize = 13,
            Padding = new Thickness(12),
            Visibility = Visibility.Collapsed
        };

        // Step 3 panel: Upgrade Summary Report
        var step3Box = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            FontSize = 13,
            Padding = new Thickness(12),
            Visibility = Visibility.Collapsed
        };

        var bodyHost = new Grid();
        bodyHost.Children.Add(step1Grid);
        bodyHost.Children.Add(step2Box);
        bodyHost.Children.Add(step3Box);
        Grid.SetRow(bodyHost, 1);
        root.Children.Add(bodyHost);

        // Footer navigation buttons: Back / Next / Launch Now / Cancel
        var footer = new DockPanel { Margin = new Thickness(0, 14, 0, 0), LastChildFill = false };
        var cancelBtn = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 7, 16, 7),
            IsCancel = true
        };
        var nextBtn = new Button
        {
            Content = "Next →",
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(18, 7, 18, 7),
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        var launchBtn = new Button
        {
            Content = "▶ Finish & Launch Game Now",
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Visibility = Visibility.Collapsed
        };
        var backBtn = new Button
        {
            Content = "← Back",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = false
        };

        DockPanel.SetDock(cancelBtn, Dock.Right);
        DockPanel.SetDock(nextBtn, Dock.Right);
        DockPanel.SetDock(launchBtn, Dock.Right);
        DockPanel.SetDock(backBtn, Dock.Right);
        footer.Children.Add(cancelBtn);
        footer.Children.Add(nextBtn);
        footer.Children.Add(launchBtn);
        footer.Children.Add(backBtn);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        void RenderStepState()
        {
            step1Grid.Visibility = currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            step2Box.Visibility = currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            step3Box.Visibility = currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            backBtn.IsEnabled = currentStep == 2;

            if (currentStep == 1)
            {
                stepEyebrow.Text = "STEP 1 OF 3 • SELECT CLASSIC STOREFILES";
                stepTitle.Text = "Choose Classic .stf Profiles to Upgrade";
                nextBtn.Content = "Next →";
            }
            else if (currentStep == 2)
            {
                var chosen = checkBoxes.Where(c => c.IsChecked == true).Select(c => (string)c.Tag).ToList();
                stepEyebrow.Text = "STEP 2 OF 3 • READY TO UPGRADE";
                stepTitle.Text = "Review Upgrade Plan";
                nextBtn.Content = $"Upgrade {chosen.Count} Profile(s) →";

                var lines = new List<string>
                {
                    $"Destination Folder:  {destinationStoreFilesFolder}",
                    "Original .stf Files: Preserved 100% untouched (StoreTrainer 1.0 & 1.1 remain fully compatible)",
                    "",
                    $"Profiles Selected ({chosen.Count}):"
                };
                foreach (var f in chosen)
                {
                    lines.Add($"  • {Path.GetFileName(f)}  -->  {Path.GetFileNameWithoutExtension(f)}.stfx");
                }
                step2Box.Text = string.Join(Environment.NewLine, lines);
            }
            else
            {
                stepEyebrow.Text = "STEP 3 OF 3 • UPGRADE COMPLETE";
                stepTitle.Text = "StoreFile Upgrade Summary Report";
                nextBtn.Content = "Finish";
                cancelBtn.Visibility = Visibility.Collapsed;
                backBtn.Visibility = Visibility.Collapsed;
                if (createdPaths.Count == 1)
                {
                    launchBtn.Visibility = Visibility.Visible;
                }
            }
        }

        backBtn.Click += (_, _) =>
        {
            if (currentStep == 2)
            {
                currentStep = 1;
                RenderStepState();
            }
        };

        nextBtn.Click += (_, _) =>
        {
            if (currentStep == 1)
            {
                var chosen = checkBoxes.Where(c => c.IsChecked == true).ToList();
                if (chosen.Count == 0)
                {
                    MessageBox.Show(dlg, "Please select at least one classic .stf file to upgrade.", "Select StoreFile", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                currentStep = 2;
                RenderStepState();
            }
            else if (currentStep == 2)
            {
                var chosenPaths = checkBoxes.Where(c => c.IsChecked == true).Select(c => (string)c.Tag).ToList();
                var reportLines = new List<string>();
                var okCount = 0;
                var errCount = 0;

                foreach (var src in chosenPaths)
                {
                    var item = service.UpgradeClassicStfToStfx(src, destinationStoreFilesFolder);
                    if (item.Success)
                    {
                        okCount++;
                        createdPaths.Add(item.OutputStfxPath);
                        reportLines.Add($"✅ {item.DisplayTitle}");
                        reportLines.Add($"   Source:  {item.SourceStfPath}");
                        reportLines.Add($"   Created: {item.OutputStfxPath}");
                    }
                    else
                    {
                        errCount++;
                        reportLines.Add($"⚠️ {item.DisplayTitle} (Failed)");
                    }

                    foreach (var n in item.Notes)
                    {
                        reportLines.Add($"   {n}");
                    }
                    reportLines.Add(string.Empty);
                }

                reportLines.Insert(0, $"Summary: {okCount} upgraded, {errCount} error(s)" + Environment.NewLine);
                step3Box.Text = string.Join(Environment.NewLine, reportLines);
                currentStep = 3;
                RenderStepState();
            }
            else
            {
                dlg.DialogResult = true;
                dlg.Close();
            }
        };

        launchBtn.Click += (_, _) =>
        {
            launchImmediately = true;
            dlg.DialogResult = true;
            dlg.Close();
        };

        cancelBtn.Click += (_, _) =>
        {
            dlg.DialogResult = false;
            dlg.Close();
        };

        dlg.Content = root;
        _ = dlg.ShowDialog();
        return new StoreFileUpgradeWizardOutcome(createdPaths, launchImmediately);
    }

    // =========================================================================
    // 2. TRAINER '.ZIP' ARCHIVE EXTRACTION DIALOG (WITH GAME-PATH UAC OPTION)
    // =========================================================================

    public static TrainerZipDialogChoice? ShowTrainerZipExtractDialog(
        Window owner,
        string zipFilePath,
        TrainerZipInspection inspection,
        string? gameFolderPath,
        string defaultTrainersSubfolder)
    {
        var hasGameFolder = !string.IsNullOrWhiteSpace(gameFolderPath) && Directory.Exists(gameFolderPath);
        TrainerZipDialogChoice? choice = null;

        var dlg = new Window
        {
            Title = $"Extract Trainer Archive — {Path.GetFileName(zipFilePath)}",
            Owner = owner,
            Width = 620,
            SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        AutomationProperties.SetName(dlg, "Extract Trainer ZIP Archive");

        var root = new StackPanel { Margin = new Thickness(22, 20, 22, 18) };

        root.Children.Add(new TextBlock
        {
            Text = "📦 Extract Companion Trainer ZIP Archive",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var docsSummary = inspection.ReadmeAndNfoEntries.Count > 0
            ? $" • Includes {string.Join(", ", inspection.ReadmeAndNfoEntries.Select(Path.GetFileName))}"
            : string.Empty;
        root.Children.Add(new TextBlock
        {
            Text = $"Found {inspection.ExecutableEntries.Count} trainer program(s) inside '{Path.GetFileName(zipFilePath)}'{docsSummary}.",
            FontSize = 12.5,
            Opacity = 0.85,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });

        root.Children.Add(new TextBlock
        {
            Text = "1. Trainer program inside archive:",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });

        var exeCombo = new ComboBox
        {
            ItemsSource = inspection.ExecutableEntries,
            SelectedIndex = 0,
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 0, 0, 14)
        };
        root.Children.Add(exeCombo);

        root.Children.Add(new TextBlock
        {
            Text = "2. Where should StoreTrainer extract this trainer?",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var radioGameDir = new RadioButton
        {
            GroupName = "ZipDest",
            IsEnabled = hasGameFolder,
            IsChecked = hasGameFolder,
            Margin = new Thickness(0, 0, 0, 8),
            Content = new TextBlock
            {
                Text = hasGameFolder
                    ? $"Extract directly into the game's folder:\n{gameFolderPath}\n(Recommended for classic trainers — Windows will ask administrator permission if the game is in Program Files)"
                    : "Extract directly into the game's folder (choose a Game Program first to enable this option)",
                TextWrapping = TextWrapping.Wrap
            }
        };

        var radioLibraryDir = new RadioButton
        {
            GroupName = "ZipDest",
            IsChecked = !hasGameFolder,
            Margin = new Thickness(0, 0, 0, 16),
            Content = new TextBlock
            {
                Text = $"Extract into my StoreFiles\\Trainers folder:\n{defaultTrainersSubfolder}\n(Keeps your game installation folder untouched; no administrator permission needed)",
                TextWrapping = TextWrapping.Wrap
            }
        };

        root.Children.Add(radioGameDir);
        root.Children.Add(radioLibraryDir);

        var footer = new DockPanel { LastChildFill = false };
        var cancelBtn = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 7, 16, 7),
            IsCancel = true
        };
        var extractBtn = new Button
        {
            Content = "📦 Extract Trainer",
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(18, 7, 18, 7),
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        DockPanel.SetDock(cancelBtn, Dock.Right);
        DockPanel.SetDock(extractBtn, Dock.Right);
        footer.Children.Add(cancelBtn);
        footer.Children.Add(extractBtn);
        root.Children.Add(footer);

        extractBtn.Click += (_, _) =>
        {
            var selectedExe = exeCombo.SelectedItem as string ?? inspection.ExecutableEntries[0];
            var useGameDir = radioGameDir.IsChecked == true && hasGameFolder;
            var dest = useGameDir ? gameFolderPath! : defaultTrainersSubfolder;
            choice = new TrainerZipDialogChoice(selectedExe, dest, useGameDir);
            dlg.DialogResult = true;
            dlg.Close();
        };

        cancelBtn.Click += (_, _) => dlg.Close();

        dlg.Content = root;
        _ = dlg.ShowDialog();
        return choice;
    }

    // =========================================================================
    // 3. CLASSIC IBM PC CP437 '.NFO' / 'FILE_ID.DIZ' / 'README' VIEWER
    // =========================================================================

    public static void ShowCp437NfoViewerDialog(
        Window owner,
        string nfoFilePath,
        Action<List<HotkeyItem>> onImportHotkeys)
    {
        var decodedText = Cp437NfoHelper.ReadNfoText(nfoFilePath);
        var detectedHotkeys = Cp437NfoHelper.ExtractHotkeysFromText(decodedText);

        var dlg = new Window
        {
            Title = $"Trainer Info Viewer — {Path.GetFileName(nfoFilePath)}",
            Owner = owner,
            Width = 760,
            Height = 640,
            MinWidth = 540,
            MinHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        AutomationProperties.SetName(dlg, "Trainer Info and ASCII Art Viewer");

        var grid = new Grid { Margin = new Thickness(18) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new TextBlock
        {
            Text = $"📄 {Path.GetFileName(nfoFilePath)}   ({detectedHotkeys.Count} shortcut key(s) detected automatically)",
            FontSize = 14.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        // Authentic high-contrast dark terminal box (>= 12:1 contrast ratio) so classic 80-column CP437 borders align cleanly!
        var artBox = new TextBox
        {
            Text = decodedText,
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New"),
            FontSize = 13,
            Background = new SolidColorBrush(Color.FromRgb(18, 22, 28)),
            Foreground = new SolidColorBrush(Color.FromRgb(226, 238, 250)),
            Padding = new Thickness(14),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetRow(artBox, 1);
        grid.Children.Add(artBox);

        var footer = new DockPanel { Margin = new Thickness(0, 12, 0, 0), LastChildFill = false };
        var closeBtn = new Button
        {
            Content = "Close",
            Padding = new Thickness(16, 7, 16, 7),
            IsDefault = true,
            IsCancel = true
        };
        DockPanel.SetDock(closeBtn, Dock.Right);
        closeBtn.Click += (_, _) => dlg.Close();
        footer.Children.Add(closeBtn);

        if (detectedHotkeys.Count > 0)
        {
            var importBtn = new Button
            {
                Content = $"⌨️ Import {detectedHotkeys.Count} Shortcut Key(s) into Profile",
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(14, 7, 14, 7)
            };
            DockPanel.SetDock(importBtn, Dock.Left);
            importBtn.Click += (_, _) =>
            {
                onImportHotkeys(detectedHotkeys);
                MessageBox.Show(
                    dlg,
                    $"Imported {detectedHotkeys.Count} trainer shortcut key(s) into your game profile!",
                    "Shortcut Keys Imported",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            footer.Children.Add(importBtn);
        }

        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        dlg.Content = grid;
        _ = dlg.ShowDialog();
    }

    // =========================================================================
    // 4. ALWAYS-ON-TOP FLOATING HOTKEY CHEAT CARD WINDOW
    // =========================================================================

    public static Window ShowHotkeyCheatCardWindow(Window? owner, StoreFileProfile profile)
    {
        var win = new Window
        {
            Title = $"Cheat Card — {profile.DisplayTitle}",
            Width = 370,
            Height = 460,
            MinWidth = 290,
            MinHeight = 260,
            Topmost = true,
            WindowStartupLocation = owner is not null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.Manual
        };
        AutomationProperties.SetName(win, $"Floating Shortcut Cheat Card for {profile.DisplayTitle}");

        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        header.Children.Add(new TextBlock
        {
            Text = $"🎮 {profile.DisplayTitle}",
            FontSize = 15.5,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        var pinCheck = new CheckBox
        {
            Content = "📌 Keep pinned above other windows (Always on Top)",
            IsChecked = true,
            Margin = new Thickness(0, 6, 0, 0)
        };
        pinCheck.Checked += (_, _) => win.Topmost = true;
        pinCheck.Unchecked += (_, _) => win.Topmost = false;
        header.Children.Add(pinCheck);
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var contentStack = new StackPanel();
        if (profile.Hotkeys.Count > 0)
        {
            foreach (var hk in profile.Hotkeys.Where(h => !string.IsNullOrWhiteSpace(h.Keys) || !string.IsNullOrWhiteSpace(h.Effect)))
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var keyBadge = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray,
                    Padding = new Thickness(7, 3, 7, 3),
                    Margin = new Thickness(0, 0, 8, 0),
                    Child = new TextBlock
                    {
                        Text = hk.Keys,
                        FontFamily = new FontFamily("Consolas, Segoe UI"),
                        FontWeight = FontWeights.Bold,
                        FontSize = 12
                    }
                };
                Grid.SetColumn(keyBadge, 0);
                row.Children.Add(keyBadge);

                var desc = new TextBlock
                {
                    Text = hk.Effect,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12.5
                };
                Grid.SetColumn(desc, 1);
                row.Children.Add(desc);
                contentStack.Children.Add(row);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.Notes))
        {
            contentStack.Children.Add(new TextBlock
            {
                Text = "Personal Notes:",
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                Margin = new Thickness(0, 8, 0, 4)
            });
            contentStack.Children.Add(new TextBlock
            {
                Text = profile.Notes,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12.5,
                Opacity = 0.9
            });
        }

        if (profile.Hotkeys.Count == 0 && string.IsNullOrWhiteSpace(profile.Notes))
        {
            contentStack.Children.Add(new TextBlock
            {
                Text = "No shortcut keys or notes added yet.\n\nAdd rows in the Shortcut Cheat Table (or click 'View .NFO' to auto-import keys from your trainer's info file)!",
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.78,
                Margin = new Thickness(0, 12, 0, 0)
            });
        }

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = contentStack
        };
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);

        var closeBtn = new Button
        {
            Content = "Close Cheat Card",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 10, 0, 0)
        };
        closeBtn.Click += (_, _) => win.Close();
        Grid.SetRow(closeBtn, 2);
        grid.Children.Add(closeBtn);

        win.Content = grid;
        win.Show(); // Modeless so the user can play their game while keeping this card visible!
        return win;
    }

    // =========================================================================
    // 5. SAVE-GAME BACKUP SNAPSHOTS MANAGER ("TRAINER INSURANCE")
    // =========================================================================

    public static void ShowSaveBackupsDialog(
        Window owner,
        StoreTrainerService service,
        StoreFileProfile profile)
    {
        var dlg = new Window
        {
            Title = $"Save-Game Backups — {profile.DisplayTitle}",
            Owner = owner,
            Width = 600,
            Height = 450,
            MinWidth = 480,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        AutomationProperties.SetName(dlg, "Manage Save-Game Backups");

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        header.Children.Add(new TextBlock
        {
            Text = $"🛡 Save-Game Backups for {profile.DisplayTitle}",
            FontSize = 16.5,
            FontWeight = FontWeights.Bold
        });
        header.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(profile.SaveFolderPath)
                ? "Choose this game's Save-Game Folder in the profile editor first to create 1-click backups."
                : $"Save Folder: {profile.SaveFolderPath}",
            FontSize = 12,
            Opacity = 0.82,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var listBox = new ListBox
        {
            DisplayMemberPath = nameof(SaveBackupInfo.FriendlyLabel),
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(listBox, 1);
        grid.Children.Add(listBox);

        void RefreshList()
        {
            listBox.ItemsSource = service.ListSaveGameBackups(profile);
            if (listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }
        }
        RefreshList();

        var footer = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var backupNowBtn = new Button
        {
            Content = "➕ Backup Saves Now",
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0),
            IsEnabled = !string.IsNullOrWhiteSpace(profile.SaveFolderPath) && Directory.Exists(profile.SaveFolderPath)
        };
        var restoreBtn = new Button
        {
            Content = "⏪ Restore Selected Backup",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var openFolderBtn = new Button
        {
            Content = "📂 Backups Folder",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var doneBtn = new Button
        {
            Content = "Done",
            Padding = new Thickness(16, 6, 16, 6),
            IsDefault = true,
            IsCancel = true
        };

        backupNowBtn.Click += (_, _) =>
        {
            try
            {
                var snap = service.CreateSaveGameBackup(profile);
                RefreshList();
                MessageBox.Show(dlg, $"Created save-game snapshot:\n{snap.FriendlyLabel}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(dlg, $"Could not create backup:\n{ex.Message}", "Backup Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        restoreBtn.Click += (_, _) =>
        {
            if (listBox.SelectedItem is not SaveBackupInfo selected)
            {
                return;
            }
            var ask = MessageBox.Show(
                dlg,
                $"Restore save-game snapshot from {selected.CreatedLocalTime:MMM d, yyyy h:mm tt} into:\n{profile.SaveFolderPath}?",
                "Restore Save-Game Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (ask != MessageBoxResult.Yes) return;

            try
            {
                service.RestoreSaveGameBackup(selected.ZipFilePath, profile.SaveFolderPath);
                MessageBox.Show(dlg, "Save-game files restored!", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(dlg, $"Could not restore backup:\n{ex.Message}", "Restore Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        openFolderBtn.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = service.GetSaveBackupsDirectoryForProfile(profile),
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        };

        doneBtn.Click += (_, _) => dlg.Close();

        footer.Children.Add(backupNowBtn);
        footer.Children.Add(restoreBtn);
        footer.Children.Add(openFolderBtn);
        footer.Children.Add(doneBtn);
        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        dlg.Content = grid;
        _ = dlg.ShowDialog();
    }
}
