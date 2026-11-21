// ================================================================================
// FILE: MainWindow.xaml.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Creative Content)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is this file?
//     This is the C# controller ("code-behind") paired with MainWindow.xaml. Every
//     button click, search keystroke, drag-and-drop gesture, and theme switch in
//     StoreTrainer 2.0 is handled right here:
//       1. Automatic Library Discovery: Scans your 'Documents\My StoreFiles' folder
//          (or any existing custom folder saved in classic 'TStore.ini') and binds
//          every '.stf' game profile to the left-hand sidebar list.
//       2. Unsaved-Changes Guard: Tracks whether you have edited fields on the right
//          without saving yet, prompting politely before switching profiles or closing
//          the window so you never lose work by accident.
//       3. Drag-and-Drop Convenience: Drop a '.stf' file anywhere on the window to
//          open it, or drop a '.exe' file onto the Game or Trainer card to fill in
//          its path and title automatically!
//       4. Mutual-Exclusivity Enforcement: Checking "Trainer starts the game
//          automatically" automatically disables the separate Game fields and
//          unchecks "Start the game first", fulfilling the exact design rule first
//          noted in classic StoreTrainer's 2018 developer notes.
// ================================================================================

using System.ComponentModel;    // 'CancelEventArgs' for intercepting window close when unsaved edits exist.
using System.Diagnostics;       // 'Process.Start' for opening folders in Windows File Explorer.
using System.IO;                // 'File', 'Directory', and 'Path' helpers.
using System.Windows;           // Core WPF window, drag-and-drop, and dialog classes.
using System.Windows.Controls;  // 'ListBox', 'ComboBoxItem', 'TextChangedEventArgs', etc.
using Microsoft.Win32;          // Native Windows 'OpenFileDialog', 'SaveFileDialog', and 'OpenFolderDialog'.
using RaggieSoft.Core.Models;   // 'RaggieSoftLegalCopy' for the Work of Fiction & Licensing notice.
using RaggieSoft.Core.Services; // 'RaggieSoftWorkspaceLocator' for first-run legal notice persistence.
using RaggieSoft.SharedWpf;     // 'RaggieSoftWpfDialogs' for shared First-Run & About modals.
using StoreTrainer.Models;      // 'StoreFileProfile' data model.
using StoreTrainer.Services;    // 'StoreTrainerService' INI & process launcher engine.

namespace StoreTrainer;

#pragma warning disable WPF0001 // Fluent ThemeMode API in .NET 10 WPF

public partial class MainWindow : Window
{
    private readonly RaggieSoftWorkspaceLocator _locator = new();
    private readonly StoreTrainerService _service = new();

    // Active library folder path (defaults to 'Documents\My StoreFiles').
    private string _libraryFolder = string.Empty;
    // Complete list of '.stf' profiles loaded from '_libraryFolder'.
    private List<StoreFileProfile> _allProfiles = [];
    // Currently edited profile in the right-hand pane.
    private StoreFileProfile _currentProfile = new();

    // Guard flag set to 'true' while C# is populating TextBoxes/CheckBoxes from a profile
    // so programmatic updates don't falsely trigger the "Unsaved Changes" badge!
    private bool _isBindingEditor;
    // Guard flag preventing SelectionChanged recursion when restoring a ListBox selection.
    private bool _isUpdatingSelection;
    // True when the user has modified fields in the editor since the last save/load.
    private bool _isDirty;
    // Current color theme label ("System", "Dark", or "Light").
    private string _currentThemeLabel = "System";

    public MainWindow()
    {
        InitializeComponent();

        // Populate the mandatory RaggieSoft Work of Fiction & Creative Licensing footer.
        ScrollableFooterTitleText.Text = RaggieSoftLegalCopy.FictitiousDisclaimerTitle.ToUpperInvariant();
        ScrollableFooterBodyText.Text = RaggieSoftLegalCopy.FictitiousDisclaimerBody;
        ScrollableFooterLicenseText.Text = RaggieSoftLegalCopy.CreativeLicenseSummary;

        ApplyThemeByName(_service.GetSavedThemePreference(), persist: false);

        Loaded += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
    }

    // Runs once the StoreTrainer window appears on screen.
    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        // Show the one-time First-Run Work of Fiction acknowledgement modal if not yet accepted.
        RaggieSoftWpfDialogs.EnsureFirstRunFictionNoticeAcknowledged(this, _locator);

