// ================================================================================
// FILE: StoreFileProfile.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Creative Content)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is a StoreFile ('.stf'), and what does this class represent?
//     In RaggieSoft StoreTrainer, every game + trainer pairing is saved in your
//     'Documents\My StoreFiles' folder as a plain-text '.stf' file structured in
//     classic Windows INI sections ('[General]', '[Game]', and '[Trainer]').
//
//     Here is what an actual '.stf' file looks like on disk:
//       [General]
//       STF.Version=1.1
//       DisplayName=Command & Conquer: Red Alert 2
//       Category=Strategy
//       Favorite=true
//       LaunchDelay=3
//       WaitForGameReady=true
//       Notes=Press F1 on the main menu to enable unlimited credits.
//
//       [Game]
//       LaunchGameFirst=true
//       Path=C:\Games\RA2\ra2.exe
//       Switch=-win
//       WorkingDirectory=
//
//       [Trainer]
//       TrainerLaunchesGame=false
//       Path=C:\Games\Trainers\ra2_plus5.exe
//       Switch=
//       WorkingDirectory=
//       Fingerprint=A1B2C3...
//
//   How does StoreTrainer 2.0 stay 100% backward-compatible with VB6 v1.0 & v1.1?
//     - Classic StoreTrainer v1.0 & v1.1 read '[General] STF.Version', '[Game]
//       LaunchGameFirst / Path / Switch', and '[Trainer] TrainerLaunchesGame /
//       Path / Switch' (plus 'LaunchDelay' in v1.1), ignoring any extra INI keys!
//     - StoreTrainer 2.0 writes 'STF.Version=1.1' and keeps every single classic
//       section and key name identical, so you can share the exact same 'My StoreFiles'
//       folder across a Windows 98 / Windows 2000 retro PC running VB6 v1.1 and a
//       modern PC running StoreTrainer 2.0!
// ================================================================================

using System.IO; // 'Path.GetFileName' and 'Path.GetFileNameWithoutExtension'.

namespace StoreTrainer.Models;

/// <summary>
/// Represents a single saved (or unsaved draft) game-and-trainer profile (<c>.stf</c>).
/// </summary>
public sealed class StoreFileProfile
{
    // Full path on disk (e.g., 'C:\Users\You\Documents\My StoreFiles\RedAlert2.stf'),
    // or empty string '""' when creating a brand-new unsaved profile.
    public string FilePath { get; set; } = string.Empty;

    // File format version written to '[General] STF.Version'. Kept at '1.1' so classic
    // VB6 StoreTrainer v1.1 can also open profiles saved by StoreTrainer 2.0!
    public string FormatVersion { get; set; } = "1.1";

    // Friendly title shown in the StoreTrainer 2.0 sidebar list (falls back to filename if blank).
    public string DisplayName { get; set; } = string.Empty;

    // Optional organizer tag ('Action', 'RPG', 'Strategy', 'Retro PC', 'Simulation', etc.).
    public string Category { get; set; } = "General";

    // Pins favorite games to the top of the library list when starred.
    public bool IsFavorite { get; set; }

    // --- Launch Order & Timing Controls ---
    // True when the trainer itself starts the game executable (disables separate game launch).
    public bool TrainerLaunchesGame { get; set; }

    // True when the game must open BEFORE the trainer attaches to it.
    public bool LaunchGameFirst { get; set; }

    // Modern 2.0 enhancement: waits until the first program's main window finishes loading
    // ('Process.WaitForInputIdle') before counting down 'LaunchDelaySeconds'.
    public bool WaitForGameReady { get; set; } = true;

    // Optional pause (0 to 30 seconds) between starting the first and second program.
    public int LaunchDelaySeconds { get; set; }

    // Free-form hotkey reminders (e.g., "Numpad 1 = Infinite Health, Numpad 2 = Ammo").
    public string Notes { get; set; } = string.Empty;

    // --- Game Program Settings ---
    public string GamePath { get; set; } = string.Empty;
    public string GameSwitch { get; set; } = string.Empty;
    // Optional custom folder override; if blank, defaults to the folder containing GamePath.
    public string GameWorkingDirectory { get; set; } = string.Empty;

    // --- Companion Trainer Program Settings ---
    public string TrainerPath { get; set; } = string.Empty;
    public string TrainerSwitch { get; set; } = string.Empty;
    // Optional custom folder override; if blank, defaults to the folder containing TrainerPath.
    public string TrainerWorkingDirectory { get; set; } = string.Empty;

    // Optional file-verification fingerprint so StoreTrainer warns if a trainer file on disk
    // was unexpectedly modified or replaced since the user pinned it.
    public bool VerifyTrainerBeforeLaunch { get; set; }
    public string TrainerFingerprint { get; set; } = string.Empty;

    // --- Computed Read-Only Properties Bound Directly by MainWindow.xaml ---

    // True if this profile has never been saved to a '.stf' file on disk yet.
    public bool IsUnsavedNew => string.IsNullOrWhiteSpace(FilePath);

    // Friendly card title shown in the left library list.
    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return DisplayName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(FilePath))
            {
                return Path.GetFileNameWithoutExtension(FilePath);
            }

            return "Untitled Game Profile";
        }
    }

    // Compact filename pill shown on each sidebar card (e.g., 'redalert2.stf').
    public string FileNameBadge =>
        string.IsNullOrWhiteSpace(FilePath) ? "unsaved draft" : Path.GetFileName(FilePath);

    // One-line summary shown beneath the title in the left library list.
    public string SubtitleSummary
    {
        get
        {
            var star = IsFavorite ? "★ Favorite • " : string.Empty;
            var cat = string.IsNullOrWhiteSpace(Category) ? "General" : Category.Trim();
            var mode = TrainerLaunchesGame
                ? "Trainer Starts Game"
                : LaunchGameFirst
                    ? "Game First → Trainer"
                    : "Game + Trainer";
            return $"{star}{cat} • {mode}";
        }
    }

    /// <summary>
    /// Creates an independent deep copy of this profile (used for Duplicate Profile and change tracking).
    /// </summary>
    public StoreFileProfile Clone() => new()
    {
        FilePath = FilePath,
        FormatVersion = FormatVersion,
        DisplayName = DisplayName,
        Category = Category,
        IsFavorite = IsFavorite,
        TrainerLaunchesGame = TrainerLaunchesGame,
        LaunchGameFirst = LaunchGameFirst,
        WaitForGameReady = WaitForGameReady,
        LaunchDelaySeconds = LaunchDelaySeconds,
        Notes = Notes,
        GamePath = GamePath,
        GameSwitch = GameSwitch,
        GameWorkingDirectory = GameWorkingDirectory,
        TrainerPath = TrainerPath,
        TrainerSwitch = TrainerSwitch,
        TrainerWorkingDirectory = TrainerWorkingDirectory,
        VerifyTrainerBeforeLaunch = VerifyTrainerBeforeLaunch,
        TrainerFingerprint = TrainerFingerprint
    };
}
