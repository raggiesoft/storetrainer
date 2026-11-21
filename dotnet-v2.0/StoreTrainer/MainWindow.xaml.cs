// ================================================================================
// FILE: MainWindow.xaml.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Creative Content)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is this file?
//     This is the C# controller ("code-behind") paired with MainWindow.xaml. Every
//     button click, drag-and-drop gesture, ZIP archive extraction, classic '.stf'
//     upgrade wizard flow, and launch sequence is coordinated right here:
//       1. Modern '.stfx' Library Discovery ('Documents\StoreFiles\*.stfx') with
//          automatic detection of unmigrated classic '.stf' profiles in 'My StoreFiles'.
//       2. Multiple Trainers per Game + Trainer '.ZIP' Archive Extractor (including
//          automatic Windows UAC permission prompt when extracting into Program Files!)
//          + Classic '.NFO' / 'file_id.diz' ASCII Art Viewer & Hotkey Importer.
//       3. Dual Launch Modes: "▶ Launch with Trainer" AND "▶ Launch Game Only (No Trainer)",
//          with Pre-Launch Save-Game Backup, CPU Core Limiter, Playtime Tracking, and
//          Auto-Close Companion Trainer on Game Exit.
//       4. Floating Always-On-Top Cheat Card, 1-Click Desktop Game Shortcut, and
//          1-Click Check for Updates via 'RaggieSoftUpdate.exe'!
// ================================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RaggieSoft.Core.Models;
using RaggieSoft.Core.Services;
using RaggieSoft.SharedWpf;
using StoreTrainer.Models;
using StoreTrainer.Services;
using StoreTrainer.Windows;

namespace StoreTrainer;

#pragma warning disable WPF0001 // Fluent ThemeMode API in .NET 10 WPF

public partial class MainWindow : Window
{
    private readonly RaggieSoftWorkspaceLocator _locator = new();
    private readonly StoreTrainerService _service = new();

    private string _libraryFolder = string.Empty;
    private List<StoreFileProfile> _allProfiles = [];
    private StoreFileProfile _currentProfile = new();
    private readonly ObservableCollection<HotkeyItem> _boundHotkeys = [];

    private bool _isBindingEditor;
    private bool _isBindingTrainerSlot;
    private bool _isUpdatingSelection;
    private bool _isDirty;
    private string _currentThemeLabel = "System";

    public MainWindow()
    {
        InitializeComponent();

        HotkeysDataGrid.ItemsSource = _boundHotkeys;
        _boundHotkeys.CollectionChanged += (_, _) =>
        {
            if (!_isBindingEditor)
            {
                SetDirtyState(true);
            }
        };

        // Populate mandatory RaggieSoft Work of Fiction & Creative Licensing footer.
        ScrollableFooterTitleText.Text = RaggieSoftLegalCopy.FictitiousDisclaimerTitle.ToUpperInvariant();
        ScrollableFooterBodyText.Text = RaggieSoftLegalCopy.FictitiousDisclaimerBody;
        ScrollableFooterLicenseText.Text = RaggieSoftLegalCopy.CreativeLicenseSummary;

        ApplyThemeByName(_service.GetSavedThemePreference(), persist: false);

        Loaded += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        RaggieSoftWpfDialogs.EnsureFirstRunFictionNoticeAcknowledged(this, _locator);

        _libraryFolder = _service.EnsureLibraryFolder();
        ActiveLibraryPathText.Text = $"📂 StoreFiles: {_libraryFolder}";

        ReloadLibraryList(selectFilePath: App.StartupEditProfilePath);
        RefreshClassicImportBanner();

        // If launched with an external '.stfx' via '/e', load it into the editor
        if (!string.IsNullOrWhiteSpace(App.StartupEditProfilePath) &&
            File.Exists(App.StartupEditProfilePath) &&
            !string.Equals(_currentProfile.FilePath, App.StartupEditProfilePath, StringComparison.OrdinalIgnoreCase))
        {
            OpenExternalStoreFileOrWizard(App.StartupEditProfilePath);
        }

        // If a classic '.stf' was passed on startup (via File -> Open, double-click, or '/l'),
        // launch the 3-step StoreFile Upgrade Wizard immediately!
        if (!string.IsNullOrWhiteSpace(App.StartupClassicStfUpgradePath) &&
            File.Exists(App.StartupClassicStfUpgradePath))
        {
            await RunUpgradeWizardAsync([App.StartupClassicStfUpgradePath]);
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            e.Cancel = true;
        }
    }

    // =========================================================================
    // LIBRARY DISCOVERY, CLASSIC '.STF' BANNER & FILTERING
    // =========================================================================