        _libraryFolder = _service.EnsureLibraryFolder();
        ActiveLibraryPathText.Text = $"📂 Library: {_libraryFolder}";

        ReloadLibraryList(selectFilePath: App.StartupEditProfilePath);

        // If StoreTrainer was invoked with '/e "C:\SomeOtherFolder\Game.stf"' outside My StoreFiles,
        // load that external '.stf' directly into the editor pane!
        if (!string.IsNullOrWhiteSpace(App.StartupEditProfilePath) &&
            File.Exists(App.StartupEditProfilePath) &&
            !string.Equals(_currentProfile.FilePath, App.StartupEditProfilePath, StringComparison.OrdinalIgnoreCase))
        {
            OpenExternalStoreFileIntoEditor(App.StartupEditProfilePath);
        }
    }

    // Prompts to save if the user clicks the window's [X] close button with unsaved edits.
    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            e.Cancel = true;
        }
    }

    // =========================================================================
    // LIBRARY DISCOVERY, SEARCH & FILTERING
    // =========================================================================

    private void ReloadLibraryList(string? selectFilePath = null)
    {
        _allProfiles = _service.LoadLibraryProfiles(_libraryFolder);
        LibrarySummaryText.Text = _allProfiles.Count == 1
            ? "1 saved game profile in My StoreFiles • Compatible with StoreTrainer 1.0, 1.1 & 2.0"
            : $"{_allProfiles.Count} saved game profiles in My StoreFiles • Compatible with StoreTrainer 1.0, 1.1 & 2.0";

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
    // TWO-WAY PROFILE <-> EDITOR FORM BINDING
    // =========================================================================

    private void BindProfileToEditor(StoreFileProfile profile, bool markDirty)
    {
        _isBindingEditor = true;
        try
        {
            _currentProfile = profile;

            ProfileDisplayNameBox.Text = profile.DisplayName;
            ProfileCategoryCombo.Text = string.IsNullOrWhiteSpace(profile.Category) ? "General" : profile.Category;
            FavoriteCheckBox.IsChecked = profile.IsFavorite;

            TrainerLaunchesGameCheckBox.IsChecked = profile.TrainerLaunchesGame;
            LaunchGameFirstCheckBox.IsChecked = !profile.TrainerLaunchesGame && profile.LaunchGameFirst;
            WaitForGameReadyCheckBox.IsChecked = profile.WaitForGameReady;
            SelectDelayComboBySeconds(profile.LaunchDelaySeconds);

            GamePathBox.Text = profile.GamePath;
            GameSwitchBox.Text = profile.GameSwitch;
            GameWorkingDirBox.Text = profile.GameWorkingDirectory;

            TrainerPathBox.Text = profile.TrainerPath;
            TrainerSwitchBox.Text = profile.TrainerSwitch;
            TrainerWorkingDirBox.Text = profile.TrainerWorkingDirectory;
            VerifyTrainerCheckBox.IsChecked = profile.VerifyTrainerBeforeLaunch;

            ProfileNotesBox.Text = profile.Notes;

            UpdateGameSectionEnabledState();
            UpdatePathValidationBadges();
            UpdateTrainerFingerprintStatusLabel();
            DeleteProfileButton.IsEnabled = !profile.IsUnsavedNew;

            SetDirtyState(markDirty);
        }
        finally
        {
            _isBindingEditor = false;
        }
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

        var delayTag = (LaunchDelayCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "0";
        _ = int.TryParse(delayTag, out var delaySeconds);
        _currentProfile.LaunchDelaySeconds = delaySeconds;

        _currentProfile.GamePath = GamePathBox.Text.Trim();
        _currentProfile.GameSwitch = GameSwitchBox.Text.Trim();
        _currentProfile.GameWorkingDirectory = GameWorkingDirBox.Text.Trim();

        _currentProfile.TrainerPath = TrainerPathBox.Text.Trim();
        _currentProfile.TrainerSwitch = TrainerSwitchBox.Text.Trim();
        _currentProfile.TrainerWorkingDirectory = TrainerWorkingDirBox.Text.Trim();
        _currentProfile.VerifyTrainerBeforeLaunch = VerifyTrainerCheckBox.IsChecked == true;
        _currentProfile.Notes = ProfileNotesBox.Text;
    }

    private void SetDirtyState(bool dirty)
    {
        _isDirty = dirty;
        ReadEditorIntoCurrentProfile();
        ProfileHeaderTitleText.Text = _currentProfile.DisplayTitle;
        UnsavedBadgeBorder.Visibility = dirty ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SelectDelayComboBySeconds(int seconds)
    {
        for (var i = 0; i < LaunchDelayCombo.Items.Count; i++)
        {
            if (LaunchDelayCombo.Items[i] is ComboBoxItem item &&
                int.TryParse(item.Tag as string, out var itemSec) &&
                itemSec >= seconds)
            {
                LaunchDelayCombo.SelectedIndex = i;
                return;
            }
        }
        LaunchDelayCombo.SelectedIndex = 0;
    }

    private void UpdateGameSectionEnabledState()
    {
        var trainerStartsGame = TrainerLaunchesGameCheckBox.IsChecked == true;
        if (trainerStartsGame)
        {
            LaunchGameFirstCheckBox.IsChecked = false;
        }

        LaunchGameFirstCheckBox.IsEnabled = !trainerStartsGame;
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
            TrainerStatusText.Text = "Optional (or choose trainer)";
        }
        else
        {
            TrainerStatusText.Text = File.Exists(trainerPath) ? "✅ File found" : "⚠️ File not found on disk";
        }
    }

    private void UpdateTrainerFingerprintStatusLabel()
    {
        if (string.IsNullOrWhiteSpace(_currentProfile.TrainerFingerprint))
        {
            TrainerFingerprintStatusText.Text = "No trusted trainer snapshot recorded yet";
            return;
        }

        var shortCode = _currentProfile.TrainerFingerprint.Length >= 12
            ? _currentProfile.TrainerFingerprint[..12]
            : _currentProfile.TrainerFingerprint;
        TrainerFingerprintStatusText.Text = $"🛡 Trusted snapshot saved ({shortCode}…)";
    }

    // Checks if the current profile has unsaved changes and asks Yes / No / Cancel.
    // Returns true if safe to proceed, or false if the user canceled.
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

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.Yes)
        {
            return PerformSave(forceSaveAs: false);
        }

        return true;
    }

    // =========================================================================
    // SAVE, SAVE AS, NEW, DUPLICATE, DELETE & LAUNCH HANDLERS
    // =========================================================================

    private bool PerformSave(bool forceSaveAs)
    {
        ReadEditorIntoCurrentProfile();

        var targetPath = _currentProfile.FilePath;
        if (forceSaveAs || string.IsNullOrWhiteSpace(targetPath))
        {
            var suggestedBase = MakeSafeFileName(
                !string.IsNullOrWhiteSpace(_currentProfile.DisplayName)
                    ? _currentProfile.DisplayName
                    : !string.IsNullOrWhiteSpace(_currentProfile.GamePath)
                        ? Path.GetFileNameWithoutExtension(_currentProfile.GamePath)
                        : "MyGame");

            var dlg = new SaveFileDialog
            {
                Title = "Save StoreFile Profile",
                Filter = "StoreTrainer Profile (*.stf)|*.stf",
                DefaultExt = ".stf",
                InitialDirectory = Directory.Exists(_libraryFolder) ? _libraryFolder : string.Empty,
                FileName = $"{suggestedBase}.stf"
            };

            if (dlg.ShowDialog(this) != true)
            {
                return false;
            }

            targetPath = dlg.FileName;
        }

        try
        {
            // If user checked "Warn me before launching" and a valid trainer exists without a snapshot yet,
            // automatically capture the snapshot right as we save!
            if (_currentProfile.VerifyTrainerBeforeLaunch &&
                string.IsNullOrWhiteSpace(_currentProfile.TrainerFingerprint) &&
                File.Exists(_currentProfile.TrainerPath))
            {
                _currentProfile.TrainerFingerprint = _service.ComputeFileFingerprint(_currentProfile.TrainerPath);
            }

            _service.SaveStoreFile(_currentProfile, targetPath);
            SetDirtyState(false);
            ReloadLibraryList(selectFilePath: targetPath);
            StatusBannerText.Text = $"💾 Saved {_currentProfile.DisplayTitle} ({Path.GetFileName(targetPath)})";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not save StoreFile:\n{ex.Message}",
                "Save Notice",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
    }

    private void SaveProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        PerformSave(forceSaveAs: false);
    }

    private void SaveAsProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        PerformSave(forceSaveAs: true);
    }

    private void NewProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            return;
        }

        _isUpdatingSelection = true;
        ProfilesListBox.SelectedItem = null;
        _isUpdatingSelection = false;

        BindProfileToEditor(new StoreFileProfile(), markDirty: false);
        ProfileDisplayNameBox.Focus();
        StatusBannerText.Text = "➕ Started a new game profile";
    }

    private void ImportProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "Open StoreFile Profile",
            Filter = "StoreTrainer Profile (*.stf)|*.stf|All Files (*.*)|*.*",
            DefaultExt = ".stf",
            InitialDirectory = Directory.Exists(_libraryFolder) ? _libraryFolder : string.Empty
        };

        if (dlg.ShowDialog(this) == true)
        {
            OpenExternalStoreFileIntoEditor(dlg.FileName);
        }
    }

    private void OpenExternalStoreFileIntoEditor(string filePath)
    {
        var res = _service.LoadStoreFile(filePath);
        if (!res.Success || res.Profile is null)
        {
            MessageBox.Show(this, res.ErrorMessage, "Open StoreFile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BindProfileToEditor(res.Profile, markDirty: false);
        StatusBannerText.Text = $"📂 Opened {res.Profile.DisplayTitle}";
    }

    private void DuplicateProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            return;
        }

        ReadEditorIntoCurrentProfile();
        var copy = _currentProfile.Clone();
        copy.FilePath = string.Empty;
        copy.DisplayName = $"{_currentProfile.DisplayTitle} (Copy)";

        _isUpdatingSelection = true;
        ProfilesListBox.SelectedItem = null;
        _isUpdatingSelection = false;

        BindProfileToEditor(copy, markDirty: true);
        StatusBannerText.Text = "📋 Created duplicate draft — click Save to add it to your library";
    }

    private void DeleteProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentProfile.IsUnsavedNew || !File.Exists(_currentProfile.FilePath))
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Delete the saved profile \"{_currentProfile.DisplayTitle}\"?\n\n" +
            "(This only removes the StoreTrainer profile shortcut; your actual game and trainer files will not be touched.)",
            "Delete Game Profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

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
        ReadEditorIntoCurrentProfile();

        // Save automatically if already saved before, or ask to save first if brand new (matching classic StoreTrainer!).
        if (_isDirty || _currentProfile.IsUnsavedNew)
        {
            if (!PerformSave(forceSaveAs: false))
            {
                return;
            }
        }

        LaunchNowButton.IsEnabled = false;
        StatusBannerText.Text = $"⏳ Launching {_currentProfile.DisplayTitle}...";

        try
        {
            var result = await _service.LaunchProfileAsync(_currentProfile, prompt =>
            {
                var answer = MessageBox.Show(
                    this,
                    prompt,
                    "Trainer File Changed — RaggieSoft StoreTrainer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);
                return answer == MessageBoxResult.Yes;
            });

            if (result.Success)
            {
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
        }
    }

    // =========================================================================
    // BROWSE, TRAINER SNAPSHOT & DRAG-AND-DROP HANDLERS
    // =========================================================================

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
            Title = "Locate Companion Trainer Program",
            Filter = "Program Files (*.exe)|*.exe|All Files (*.*)|*.*",
            DefaultExt = ".exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dlg.ShowDialog(this) == true)
        {
            AssignTrainerExecutable(dlg.FileName);
        }
    }

    private void AssignGameExecutable(string exePath)
    {
        GamePathBox.Text = exePath;
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
        if (VerifyTrainerCheckBox.IsChecked == true && File.Exists(exePath))
        {
            _currentProfile.TrainerFingerprint = _service.ComputeFileFingerprint(exePath);
            UpdateTrainerFingerprintStatusLabel();
        }
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void PinTrainerFingerprintButton_OnClick(object sender, RoutedEventArgs e)
    {
        var trainerPath = TrainerPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(trainerPath) || !File.Exists(trainerPath))
        {
            MessageBox.Show(
                this,
                "Please choose an existing trainer file first before recording a trusted snapshot.",
                "Trainer File Needed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _currentProfile.TrainerFingerprint = _service.ComputeFileFingerprint(trainerPath);
        VerifyTrainerCheckBox.IsChecked = true;
        UpdateTrainerFingerprintStatusLabel();
        SetDirtyState(true);
        StatusBannerText.Text = "🛡 Recorded trusted trainer file snapshot";
    }

    private void VerifyTrainerCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isBindingEditor)
        {
            return;
        }

        if (VerifyTrainerCheckBox.IsChecked == true &&
            string.IsNullOrWhiteSpace(_currentProfile.TrainerFingerprint) &&
            File.Exists(TrainerPathBox.Text.Trim()))
        {
            _currentProfile.TrainerFingerprint = _service.ComputeFileFingerprint(TrainerPathBox.Text.Trim());
            UpdateTrainerFingerprintStatusLabel();
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

    private void Window_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return;
        }

        var first = files[0];
        if (first.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
        {
            if (ConfirmDiscardOrSaveUnsavedChanges())
            {
                OpenExternalStoreFileIntoEditor(first);
            }
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
            if (files[0].EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
            {
                if (ConfirmDiscardOrSaveUnsavedChanges())
                {
                    OpenExternalStoreFileIntoEditor(files[0]);
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

    private void TrainerSectionBorder_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            if (files[0].EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
            {
                if (ConfirmDiscardOrSaveUnsavedChanges())
                {
                    OpenExternalStoreFileIntoEditor(files[0]);
                }
            }
            else
            {
                AssignTrainerExecutable(files[0]);
                StatusBannerText.Text = $"🎯 Set trainer program to {Path.GetFileName(files[0])}";
            }
            e.Handled = true;
        }
    }

    // =========================================================================
    // FIELD CHANGE, FOLDER & THEME HANDLERS
    // =========================================================================

    private void ProfilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection || ProfilesListBox.SelectedItem is not StoreFileProfile selected)
        {
            return;
        }

        if (string.Equals(selected.FilePath, _currentProfile.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

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
        if (IsLoaded)
        {
            ApplyLibraryFilter(_currentProfile.FilePath);
        }
    }

    private void LibraryCategoryFilterCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyLibraryFilter(_currentProfile.FilePath);
        }
    }

    private void TrainerLaunchesGameCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isBindingEditor)
        {
            return;
        }

        UpdateGameSectionEnabledState();
        UpdatePathValidationBadges();
        SetDirtyState(true);
    }

    private void EditorField_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            SetDirtyState(true);
        }
    }

    private void EditorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            SetDirtyState(true);
        }
    }

    private void ProfileCategoryCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            SetDirtyState(true);
        }
    }

    private void ProfileCategoryCombo_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            SetDirtyState(true);
        }
    }

    private void LaunchDelayCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            SetDirtyState(true);
        }
    }

    private void GamePathBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            UpdatePathValidationBadges();
            SetDirtyState(true);
        }
    }

    private void TrainerPathBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isBindingEditor)
        {
            UpdatePathValidationBadges();
            SetDirtyState(true);
        }
    }

    private void OpenLibraryFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_libraryFolder);
        OpenFolderInExplorer(_libraryFolder);
    }

    private void ChangeLibraryFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardOrSaveUnsavedChanges())
        {
            return;
        }

        var dlg = new OpenFolderDialog
        {
            Title = "Choose Your My StoreFiles Library Folder",
            InitialDirectory = Directory.Exists(_libraryFolder) ? _libraryFolder : string.Empty
        };

        if (dlg.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(dlg.FolderName))
        {
            _libraryFolder = dlg.FolderName;
            _service.SetLibraryFolder(_libraryFolder);
            ActiveLibraryPathText.Text = $"📂 Library: {_libraryFolder}";
            ReloadLibraryList();
            StatusBannerText.Text = $"📁 Switched library folder to {_libraryFolder}";
        }
    }

    private void OpenGameFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dir = Path.GetDirectoryName(GamePathBox.Text.Trim());
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        {
            OpenFolderInExplorer(dir);
        }
    }

    private void OpenTrainerFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dir = Path.GetDirectoryName(TrainerPathBox.Text.Trim());
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        {
            OpenFolderInExplorer(dir);
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
        RaggieSoftWpfDialogs.ShowAboutAndLegalDialog(this, "RaggieSoft StoreTrainer 2.0", "RaggieSoft Media");
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
        catch
        {
            // Ignore shell launch error
        }
    }

    private static string MakeSafeFileName(string raw)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(raw.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "MyGame" : cleaned;
    }
}
