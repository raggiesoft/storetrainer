// ================================================================================
// FILE: StoreTrainerService.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Artwork)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What does this service class do?
//     Think of 'StoreTrainerService' as the engine under the hood of StoreTrainer 2.0.
//     While 'MainWindow.xaml' draws the buttons and text boxes, this class performs
//     every file, archive, icon, backup, shortcut, and process-launching operation:
//
//     1. Modern '.stfx' JSON Library ('Documents\StoreFiles\*.stfx'):
//        Reads and writes structured JSON game profiles using .NET 10's built-in
//        'System.Text.Json' serializer, and stores app preferences in
//        '%APPDATA%\RaggieSoft\storetrainer-preferences.json'.
//
//     2. Non-Destructive Classic '.stf' Reader & Upgrade Engine:
//        Reads classic StoreTrainer 1.0 & 1.1 INI files ('My Documents\My StoreFiles\*.stf')
//        and converts them into '.stfx' JSON files inside 'Documents\StoreFiles\'
//        WITHOUT ever modifying or deleting your original '.stf' files!
//
//     3. Trainer '.ZIP' Archive Inspector & Windows UAC Elevated Extractor:
//        Inspects '.zip' trainer downloads for '.exe' and '.nfo'/'file_id.diz' files,
//        guards against Zip-Slip path traversal, and extracts either into
//        'Documents\StoreFiles\Trainers\<Game>\' OR directly into the game's install
//        folder — automatically requesting Windows UAC administrator approval
//        ('Verb = "runas"') when a game lives inside 'C:\Program Files'!
//
//     4. Native Windows Game Icon Extraction ('ExtractAssociatedIconW'):
//        Pulls the real embedded icon straight out of each game's '.exe' file so
//        your library sidebar displays genuine game icons!
//
//     5. Pre-Launch Save-Game Backup & 1-Click Rollback ("Trainer Insurance"):
//        Creates rolling '.zip' snapshots of a game's save folder before launching
//        with a trainer, and restores any snapshot in one click.
//
//     6. Smart Launch Sequencer, CPU Core Limiter & Auto-Close Trainer:
//        Supports both "Launch with Trainer" and "Launch Clean (No Trainer)",
//        applies 1-core / 2-core CPU affinity for vintage 1990s/2000s games, tracks
//        playtime, and automatically closes the companion trainer when the game exits!
// ================================================================================

using System.ComponentModel;             // 'Win32Exception' (catches UAC cancel code 1223 cleanly).
using System.Diagnostics;                // 'Process' and 'ProcessStartInfo' for launching games, trainers, and UAC helpers.
using System.IO;                         // 'File', 'Directory', and 'Path' filesystem utilities.
using System.IO.Compression;             // 'ZipFile' & 'ZipArchive' for trainer '.zip' extraction and save-game backups.
using System.Reflection;                 // 'BindingFlags' for late-bound Windows 'WScript.Shell' '.lnk' shortcut creation.
using System.Runtime.InteropServices;    // '[DllImport]' for Win32 'ExtractAssociatedIconW' and 'DestroyIcon'.
using System.Security.Cryptography;      // 'SHA256' for trusted trainer file safety snapshots.
using System.Text;                       // 'Encoding' and 'StringBuilder'.
using System.Text.Json;                  // 'JsonSerializer' for '.stfx' JSON profiles and user preferences.
using System.Windows;                    // 'Int32Rect' for WPF icon conversion.
using System.Windows.Interop;            // 'Imaging.CreateBitmapSourceFromHIcon' for Win32 HICON -> WPF ImageSource.
using System.Windows.Media;              // 'ImageSource' bound in the sidebar DataTemplate.
using System.Windows.Media.Imaging;      // 'BitmapSizeOptions'.
using StoreTrainer.Models;               // 'StoreFileProfile', 'TrainerEntry', and 'HotkeyItem'.

namespace StoreTrainer.Services;

#region Result & DTO Records

/// <summary>
/// Result returned when loading either a modern <c>.stfx</c> JSON profile or a classic <c>.stf</c> INI file.
/// </summary>
public sealed record StoreFileLoadResult(
    bool Success,
    StoreFileProfile? Profile,
    bool IsClassicStfUpgradeCandidate,
    string ErrorMessage = "");

/// <summary>
/// Summary row produced by the StoreFile Upgrade Wizard for each classic <c>.stf</c> file converted to <c>.stfx</c>.
/// </summary>
public sealed record StfUpgradeReportItem(
    string SourceStfPath,
    string OutputStfxPath,
    string DisplayTitle,
    bool Success,
    IReadOnlyList<string> Notes);

/// <summary>
/// Contents discovered inside a trainer <c>.zip</c> archive before extraction.
/// </summary>
public sealed record TrainerZipInspection(
    bool Success,
    IReadOnlyList<string> ExecutableEntries,
    IReadOnlyList<string> ReadmeAndNfoEntries,
    int TotalFileCount,
    string ErrorMessage = "");

/// <summary>
/// Result of extracting a trainer <c>.zip</c> archive (directly or via Windows UAC elevation).
/// </summary>
public sealed record TrainerZipExtractResult(
    bool Success,
    bool UsedElevation,
    string DestinationDirectory,
    string ExtractedTrainerPath,
    string ExtractedNfoPath,
    string Message);

/// <summary>
/// Metadata for one timestamped save-game backup <c>.zip</c> snapshot.
/// </summary>
public sealed record SaveBackupInfo(
    string ZipFilePath,
    string FileName,
    DateTime CreatedLocalTime,
    long SizeBytes)
{
    public string FriendlyLabel =>
        $"{CreatedLocalTime:MMM d, yyyy  h:mm:ss tt}  ({FormatSize(SizeBytes)})";

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}