    private void RefreshClassicImportBanner()
    {
        if (_service.HasDismissedClassicImportBanner())
        {
            ClassicImportBannerBorder.Visibility = Visibility.Collapsed;
            return;
        }

        var unmigrated = _service.FindUnmigratedClassicStfFiles(_libraryFolder);
        if (unmigrated.Count > 0)
        {
            ClassicImportBannerText.Text =
                $"✨ Found {unmigrated.Count} classic StoreFile profile(s) (.stf) in My StoreFiles — upgrade them to StoreTrainer 2.0 (.stfx) while keeping your originals untouched!";
            ClassicImportBannerBorder.Visibility = Visibility.Visible;
        }
        else
        {
            ClassicImportBannerBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void ReloadLibraryList(string? selectFilePath = null)
    {
        _allProfiles = _service.LoadLibraryProfiles(_libraryFolder);
        LibrarySummaryText.Text = _allProfiles.Count == 1
            ? "1 saved game profile in Documents\\StoreFiles"
            : $"{_allProfiles.Count} saved game profiles in Documents\\StoreFiles";

        ApplyLibraryFilter(selectFilePath);
    }

    private void ApplyLibraryFilter(string? selectFilePath = null)
    {
        var search = LibrarySearchBox?.Text.Trim() ?? string.Empty;
        var categoryTag = (LibraryCategoryFilterCombo?.SelectedItem as ComboBoxItem)?.Tag as string ?? "ALL";

        IEnumerable<StoreFileProfile> filtered = _allProfiles;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(p =>
                p.DisplayTitle.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Notes.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Hotkeys.Any(h => h.Keys.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   h.Effect.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                p.FileNameBadge.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filtered = categoryTag switch
        {
            "ALL" => filtered,
            "FAVORITES" => filtered.Where(p => p.IsFavorite),
            _ => filtered.Where(p => string.Equals(p.Category, categoryTag, StringComparison.OrdinalIgnoreCase))
        };

        var visibleList = filtered.ToList();
        _isUpdatingSelection = true;
        try
        {
            ProfilesListBox.ItemsSource = visibleList;
            EmptyLibraryText.Visibility = visibleList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            StoreFileProfile? target = null;
            if (!string.IsNullOrWhiteSpace(selectFilePath))
            {
                target = visibleList.FirstOrDefault(p =>
                    string.Equals(p.FilePath, selectFilePath, StringComparison.OrdinalIgnoreCase));
            }
            target ??= visibleList.FirstOrDefault();

            if (target is not null)
            {
                ProfilesListBox.SelectedItem = target;
                BindProfileToEditor(target.Clone(), markDirty: false);
            }
            else if (_allProfiles.Count == 0)
            {
                BindProfileToEditor(new StoreFileProfile(), markDirty: false);
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    // =========================================================================
    // TWO-WAY PROFILE <-> EDITOR FORM BINDING (INCLUDING MULTI-TRAINER SLOTS)
    // =========================================================================

    private void BindProfileToEditor(StoreFileProfile profile, bool markDirty)
    {
        _isBindingEditor = true;
        try
        {
            _currentProfile = profile;
            _ = _currentProfile.GetOrCreateActiveTrainer();

            ProfileDisplayNameBox.Text = profile.DisplayName;
            ProfileCategoryCombo.Text = string.IsNullOrWhiteSpace(profile.Category) ? "General" : profile.Category;
            FavoriteCheckBox.IsChecked = profile.IsFavorite;
            PlaytimeStatsText.Text = profile.PlaytimeSummaryLabel;

            TrainerLaunchesGameCheckBox.IsChecked = profile.TrainerLaunchesGame;
            LaunchGameFirstCheckBox.IsChecked = !profile.TrainerLaunchesGame && profile.LaunchGameFirst;
            WaitForGameReadyCheckBox.IsChecked = profile.WaitForGameReady;
            AutoCloseTrainerCheckBox.IsChecked = profile.AutoCloseTrainerOnGameExit;
            SelectComboByIntTag(LaunchDelayCombo, profile.LaunchDelaySeconds);
            SelectComboByIntTag(CpuCoreLimitCombo, profile.CpuCoreLimit);

            GamePathBox.Text = profile.GamePath;
            GameSwitchBox.Text = profile.GameSwitch;
            GameWorkingDirBox.Text = profile.GameWorkingDirectory;

            SaveFolderPathBox.Text = profile.SaveFolderPath;
            BackupSavesBeforeLaunchCheckBox.IsChecked = profile.BackupSavesBeforeLaunch;

            _boundHotkeys.Clear();
            foreach (var hk in profile.Hotkeys)
            {
                _boundHotkeys.Add(hk.Clone());
            }

            ProfileNotesBox.Text = profile.Notes;

            RefreshTrainerSlotsComboAndBindActive();
            UpdateGameSectionEnabledState();
            UpdatePathValidationBadges();
            DeleteProfileButton.IsEnabled = !profile.IsUnsavedNew;

            SetDirtyState(markDirty);
        }
        finally
        {
            _isBindingEditor = false;
        }
    }

    private void RefreshTrainerSlotsComboAndBindActive()
    {
        _isBindingTrainerSlot = true;
        try
        {
            var active = _currentProfile.GetOrCreateActiveTrainer();
            ActiveTrainerCombo.ItemsSource = null;
            ActiveTrainerCombo.ItemsSource = _currentProfile.Trainers;
            ActiveTrainerCombo.SelectedItem = active;
            RemoveTrainerSlotButton.IsEnabled = _currentProfile.Trainers.Count > 1;

            TrainerLabelBox.Text = active.Label;
            TrainerPathBox.Text = active.Path;
            TrainerSwitchBox.Text = active.Switch;
            TrainerWorkingDirBox.Text = active.WorkingDirectory;
            VerifyTrainerCheckBox.IsChecked = active.VerifyBeforeLaunch;
            UpdateTrainerFingerprintStatusLabel(active);
        }
        finally
        {
            _isBindingTrainerSlot = false;
        }
    }

    private void SaveCurrentTrainerFieldsIntoActiveSlot()
    {
        if (_isBindingTrainerSlot)
        {
            return;
        }

        var active = _currentProfile.GetOrCreateActiveTrainer();
        active.Label = string.IsNullOrWhiteSpace(TrainerLabelBox.Text) ? "Primary Trainer" : TrainerLabelBox.Text.Trim();
        active.Path = TrainerPathBox.Text.Trim();
        active.Switch = TrainerSwitchBox.Text.Trim();
        active.WorkingDirectory = TrainerWorkingDirBox.Text.Trim();
        active.VerifyBeforeLaunch = VerifyTrainerCheckBox.IsChecked == true;
    }

    private void ReadEditorIntoCurrentProfile()
    {
        _currentProfile.DisplayName = ProfileDisplayNameBox.Text.Trim();
        _currentProfile.Category = string.IsNullOrWhiteSpace(ProfileCategoryCombo.Text)
            ? "General"
            : ProfileCategoryCombo.Text.Trim();
        _currentProfile.IsFavorite = FavoriteCheckBox.IsChecked == true;

        _currentProfile.TrainerLaunchesGame = TrainerLaunchesGameCheckBox.IsChecked == true;
        _currentProfile.LaunchGameFirst = !_currentProfile.TrainerLaunchesGame &&
                                          LaunchGameFirstCheckBox.IsChecked == true;
        _currentProfile.WaitForGameReady = WaitForGameReadyCheckBox.IsChecked == true;
        _currentProfile.AutoCloseTrainerOnGameExit = AutoCloseTrainerCheckBox.IsChecked == true;

        _ = int.TryParse((LaunchDelayCombo.SelectedItem as ComboBoxItem)?.Tag as string, out var delaySeconds);
        _currentProfile.LaunchDelaySeconds = delaySeconds;

        _ = int.TryParse((CpuCoreLimitCombo.SelectedItem as ComboBoxItem)?.Tag as string, out var coreLimit);
        _currentProfile.CpuCoreLimit = coreLimit;

        _currentProfile.GamePath = GamePathBox.Text.Trim();
        _currentProfile.GameSwitch = GameSwitchBox.Text.Trim();
        _currentProfile.GameWorkingDirectory = GameWorkingDirBox.Text.Trim();

        SaveCurrentTrainerFieldsIntoActiveSlot();

        _currentProfile.SaveFolderPath = SaveFolderPathBox.Text.Trim();
        _currentProfile.BackupSavesBeforeLaunch = BackupSavesBeforeLaunchCheckBox.IsChecked == true;

        _currentProfile.Hotkeys = _boundHotkeys
            .Where(h => !string.IsNullOrWhiteSpace(h.Keys) || !string.IsNullOrWhiteSpace(h.Effect))
            .Select(h => h.Clone())
            .ToList();
        _currentProfile.Notes = ProfileNotesBox.Text;
    }

    private void SetDirtyState(bool dirty)
    {
        _isDirty = dirty;
        ReadEditorIntoCurrentProfile();
        ProfileHeaderTitleText.Text = _currentProfile.DisplayTitle;
        UnsavedBadgeBorder.Visibility = dirty ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SelectComboByIntTag(ComboBox combo, int targetValue)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item &&
                int.TryParse(item.Tag as string, out var val) &&
                val == targetValue)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void UpdateGameSectionEnabledState()
    {
        var trainerStartsGame = TrainerLaunchesGameCheckBox.IsChecked == true;
        if (trainerStartsGame)
        {
            LaunchGameFirstCheckBox.IsChecked = false;
        }

        LaunchGameFirstCheckBox.IsEnabled = !trainerStartsGame;
        LaunchCleanButton.IsEnabled = !trainerStartsGame;
        GamePathBox.IsEnabled = !trainerStartsGame;
        BrowseGameButton.IsEnabled = !trainerStartsGame;
        GameSwitchBox.IsEnabled = !trainerStartsGame;
        GameWorkingDirBox.IsEnabled = !trainerStartsGame;
        GameSectionBorder.Opacity = trainerStartsGame ? 0.62 : 1.0;
    }

    private void UpdatePathValidationBadges()
    {
        var gamePath = GamePathBox.Text.Trim();
        if (TrainerLaunchesGameCheckBox.IsChecked == true)
        {
            GameStatusText.Text = "Started automatically by trainer";
        }
        else if (string.IsNullOrWhiteSpace(gamePath))
        {
            GameStatusText.Text = "Choose a game program";
        }
        else
        {
            GameStatusText.Text = File.Exists(gamePath) ? "✅ File found" : "⚠️ File not found on disk";
        }

        var trainerPath = TrainerPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(trainerPath))
        {
            TrainerStatusText.Text = "Optional (or choose/extract trainer)";
        }
        else
        {
            TrainerStatusText.Text = File.Exists(trainerPath) ? "✅ File found" : "⚠️ File not found on disk";
        }
    }

    private void UpdateTrainerFingerprintStatusLabel(TrainerEntry active)
    {
        if (string.IsNullOrWhiteSpace(active.Fingerprint))
        {
            TrainerFingerprintStatusText.Text = "No trusted trainer snapshot recorded yet";
            return;
        }

        var shortCode = active.Fingerprint.Length >= 12 ? active.Fingerprint[..12] : active.Fingerprint;
        TrainerFingerprintStatusText.Text = $"🛡 Trusted snapshot saved ({shortCode}…)";
    }

    private bool ConfirmDiscardOrSaveUnsavedChanges()
    {
        if (!_isDirty)
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            $"Would you like to save your changes to \"{_currentProfile.DisplayTitle}\"?",
            "Unsaved Changes — RaggieSoft StoreTrainer",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Yes);

        if (result == MessageBoxResult.Cancel) return false;
        if (result == MessageBoxResult.Yes) return PerformSave(forceSaveAs: false);
        return true;
    }

    // =========================================================================
    // SAVE, UPGRADE WIZARD, LAUNCH (WITH TRAINER / CLEAN), & DESKTOP SHORTCUT
    // =========================================================================

    private bool PerformSave(bool forceSaveAs)
    {
        HotkeysDataGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        ReadEditorIntoCurrentProfile();

        var targetPath = _currentProfile.FilePath;
        if (forceSaveAs || string.IsNullOrWhiteSpace(targetPath))
        {
            var suggestedBase = StoreTrainerService.MakeSafeFileSlug(
                !string.IsNullOrWhiteSpace(_currentProfile.DisplayName)
                    ? _currentProfile.DisplayName
                    : !string.IsNullOrWhiteSpace(_currentProfile.GamePath)
                        ? Path.GetFileNameWithoutExtension(_currentProfile.GamePath)
                        : "MyGame");

            var dlg = new SaveFileDialog
            {
                Title = "Save StoreTrainer 2.0 Profile (.stfx)",
                Filter = "StoreTrainer 2.0 Profile (*.stfx)|*.stfx",
                DefaultExt = ".stfx",
                InitialDirectory = Directory.Exists(_libraryFolder) ? _libraryFolder : string.Empty,
                FileName = $"{suggestedBase}.stfx"
            };

            if (dlg.ShowDialog(this) != true)
            {
                return false;
            }
            targetPath = dlg.FileName;
        }

        try
        {
            var active = _currentProfile.GetOrCreateActiveTrainer();
            if (active.VerifyBeforeLaunch &&
                string.IsNullOrWhiteSpace(active.Fingerprint) &&
                File.Exists(active.Path))
            {
                active.Fingerprint = _service.ComputeFileFingerprint(active.Path);
            }

            _service.SaveStoreFile(_currentProfile, targetPath);
            SetDirtyState(false);
            ReloadLibraryList(selectFilePath: targetPath);
            StatusBannerText.Text = $"💾 Saved {_currentProfile.DisplayTitle} ({Path.GetFileName(targetPath)})";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save profile:\n{ex.Message}", "Save Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void SaveProfileButton_OnClick(object sender, RoutedEventArgs e) => PerformSave(forceSaveAs: false);
    private void SaveAsProfileButton_OnClick(object sender, RoutedEventArgs e) => PerformSave(forceSaveAs: true);

    private void NewProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges()) return;

        _isUpdatingSelection = true;
        ProfilesListBox.SelectedItem = null;
        _isUpdatingSelection = false;

        BindProfileToEditor(new StoreFileProfile(), markDirty: false);
        ProfileDisplayNameBox.Focus();
        StatusBannerText.Text = "➕ Started a new .stfx game profile";
    }

    private void ImportProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges()) return;

        var dlg = new OpenFileDialog
        {
            Title = "Open StoreFile Profile (.stfx or Classic .stf)",
            Filter = "All StoreTrainer Profiles (*.stfx;*.stf)|*.stfx;*.stf|StoreTrainer 2.0 Profile (*.stfx)|*.stfx|Classic StoreTrainer 1.0/1.1 Profile (*.stf)|*.stf",
            InitialDirectory = Directory.Exists(_libraryFolder) ? _libraryFolder : string.Empty
        };

        if (dlg.ShowDialog(this) == true)
        {
            OpenExternalStoreFileOrWizard(dlg.FileName);
        }
    }

    private async void OpenExternalStoreFileOrWizard(string filePath)
    {
        if (filePath.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
        {
            await RunUpgradeWizardAsync([filePath]);
            return;
        }

        var res = _service.LoadStoreFile(filePath);
        if (!res.Success || res.Profile is null)
        {
            MessageBox.Show(this, res.ErrorMessage, "Open StoreFile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BindProfileToEditor(res.Profile, markDirty: false);
        StatusBannerText.Text = $"📂 Opened {res.Profile.DisplayTitle}";
    }

    private async void OpenUpgradeWizardBannerButton_OnClick(object sender, RoutedEventArgs e)
    {
        var candidates = _service.FindUnmigratedClassicStfFiles(_libraryFolder);
        await RunUpgradeWizardAsync(candidates);
    }

    private async void OpenUpgradeWizardButton_OnClick(object sender, RoutedEventArgs e)
    {
        var candidates = _service.FindUnmigratedClassicStfFiles(_libraryFolder);
        await RunUpgradeWizardAsync(candidates);
    }

    private void DismissClassicBannerButton_OnClick(object sender, RoutedEventArgs e)
    {
        _service.SetDismissedClassicImportBanner(true);
        ClassicImportBannerBorder.Visibility = Visibility.Collapsed;
    }

    private async Task RunUpgradeWizardAsync(IReadOnlyList<string> initialFiles)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            return;
        }

        var outcome = StoreTrainerPopups.ShowStoreFileUpgradeWizardDialog(
            this,
            _service,
            initialFiles,
            _libraryFolder);

        if (outcome.CreatedStfxPaths.Count > 0)
        {
            var firstCreated = outcome.CreatedStfxPaths[0];
            ReloadLibraryList(selectFilePath: firstCreated);
            RefreshClassicImportBanner();
            StatusBannerText.Text = $"✅ Upgraded {outcome.CreatedStfxPaths.Count} classic StoreFile(s) into Documents\\StoreFiles";

            if (outcome.LaunchFirstProfileImmediately)
            {
                await ExecuteLaunchSequenceAsync(launchWithTrainer: true);
            }
        }
    }

    private void DuplicateProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges()) return;

        ReadEditorIntoCurrentProfile();
        var copy = _currentProfile.Clone();
        copy.FilePath = string.Empty;
        copy.DisplayName = $"{_currentProfile.DisplayTitle} (Copy)";

        _isUpdatingSelection = true;
        ProfilesListBox.SelectedItem = null;
        _isUpdatingSelection = false;

        BindProfileToEditor(copy, markDirty: true);
        StatusBannerText.Text = "📋 Created duplicate draft — click Save to add it to StoreFiles";
    }

    private void DeleteProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentProfile.IsUnsavedNew || !File.Exists(_currentProfile.FilePath)) return;

        var confirm = MessageBox.Show(
            this,
            $"Delete the saved profile \"{_currentProfile.DisplayTitle}\"?\n\n" +
            "(This only removes the .stfx profile shortcut; your actual game, save files, and trainer files are never touched.)",
            "Delete Game Profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var deletedTitle = _currentProfile.DisplayTitle;
            File.Delete(_currentProfile.FilePath);
            _isDirty = false;
            ReloadLibraryList();
            StatusBannerText.Text = $"🗑 Deleted profile {deletedTitle}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete profile:\n{ex.Message}", "Delete Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void LaunchNowButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ExecuteLaunchSequenceAsync(launchWithTrainer: true);
    }

    private async void LaunchCleanButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ExecuteLaunchSequenceAsync(launchWithTrainer: false);
    }

