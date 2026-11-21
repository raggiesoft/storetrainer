// ================================================================================
// FILE: StoreFileProfile.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// FORMAT: '.stfx' (Structured JSON StoreTrainer Profile in 'Documents\StoreFiles\')
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Artwork)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is a '.stfx' file, and why did StoreTrainer 2.0 evolve from '.stf' to '.stfx'?
//     - Classic StoreTrainer 1.0 & 1.1 (built in Visual Basic 6.0 for Windows 95–2000)
//       saved profiles as flat INI files ('My Documents\My StoreFiles\*.stf').
//     - Just as Microsoft evolved '.doc' -> '.docx', '.sln' -> '.slnx', and 'My Documents'
//       -> 'Documents', StoreTrainer 2.0 stores modern profiles as structured JSON files
//       with the '.stfx' extension inside 'Documents\StoreFiles\'!
//     - Keeping '.stfx' separate from '.stf' means our **StoreFile Upgrade Wizard**
//       imports classic '.stf' profiles into 'Documents\StoreFiles\*.stfx' while leaving
//       your original 'My StoreFiles\*.stf' files 100% untouched!
//
//   What rich data can a single '.stfx' JSON file hold?
//     1. Multiple Companion Trainers per Game ('Trainers' list — e.g., a +5 trainer
//        for v1.0 and a +12 trainer for v1.04, plus '.zip' archive origin, '.nfo' path,
//        and trusted SHA-256 file snapshot!).
//     2. Structured Hotkey Cheat Table ('Hotkeys' list — e.g., 'Numpad 1' -> 'Unlimited
//        Health') powering the floating Always-On-Top Cheat Card!
//     3. Pre-Launch Save-Game Backup ("Trainer Insurance") folder & rolling snapshot cap.
//     4. Process & Retro Compatibility Controls: Auto-close trainer when the game exits,
//        Wait for first window ready, 0–30s launch delay, and CPU Core Limiter (1 or 2
//        cores for classic 1990s/2000s games on modern multi-core CPUs).
//     5. Automatic Playtime & Launch Counter tracking.
// ================================================================================

using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace StoreTrainer.Models;

/// <summary>
/// One companion trainer entry attached to a <see cref="StoreFileProfile"/>.
/// A single game can store multiple trainers (for example, different game patch versions).
/// </summary>
public sealed class TrainerEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    [JsonPropertyName("label")]
    public string Label { get; set; } = "Primary Trainer";

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("switchArgs")]
    public string Switch { get; set; } = string.Empty;

    [JsonPropertyName("workingDirectory")]
    public string WorkingDirectory { get; set; } = string.Empty;

    // Optional companion '.nfo' / 'file_id.diz' / 'readme.txt' extracted from the trainer ZIP.
    [JsonPropertyName("nfoPath")]
    public string NfoPath { get; set; } = string.Empty;

    [JsonPropertyName("verifyBeforeLaunch")]
    public bool VerifyBeforeLaunch { get; set; }

    [JsonPropertyName("sha256Fingerprint")]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonIgnore]
    public string ComboDisplayLabel =>
        string.IsNullOrWhiteSpace(Path)
            ? $"{Label} (no file selected)"
            : $"{Label} — {System.IO.Path.GetFileName(Path)}";

    public TrainerEntry Clone() => new()
    {
        Id = Id,
        Label = Label,
        Path = Path,
        Switch = Switch,
        WorkingDirectory = WorkingDirectory,
        NfoPath = NfoPath,
        VerifyBeforeLaunch = VerifyBeforeLaunch,
        Fingerprint = Fingerprint
    };
}

/// <summary>
/// One shortcut row displayed in the profile editor and floating Hotkey Cheat Card.
/// </summary>
public sealed class HotkeyItem
{
    [JsonPropertyName("keys")]
    public string Keys { get; set; } = string.Empty;

    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;

    public HotkeyItem Clone() => new() { Keys = Keys, Effect = Effect };
}

/// <summary>
/// Modern StoreTrainer 2.0 JSON profile (<c>.stfx</c>).
/// </summary>
public sealed class StoreFileProfile
{
    // On-disk path (e.g., 'C:\Users\You\Documents\StoreFiles\RedAlert2.stfx').
    // Marked [JsonIgnore] so moving or renaming a '.stfx' file never leaves stale paths inside the JSON!
    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;

