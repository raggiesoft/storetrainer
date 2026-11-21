// ================================================================================
// FILE: StoreTrainerService.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Creative Content)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is this file?
//     Think of StoreTrainerService.cs as the back-end engine behind StoreTrainer 2.0.
//     While MainWindow.xaml draws buttons and text boxes, this service handles all
//     disk files, folder discovery, file verification, and program launching:
//       1. Classic 'TStore.ini' & 'My StoreFiles' Compatibility:
//          Reads and updates '%APPDATA%\RaggieSoft\StoreTrainer\TStore.ini' — the
//          exact same configuration file created by classic VB6 StoreTrainer 1.0 & 1.1!
//          That means when an existing StoreTrainer 1.0/1.1 user upgrades to 2.0,
//          StoreTrainer 2.0 immediately finds their custom 'My StoreFiles' folder
//          with zero manual re-importing.
//       2. Zero-Dependency INI Parser & Writer:
//          Reads and writes classic '[Section] Key=Value' '.stf' files cleanly in
//          UTF-8/ANSI-compatible plain text, preserving any third-party or future keys.
//       3. Smart Game & Trainer Launch Sequencer ('LaunchProfileAsync'):
//          Fixes every classic working-directory quirk by setting each launched
//          program's working folder to its own parent folder (or the user's custom
//          working folder), optionally waiting for the first window to finish
//          opening ('WaitForInputIdle'), pausing for 'LaunchDelaySeconds', and
//          starting the companion program cleanly.
//       4. Optional Trainer File Verification ('ComputeFileFingerprint'):
//          Computes a cryptographic SHA-256 fingerprint of a trainer '.exe' when
//          pinned by the user, warning before launch if the file on disk ever
//          changes unexpectedly.
// ================================================================================

using System.Diagnostics;           // 'Process' and 'ProcessStartInfo' for starting games & trainers.
using System.IO;                    // 'File', 'Directory', and 'Path' operations.
using System.Security.Cryptography; // 'SHA256' for trainer file verification fingerprints.
using System.Text;                  // 'Encoding' and 'StringBuilder'.
using StoreTrainer.Models;          // 'StoreFileProfile' data model.

namespace StoreTrainer.Services;

/// <summary>
/// Result record returned when loading a <c>.stf</c> profile from disk.
/// </summary>
public sealed record StoreFileLoadResult(bool Success, StoreFileProfile? Profile, string ErrorMessage);

/// <summary>
/// Result record returned when launching a game and trainer profile.
/// </summary>
public sealed record StoreTrainerLaunchResult(bool Success, int ErrorCode, string Message);

/// <summary>
/// Manages StoreTrainer configuration (<c>TStore.ini</c>), <c>.stf</c> profile files,
/// trainer file fingerprints, and sequenced game/trainer process launching.
/// </summary>
public sealed class StoreTrainerService
{
    // Highest '.stf' format version understood by StoreTrainer 2.0 ('1.0' and '1.1' both load cleanly).
    public const string CurrentSupportedStfVersion = "1.1";

    /// <summary>
    /// Path to classic StoreTrainer roaming settings directory (<c>%APPDATA%\RaggieSoft\StoreTrainer</c>).
    /// </summary>
    public string ClassicAppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RaggieSoft", "StoreTrainer");

    /// <summary>
    /// Path to classic <c>TStore.ini</c> inside <c>%APPDATA%\RaggieSoft\StoreTrainer\TStore.ini</c>.
    /// </summary>
    public string ClassicIniFilePath => Path.Combine(ClassicAppDataDirectory, "TStore.ini");

    /// <summary>
    /// Resolves (and creates if missing) the user's active <c>My StoreFiles</c> library folder,
    /// honoring any existing custom folder saved in <c>TStore.ini</c> by StoreTrainer 1.0 or 1.1.
    /// </summary>
    public string EnsureLibraryFolder()
    {
        Directory.CreateDirectory(ClassicAppDataDirectory);

        var iniData = File.Exists(ClassicIniFilePath)
            ? ReadIniSections(ClassicIniFilePath)
            : new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        var configuredFolder = GetIniValue(iniData, "Library", "Folder", string.Empty);
        if (string.IsNullOrWhiteSpace(configuredFolder) ||
            string.Equals(configuredFolder, "NO_FOLDER_CREATED", StringComparison.OrdinalIgnoreCase))
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            configuredFolder = Path.Combine(documents, "My StoreFiles");
        }