    private async Task ExecuteLaunchSequenceAsync(bool launchWithTrainer)
    {
        HotkeysDataGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        ReadEditorIntoCurrentProfile();

        if (_isDirty || _currentProfile.IsUnsavedNew)
        {
            if (!PerformSave(forceSaveAs: false))
            {
                return;
            }
        }

        LaunchNowButton.IsEnabled = false;
        LaunchCleanButton.IsEnabled = false;
        StatusBannerText.Text = $"⏳ Launching {_currentProfile.DisplayTitle}...";

        try
        {
            var result = await _service.LaunchProfileAsync(
                _currentProfile,
                launchWithTrainer: launchWithTrainer,
                confirmFingerprintMismatch: prompt =>
                {
                    var answer = MessageBox.Show(
                        this,
                        prompt,
                        "Trainer File Changed — RaggieSoft StoreTrainer",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning,
                        MessageBoxResult.No);
                    return answer == MessageBoxResult.Yes;
                },
                onSessionStatsUpdated: updated =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (string.Equals(updated.FilePath, _currentProfile.FilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            PlaytimeStatsText.Text = updated.PlaytimeSummaryLabel;
                        }
                        ReloadLibraryList(_currentProfile.FilePath);
                    });
                });

            if (result.Success)
            {
                PlaytimeStatsText.Text = _currentProfile.PlaytimeSummaryLabel;
                StatusBannerText.Text = $"🚀 {result.Message}";
            }
            else
            {
                StatusBannerText.Text = $"⚠️ {result.Message}";
                MessageBox.Show(this, result.Message, "Launch Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            LaunchNowButton.IsEnabled = true;
            UpdateGameSectionEnabledState();
        }
    }