    // Extracted game executable icon cached at runtime for the left-hand library list.
    [JsonIgnore]
    public ImageSource? GameIconSource { get; set; }

    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "2.0";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }

    // --- Launch Sequence & Retro Compatibility Settings ---
    [JsonPropertyName("trainerLaunchesGame")]
    public bool TrainerLaunchesGame { get; set; }

    [JsonPropertyName("launchGameFirst")]
    public bool LaunchGameFirst { get; set; }

    [JsonPropertyName("waitForGameReady")]
    public bool WaitForGameReady { get; set; } = true;

    [JsonPropertyName("launchDelaySeconds")]
    public int LaunchDelaySeconds { get; set; }

    // Automatically closes the companion trainer process when the player exits the game!
    [JsonPropertyName("autoCloseTrainerOnGameExit")]
    public bool AutoCloseTrainerOnGameExit { get; set; } = true;

    // CPU Core Limiter for classic 1990s/2000s games: 0 = All Cores, 1 = Single Core, 2 = Dual Core.
    [JsonPropertyName("cpuCoreLimit")]
    public int CpuCoreLimit { get; set; }

    // --- Game Executable Configuration ---
    [JsonPropertyName("gamePath")]
    public string GamePath { get; set; } = string.Empty;

    [JsonPropertyName("gameSwitch")]
    public string GameSwitch { get; set; } = string.Empty;

    [JsonPropertyName("gameWorkingDirectory")]
    public string GameWorkingDirectory { get; set; } = string.Empty;

    // --- Companion Trainers List (Supports 1 or Many Trainers per Game!) ---
    [JsonPropertyName("activeTrainerId")]
    public string ActiveTrainerId { get; set; } = string.Empty;

    [JsonPropertyName("trainers")]
    public List<TrainerEntry> Trainers { get; set; } = [new TrainerEntry()];

    // --- Pre-Launch Save-Game Backup ("Trainer Insurance") ---
    [JsonPropertyName("saveFolderPath")]
    public string SaveFolderPath { get; set; } = string.Empty;

    [JsonPropertyName("backupSavesBeforeLaunch")]
    public bool BackupSavesBeforeLaunch { get; set; }

    [JsonPropertyName("maxSaveBackups")]
    public int MaxSaveBackups { get; set; } = 5;

    // --- Hotkey Cheat Sheet & Personal Notes ---
    [JsonPropertyName("hotkeys")]
    public List<HotkeyItem> Hotkeys { get; set; } = [];

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    // --- Playtime & Launch History ---
    [JsonPropertyName("lastPlayedUtc")]
    public DateTime? LastPlayedUtc { get; set; }

    [JsonPropertyName("totalPlayMinutes")]
    public int TotalPlayMinutes { get; set; }

    [JsonPropertyName("launchCount")]
    public int LaunchCount { get; set; }

    // Returns (and creates if empty) the currently selected companion trainer.
    public TrainerEntry GetOrCreateActiveTrainer()
    {
        if (Trainers.Count == 0)
        {
            var created = new TrainerEntry();
            Trainers.Add(created);
            ActiveTrainerId = created.Id;
            return created;
        }

        var match = Trainers.FirstOrDefault(t =>
            string.Equals(t.Id, ActiveTrainerId, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return match;
        }

        ActiveTrainerId = Trainers[0].Id;
        return Trainers[0];
    }

    // --- Computed Read-Only Properties Bound by MainWindow.xaml ---

    [JsonIgnore]
    public bool IsUnsavedNew => string.IsNullOrWhiteSpace(FilePath);

    [JsonIgnore]
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

    [JsonIgnore]
    public string FileNameBadge =>
        string.IsNullOrWhiteSpace(FilePath) ? "unsaved .stfx" : Path.GetFileName(FilePath);

    [JsonIgnore]
    public string SubtitleSummary
    {
        get
        {
            var star = IsFavorite ? "★ " : string.Empty;
            var cat = string.IsNullOrWhiteSpace(Category) ? "General" : Category.Trim();
            var playInfo = TotalPlayMinutes > 0
                ? $" • {FormatPlaytime(TotalPlayMinutes)}"
                : LaunchCount > 0
                    ? $" • Played {LaunchCount}x"
                    : string.Empty;
            return $"{star}{cat} • {Trainers.Count(t => !string.IsNullOrWhiteSpace(t.Path))} trainer(s){playInfo}";
        }
    }

    [JsonIgnore]
    public string PlaytimeSummaryLabel
    {
        get
        {
            if (LaunchCount == 0 && TotalPlayMinutes == 0)
            {
                return "Not launched yet";
            }

            var lastStr = LastPlayedUtc.HasValue
                ? $" • Last played {LastPlayedUtc.Value.ToLocalTime():MMM d, yyyy}"
                : string.Empty;
            return $"Played {FormatPlaytime(TotalPlayMinutes)} across {LaunchCount} session(s){lastStr}";
        }
    }

    private static string FormatPlaytime(int totalMinutes)
    {
        if (totalMinutes <= 0) return "< 1 min";
        if (totalMinutes < 60) return $"{totalMinutes} min";
        var hrs = totalMinutes / 60;
        var mins = totalMinutes % 60;
        return mins == 0 ? $"{hrs} hr" : $"{hrs} hr {mins} min";
    }

    public StoreFileProfile Clone() => new()
    {
        FilePath = FilePath,
        GameIconSource = GameIconSource,
        SchemaVersion = SchemaVersion,
        DisplayName = DisplayName,
        Category = Category,
        IsFavorite = IsFavorite,
        TrainerLaunchesGame = TrainerLaunchesGame,
        LaunchGameFirst = LaunchGameFirst,
        WaitForGameReady = WaitForGameReady,
        LaunchDelaySeconds = LaunchDelaySeconds,
        AutoCloseTrainerOnGameExit = AutoCloseTrainerOnGameExit,
        CpuCoreLimit = CpuCoreLimit,
        GamePath = GamePath,
        GameSwitch = GameSwitch,
        GameWorkingDirectory = GameWorkingDirectory,
        ActiveTrainerId = ActiveTrainerId,
        Trainers = Trainers.Select(t => t.Clone()).ToList(),
        SaveFolderPath = SaveFolderPath,
        BackupSavesBeforeLaunch = BackupSavesBeforeLaunch,
        MaxSaveBackups = MaxSaveBackups,
        Hotkeys = Hotkeys.Select(h => h.Clone()).ToList(),
        Notes = Notes,
        LastPlayedUtc = LastPlayedUtc,
        TotalPlayMinutes = TotalPlayMinutes,
        LaunchCount = LaunchCount
    };
}