        Directory.CreateDirectory(configuredFolder);

        // Keep classic TStore.ini up to date so VB6 v1.1 and v2.0 stay synchronized if both are used.
        SetIniValue(iniData, "General", "Version", "2.0.0");
        SetIniValue(iniData, "General", "Path", AppContext.BaseDirectory.TrimEnd('\\', '/'));
        SetIniValue(iniData, "Library", "Folder", configuredFolder);
        WriteIniSections(ClassicIniFilePath, iniData);

        return configuredFolder;
    }

    /// <summary>
    /// Updates the active StoreFiles library directory in <c>TStore.ini</c>.
    /// </summary>
    public void SetLibraryFolder(string newFolderPath)
    {
        if (string.IsNullOrWhiteSpace(newFolderPath))
        {
            return;
        }

        Directory.CreateDirectory(newFolderPath);
        Directory.CreateDirectory(ClassicAppDataDirectory);

        var iniData = File.Exists(ClassicIniFilePath)
            ? ReadIniSections(ClassicIniFilePath)
            : new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        SetIniValue(iniData, "Library", "Folder", newFolderPath);
        WriteIniSections(ClassicIniFilePath, iniData);
    }

    /// <summary>
    /// Reads or saves the user's preferred color theme (<c>System</c>, <c>Dark</c>, or <c>Light</c>)
    /// inside <c>TStore.ini</c> under <c>[Appearance] Theme</c>.
    /// </summary>
    public string GetSavedThemePreference()
    {
        if (!File.Exists(ClassicIniFilePath))
        {
            return "System";
        }

        var iniData = ReadIniSections(ClassicIniFilePath);
        return GetIniValue(iniData, "Appearance", "Theme", "System");
    }

    public void SaveThemePreference(string theme)
    {
        Directory.CreateDirectory(ClassicAppDataDirectory);
        var iniData = File.Exists(ClassicIniFilePath)
            ? ReadIniSections(ClassicIniFilePath)
            : new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        SetIniValue(iniData, "Appearance", "Theme", theme);
        WriteIniSections(ClassicIniFilePath, iniData);
    }

    /// <summary>
    /// Scans the user's library folder for all <c>*.stf</c> profiles, sorted with Favorites first
    /// and then alphabetically by <see cref="StoreFileProfile.DisplayTitle"/>.
    /// </summary>
    public List<StoreFileProfile> LoadLibraryProfiles(string libraryFolder)
    {
        var list = new List<StoreFileProfile>();
        if (string.IsNullOrWhiteSpace(libraryFolder) || !Directory.Exists(libraryFolder))
        {
            return list;
        }

        foreach (var file in Directory.EnumerateFiles(libraryFolder, "*.stf", SearchOption.TopDirectoryOnly))
        {
            var res = LoadStoreFile(file);
            if (res.Success && res.Profile is not null)
            {
                list.Add(res.Profile);
            }
        }

        // OrderByDescending(p => p.IsFavorite) puts 'true' (starred favorites) above 'false',
        // and ThenBy(..., OrdinalIgnoreCase) sorts alphabetically within each group!
        return list
            .OrderByDescending(p => p.IsFavorite)
            .ThenBy(p => p.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Loads a single <c>.stf</c> INI profile from disk, validating compatibility with
    /// classic <c>1.0</c>, <c>1.1</c>, and <c>2.0</c> profile keys.
    /// </summary>
    public StoreFileLoadResult LoadStoreFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new StoreFileLoadResult(false, null, $"The StoreFile could not be found:\n{filePath}");
        }

        try
        {
            var sections = ReadIniSections(filePath);
            var stfVer = GetIniValue(sections, "General", "STF.Version", "1.0");

            // Reject only if a future major version (e.g. 3.0) is encountered; accept both 1.0 and 1.1!
            if (CompareDottedVersions(stfVer, "2.0") > 0)
            {
                return new StoreFileLoadResult(
                    false,
                    null,
                    $"This StoreFile requires a newer version of RaggieSoft StoreTrainer (file format {stfVer}).");
            }

            var delayRaw = GetIniValue(sections, "General", "LaunchDelay", "0");
            _ = int.TryParse(delayRaw, out var delaySec);
            delaySec = Math.Clamp(delaySec, 0, 30);

            var trainerFingerprint = GetIniValue(sections, "Trainer", "Fingerprint", string.Empty);
            var verifyRaw = GetIniValue(
                sections,
                "Trainer",
                "VerifyBeforeLaunch",
                !string.IsNullOrWhiteSpace(trainerFingerprint) ? "true" : "false");

            var trainerLaunchesGame = ParseIniBool(GetIniValue(sections, "Trainer", "TrainerLaunchesGame", "false"));
            var launchGameFirst = !trainerLaunchesGame &&
                                  ParseIniBool(GetIniValue(sections, "Game", "LaunchGameFirst", "false"));

            // Unescape '\n' tokens back into real multi-line notes for the editor TextBox.
            var rawNotes = GetIniValue(sections, "General", "Notes", string.Empty)
                .Replace("\\n", Environment.NewLine, StringComparison.Ordinal);

            var profile = new StoreFileProfile
            {
                FilePath = filePath,
                FormatVersion = "1.1",
                DisplayName = GetIniValue(sections, "General", "DisplayName", string.Empty),
                Category = GetIniValue(sections, "General", "Category", "General"),
                IsFavorite = ParseIniBool(GetIniValue(sections, "General", "Favorite", "false")),
                LaunchDelaySeconds = delaySec,
                WaitForGameReady = ParseIniBool(GetIniValue(sections, "General", "WaitForGameReady", "true")),
                Notes = rawNotes,
                TrainerLaunchesGame = trainerLaunchesGame,
                LaunchGameFirst = launchGameFirst,
                GamePath = GetIniValue(sections, "Game", "Path", string.Empty),
                GameSwitch = GetIniValue(sections, "Game", "Switch", string.Empty),
                GameWorkingDirectory = GetIniValue(sections, "Game", "WorkingDirectory", string.Empty),
                TrainerPath = GetIniValue(sections, "Trainer", "Path", string.Empty),
                TrainerSwitch = GetIniValue(sections, "Trainer", "Switch", string.Empty),
                TrainerWorkingDirectory = GetIniValue(sections, "Trainer", "WorkingDirectory", string.Empty),
                TrainerFingerprint = trainerFingerprint,
                VerifyTrainerBeforeLaunch = ParseIniBool(verifyRaw)
            };

            RecordRecentStoreFile(filePath);
            return new StoreFileLoadResult(true, profile, string.Empty);
        }
        catch (Exception ex)
        {
            return new StoreFileLoadResult(false, null, $"Could not read StoreFile:\n{ex.Message}");
        }
    }

    /// <summary>
    /// Writes a <see cref="StoreFileProfile"/> to disk in backward-compatible <c>.stf</c> INI format.
    /// </summary>
    public void SaveStoreFile(StoreFileProfile profile, string targetFilePath)
    {
        var dir = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Enforce mutual exclusivity (if the trainer launches the game, 'LaunchGameFirst' must be false).
        if (profile.TrainerLaunchesGame)
        {
            profile.LaunchGameFirst = false;
        }

        var sections = File.Exists(targetFilePath)
            ? ReadIniSections(targetFilePath)
            : new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Write '1.1' so classic VB6 StoreTrainer v1.1 can open this exact '.stf' file too!
        SetIniValue(sections, "General", "STF.Version", CurrentSupportedStfVersion);
        SetIniValue(sections, "General", "DisplayName", profile.DisplayName.Trim());
        SetIniValue(sections, "General", "Category", string.IsNullOrWhiteSpace(profile.Category) ? "General" : profile.Category.Trim());
        SetIniValue(sections, "General", "Favorite", profile.IsFavorite ? "true" : "false");
        SetIniValue(sections, "General", "LaunchDelay", Math.Clamp(profile.LaunchDelaySeconds, 0, 30).ToString());
        SetIniValue(sections, "General", "WaitForGameReady", profile.WaitForGameReady ? "true" : "false");

        // Collapse multi-line notes onto a single INI line using '\n' escape tokens so classic
        // Win32 GetPrivateProfileStringA never trips over embedded line breaks.
        var singleLineNotes = (profile.Notes ?? string.Empty)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\n", StringComparison.Ordinal);
        SetIniValue(sections, "General", "Notes", singleLineNotes);

        SetIniValue(sections, "Game", "LaunchGameFirst", profile.LaunchGameFirst ? "true" : "false");
        SetIniValue(sections, "Game", "Path", profile.GamePath.Trim());
        SetIniValue(sections, "Game", "Switch", profile.GameSwitch.Trim());
        SetIniValue(sections, "Game", "WorkingDirectory", profile.GameWorkingDirectory.Trim());

        SetIniValue(sections, "Trainer", "TrainerLaunchesGame", profile.TrainerLaunchesGame ? "true" : "false");
        SetIniValue(sections, "Trainer", "Path", profile.TrainerPath.Trim());
        SetIniValue(sections, "Trainer", "Switch", profile.TrainerSwitch.Trim());
        SetIniValue(sections, "Trainer", "WorkingDirectory", profile.TrainerWorkingDirectory.Trim());
        SetIniValue(sections, "Trainer", "VerifyBeforeLaunch", profile.VerifyTrainerBeforeLaunch ? "true" : "false");
        SetIniValue(sections, "Trainer", "Fingerprint", profile.TrainerFingerprint.Trim());

        WriteIniSections(targetFilePath, sections);
        profile.FilePath = targetFilePath;
        RecordRecentStoreFile(targetFilePath);
    }

    /// <summary>
    /// Computes an uppercase hexadecimal fingerprint of a trainer file on disk so
    /// StoreTrainer can alert the user if the file is ever altered unexpectedly.
    /// </summary>
    public string ComputeFileFingerprint(string executablePath)
    {
        using var stream = File.OpenRead(executablePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Validates paths and optional trainer fingerprint, then launches the game and/or
    /// companion trainer in the user's configured sequence with proper working directories.
    /// </summary>
    public async Task<StoreTrainerLaunchResult> LaunchProfileAsync(
        StoreFileProfile profile,
        Func<string, bool>? confirmFingerprintMismatch = null)
    {
        // 1. Verify Trainer exists if provided (required unless user saved a game-only profile,
        //    though classic StoreTrainer always pairs a trainer — we allow optional blank trainer
        //    ONLY when TrainerLaunchesGame is false AND GamePath exists, giving 2.0 users bonus
        //    flexibility to also launch standalone games with custom launch options!).
        var hasTrainer = !string.IsNullOrWhiteSpace(profile.TrainerPath);
        var hasGame = !string.IsNullOrWhiteSpace(profile.GamePath);

        if (profile.TrainerLaunchesGame && !hasTrainer)
        {
            return new StoreTrainerLaunchResult(
                false,
                2,
                "\"Trainer Launches Game\" is checked, so please choose a trainer program file first.");
        }

        if (!hasTrainer && !hasGame)
        {
            return new StoreTrainerLaunchResult(
                false,
                2,
                "Please choose a game file (and optional trainer file) before launching.");
        }

        if (hasTrainer && !File.Exists(profile.TrainerPath))
        {
            return new StoreTrainerLaunchResult(
                false,
                2,
                $"The trainer file could not be found at:\n{profile.TrainerPath}");
        }

        if (!profile.TrainerLaunchesGame && hasGame && !File.Exists(profile.GamePath))
        {
            return new StoreTrainerLaunchResult(
                false,
                3,
                $"The game file could not be found at:\n{profile.GamePath}");
        }

        if (!profile.TrainerLaunchesGame && !hasGame)
        {
            return new StoreTrainerLaunchResult(
                false,
                3,
                "Please choose the game program file, or check \"Trainer Launches Game\" if your trainer starts the game automatically.");
        }

        // 2. Optional Trainer File Verification Check
        if (hasTrainer && profile.VerifyTrainerBeforeLaunch && !string.IsNullOrWhiteSpace(profile.TrainerFingerprint))
        {
            var currentFingerprint = ComputeFileFingerprint(profile.TrainerPath);
            if (!string.Equals(currentFingerprint, profile.TrainerFingerprint.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var prompt =
                    $"The trainer file on disk has changed since you pinned its safety verification:\n\n" +
                    $"{profile.TrainerPath}\n\n" +
                    "Do you still want to launch this trainer?";
                var proceed = confirmFingerprintMismatch?.Invoke(prompt) ?? false;
                if (!proceed)
                {
                    return new StoreTrainerLaunchResult(false, 4, "Launch canceled because the trainer file changed.");
                }
            }
        }

        // 3. Execute launch sequence
        try
        {
            var delayMs = Math.Clamp(profile.LaunchDelaySeconds, 0, 30) * 1000;

            // Mode A: Trainer launches the game by itself -> start Trainer only!
            if (profile.TrainerLaunchesGame)
            {
                StartProgram(profile.TrainerPath, profile.TrainerSwitch, profile.TrainerWorkingDirectory, waitForReady: false);
                return new StoreTrainerLaunchResult(true, 0, $"Launched trainer for {profile.DisplayTitle}");
            }

            // Mode B: Game only (no trainer path entered) -> start Game with its switches!
            if (!hasTrainer)
            {
                StartProgram(profile.GamePath, profile.GameSwitch, profile.GameWorkingDirectory, waitForReady: false);
                return new StoreTrainerLaunchResult(true, 0, $"Launched {profile.DisplayTitle}");
            }

            // Mode C: Launch Game First -> wait for game window / delay -> launch Trainer
            if (profile.LaunchGameFirst)
            {
                StartProgram(profile.GamePath, profile.GameSwitch, profile.GameWorkingDirectory, waitForReady: profile.WaitForGameReady);
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs);
                }
                StartProgram(profile.TrainerPath, profile.TrainerSwitch, profile.TrainerWorkingDirectory, waitForReady: false);
                return new StoreTrainerLaunchResult(true, 0, $"Launched {profile.DisplayTitle} (game first, then trainer)");
            }

            // Mode D: Default simultaneous / Trainer-then-Game launch
            StartProgram(profile.TrainerPath, profile.TrainerSwitch, profile.TrainerWorkingDirectory, waitForReady: profile.WaitForGameReady);
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
            StartProgram(profile.GamePath, profile.GameSwitch, profile.GameWorkingDirectory, waitForReady: false);
            return new StoreTrainerLaunchResult(true, 0, $"Launched {profile.DisplayTitle} and companion trainer");
        }
        catch (Exception ex)
        {
            return new StoreTrainerLaunchResult(false, 5, $"Could not start program:\n{ex.Message}");
        }
    }

    // Starts an executable with its switches (preserving multi-flag arguments cleanly without
    // wrapping switches inside extra quotes!) and sets WorkingDirectory to its parent folder
    // unless a custom working folder was specified.
    private static void StartProgram(string exePath, string switches, string customWorkingDir, bool waitForReady)
    {
        var workingDir = !string.IsNullOrWhiteSpace(customWorkingDir) && Directory.Exists(customWorkingDir)
            ? customWorkingDir
            : Path.GetDirectoryName(exePath) ?? string.Empty;

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = switches?.Trim() ?? string.Empty,
            WorkingDirectory = workingDir,
            UseShellExecute = true
        };

        using var proc = Process.Start(psi);
        if (waitForReady && proc is not null)
        {
            try
            {
                // Wait up to 4 seconds for the first program's window message loop to become ready.
                // Wrapped in try/catch because console launchers or elevated UAC processes may not
                // expose a GUI input loop to WaitForInputIdle.
                proc.WaitForInputIdle(4000);
            }
            catch
            {
                // Safe to proceed immediately if target has no standard Win32 GUI message loop.
            }
        }
    }

    // Updates '[Recent] File1..File5' inside 'TStore.ini' so recent files stay synchronized with VB6 v1.1!
    private void RecordRecentStoreFile(string filePath)
    {
        try
        {
            Directory.CreateDirectory(ClassicAppDataDirectory);
            var iniData = File.Exists(ClassicIniFilePath)
                ? ReadIniSections(ClassicIniFilePath)
                : new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            var recent = new List<string> { filePath };
            for (var i = 1; i <= 5; i++)
            {
                var existing = GetIniValue(iniData, "Recent", $"File{i}", string.Empty);
                if (!string.IsNullOrWhiteSpace(existing) &&
                    !recent.Contains(existing, StringComparer.OrdinalIgnoreCase))
                {
                    recent.Add(existing);
                }
            }

            for (var i = 0; i < Math.Min(5, recent.Count); i++)
            {
                SetIniValue(iniData, "Recent", $"File{i + 1}", recent[i]);
            }

            WriteIniSections(ClassicIniFilePath, iniData);
        }
        catch
        {
            // Ignore non-critical MRU write issues
        }
    }

    // =========================================================================
    // ZERO-DEPENDENCY CLASSIC WINDOWS INI READER / WRITER
    // =========================================================================

    private static Dictionary<string, Dictionary<string, string>> ReadIniSections(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var currentSection = "General";
        result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']') && line.Length > 2)
            {
                currentSection = line[1..^1].Trim();
                if (!result.ContainsKey(currentSection))
                {
                    result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            var eqIdx = line.IndexOf('=');
            if (eqIdx > 0)
            {
                var key = line[..eqIdx].Trim();
                var val = line[(eqIdx + 1)..].Trim();
                result[currentSection][key] = val;
            }
        }

        return result;
    }

    private static void WriteIniSections(string path, Dictionary<string, Dictionary<string, string>> sections)
    {
        var sb = new StringBuilder();
        foreach (var (sectionName, kvps) in sections)
        {
            if (kvps.Count == 0)
            {
                continue;
            }

            sb.Append('[').Append(sectionName).AppendLine("]");
            foreach (var (key, val) in kvps)
            {
                sb.Append(key).Append('=').AppendLine(val ?? string.Empty);
            }
            sb.AppendLine();
        }

        // Write UTF-8 without BOM so classic ANSI Win32 'GetPrivateProfileStringA' in VB6 v1.1
        // reads the '[General]' header on line 1 cleanly without seeing 3 invisible BOM bytes!
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string GetIniValue(
        Dictionary<string, Dictionary<string, string>> sections,
        string section,
        string key,
        string defaultValue)
    {
        if (sections.TryGetValue(section, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }
        return defaultValue;
    }

    private static void SetIniValue(
        Dictionary<string, Dictionary<string, string>> sections,
        string section,
        string key,
        string value)
    {
        if (!sections.TryGetValue(section, out var dict))
        {
            dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            sections[section] = dict;
        }
        dict[key] = value;
    }

    private static bool ParseIniBool(string raw) =>
        string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);

    private static int CompareDottedVersions(string v1, string v2)
    {
        var p1 = v1.Split('.');
        var p2 = v2.Split('.');
        var len = Math.Max(p1.Length, p2.Length);
        for (var i = 0; i < len; i++)
        {
            var n1 = i < p1.Length && int.TryParse(p1[i], out var a) ? a : 0;
            var n2 = i < p2.Length && int.TryParse(p2[i], out var b) ? b : 0;
            if (n1 != n2)
            {
                return n1.CompareTo(n2);
            }
        }
        return 0;
    }
}