/// <summary>
/// Result returned after launching a profile's game and/or companion trainer.
/// </summary>
public sealed record StoreTrainerLaunchResult(
    bool Success,
    int ErrorCode,
    string Message,
    Task? SessionMonitorTask = null);

#endregion

public sealed class StoreTrainerService
{
    // Shared JSON serializer settings: pretty-printed 2-space indentation and case-insensitive property matching.
    private static readonly JsonSerializerOptions StfxJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    // Private preferences JSON stored at '%APPDATA%\RaggieSoft\storetrainer-preferences.json'.
    private sealed class StoreTrainerUserPreferences
    {
        public string LibraryFolder { get; set; } = string.Empty;
        public string Theme { get; set; } = "System";
        public bool DismissedClassicImportBanner { get; set; }
    }

    // Native Win32 P/Invoke declarations (called ONLY when OperatingSystem.IsWindows() is true)
    // to pull embedded icons out of '.exe' game binaries on Windows.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractAssociatedIconW(IntPtr hInst, StringBuilder lpIconPath, ref ushort lpiIcon);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // =========================================================================
    // 1. LIBRARY PATHS & USER PREFERENCES ('Documents\StoreFiles' + JSON Prefs)
    // =========================================================================

    private static string GetPreferencesFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "RaggieSoft");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "storetrainer-preferences.json");
    }

    private StoreTrainerUserPreferences ReadPreferences()
    {
        try
        {
            var path = GetPreferencesFilePath();
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<StoreTrainerUserPreferences>(File.ReadAllText(path), StfxJsonOptions)
                       ?? new StoreTrainerUserPreferences();
            }
        }
        catch
        {
            // Return defaults if unreadable
        }
        return new StoreTrainerUserPreferences();
    }

    private void WritePreferences(StoreTrainerUserPreferences prefs)
    {
        try
        {
            File.WriteAllText(GetPreferencesFilePath(), JsonSerializer.Serialize(prefs, StfxJsonOptions));
        }
        catch
        {
            // Ignore write errors on read-only media
        }
    }

    /// <summary>
    /// Returns the modern default StoreTrainer 2.0 library directory: <c>Documents\StoreFiles</c>.
    /// </summary>
    public string GetDefaultStoreFilesFolder()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "StoreFiles");
    }

    /// <summary>
    /// Returns the classic StoreTrainer 1.0/1.1 directory (<c>Documents\My StoreFiles</c>),
    /// honoring any custom <c>StoreFileDir</c> saved in classic <c>%APPDATA%\RaggieSoft\StoreTrainer\TStore.ini</c>.
    /// </summary>
    public string GetClassicMyStoreFilesFolder()
    {
        try
        {
            var classicIni = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RaggieSoft",
                "StoreTrainer",
                "TStore.ini");
            if (File.Exists(classicIni))
            {
                var sections = ParseIniLines(File.ReadAllLines(classicIni));
                if (TryGetIniValue(sections, "General", "StoreFileDir", out var customDir) &&
                    !string.IsNullOrWhiteSpace(customDir) &&
                    Directory.Exists(customDir))
                {
                    return customDir;
                }
            }
        }
        catch
        {
            // Fall back to standard My StoreFiles path
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My StoreFiles");
    }

    /// <summary>
    /// Ensures the active <c>StoreFiles</c> folder exists on disk and returns its path.
    /// </summary>
    public string EnsureLibraryFolder()
    {
        var prefs = ReadPreferences();
        var folder = !string.IsNullOrWhiteSpace(prefs.LibraryFolder)
            ? prefs.LibraryFolder
            : GetDefaultStoreFilesFolder();

        Directory.CreateDirectory(folder);
        return folder;
    }

    public void SaveCustomLibraryFolder(string folderPath)
    {
        var prefs = ReadPreferences();
        prefs.LibraryFolder = folderPath;
        WritePreferences(prefs);
    }

    public string GetSavedThemePreference() => ReadPreferences().Theme;

    public void SaveThemePreference(string theme)
    {
        var prefs = ReadPreferences();
        prefs.Theme = theme;
        WritePreferences(prefs);
    }

    public bool HasDismissedClassicImportBanner() => ReadPreferences().DismissedClassicImportBanner;

    public void SetDismissedClassicImportBanner(bool dismissed)
    {
        var prefs = ReadPreferences();
        prefs.DismissedClassicImportBanner = dismissed;
        WritePreferences(prefs);
    }

    // =========================================================================
    // 2. '.STFX' JSON PROFILE LOADING, SAVING & NATIVE ICON EXTRACTION
    // =========================================================================

    /// <summary>
    /// Loads every <c>.stfx</c> profile in <paramref name="libraryFolder"/>, sorted with
    /// Favorites pinned at the top and alphabetical by title underneath.
    /// </summary>
    public List<StoreFileProfile> LoadLibraryProfiles(string libraryFolder)
    {
        var profiles = new List<StoreFileProfile>();
        if (string.IsNullOrWhiteSpace(libraryFolder) || !Directory.Exists(libraryFolder))
        {
            return profiles;
        }

        foreach (var file in Directory.EnumerateFiles(libraryFolder, "*.stfx", SearchOption.TopDirectoryOnly))
        {
            var res = LoadStoreFile(file);
            if (res.Success && res.Profile is not null)
            {
                profiles.Add(res.Profile);
            }
        }

        return profiles
            .OrderByDescending(p => p.IsFavorite)
            .ThenBy(p => p.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Reads either a modern <c>.stfx</c> JSON file or a classic <c>.stf</c> INI file.
    /// </summary>
    public StoreFileLoadResult LoadStoreFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new StoreFileLoadResult(false, null, false, $"Profile file not found:\n{filePath}");
        }

        // Classic '.stf' INI detection -> parse non-destructively and mark as Upgrade Wizard candidate!
        if (filePath.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
        {
            return ParseClassicStfFile(filePath);
        }

        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var profile = JsonSerializer.Deserialize<StoreFileProfile>(json, StfxJsonOptions);
            if (profile is null)
            {
                return new StoreFileLoadResult(false, null, false, "This .stfx profile appears to be empty.");
            }

            profile.FilePath = filePath;
            _ = profile.GetOrCreateActiveTrainer();
            profile.GameIconSource = TryExtractExecutableIcon(profile.GamePath);
            return new StoreFileLoadResult(true, profile, IsClassicStfUpgradeCandidate: false);
        }
        catch (Exception ex)
        {
            return new StoreFileLoadResult(
                false,
                null,
                false,
                $"Could not read '{Path.GetFileName(filePath)}':\n{ex.Message}");
        }
    }

    /// <summary>
    /// Writes <paramref name="profile"/> to <paramref name="targetPath"/> in clean <c>.stfx</c> JSON format.
    /// </summary>
    public void SaveStoreFile(StoreFileProfile profile, string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        profile.SchemaVersion = "2.0";
        _ = profile.GetOrCreateActiveTrainer();

        var json = JsonSerializer.Serialize(profile, StfxJsonOptions);
        File.WriteAllText(targetPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        profile.FilePath = targetPath;
        profile.GameIconSource = TryExtractExecutableIcon(profile.GamePath);
    }

    /// <summary>
    /// Extracts the native Windows icon embedded inside a game <c>.exe</c> file so the library
    /// sidebar shows each game's real artwork! Returns <c>null</c> cleanly if unavailable.
    /// </summary>
    public ImageSource? TryExtractExecutableIcon(string? exePath)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return null;
        }

        var hIcon = IntPtr.Zero;
        try
        {
            var sb = new StringBuilder(exePath, 512);
            ushort iconIndex = 0;
            hIcon = ExtractAssociatedIconW(IntPtr.Zero, sb, ref iconIndex);
            if (hIcon == IntPtr.Zero)
            {
                return null;
            }

            var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
            {
                _ = DestroyIcon(hIcon);
            }
        }
    }

    // =========================================================================
    // 3. CLASSIC '.STF' (1.0 / 1.1) DISCOVERY & STOREFILE UPGRADE WIZARD ENGINE
    // =========================================================================

    /// <summary>
    /// Finds classic <c>.stf</c> files in <c>Documents\My StoreFiles</c> that have not yet been
    /// imported into <paramref name="modernStoreFilesFolder"/> as <c>.stfx</c> profiles.
    /// </summary>
    public List<string> FindUnmigratedClassicStfFiles(string modernStoreFilesFolder)
    {
        var unmigrated = new List<string>();
        var classicFolder = GetClassicMyStoreFilesFolder();
        if (!Directory.Exists(classicFolder))
        {
            return unmigrated;
        }

        foreach (var stfPath in Directory.EnumerateFiles(classicFolder, "*.stf", SearchOption.TopDirectoryOnly))
        {
            var baseName = Path.GetFileNameWithoutExtension(stfPath);
            var matchingStfx = Path.Combine(modernStoreFilesFolder, $"{baseName}.stfx");
            if (!File.Exists(matchingStfx))
            {
                unmigrated.Add(stfPath);
            }
        }

        return unmigrated.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Parses a classic VB6 StoreTrainer 1.0 or 1.1 <c>.stf</c> INI file into a modern
    /// <see cref="StoreFileProfile"/> in memory without modifying the <c>.stf</c> file.
    /// </summary>
    public StoreFileLoadResult ParseClassicStfFile(string stfPath)
    {
        try
        {
            // Classic VB6 wrote Windows-1252 / ANSI; UTF-8 with fallback handles all ASCII & extended paths cleanly.
            var lines = File.ReadAllLines(stfPath);
            var sections = ParseIniLines(lines);

            if (!TryGetIniValue(sections, "StoreFile", "STF.Version", out var stfVer) ||
                (stfVer != "1.0" && stfVer != "1.1"))
            {
                return new StoreFileLoadResult(
                    false,
                    null,
                    true,
                    $"'{Path.GetFileName(stfPath)}' does not contain a valid classic [StoreFile] header.");
            }

            _ = TryGetIniValue(sections, "StoreFile", "LaunchTrainerOnly", out var launchTrainerOnlyRaw);
            _ = TryGetIniValue(sections, "StoreFile", "LaunchGameFirst", out var launchGameFirstRaw);
            _ = TryGetIniValue(sections, "StoreFile", "LaunchDelay", out var launchDelayRaw);
            _ = TryGetIniValue(sections, "StoreFile", "WaitForGameReady", out var waitReadyRaw);

            _ = TryGetIniValue(sections, "Game", "Path", out var gamePath);
            _ = TryGetIniValue(sections, "Game", "Switch", out var gameSwitch);
            _ = TryGetIniValue(sections, "Game", "WorkingDirectory", out var gameWorkDir);

            _ = TryGetIniValue(sections, "Trainer", "Path", out var trainerPath);
            _ = TryGetIniValue(sections, "Trainer", "Switch", out var trainerSwitch);
            _ = TryGetIniValue(sections, "Trainer", "WorkingDirectory", out var trainerWorkDir);

            _ = TryGetIniValue(sections, "StoreTrainer2", "DisplayName", out var displayName);
            _ = TryGetIniValue(sections, "StoreTrainer2", "Category", out var category);
            _ = TryGetIniValue(sections, "StoreTrainer2", "Notes", out var notes);

            var trainerStartsGame = ParseClassicBool(launchTrainerOnlyRaw);
            var launchGameFirst = !trainerStartsGame && ParseClassicBool(launchGameFirstRaw);
            _ = int.TryParse(launchDelayRaw, out var delaySeconds);
            var waitReady = string.IsNullOrWhiteSpace(waitReadyRaw) || ParseClassicBool(waitReadyRaw);

            var inferredTitle = !string.IsNullOrWhiteSpace(displayName)
                ? displayName.Trim()
                : Path.GetFileNameWithoutExtension(stfPath);

            var primaryTrainer = new TrainerEntry
            {
                Label = "Imported Classic Trainer",
                Path = (trainerPath ?? string.Empty).Trim(),
                Switch = (trainerSwitch ?? string.Empty).Trim(),
                WorkingDirectory = (trainerWorkDir ?? string.Empty).Trim()
            };

            var profile = new StoreFileProfile
            {
                SchemaVersion = "2.0",
                DisplayName = inferredTitle,
                Category = string.IsNullOrWhiteSpace(category) ? "Retro & Classic PC" : category.Trim(),
                TrainerLaunchesGame = trainerStartsGame,
                LaunchGameFirst = launchGameFirst,
                WaitForGameReady = waitReady,
                LaunchDelaySeconds = Math.Clamp(delaySeconds, 0, 120),
                GamePath = (gamePath ?? string.Empty).Trim(),
                GameSwitch = (gameSwitch ?? string.Empty).Trim(),
                GameWorkingDirectory = (gameWorkDir ?? string.Empty).Trim(),
                ActiveTrainerId = primaryTrainer.Id,
                Trainers = [primaryTrainer],
                Notes = (notes ?? string.Empty).Replace(@"\n", Environment.NewLine)
            };

            profile.GameIconSource = TryExtractExecutableIcon(profile.GamePath);
            return new StoreFileLoadResult(true, profile, IsClassicStfUpgradeCandidate: true);
        }
        catch (Exception ex)
        {
            return new StoreFileLoadResult(
                false,
                null,
                true,
                $"Could not read classic StoreFile '{Path.GetFileName(stfPath)}':\n{ex.Message}");
        }
    }

    /// <summary>
    /// Converts one classic <c>.stf</c> file into a new <c>.stfx</c> JSON file inside
    /// <paramref name="destinationStoreFilesFolder"/> while leaving the original <c>.stf</c> untouched.
    /// </summary>
    public StfUpgradeReportItem UpgradeClassicStfToStfx(string sourceStfPath, string destinationStoreFilesFolder)
    {
        var notes = new List<string>();
        var parsed = ParseClassicStfFile(sourceStfPath);
        if (!parsed.Success || parsed.Profile is null)
        {
            return new StfUpgradeReportItem(
                sourceStfPath,
                string.Empty,
                Path.GetFileNameWithoutExtension(sourceStfPath),
                Success: false,
                Notes: [parsed.ErrorMessage]);
        }

        Directory.CreateDirectory(destinationStoreFilesFolder);
        var baseName = MakeSafeFileSlug(Path.GetFileNameWithoutExtension(sourceStfPath));
        var destPath = Path.Combine(destinationStoreFilesFolder, $"{baseName}.stfx");

        // Avoid overwriting an existing .stfx silently by appending (Imported) if needed
        if (File.Exists(destPath))
        {
            var counter = 2;
            while (File.Exists(destPath))
            {
                destPath = Path.Combine(destinationStoreFilesFolder, $"{baseName} ({counter}).stfx");
                counter++;
            }
            notes.Add($"Saved alongside existing profile as '{Path.GetFileName(destPath)}'.");
        }

        var profile = parsed.Profile;
        if (!profile.TrainerLaunchesGame && !string.IsNullOrWhiteSpace(profile.GamePath) && !File.Exists(profile.GamePath))
        {
            notes.Add($"Note: Game path '{profile.GamePath}' was not found on this PC (you can update its location anytime).");
        }

        var trainer = profile.GetOrCreateActiveTrainer();
        if (!string.IsNullOrWhiteSpace(trainer.Path))
        {
            if (File.Exists(trainer.Path))
            {
                // Auto-discover any sibling .nfo or file_id.diz sitting next to the classic trainer!
                var siblingNfo = FindCompanionNfoInDirectory(Path.GetDirectoryName(trainer.Path));
                if (!string.IsNullOrWhiteSpace(siblingNfo))
                {
                    trainer.NfoPath = siblingNfo;
                    var extractedKeys = Cp437NfoHelper.ExtractHotkeysFromText(Cp437NfoHelper.ReadNfoText(siblingNfo));
                    if (extractedKeys.Count > 0)
                    {
                        profile.Hotkeys = extractedKeys;
                        notes.Add($"Auto-imported {extractedKeys.Count} shortcut key(s) from '{Path.GetFileName(siblingNfo)}'!");
                    }
                }
            }
            else
            {
                notes.Add($"Note: Trainer path '{trainer.Path}' was not found on this PC.");
            }
        }

        if (notes.Count == 0)
        {
            notes.Add("Upgraded cleanly with 0 issues (original .stf preserved untouched).");
        }

        SaveStoreFile(profile, destPath);
        return new StfUpgradeReportItem(sourceStfPath, destPath, profile.DisplayTitle, Success: true, Notes: notes);
    }

    // =========================================================================
    // 4. TRAINER '.ZIP' INSPECTOR & WINDOWS UAC ELEVATED EXTRACTOR
    // =========================================================================

    /// <summary>
    /// Inspects a <c>.zip</c> trainer archive without extracting it yet, listing all <c>.exe</c>
    /// binaries and <c>.nfo</c>/<c>.diz</c>/<c>.txt</c> documentation files inside.
    /// </summary>
    public TrainerZipInspection InspectTrainerZip(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
        {
            return new TrainerZipInspection(false, [], [], 0, "Archive file was not found on disk.");
        }

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var exes = new List<string>();
            var docs = new List<string>();
            var fileCount = 0;

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue; // Directory entry
                }

                fileCount++;
                var ext = Path.GetExtension(entry.Name);
                if (string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    exes.Add(entry.FullName);
                }
                else if (string.Equals(ext, ".nfo", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(ext, ".diz", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    docs.Add(entry.FullName);
                }
            }

            if (exes.Count == 0)
            {
                return new TrainerZipInspection(
                    false,
                    exes,
                    docs,
                    fileCount,
                    "This ZIP archive does not contain any trainer .exe program files.");
            }

            return new TrainerZipInspection(true, exes, docs, fileCount);
        }
        catch (Exception ex)
        {
            return new TrainerZipInspection(false, [], [], 0, $"Could not read ZIP archive:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Extracts a trainer <c>.zip</c> archive into <paramref name="destinationDirectory"/>.
    /// If <paramref name="destinationDirectory"/> is inside a protected location such as
    /// <c>C:\Program Files (x86)\...</c>, automatically stages the extracted files in <c>%TEMP%</c>
    /// and invokes an elevated copy helper (<c>Verb = "runas"</c>) so Windows displays a standard
    /// UAC permission prompt!
    /// </summary>
    public async Task<TrainerZipExtractResult> ExtractTrainerZipAsync(
        string zipPath,
        string destinationDirectory,
        string preferredExecutableEntry)
    {
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            return new TrainerZipExtractResult(false, false, "", "", "", "Please choose a destination folder.");
        }

        // First stage safely into a fresh %TEMP% directory (protecting against Zip-Slip paths!)
        var stageDir = Path.Combine(Path.GetTempPath(), $"RaggieSoftTrainerStage_{Guid.NewGuid():N}");
        Directory.CreateDirectory(stageDir);

        try
        {
            ExtractZipSafelyToDirectory(zipPath, stageDir);

            var usedElevation = false;
            if (CanWriteDirectlyToDirectory(destinationDirectory))
            {
                CopyDirectoryContentsRecursive(stageDir, destinationDirectory);
            }
            else
            {
                if (!OperatingSystem.IsWindows())
                {
                    return new TrainerZipExtractResult(
                        false,
                        false,
                        destinationDirectory,
                        "",
                        "",
                        "Write permission was denied for the selected destination folder.");
                }

                // Trigger native Windows UAC prompt via StoreTrainer.exe --elevated-copy "<stageDir>" "<destDir>"
                var selfExe = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(selfExe) || !File.Exists(selfExe))
                {
                    return new TrainerZipExtractResult(
                        false,
                        false,
                        destinationDirectory,
                        "",
                        "",
                        "Could not locate StoreTrainer executable for administrator permission prompt.");
                }

                try
                {
                    using var elevatedProc = Process.Start(new ProcessStartInfo
                    {
                        FileName = selfExe,
                        Arguments = $"--elevated-copy \"{stageDir}\" \"{destinationDirectory}\"",
                        Verb = "runas",
                        UseShellExecute = true
                    });

                    if (elevatedProc is null)
                    {
                        return new TrainerZipExtractResult(
                            false,
                            true,
                            destinationDirectory,
                            "",
                            "",
                            "Windows could not start the administrator file copy helper.");
                    }

                    await elevatedProc.WaitForExitAsync();
                    usedElevation = true;
                    if (elevatedProc.ExitCode != 0)
                    {
                        return new TrainerZipExtractResult(
                            false,
                            true,
                            destinationDirectory,
                            "",
                            "",
                            "Could not copy trainer files into the protected game folder.");
                    }
                }
                catch (Win32Exception w32) when (w32.NativeErrorCode == 1223)
                {
                    // Win32 error 1223 = ERROR_CANCELLED (the user clicked "No" on the Windows UAC prompt)
                    return new TrainerZipExtractResult(
                        false,
                        true,
                        destinationDirectory,
                        "",
                        "",
                        "Administrator permission was declined — no files were copied into the game folder.");
                }
            }

            // Resolve final extracted paths for the chosen trainer .exe and any companion .nfo/.diz/.txt
            var normalizedRelExe = preferredExecutableEntry.Replace('/', Path.DirectorySeparatorChar);
            var finalTrainerPath = Path.Combine(destinationDirectory, normalizedRelExe);
            if (!File.Exists(finalTrainerPath))
            {
                finalTrainerPath = Directory
                    .EnumerateFiles(destinationDirectory, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault() ?? string.Empty;
            }

            var nfoPath = FindCompanionNfoInDirectory(Path.GetDirectoryName(finalTrainerPath))
                          ?? FindCompanionNfoInDirectory(destinationDirectory)
                          ?? string.Empty;

            return new TrainerZipExtractResult(
                Success: true,
                UsedElevation: usedElevation,
                DestinationDirectory: destinationDirectory,
                ExtractedTrainerPath: finalTrainerPath,
                ExtractedNfoPath: nfoPath,
                Message: usedElevation
                    ? $"Extracted trainer into game folder (with Windows administrator permission)"
                    : $"Extracted trainer into {destinationDirectory}");
        }
        catch (Exception ex)
        {
            return new TrainerZipExtractResult(
                false,
                false,
                destinationDirectory,
                "",
                "",
                $"Could not extract trainer ZIP:\n{ex.Message}");
        }
        finally
        {
            try
            {
                if (Directory.Exists(stageDir))
                {
                    Directory.Delete(stageDir, recursive: true);
                }
            }
            catch
            {
                // Ignore temp cleanup issue
            }
        }
    }

    /// <summary>
    /// Executed inside the UAC-elevated child process (<c>StoreTrainer.exe --elevated-copy "src" "dest"</c>).
    /// Copies staged trainer files into a protected game directory and exits immediately.
    /// </summary>
    public static int PerformElevatedCopy(string sourceStagingDir, string targetGameDir)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceStagingDir) || !Directory.Exists(sourceStagingDir) ||
                string.IsNullOrWhiteSpace(targetGameDir))
            {
                return 2;
            }

            Directory.CreateDirectory(targetGameDir);
            CopyDirectoryContentsRecursive(sourceStagingDir, targetGameDir);
            return 0;
        }
        catch
        {
            return 1;
        }
    }

    private static void ExtractZipSafelyToDirectory(string zipPath, string destDir)
    {
        var fullDestRoot = Path.GetFullPath(destDir);
        if (!fullDestRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullDestRoot += Path.DirectorySeparatorChar;
        }

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var targetPath = Path.GetFullPath(Path.Combine(destDir, entry.FullName));
            // Zip-Slip security check: ensure every entry stays strictly inside destDir!
            if (!targetPath.StartsWith(fullDestRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Blocked unsafe archive entry path: {entry.FullName}");
            }

            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }
            entry.ExtractToFile(targetPath, overwrite: true);
        }
    }

    private static bool CanWriteDirectlyToDirectory(string dirPath)
    {
        try
        {
            Directory.CreateDirectory(dirPath);
            var probeFile = Path.Combine(dirPath, $".raggiesoft_write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probeFile, "");
            File.Delete(probeFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void CopyDirectoryContentsRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSub = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectoryContentsRecursive(subDir, destSub);
        }
    }

    public static string? FindCompanionNfoInDirectory(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            return null;
        }

        // Priority 1: *.nfo -> Priority 2: file_id.diz / *.diz -> Priority 3: *readme*.txt
        var nfo = Directory.EnumerateFiles(dir, "*.nfo", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (nfo is not null) return nfo;

        var diz = Directory.EnumerateFiles(dir, "*.diz", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (diz is not null) return diz;

        return Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Contains("read", StringComparison.OrdinalIgnoreCase) ||
                                 Path.GetFileName(f).Contains("trn", StringComparison.OrdinalIgnoreCase) ||
                                 Path.GetFileName(f).Contains(" trainer", StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    // 5. PRE-LAUNCH SAVE-GAME BACKUP & 1-CLICK ROLLBACK ("TRAINER INSURANCE")
    // =========================================================================

    public string GetSaveBackupsDirectoryForProfile(StoreFileProfile profile)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var slug = MakeSafeFileSlug(profile.DisplayTitle);
        var folder = Path.Combine(localAppData, "RaggieSoft", "Cache", "StoreTrainer", "SaveBackups", slug);
        Directory.CreateDirectory(folder);
        return folder;
    }

    public List<SaveBackupInfo> ListSaveGameBackups(StoreFileProfile profile)
    {
        var dir = GetSaveBackupsDirectoryForProfile(profile);
        if (!Directory.Exists(dir))
        {
            return [];
        }

        return Directory.EnumerateFiles(dir, "SaveBackup_*.zip", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new SaveBackupInfo(path, info.Name, info.CreationTime, info.Length);
            })
            .OrderByDescending(b => b.CreatedLocalTime)
            .ToList();
    }

    public SaveBackupInfo CreateSaveGameBackup(StoreFileProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.SaveFolderPath) || !Directory.Exists(profile.SaveFolderPath))
        {
            throw new DirectoryNotFoundException("The configured Save-Game folder was not found on disk.");
        }

        var backupsDir = GetSaveBackupsDirectoryForProfile(profile);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var zipPath = Path.Combine(backupsDir, $"SaveBackup_{stamp}.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(profile.SaveFolderPath, zipPath, CompressionLevel.Fastest, includeBaseDirectory: false);

        // Prune older snapshots beyond MaxSaveBackups
        var maxKeep = Math.Clamp(profile.MaxSaveBackups, 1, 25);
        var all = ListSaveGameBackups(profile);
        foreach (var old in all.Skip(maxKeep))
        {
            try { File.Delete(old.ZipFilePath); } catch { /* ignore */ }
        }

        var created = new FileInfo(zipPath);
        return new SaveBackupInfo(zipPath, created.Name, created.CreationTime, created.Length);
    }

    public void RestoreSaveGameBackup(string backupZipPath, string saveFolderPath)
    {
        if (!File.Exists(backupZipPath))
        {
            throw new FileNotFoundException("Selected backup archive was not found.", backupZipPath);
        }
        if (string.IsNullOrWhiteSpace(saveFolderPath))
        {
            throw new InvalidOperationException("Please configure the game's Save-Game folder first.");
        }

        Directory.CreateDirectory(saveFolderPath);
        ZipFile.ExtractToDirectory(backupZipPath, saveFolderPath, overwriteFiles: true);
    }

    // =========================================================================
    // 6. SHA-256 FINGERPRINT, DESKTOP SHORTCUT (.LNK) & SMART LAUNCH SEQUENCER
    // =========================================================================

    public string ComputeFileFingerprint(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Creates a one-click Desktop shortcut (<c>.lnk</c>) using the game's own icon that launches
    /// <c>StoreTrainer.exe /l "&lt;profile.stfx&gt;"</c>.
    /// </summary>
    public (bool Success, string Message) CreateDesktopLaunchShortcut(StoreFileProfile profile)
    {
        if (!OperatingSystem.IsWindows())
        {
            return (false, "Desktop shortcuts (.lnk) can only be created on Windows.");
        }

        if (profile.IsUnsavedNew || !File.Exists(profile.FilePath))
        {
            return (false, "Please save this game profile first before creating a Desktop shortcut.");
        }

        var selfExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(selfExe) || !File.Exists(selfExe))
        {
            return (false, "Could not determine StoreTrainer application path.");
        }

        try
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktopDir, $"{MakeSafeFileSlug(profile.DisplayTitle)}.lnk");

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return (false, "Windows Shortcut Scripting Host is unavailable on this system.");
            }

            var shell = Activator.CreateInstance(shellType)!;
            var shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                [shortcutPath])!;

            var scType = shortcut.GetType();
            scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, [selfExe]);
            scType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, [$"/l \"{profile.FilePath}\""]);
            scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, [Path.GetDirectoryName(selfExe) ?? ""]);
            scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, [$"Launch {profile.DisplayTitle} via RaggieSoft StoreTrainer"]);

            var iconSourceExe = !string.IsNullOrWhiteSpace(profile.GamePath) && File.Exists(profile.GamePath)
                ? profile.GamePath
                : selfExe;
            scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, [$"{iconSourceExe},0"]);
            scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);

            return (true, $"Created Desktop shortcut for {profile.DisplayTitle}");
        }
        catch (Exception ex)
        {
            return (false, $"Could not create Desktop shortcut:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Launches a profile's game and companion trainer (or game-only clean mode when
    /// <paramref name="launchWithTrainer"/> is <c>false</c>), creates pre-launch save backups,
    /// applies CPU core affinity, tracks playtime, and auto-closes the trainer when the game exits!
    /// </summary>
    public async Task<StoreTrainerLaunchResult> LaunchProfileAsync(
        StoreFileProfile profile,
        bool launchWithTrainer = true,
        Func<string, bool>? confirmFingerprintMismatch = null,
        Action<StoreFileProfile>? onSessionStatsUpdated = null)
    {
        var activeTrainer = profile.GetOrCreateActiveTrainer();
        var hasTrainerConfigured = launchWithTrainer && !string.IsNullOrWhiteSpace(activeTrainer.Path);

        // Validate paths before starting anything
        if (profile.TrainerLaunchesGame && launchWithTrainer)
        {
            if (string.IsNullOrWhiteSpace(activeTrainer.Path) || !File.Exists(activeTrainer.Path))
            {
                return new StoreTrainerLaunchResult(false, 2, $"Trainer program was not found:\n{activeTrainer.Path}");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(profile.GamePath) || !File.Exists(profile.GamePath))
            {
                return new StoreTrainerLaunchResult(false, 3, $"Game program was not found:\n{profile.GamePath}");
            }

            if (hasTrainerConfigured && !File.Exists(activeTrainer.Path))
            {
                return new StoreTrainerLaunchResult(false, 2, $"Companion trainer file was not found:\n{activeTrainer.Path}");
            }
        }

        // Verify SHA-256 trainer snapshot if requested
        if (hasTrainerConfigured && activeTrainer.VerifyBeforeLaunch && !string.IsNullOrWhiteSpace(activeTrainer.Fingerprint))
        {
            var liveHash = ComputeFileFingerprint(activeTrainer.Path);
            if (!string.Equals(liveHash, activeTrainer.Fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                var prompt =
                    $"The companion trainer file '{Path.GetFileName(activeTrainer.Path)}' has changed since you saved its trusted snapshot.\n\n" +
                    $"Saved snapshot:  {activeTrainer.Fingerprint[..Math.Min(16, activeTrainer.Fingerprint.Length)]}…\n" +
                    $"Current file:    {liveHash[..Math.Min(16, liveHash.Length)]}…\n\n" +
                    "Do you still want to launch this trainer?";
                if (confirmFingerprintMismatch is null || !confirmFingerprintMismatch(prompt))
                {
                    return new StoreTrainerLaunchResult(false, 4, "Launch canceled because the trainer file changed.");
                }
            }
        }

        // Optional Pre-Launch Save-Game Backup ("Trainer Insurance")
        if (launchWithTrainer &&
            profile.BackupSavesBeforeLaunch &&
            !string.IsNullOrWhiteSpace(profile.SaveFolderPath) &&
            Directory.Exists(profile.SaveFolderPath))
        {
            try
            {
                _ = CreateSaveGameBackup(profile);
            }
            catch
            {
                // Never block game startup if a save file is locked; continue cleanly
            }
        }

        Process? gameProcess = null;
        Process? trainerProcess = null;

        try
        {
            if (profile.TrainerLaunchesGame && launchWithTrainer)
            {
                trainerProcess = StartExternalProgram(
                    activeTrainer.Path,
                    activeTrainer.Switch,
                    activeTrainer.WorkingDirectory,
                    cpuCoreLimit: 0);
            }
            else if (!hasTrainerConfigured)
            {
                // Clean launch (No Trainer) or profile without a trainer assigned
                gameProcess = StartExternalProgram(
                    profile.GamePath,
                    profile.GameSwitch,
                    profile.GameWorkingDirectory,
                    profile.CpuCoreLimit);
            }
            else if (profile.LaunchGameFirst)
            {
                gameProcess = StartExternalProgram(
                    profile.GamePath,
                    profile.GameSwitch,
                    profile.GameWorkingDirectory,
                    profile.CpuCoreLimit);

                await WaitBetweenProgramsAsync(gameProcess, profile.WaitForGameReady, profile.LaunchDelaySeconds);

                trainerProcess = StartExternalProgram(
                    activeTrainer.Path,
                    activeTrainer.Switch,
                    activeTrainer.WorkingDirectory,
                    cpuCoreLimit: 0);
            }
            else
            {
                trainerProcess = StartExternalProgram(
                    activeTrainer.Path,
                    activeTrainer.Switch,
                    activeTrainer.WorkingDirectory,
                    cpuCoreLimit: 0);

                await WaitBetweenProgramsAsync(trainerProcess, profile.WaitForGameReady, profile.LaunchDelaySeconds);

                gameProcess = StartExternalProgram(
                    profile.GamePath,
                    profile.GameSwitch,
                    profile.GameWorkingDirectory,
                    profile.CpuCoreLimit);
            }
        }
        catch (Exception ex)
        {
            return new StoreTrainerLaunchResult(false, 5, $"Could not start program:\n{ex.Message}");
        }

        // Update launch counter & timestamp immediately
        var sessionStartUtc = DateTime.UtcNow;
        profile.LaunchCount++;
        profile.LastPlayedUtc = sessionStartUtc;
        if (!profile.IsUnsavedNew && File.Exists(profile.FilePath))
        {
            try { SaveStoreFile(profile, profile.FilePath); } catch { /* ignore */ }
        }

        // Background session monitor: tracks elapsed playtime and auto-closes trainer when game exits!
        Task? monitorTask = null;
        if (gameProcess is not null)
        {
            monitorTask = MonitorGameSessionAndAutoCloseTrainerAsync(
                profile,
                gameProcess,
                trainerProcess,
                sessionStartUtc,
                onSessionStatsUpdated);
        }

        var modeDesc = hasTrainerConfigured ? $"with {activeTrainer.Label}" : "(Game Only — No Trainer)";
        return new StoreTrainerLaunchResult(true, 0, $"Launched {profile.DisplayTitle} {modeDesc}", monitorTask);
    }

    private static Process? StartExternalProgram(string exePath, string args, string customWorkDir, int cpuCoreLimit)
    {
        var workDir = !string.IsNullOrWhiteSpace(customWorkDir) && Directory.Exists(customWorkDir)
            ? customWorkDir
            : Path.GetDirectoryName(exePath) ?? string.Empty;

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args ?? string.Empty,
            WorkingDirectory = workDir,
            UseShellExecute = true
        };

        var proc = Process.Start(psi);
        if (proc is not null && OperatingSystem.IsWindows() && cpuCoreLimit is 1 or 2)
        {
            try
            {
                // Bitmask 0x1 = Core 0 only; Bitmask 0x3 = Cores 0 & 1 only (fixes classic multi-core physics/audio bugs!)
                proc.ProcessorAffinity = cpuCoreLimit == 1 ? (IntPtr)0x1 : (IntPtr)0x3;
            }
            catch
            {
                // Ignore if process elevated via UAC and restricts handle access
            }
        }
        return proc;
    }

    private static async Task WaitBetweenProgramsAsync(Process? firstProcess, bool waitForInputIdle, int extraDelaySeconds)
    {
        if (waitForInputIdle && firstProcess is not null && OperatingSystem.IsWindows())
        {
            try
            {
                await Task.Run(() => firstProcess.WaitForInputIdle(5000));
            }
            catch
            {
                // Console / launcher stubs without a Win32 message loop throw InvalidOperationException; safe to ignore!
            }
        }

        if (extraDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(extraDelaySeconds, 0, 120)));
        }
    }

    private async Task MonitorGameSessionAndAutoCloseTrainerAsync(
        StoreFileProfile profile,
        Process gameProcess,
        Process? trainerProcess,
        DateTime sessionStartUtc,
        Action<StoreFileProfile>? onSessionStatsUpdated)
    {
        try
        {
            using (gameProcess)
            {
                await gameProcess.WaitForExitAsync();
            }

            var elapsedMinutes = Math.Max(1, (int)Math.Round((DateTime.UtcNow - sessionStartUtc).TotalMinutes));
            profile.TotalPlayMinutes += elapsedMinutes;
            profile.LastPlayedUtc = DateTime.UtcNow;

            if (!profile.IsUnsavedNew && File.Exists(profile.FilePath))
            {
                try { SaveStoreFile(profile, profile.FilePath); } catch { /* ignore */ }
            }

            // Auto-Close Companion Trainer when the game exits if enabled!
            if (profile.AutoCloseTrainerOnGameExit && trainerProcess is not null)
            {
                using (trainerProcess)
                {
                    if (!trainerProcess.HasExited)
                    {
                        try
                        {
                            if (trainerProcess.CloseMainWindow())
                            {
                                await Task.Delay(1800);
                            }
                            if (!trainerProcess.HasExited)
                            {
                                trainerProcess.Kill();
                            }
                        }
                        catch
                        {
                            // Ignore if trainer was elevated or already closed by user
                        }
                    }
                }
            }

            onSessionStatsUpdated?.Invoke(profile);
        }
        catch
        {
            // Ignore session monitor errors
        }
    }

    // =========================================================================
    // 7. SMALL CLASSIC INI & SAFE FILENAME HELPERS
    // =========================================================================

    public static string MakeSafeFileSlug(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "MyGame";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(rawName.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "MyGame" : cleaned;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIniLines(IEnumerable<string> lines)
    {
        var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var currentSection = string.Empty;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Length > 2)
            {
                currentSection = trimmed[1..^1].Trim();
                if (!map.ContainsKey(currentSection))
                {
                    map[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            var eq = trimmed.IndexOf('=');
            if (eq > 0 && currentSection.Length > 0)
            {
                var key = trimmed[..eq].Trim();
                var val = trimmed[(eq + 1)..].Trim();
                map[currentSection][key] = val;
            }
        }

        return map;
    }

    private static bool TryGetIniValue(
        Dictionary<string, Dictionary<string, string>> sections,
        string section,
        string key,
        out string value)
    {
        value = string.Empty;
        if (sections.TryGetValue(section, out var kv) && kv.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }
        return false;
    }

    private static bool ParseClassicBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var t = raw.Trim();
        return string.Equals(t, "True", StringComparison.OrdinalIgnoreCase) ||
               t == "1" ||
               t == "-1"; // Classic VB6 CStr(CBool(-1)) compatibility!
    }
}