    private void CreateDesktopShortcutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isDirty || _currentProfile.IsUnsavedNew)
        {
            if (!PerformSave(forceSaveAs: false))
            {
                return;
            }
        }

        var (ok, msg) = _service.CreateDesktopLaunchShortcut(_currentProfile);
        StatusBannerText.Text = ok ? $"🔗 {msg}" : $"⚠️ {msg}";
        MessageBox.Show(this, msg, "Desktop Game Shortcut", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    // =========================================================================
    // MULTI-TRAINER SLOTS, ZIP ARCHIVE EXTRACTOR (UAC) & .NFO VIEWER
    // =========================================================================

    private void ActiveTrainerCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBindingEditor || _isBindingTrainerSlot || ActiveTrainerCombo.SelectedItem is not TrainerEntry selected)
        {
            return;
        }

        SaveCurrentTrainerFieldsIntoActiveSlot();
        _currentProfile.ActiveTrainerId = selected.Id;

        _isBindingTrainerSlot = true;
        try
        {
            TrainerLabelBox.Text = selected.Label;
            TrainerPathBox.Text = selected.Path;
            TrainerSwitchBox.Text = selected.Switch;
            TrainerWorkingDirBox.Text = selected.WorkingDirectory;
            VerifyTrainerCheckBox.IsChecked = selected.VerifyBeforeLaunch;
            UpdateTrainerFingerprintStatusLabel(selected);
            UpdatePathValidationBadges();
        }
        finally
        {
            _isBindingTrainerSlot = false;
        }
        SetDirtyState(true);
    }

    private void AddTrainerSlotButton_OnClick(object sender, RoutedEventArgs e)
    {
        SaveCurrentTrainerFieldsIntoActiveSlot();
        var nextNum = _currentProfile.Trainers.Count + 1;
        var slot = new TrainerEntry { Label = $"Trainer #{nextNum}" };
        _currentProfile.Trainers.Add(slot);
        _currentProfile.ActiveTrainerId = slot.Id;
        RefreshTrainerSlotsComboAndBindActive();
        UpdatePathValidationBadges();
        SetDirtyState(true);
        StatusBannerText.Text = $"➕ Added {slot.Label}";
    }

    private void RemoveTrainerSlotButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentProfile.Trainers.Count <= 1) return;

        var active = _currentProfile.GetOrCreateActiveTrainer();
        _currentProfile.Trainers.Remove(active);
        _currentProfile.ActiveTrainerId = _currentProfile.Trainers[0].Id;
        RefreshTrainerSlotsComboAndBindActive();
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void TrainerLabelBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBindingEditor || _isBindingTrainerSlot) return;
        SaveCurrentTrainerFieldsIntoActiveSlot();
        ActiveTrainerCombo.Items.Refresh();
        SetDirtyState(true);
    }

    private async void ExtractTrainerZipButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Companion Trainer ZIP Archive",
            Filter = "ZIP Archives (*.zip)|*.zip|All Files (*.*)|*.*",
            DefaultExt = ".zip"
        };

        if (dlg.ShowDialog(this) == true)
        {
            await HandleTrainerZipArchiveAsync(dlg.FileName);
        }
    }

    private async Task HandleTrainerZipArchiveAsync(string zipFilePath)
    {
        var inspection = _service.InspectTrainerZip(zipFilePath);
        if (!inspection.Success)
        {
            MessageBox.Show(this, inspection.ErrorMessage, "Trainer ZIP Archive", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ReadEditorIntoCurrentProfile();
        var gameDir = !string.IsNullOrWhiteSpace(_currentProfile.GamePath)
            ? Path.GetDirectoryName(_currentProfile.GamePath)
            : null;
        var slug = StoreTrainerService.MakeSafeFileSlug(
            !string.IsNullOrWhiteSpace(_currentProfile.DisplayName)
                ? _currentProfile.DisplayName
                : Path.GetFileNameWithoutExtension(zipFilePath));
        var defaultTrainersFolder = Path.Combine(_libraryFolder, "Trainers", slug);

        var choice = StoreTrainerPopups.ShowTrainerZipExtractDialog(
            this,
            zipFilePath,
            inspection,
            gameDir,
            defaultTrainersFolder);

        if (choice is null)
        {
            return;
        }

        StatusBannerText.Text = "⏳ Extracting trainer archive...";
        var extractRes = await _service.ExtractTrainerZipAsync(
            zipFilePath,
            choice.DestinationDirectory,
            choice.SelectedExecutableEntry);

        if (!extractRes.Success)
        {
            StatusBannerText.Text = $"⚠️ {extractRes.Message}";
            MessageBox.Show(this, extractRes.Message, "Extract Trainer ZIP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Populate active trainer slot with the extracted trainer .exe and any .nfo/.diz!
        var active = _currentProfile.GetOrCreateActiveTrainer();
        active.Path = extractRes.ExtractedTrainerPath;
        if (!string.IsNullOrWhiteSpace(extractRes.ExtractedNfoPath))
        {
            active.NfoPath = extractRes.ExtractedNfoPath;
            var autoKeys = Cp437NfoHelper.ExtractHotkeysFromText(Cp437NfoHelper.ReadNfoText(extractRes.ExtractedNfoPath));
            if (autoKeys.Count > 0 && _boundHotkeys.Count == 0)
            {
                foreach (var hk in autoKeys)
                {
                    _boundHotkeys.Add(hk);
                }
            }
        }

        if (string.IsNullOrWhiteSpace(ProfileDisplayNameBox.Text))
        {
            ProfileDisplayNameBox.Text = Path.GetFileNameWithoutExtension(zipFilePath);
        }

        RefreshTrainerSlotsComboAndBindActive();
        UpdatePathValidationBadges();
        SetDirtyState(true);
        StatusBannerText.Text = $"📦 {extractRes.Message}";
    }

    private void ViewTrainerNfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        var active = _currentProfile.GetOrCreateActiveTrainer();
        var nfoPath = active.NfoPath;

        if (string.IsNullOrWhiteSpace(nfoPath) || !File.Exists(nfoPath))
        {
            nfoPath = StoreTrainerService.FindCompanionNfoInDirectory(Path.GetDirectoryName(TrainerPathBox.Text.Trim())) ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(nfoPath) || !File.Exists(nfoPath))
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Trainer Info File (.nfo, .diz, or .txt)",
                Filter = "Trainer Info Files (*.nfo;*.diz;*.txt)|*.nfo;*.diz;*.txt|All Files (*.*)|*.*",
                InitialDirectory = Directory.Exists(Path.GetDirectoryName(TrainerPathBox.Text.Trim()))
                    ? Path.GetDirectoryName(TrainerPathBox.Text.Trim())!
                    : string.Empty
            };
            if (dlg.ShowDialog(this) != true)
            {
                return;
            }
            nfoPath = dlg.FileName;
            active.NfoPath = nfoPath;
            SetDirtyState(true);
        }

        StoreTrainerPopups.ShowCp437NfoViewerDialog(this, nfoPath, importedKeys =>
        {
            foreach (var hk in importedKeys)
            {
                if (!_boundHotkeys.Any(existing => string.Equals(existing.Keys, hk.Keys, StringComparison.OrdinalIgnoreCase)))
                {
                    _boundHotkeys.Add(hk);
                }
            }
            SetDirtyState(true);
        });
    }

    // =========================================================================
    // CHEAT CARD, SAVE BACKUPS, BROWSE & DRAG-AND-DROP HANDLERS
    // =========================================================================

    private void PopOutCheatCardButton_OnClick(object sender, RoutedEventArgs e)
    {
        HotkeysDataGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        ReadEditorIntoCurrentProfile();
        _ = StoreTrainerPopups.ShowHotkeyCheatCardWindow(this, _currentProfile);
    }

    private void AddHotkeyRowButton_OnClick(object sender, RoutedEventArgs e)
    {
        _boundHotkeys.Add(new HotkeyItem { Keys = "Numpad 1", Effect = "Unlimited Health" });
        SetDirtyState(true);
    }

    private void HotkeysDataGrid_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (!_isBindingEditor)
        {
            Dispatcher.BeginInvoke(() => SetDirtyState(true));
        }
    }

    private void BrowseSaveFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Choose This Game's Saved-Games Folder",
            InitialDirectory = Directory.Exists(SaveFolderPathBox.Text.Trim())
                ? SaveFolderPathBox.Text.Trim()
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dlg.ShowDialog(this) == true)
        {
            SaveFolderPathBox.Text = dlg.FolderName;
            BackupSavesBeforeLaunchCheckBox.IsChecked = true;
            SetDirtyState(true);
        }
    }

    private void ManageSaveBackupsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ReadEditorIntoCurrentProfile();
        StoreTrainerPopups.ShowSaveBackupsDialog(this, _service, _currentProfile);
    }

    private void BrowseGameButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Locate Game Program",
            Filter = "Program Files (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All Files (*.*)|*.*",
            DefaultExt = ".exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dlg.ShowDialog(this) == true)
        {
            AssignGameExecutable(dlg.FileName);
        }
    }

    private void BrowseTrainerButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Locate Companion Trainer Program or ZIP Archive",
            Filter = "Trainer Program or ZIP (*.exe;*.zip)|*.exe;*.zip|All Files (*.*)|*.*",
            DefaultExt = ".exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dlg.ShowDialog(this) == true)
        {
            if (dlg.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _ = HandleTrainerZipArchiveAsync(dlg.FileName);
            }
            else
            {
                AssignTrainerExecutable(dlg.FileName);
            }
        }
    }

    private void AssignGameExecutable(string exePath)
    {
        GamePathBox.Text = exePath;
        _currentProfile.GameIconSource = _service.TryExtractExecutableIcon(exePath);
        if (string.IsNullOrWhiteSpace(ProfileDisplayNameBox.Text))
        {
            ProfileDisplayNameBox.Text = Path.GetFileNameWithoutExtension(exePath);
        }
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void AssignTrainerExecutable(string exePath)
    {
        TrainerPathBox.Text = exePath;
        var active = _currentProfile.GetOrCreateActiveTrainer();
        active.Path = exePath;

        var siblingNfo = StoreTrainerService.FindCompanionNfoInDirectory(Path.GetDirectoryName(exePath));
        if (!string.IsNullOrWhiteSpace(siblingNfo))
        {
            active.NfoPath = siblingNfo;
        }

        if (VerifyTrainerCheckBox.IsChecked == true && File.Exists(exePath))
        {
            active.Fingerprint = _service.ComputeFileFingerprint(exePath);
            UpdateTrainerFingerprintStatusLabel(active);
        }
        ActiveTrainerCombo.Items.Refresh();
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void PinTrainerFingerprintButton_OnClick(object sender, RoutedEventArgs e)
    {
        var trainerPath = TrainerPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(trainerPath) || !File.Exists(trainerPath))
        {
            MessageBox.Show(this, "Please choose an existing trainer file first before recording a trusted snapshot.", "Trainer File Needed", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var active = _currentProfile.GetOrCreateActiveTrainer();
        active.Fingerprint = _service.ComputeFileFingerprint(trainerPath);
        VerifyTrainerCheckBox.IsChecked = true;
        UpdateTrainerFingerprintStatusLabel(active);
        SetDirtyState(true);
        StatusBannerText.Text = "🛡 Recorded trusted trainer file snapshot";
    }

    private void VerifyTrainerCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isBindingEditor || _isBindingTrainerSlot) return;

        var active = _currentProfile.GetOrCreateActiveTrainer();
        if (VerifyTrainerCheckBox.IsChecked == true &&
            string.IsNullOrWhiteSpace(active.Fingerprint) &&
            File.Exists(TrainerPathBox.Text.Trim()))
        {
            active.Fingerprint = _service.ComputeFileFingerprint(TrainerPathBox.Text.Trim());
            UpdateTrainerFingerprintStatusLabel(active);
        }
        SetDirtyState(true);
    }

    private void Window_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;

        var first = files[0];
        if (first.EndsWith(".stfx", StringComparison.OrdinalIgnoreCase) ||
            first.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
        {
            if (ConfirmDiscardOrSaveUnsavedChanges())
            {
                OpenExternalStoreFileOrWizard(first);
            }
            e.Handled = true;
        }
        else if (first.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            await HandleTrainerZipArchiveAsync(first);
            e.Handled = true;
        }
        else if (first.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            AssignGameExecutable(first);
            StatusBannerText.Text = $"🎯 Set game program to {Path.GetFileName(first)}";
            e.Handled = true;
        }
    }

    private void GameSectionBorder_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            if (files[0].EndsWith(".stfx", StringComparison.OrdinalIgnoreCase) ||
                files[0].EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
            {
                if (ConfirmDiscardOrSaveUnsavedChanges())
                {
                    OpenExternalStoreFileOrWizard(files[0]);
                }
            }
            else
            {
                AssignGameExecutable(files[0]);
                StatusBannerText.Text = $"🎯 Set game program to {Path.GetFileName(files[0])}";
            }
            e.Handled = true;
        }
    }

    private async void TrainerSectionBorder_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            var dropped = files[0];
            if (dropped.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await HandleTrainerZipArchiveAsync(dropped);
            }
            else if (dropped.EndsWith(".nfo", StringComparison.OrdinalIgnoreCase) ||
                     dropped.EndsWith(".diz", StringComparison.OrdinalIgnoreCase))
            {
                _currentProfile.GetOrCreateActiveTrainer().NfoPath = dropped;
                SetDirtyState(true);
                ViewTrainerNfoButton_OnClick(sender, e);
            }
            else if (dropped.EndsWith(".stfx", StringComparison.OrdinalIgnoreCase) ||
                     dropped.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
            {
                if (ConfirmDiscardOrSaveUnsavedChanges())
                {
                    OpenExternalStoreFileOrWizard(dropped);
                }
            }
            else
            {
                AssignTrainerExecutable(dropped);
                StatusBannerText.Text = $"🎯 Set trainer program to {Path.GetFileName(dropped)}";
            }
            e.Handled = true;
        }
    }

    // =========================================================================
    // SELECTION, TOOLBAR, UPDATES & THEME HANDLERS
    // =========================================================================

    private void ProfilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection || ProfilesListBox.SelectedItem is not StoreFileProfile selected) return;
        if (string.Equals(selected.FilePath, _currentProfile.FilePath, StringComparison.OrdinalIgnoreCase)) return;

        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            _isUpdatingSelection = true;
            ProfilesListBox.SelectedItem = _allProfiles.FirstOrDefault(p =>
                string.Equals(p.FilePath, _currentProfile.FilePath, StringComparison.OrdinalIgnoreCase));
            _isUpdatingSelection = false;
            return;
        }

        BindProfileToEditor(selected.Clone(), markDirty: false);
        StatusBannerText.Text = $"📄 Viewing {selected.DisplayTitle}";
    }

    private void LibrarySearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded) ApplyLibraryFilter(_currentProfile.FilePath);
    }

    private void LibraryCategoryFilterCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) ApplyLibraryFilter(_currentProfile.FilePath);
    }

    private void TrainerLaunchesGameCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isBindingEditor) return;
        UpdateGameSectionEnabledState();
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void EditorField_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_isBindingEditor) SetDirtyState(true);
    }

    private void EditorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isBindingEditor && !_isBindingTrainerSlot) SetDirtyState(true);
    }

    private void GamePathBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBindingEditor) return;
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void TrainerPathBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isBindingEditor || _isBindingTrainerSlot) return;
        SaveCurrentTrainerFieldsIntoActiveSlot();
        ActiveTrainerCombo.Items.Refresh();
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void ProfileCategoryCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isBindingEditor && IsLoaded) SetDirtyState(true);
    }

    private void ProfileCategoryCombo_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isBindingEditor) SetDirtyState(true);
    }

    private void LaunchDelayCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isBindingEditor && IsLoaded) SetDirtyState(true);
    }

    private void CpuCoreLimitCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isBindingEditor && IsLoaded) SetDirtyState(true);
    }

    private void OpenGameFolderButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenContainingFolder(GamePathBox.Text.Trim());

    private void OpenTrainerFolderButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenContainingFolder(TrainerPathBox.Text.Trim());

    private void OpenLibraryFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_libraryFolder);
        OpenFolderInExplorer(_libraryFolder);
    }

    private void ChangeLibraryFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges()) return;

        var dlg = new OpenFolderDialog
        {
            Title = "Choose StoreFiles Library Folder",
            InitialDirectory = Directory.Exists(_libraryFolder)
                ? _libraryFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dlg.ShowDialog(this) == true)
        {
            _libraryFolder = dlg.FolderName;
            _service.SaveCustomLibraryFolder(_libraryFolder);
            ActiveLibraryPathText.Text = $"📂 StoreFiles: {_libraryFolder}";
            ReloadLibraryList();
            RefreshClassicImportBanner();
            StatusBannerText.Text = $"📁 Switched StoreFiles folder to {_libraryFolder}";
        }
    }

    private async void CheckUpdatesButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RaggieSoftUpdaterLauncher.TryLaunchInteractiveWizard("storetrainer"))
        {
            StatusBannerText.Text = "🔄 Opened RaggieSoft Software Update Wizard";
            return;
        }

        // Graceful inline fallback if RaggieSoftUpdate.exe hasn't been compiled yet in local dev
        CheckUpdatesButton.IsEnabled = false;
        StatusBannerText.Text = "⏳ Checking for updates...";
        try
        {
            var mgr = new RaggieSoftSuiteManager(_locator);
            var apps = await mgr.LoadSuiteAppsAsync(checkRemoteManifest: true);
            var st = apps.FirstOrDefault(a => a.Id == "storetrainer" || a.Id == "store-trainer");
            var msg = st is not null && st.IsUpdateAvailable
                ? $"RaggieSoft StoreTrainer v{st.LatestVersion} is available! Open RaggieSoft Launcher to update."
                : "You are running the latest version of RaggieSoft StoreTrainer (v2.0.0).";
            StatusBannerText.Text = $"✅ {msg}";
            MessageBox.Show(this, msg, "RaggieSoft Software Update", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void ThemeToggleButton_OnClick(object sender, RoutedEventArgs e)
    {
        var next = _currentThemeLabel switch
        {
            "System" => "Dark",
            "Dark" => "Light",
            _ => "System"
        };
        ApplyThemeByName(next, persist: true);
    }

    private void ApplyThemeByName(string themeName, bool persist)
    {
        if (string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            Application.Current.ThemeMode = ThemeMode.Dark;
            _currentThemeLabel = "Dark";
        }
        else if (string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase))
        {
            Application.Current.ThemeMode = ThemeMode.Light;
            _currentThemeLabel = "Light";
        }
        else
        {
            Application.Current.ThemeMode = ThemeMode.System;
            _currentThemeLabel = "System";
        }

        ThemeToggleButton.Content = $"🌓 Theme: {_currentThemeLabel}";
        if (persist)
        {
            _service.SaveThemePreference(_currentThemeLabel);
        }
    }

    private void AboutAndLegalButton_OnClick(object sender, RoutedEventArgs e)
    {
        RaggieSoftWpfDialogs.ShowAboutAndLegalDialog(
            this,
            "RaggieSoft StoreTrainer",
            "RaggieSoft Media",
            version: "2.0.0");
    }

    private static void OpenContainingFolder(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;
        var dir = Directory.Exists(filePath) ? filePath : Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        {
            OpenFolderInExplorer(dir);
        }
    }

    private static void OpenFolderInExplorer(string directoryPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }
}
