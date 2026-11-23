// ================================================================================
// FILE: RaggieSoftSuiteManager.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file is the brain behind **RaggieSoft Launcher** (`RaggieSoftLauncher.exe`),
//     our friendly "App Hub" for Windows (similar in spirit to Adobe Creative Cloud
//     or JetBrains Toolbox, but completely lightweight, offline-tolerant, and free
//     of background bloatware).
//
//   How does Windows track installed programs, and what does this file do?
//     When you install a Windows desktop app using an **Inno Setup** `Setup.exe`
//     installer, Windows records an "Uninstall" entry inside the **Windows Registry**
//     (Windows' built-in hierarchical configuration database—the same list that powers
//     Windows' *Settings -> Apps -> Installed apps* screen):
//       - **All-Users / Admin Install (`HKEY_LOCAL_MACHINE` / `HKLM`)**:
//         Installed under `C:\Program Files\RaggieSoft\<App>\`
//       - **Current-User Install (`HKEY_CURRENT_USER` / `HKCU`)**:
//         Installed under `C:\Users\<You>\AppData\Local\Programs\RaggieSoft\<App>\`
//
//     `RaggieSoftSuiteManager` orchestrates three core jobs for the user:
//       1. **Catalog + Registry Discovery (`LoadSuiteAppsAsync` & `RefreshInstalledState`)**:
//          Starts with our built-in catalog (`Book Reader`, `The Listening Room`,
//          and `Backstage`), optionally checks `apps-manifest.json` on
//          `https://assets.raggiesoft.com` for newer versions or brand-new apps,
//          and scans the Windows Registry + disk folders to detect what's already
//          installed and whether an update is available!
//       2. **Download & One-Click Install (`LocateOrDownloadInstallerAsync` & `RunInnoSetupInstallerAsync`)**:
//          Finds a local `RaggieSoft-*-Setup.exe` if bundled nearby (e.g. USB stick)
//          or downloads it with a live progress bar into `%LOCALAPPDATA%\RaggieSoft\Cache\Installers\`,
//          then runs it either silently in one click (`/VERYSILENT`) or in interactive
//          Setup Wizard mode.
//       3. **Clean Uninstall (`RunInnoSetupUninstallerAsync`)**:
//          Launches the app's registered `unins000.exe` uninstaller and refreshes
//          the Launcher window automatically as soon as uninstallation finishes.
// ================================================================================

using System.Diagnostics;             // `Process`, `ProcessStartInfo`, and `FileVersionInfo`.
using System.Net.Http;                // `HttpCompletionOption` for streaming large `Setup.exe` downloads.
using System.Runtime.InteropServices; // OS/platform interop support.
using System.Text.Json;               // `JsonSerializer` for parsing `apps-manifest.json`.
using System.Text.Json.Serialization; // `[JsonPropertyName]` and `[JsonIgnore]` attributes.
using Microsoft.Win32;                // `Registry`, `RegistryKey`, `RegistryHive`, and `RegistryView` on Windows.

namespace RaggieSoft.Core.Services;

/// <summary>
/// Represents a RaggieSoft desktop application managed by RaggieSoft Launcher, combining
/// static catalog metadata with live installed/update status on the user's PC.
/// </summary>
public sealed class RaggieSoftSuiteAppItem
{
    // Short internal identifier, e.g. `"book-reader"`, `"listening-room"`, or `"backstage"`.
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Unique GUID assigned to this app's Inno Setup `.iss` script (e.g. `"{8E92D4A1-...}"`).
    // Inno Setup creates a Registry key named `<InnoAppId>_is1` under Windows' `Uninstall` key!
    [JsonPropertyName("innoAppId")]
    public string InnoAppId { get; set; } = string.Empty;

    // Consumer-facing app name, creative imprint eyebrow, category pill, and description.
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("imprint")]
    public string Imprint { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // Latest published version string (`"1.0.0"`), main `.exe` filename, and default install subfolder.
    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; set; } = "1.0.0";

    [JsonPropertyName("exeName")]
    public string ExeName { get; set; } = string.Empty;

    [JsonPropertyName("defaultSubFolder")]
    public string DefaultSubFolder { get; set; } = string.Empty;

    // Installer `.exe` filename, CDN download URL, companion website URL, and embedded WPF icon URI.
    // (Note on `pack://application:,,,/Assets/BookReader.png`: in WPF, `pack://application:,,,/` is
    // Microsoft's URI scheme for loading an image compiled right inside the `.exe` binary!)
    [JsonPropertyName("installerFileName")]
    public string InstallerFileName { get; set; } = string.Empty;

    [JsonPropertyName("installerDownloadUrl")]
    public string InstallerDownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("webUrl")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("iconPackUri")]
    public string IconPackUri { get; set; } = string.Empty;

    // --- Live Runtime State Populated by `RaggieSoftSuiteManager.RefreshInstalledState()` (`[JsonIgnore]`) ---
    [JsonIgnore]
    public bool IsInstalled { get; set; }

    [JsonIgnore]
    public string InstalledVersion { get; set; } = string.Empty;

    [JsonIgnore]
    public string InstallLocation { get; set; } = string.Empty;

    [JsonIgnore]
    public string ExecutablePath { get; set; } = string.Empty;

    [JsonIgnore]
    public string UninstallerPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string InstallScopeLabel { get; set; } = string.Empty;

    // `true` when the app is installed locally AND its version differs from `LatestVersion`.
    [JsonIgnore]
    public bool IsUpdateAvailable =>
        IsInstalled &&
        !string.IsNullOrWhiteSpace(InstalledVersion) &&
        !string.IsNullOrWhiteSpace(LatestVersion) &&
        !string.Equals(InstalledVersion.Trim(), LatestVersion.Trim(), StringComparison.OrdinalIgnoreCase);

    // Consumer-friendly status pill shown on the Launcher card.
    [JsonIgnore]
    public string StatusBadgeText =>
        !IsInstalled
            ? $"☁️ Available (v{LatestVersion})"
            : IsUpdateAvailable
                ? $"⬆️ Update Available (v{InstalledVersion} → v{LatestVersion})"
                : $"✅ Installed (v{InstalledVersion})";

    // Friendly installation folder readout (or ready-to-install message when not yet installed).
    [JsonIgnore]
    public string InstallLocationDisplay =>
        IsInstalled && !string.IsNullOrWhiteSpace(InstallLocation)
            ? $"📂 Installed in: {InstallLocation}"
            : "📦 Ready to download and install";

    // Dynamic label for the card's primary install/update/reinstall action button.
    [JsonIgnore]
    public string PrimaryInstallButtonText =>
        !IsInstalled
            ? "⬇ Download & Install"
            : IsUpdateAvailable
                ? $"🔄 Update to v{LatestVersion}"
                : "🔄 Reinstall / Repair";
}

/// <summary>
/// Discovers, downloads, installs, updates, and uninstalls RaggieSoft Windows desktop applications.
/// </summary>
public sealed class RaggieSoftSuiteManager
{
    // Stable Inno Setup `AppId` GUIDs shared with our `.iss` installer scripts.
    public const string LauncherInnoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B000}";
    public const string BookReaderInnoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B001}";
    public const string ListeningRoomInnoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B002}";
    public const string BackstageInnoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B003}";
    public const string StoreTrainerInnoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B004}";

    // Optional cloud JSON manifest so future RaggieSoft desktop apps or version bumps appear
    // automatically in already-installed copies of RaggieSoft Launcher!
    public const string RemoteManifestUrl =
        "https://assets.raggiesoft.com/desktop-apps/windows/apps-manifest.json";

    private readonly RaggieSoftWorkspaceLocator _locator;

    public RaggieSoftSuiteManager(RaggieSoftWorkspaceLocator locator)
    {
        _locator = locator;
    }

    /// <summary>
    /// Loads the complete list of RaggieSoft applications, merging the built-in
    /// catalog, any remote manifest additions, and live installation state.
    /// </summary>
    public async Task<List<RaggieSoftSuiteAppItem>> LoadSuiteAppsAsync(bool checkRemoteManifest = false)
    {
        // Start with the authoritative built-in list so the Launcher works 100% offline immediately.
        var apps = GetBuiltInCatalog();

        if (checkRemoteManifest)
        {
            try
            {
                // Cap the remote manifest check at 6 seconds (`CancellationTokenSource(TimeSpan.FromSeconds(6))`)
                // so a slow hotel Wi-Fi connection can never delay the Launcher UI!
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var json = await RaggieSoftWorkspaceLocator.SharedHttpClient.GetStringAsync(RemoteManifestUrl, cts.Token);
                var remoteApps = JsonSerializer.Deserialize<List<RaggieSoftSuiteAppItem>>(json);
                if (remoteApps is not null)
                {
                    foreach (var remote in remoteApps)
                    {
                        var existing = apps.FirstOrDefault(a =>
                            string.Equals(a.Id, remote.Id, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(a.InnoAppId, remote.InnoAppId, StringComparison.OrdinalIgnoreCase));
                        if (existing is not null)
                        {
                            // Update version / description / download URL on known built-in apps.
                            existing.LatestVersion = remote.LatestVersion;
                            if (!string.IsNullOrWhiteSpace(remote.Description))
                            {
                                existing.Description = remote.Description;
                            }
                            if (!string.IsNullOrWhiteSpace(remote.InstallerDownloadUrl))
                            {
                                existing.InstallerDownloadUrl = remote.InstallerDownloadUrl;
                            }
                        }
                        else
                        {
                            // Brand-new app published to `apps-manifest.json` after this Launcher was compiled!
                            if (string.IsNullOrWhiteSpace(remote.IconPackUri))
                            {
                                remote.IconPackUri = "pack://application:,,,/Assets/RaggieSoftLauncher.png";
                            }
                            apps.Add(remote);
                        }
                    }
                }
            }
            catch
            {
                // Offline or manifest not yet published to CDN; built-in catalog + Registry discovery remain authoritative.
            }
        }

        // Cross-check every catalog entry against the user's Windows Registry and disk folders.
        RefreshInstalledState(apps);
        return apps;
    }

    /// <summary>
    /// Built-in canonical RaggieSoft desktop application suite catalog.
    /// </summary>
    private static List<RaggieSoftSuiteAppItem> GetBuiltInCatalog()
    {
        // Note `@"RaggieSoft\Book Reader"`: the `@` verbatim string prefix lets us write a Windows
        // backslash `\` cleanly without escaping it as `"RaggieSoft\\Book Reader"`.
        return
        [
            new RaggieSoftSuiteAppItem
            {
                Id = "book-reader",
                InnoAppId = BookReaderInnoAppId,
                Title = "RaggieSoft Book Reader",
                Imprint = "OCEAN VIEW ARCHIVES",
                Category = "Books",
                Description =
                    "Read all 30+ Ocean View Archives novels and series lore volumes with full-color scene illustrations, " +
                    "distraction-free illustration galleries, customizable reading fonts (including OpenDyslexic), one-click offline " +
                    "book downloads, and automatic soundtrack playback with The Listening Room.",
                LatestVersion = "1.0.0",
                ExeName = "BookReader.exe",
                DefaultSubFolder = @"RaggieSoft\Book Reader",
                InstallerFileName = "raggiesoft-book-reader-setup.exe",
                InstallerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/book-reader/raggiesoft-book-reader-setup.exe",
                WebUrl = "https://books.raggiesoft.com",
                IconPackUri = "pack://application:,,,/Assets/BookReader.png"
            },
            new RaggieSoftSuiteAppItem
            {
                Id = "listening-room",
                InnoAppId = ListeningRoomInnoAppId,
                Title = "RaggieSoft The Listening Room",
                Imprint = "ENGINE ROOM RECORDS",
                Category = "Music",
                Description =
                    "Listen to and download the complete Engine Room Records music catalog (The Stardust Engine, Aubriener / Aubrie Duval, " +
                    "Holly O'Connell, Psyclops, Todd Ragan, and more) with time-synced lyrics, full liner notes, song creation notes, " +
                    "offline album downloads, and companion novel integration with RaggieSoft Book Reader.",
                LatestVersion = "1.0.0",
                ExeName = "ListeningRoom.exe",
                DefaultSubFolder = @"RaggieSoft\Listening Room",
                InstallerFileName = "raggiesoft-listening-room-setup.exe",
                InstallerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/listening-room/raggiesoft-listening-room-setup.exe",
                WebUrl = "https://music.raggiesoft.com",
                IconPackUri = "pack://application:,,,/Assets/ListeningRoom.png"
            },
            new RaggieSoftSuiteAppItem
            {
                Id = "backstage",
                InnoAppId = BackstageInnoAppId,
                Title = "RaggieSoft Backstage",
                Imprint = "RAGGIESOFT MEDIA",
                Category = "Bonus Features",
                Description =
                    "Explore 50+ behind-the-scenes dossiers, character correspondence, campus architecture guides, " +
                    "and story timelines across The Rachel Narrative, Katie & Nathaniel, The Quantum Directive: The Caretakers, and The Eleventh Child, " +
                    "complete with full-color illustrations, offline archive saving, and one-click companion links to Book Reader and The Listening Room.",
                LatestVersion = "1.0.0",
                ExeName = "Backstage.exe",
                DefaultSubFolder = @"RaggieSoft\Backstage",
                InstallerFileName = "raggiesoft-backstage-setup.exe",
                InstallerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/backstage/raggiesoft-backstage-setup.exe",
                WebUrl = "https://backstage.raggiesoft.com",
                IconPackUri = "pack://application:,,,/Assets/Backstage.png"
            },
            new RaggieSoftSuiteAppItem
            {
                Id = "storetrainer",
                InnoAppId = StoreTrainerInnoAppId,
                Title = "RaggieSoft StoreTrainer",
                Imprint = "RAGGIESOFT UTILITIES",
                Category = "Utilities",
                Description =
                    "Organize and launch classic and modern PC games alongside their companion game trainers, launch options, " +
                    "personal hotkey notes, and trusted trainer file checks. Seamlessly reads and upgrades existing StoreTrainer 1.0 and 1.1 My StoreFiles libraries.",
                LatestVersion = "2.0.0",
                ExeName = "StoreTrainer.exe",
                DefaultSubFolder = @"RaggieSoft\StoreTrainer",
                InstallerFileName = "raggiesoft-storetrainer-setup.exe",
                InstallerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/storetrainer/raggiesoft-storetrainer-setup.exe",
                WebUrl = "https://storetrainer.raggiesoft.com",
                IconPackUri = "pack://application:,,,/Assets/StoreTrainer.png"
            }
        ];
    }

    /// <summary>
    /// Scans the Windows Registry (<c>HKLM</c> &amp; <c>HKCU</c> Uninstall keys) and standard
    /// Program Files / LocalAppData installation directories to populate the
    /// live installation status of every RaggieSoft application. Also dynamically
    /// adds any newly discovered RaggieSoft Inno Setup applications found in the Registry!
    /// </summary>
    public void RefreshInstalledState(List<RaggieSoftSuiteAppItem> apps)
    {
        // Reset runtime fields first so a newly uninstalled app immediately clears back to "Available".
        foreach (var app in apps)
        {
            app.IsInstalled = false;
            app.InstalledVersion = string.Empty;
            app.InstallLocation = string.Empty;
            app.ExecutablePath = string.Empty;
            app.UninstallerPath = string.Empty;
            app.InstallScopeLabel = string.Empty;
        }

        // `OperatingSystem.IsWindows()` tells .NET's cross-platform analyzer that the Registry calls
        // inside this `if` block only run on Windows (where the Windows Registry exists!).
        if (OperatingSystem.IsWindows())
        {
            ScanRegistryHiveForRaggieSoftApps(RegistryHive.LocalMachine, "All Users • Program Files", apps);
            ScanRegistryHiveForRaggieSoftApps(RegistryHive.CurrentUser, "Current User • Local App Data", apps);
            // Also detect classic 32-bit StoreTrainer v1.0 / v1.1 (installed via NSIS into WOW6432Node /
            // Program Files (x86)) so RaggieSoft Launcher can launch TStore.exe OR upgrade it in-place to v2.0!
            ScanLegacy32BitStoreTrainer(apps);
        }

        // Fallback file-system probe for standard `{autopf}\RaggieSoft\<App>` directories in case
        // an app was copied portably without writing Registry keys.
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (var app in apps)
        {
            if (app.IsInstalled && File.Exists(app.ExecutablePath))
            {
                continue;
            }

            var pfDir = Path.Combine(programFiles, app.DefaultSubFolder);
            var localDir = Path.Combine(localAppData, "Programs", app.DefaultSubFolder);

            if (File.Exists(Path.Combine(pfDir, app.ExeName)))
            {
                PopulateFromDirectory(app, pfDir, "All Users • Program Files");
            }
            else if (File.Exists(Path.Combine(localDir, app.ExeName)))
            {
                PopulateFromDirectory(app, localDir, "Current User • Local App Data");
            }
        }
    }

    // Populates installation details directly from an on-disk folder and reads the `.exe`'s embedded
    // PE version header (`FileVersionInfo.GetVersionInfo`, stripping any `+commitHash` suffix).
    private static void PopulateFromDirectory(RaggieSoftSuiteAppItem app, string directory, string scopeLabel)
    {
        var exePath = Path.Combine(directory, app.ExeName);
        var uninsPath = Path.Combine(directory, "unins000.exe");
        app.IsInstalled = true;
        app.InstallLocation = directory;
        app.ExecutablePath = exePath;
        app.UninstallerPath = File.Exists(uninsPath) ? uninsPath : string.Empty;
        app.InstallScopeLabel = scopeLabel;

        if (string.IsNullOrWhiteSpace(app.InstalledVersion))
        {
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(exePath);
                app.InstalledVersion = !string.IsNullOrWhiteSpace(fvi.ProductVersion)
                    ? fvi.ProductVersion.Split('+')[0]
                    : app.LatestVersion;
            }
            catch
            {
                app.InstalledVersion = app.LatestVersion;
            }
        }
    }

    // Walks `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` in either `HKEY_LOCAL_MACHINE`
    // or `HKEY_CURRENT_USER` (64-bit view) in strictly read-only mode (`writable: false`).
    private static void ScanRegistryHiveForRaggieSoftApps(
        RegistryHive hive,
        string scopeLabel,
        List<RaggieSoftSuiteAppItem> apps)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string uninstallSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        try
        {
            // `using var` guarantees Windows kernel Registry handles (`HKEY`) are closed immediately
            // when leaving each scope.
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var uninstallKey = baseKey.OpenSubKey(uninstallSubKey, writable: false);
            if (uninstallKey is null)
            {
                return;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                // Skip the Launcher's own Uninstall entry so Launcher doesn't list itself inside itself!
                if (subKeyName.StartsWith(LauncherInnoAppId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var appKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                if (appKey is null)
                {
                    continue;
                }

                // `as string ?? string.Empty` safely casts the `object?` Registry value to `string`,
                // defaulting to `""` if the value is missing or not a string.
                var displayName = appKey.GetValue("DisplayName") as string ?? string.Empty;
                var publisher = appKey.GetValue("Publisher") as string ?? string.Empty;
                var displayVersion = appKey.GetValue("DisplayVersion") as string ?? "1.0.0";
                var installLocation = (appKey.GetValue("InstallLocation") as string ?? string.Empty).TrimEnd('\\', '/');
                var uninstallString = ExtractExecutablePathFromCommand(appKey.GetValue("UninstallString") as string ?? string.Empty);

                var isRaggieSoft =
                    publisher.Contains("RaggieSoft", StringComparison.OrdinalIgnoreCase) ||
                    displayName.StartsWith("RaggieSoft", StringComparison.OrdinalIgnoreCase);

                if (!isRaggieSoft)
                {
                    continue;
                }

                // Match against known catalog items by InnoAppId or DisplayName
                var matched = apps.FirstOrDefault(a =>
                    (!string.IsNullOrWhiteSpace(a.InnoAppId) &&
                     subKeyName.StartsWith(a.InnoAppId, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(a.Title, displayName, StringComparison.OrdinalIgnoreCase));

                if (matched is null)
                {
                    // Dynamically discovered future RaggieSoft application in the Registry!
                    if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
                    {
                        continue;
                    }

                    // Pick the first `.exe` in the install folder that isn't Inno Setup's `unins000.exe`.
                    var discoveredExe = Directory.GetFiles(installLocation, "*.exe")
                        .FirstOrDefault(f => !Path.GetFileName(f).StartsWith("unins", StringComparison.OrdinalIgnoreCase));

                    matched = new RaggieSoftSuiteAppItem
                    {
                        Id = subKeyName.Replace("_is1", string.Empty, StringComparison.OrdinalIgnoreCase).Trim('{', '}').ToLowerInvariant(),
                        InnoAppId = subKeyName.Replace("_is1", string.Empty, StringComparison.OrdinalIgnoreCase),
                        Title = displayName,
                        Imprint = publisher.ToUpperInvariant(),
                        Category = "Apps",
                        Description = $"Discovered installed RaggieSoft application ({displayName}).",
                        LatestVersion = displayVersion,
                        ExeName = discoveredExe is not null ? Path.GetFileName(discoveredExe) : string.Empty,
                        DefaultSubFolder = Path.Combine("RaggieSoft", displayName),
                        IconPackUri = "pack://application:,,,/Assets/RaggieSoftLauncher.png"
                    };
                    apps.Add(matched);
                }

                if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
                {
                    var candidateExe = !string.IsNullOrWhiteSpace(matched.ExeName)
                        ? Path.Combine(installLocation, matched.ExeName)
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(candidateExe) || !File.Exists(candidateExe))
                    {
                        candidateExe = Directory.GetFiles(installLocation, "*.exe")
                            .FirstOrDefault(f => !Path.GetFileName(f).StartsWith("unins", StringComparison.OrdinalIgnoreCase))
                            ?? string.Empty;
                    }

                    if (!string.IsNullOrWhiteSpace(candidateExe) && File.Exists(candidateExe))
                    {
                        matched.IsInstalled = true;
                        matched.InstalledVersion = displayVersion;
                        matched.InstallLocation = installLocation;
                        matched.ExecutablePath = candidateExe;
                        matched.UninstallerPath = File.Exists(uninstallString)
                            ? uninstallString
                            : Path.Combine(installLocation, "unins000.exe");
                        matched.InstallScopeLabel = scopeLabel;
                    }
                }
            }
        }
        catch
        {
            // Ignore registry permission/access errors
        }
    }

    // Detects classic 32-bit Visual Basic 6.0 StoreTrainer (v1.0 or v1.1) installed by NSIS under
    // 32-bit Registry ('RegistryView.Registry32' / 'WOW6432Node') when 64-bit StoreTrainer 2.0 has
    // not yet replaced it. This lets RaggieSoft Launcher launch classic 'TStore.exe' directly AND
    // offer a one-click 'Update to v2.0.0' button that triggers StoreTrainer.iss's surgical upgrade!
    private static void ScanLegacy32BitStoreTrainer(List<RaggieSoftSuiteAppItem> apps)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var storeTrainerCard = apps.FirstOrDefault(a =>
            string.Equals(a.Id, "storetrainer", StringComparison.OrdinalIgnoreCase));
        if (storeTrainerCard is null || storeTrainerCard.IsInstalled)
        {
            return;
        }

        try
        {
            using var hklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var legacyAppKey = hklm32.OpenSubKey(@"Software\RaggieSoft\StoreTrainer", writable: false);
            using var legacyUninsKey = hklm32.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer",
                writable: false);

            var legacyDir = (legacyAppKey?.GetValue("Installation Folder") as string ??
                             legacyUninsKey?.GetValue("InstallLocation") as string ??
                             string.Empty).TrimEnd('\\', '/');

            if (string.IsNullOrWhiteSpace(legacyDir))
            {
                var pfX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrWhiteSpace(pfX86))
                {
                    legacyDir = Path.Combine(pfX86, "RaggieSoft", "StoreTrainer");
                }
            }

            var legacyExe = !string.IsNullOrWhiteSpace(legacyDir)
                ? Path.Combine(legacyDir, "TStore.exe")
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(legacyExe) && File.Exists(legacyExe))
            {
                var rawVersion = legacyAppKey?.GetValue("Product Version") as string ??
                                 legacyUninsKey?.GetValue("DisplayVersion") as string ??
                                 string.Empty;
                if (string.IsNullOrWhiteSpace(rawVersion))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(legacyExe);
                    rawVersion = !string.IsNullOrWhiteSpace(fvi.FileVersion) ? fvi.FileVersion : "1.1.0";
                }

                // Normalize 4-part VB6 version ('1.1.0.0' -> '1.1.0') for clean badge display.
                var parts = rawVersion.Trim().Split('.');
                var cleanVer = parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : rawVersion.Trim();

                var legacyUninstaller = Path.Combine(legacyDir, "uninstall.exe");
                storeTrainerCard.IsInstalled = true;
                storeTrainerCard.InstalledVersion = cleanVer;
                storeTrainerCard.InstallLocation = legacyDir;
                storeTrainerCard.ExecutablePath = legacyExe;
                storeTrainerCard.UninstallerPath = File.Exists(legacyUninstaller) ? legacyUninstaller : string.Empty;
                storeTrainerCard.InstallScopeLabel = "Classic Edition • Program Files";
            }
        }
        catch
        {
            // Ignore 32-bit registry access errors
        }
    }

    // Windows Registry `UninstallString` values are often quoted command lines like
    // `"C:\Program Files\RaggieSoft\Book Reader\unins000.exe"`. This helper strips the outer
    // quotes (and any trailing flags) to extract the clean file path to `unins000.exe`.
    private static string ExtractExecutablePathFromCommand(string rawCommand)
    {
        if (string.IsNullOrWhiteSpace(rawCommand))
        {
            return string.Empty;
        }

        var trimmed = rawCommand.Trim();
        if (trimmed.StartsWith('"'))
        {
            var endQuote = trimmed.IndexOf('"', 1);
            if (endQuote > 1)
            {
                return trimmed[1..endQuote];
            }
        }

        var exeIdx = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIdx > 0)
        {
            return trimmed[..(exeIdx + 4)];
        }

        return trimmed;
    }

    /// <summary>
    /// Resolves a local <c>Setup.exe</c> installer if one was already built in <c>..\Installers\</c>
    /// or cached in <c>%LOCALAPPDATA%\RaggieSoft\Cache\Installers\</c>, or downloads it from
    /// <c>https://assets.raggiesoft.com/desktop-apps/windows/&lt;InstallerFileName&gt;</c>.
    /// </summary>
    public async Task<string> LocateOrDownloadInstallerAsync(
        RaggieSoftSuiteAppItem app,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Check adjacent or solution `Installers\` directory first (great for local builds & USB/offline bundles).
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            var localInstaller = Path.Combine(dir.FullName, "Installers", app.InstallerFileName);
            if (File.Exists(localInstaller))
            {
                progress?.Report($"Preparing {app.Title} setup...");
                return localInstaller;
            }

            var directSiblingInstaller = Path.Combine(dir.FullName, app.InstallerFileName);
            if (File.Exists(directSiblingInstaller))
            {
                progress?.Report($"Preparing {app.Title} setup...");
                return directSiblingInstaller;
            }

            dir = dir.Parent;
        }

        // 2. Download from CDN into `%LOCALAPPDATA%\RaggieSoft\Cache\Installers\` in 64 KB chunks
        //    so we can compute and report live percentage progress (`pct%`) to the user!
        var cacheInstallersDir = Path.Combine(_locator.LocalCacheDirectory, "Installers");
        Directory.CreateDirectory(cacheInstallersDir);
        var targetInstallerPath = Path.Combine(cacheInstallersDir, app.InstallerFileName);

        progress?.Report($"Downloading {app.Title}...");
        using var response = await RaggieSoftWorkspaceLocator.SharedHttpClient.GetAsync(
            app.InstallerDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var tempFile = $"{targetInstallerPath}.tmp-{Guid.NewGuid():N}";

        await using (var netStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            // 65,536 bytes = 64 KB buffer; `buffer.AsMemory(0, read)` writes ONLY the exact slice
            // of bytes received on this chunk without allocating a new array!
            var buffer = new byte[65536];
            long totalRead = 0;
            int read;
            while ((read = await netStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;
                if (totalBytes > 0)
                {
                    var pct = (int)((totalRead * 100L) / totalBytes);
                    progress?.Report($"Downloading {app.Title} Setup... {pct}% ({FormatByteSize(totalRead)} / {FormatByteSize(totalBytes)})");
                }
                else
                {
                    progress?.Report($"Downloading {app.Title} Setup... ({FormatByteSize(totalRead)})");
                }
            }
        }

        // Atomically move the finished `.tmp-*` file into place.
        File.Move(tempFile, targetInstallerPath, overwrite: true);
        return targetInstallerPath;
    }

    /// <summary>
    /// Runs an application's Inno Setup installer (either silently or interactively)
    /// and waits asynchronously for it to complete.
    /// </summary>
    public async Task<bool> RunInnoSetupInstallerAsync(
        RaggieSoftSuiteAppItem app,
        bool silentInstall,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var installerPath = await LocateOrDownloadInstallerAsync(app, progress, cancellationToken);

        // Standard Inno Setup automation flags when `silentInstall` is `true`:
        //   - `/VERYSILENT`: Installs with zero wizard windows
        //   - `/SUPPRESSMSGBOXES`: Auto-accepts default prompts without popups
        //   - `/NORESTART`: Never reboots the user's PC
        //   - `/CLOSEAPPLICATIONS`: Cleanly closes an older running copy of the app during an update
        var args = silentInstall
            ? "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"
            : string.Empty;

        progress?.Report(silentInstall
            ? $"Installing {app.Title} silently..."
            : $"Running {app.Title} Setup Wizard...");

        var psi = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = args,
            UseShellExecute = true
        };

        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return false;
        }

        // `WaitForExitAsync` pauses asynchronously until `Setup.exe` finishes; ExitCode `0` means success!
        await proc.WaitForExitAsync(cancellationToken);
        return proc.ExitCode == 0;
    }

    /// <summary>
    /// Runs an installed application's Inno Setup uninstaller (<c>unins000.exe</c>) and
    /// waits for it to complete so the Launcher can immediately refresh its status.
    /// </summary>
    public async Task<bool> RunInnoSetupUninstallerAsync(
        RaggieSoftSuiteAppItem app,
        bool silentUninstall = false,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(app.UninstallerPath) || !File.Exists(app.UninstallerPath))
        {
            return false;
        }

        var isLegacyNsisUninstaller = Path.GetFileName(app.UninstallerPath)
            .Equals("uninstall.exe", StringComparison.OrdinalIgnoreCase);
        var args = silentUninstall
            ? (isLegacyNsisUninstaller ? "/S" : "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART")
            : string.Empty;

        progress?.Report($"Running {app.Title} uninstaller...");

        var psi = new ProcessStartInfo
        {
            FileName = app.UninstallerPath,
            Arguments = args,
            UseShellExecute = true
        };

        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return false;
        }

        await proc.WaitForExitAsync(cancellationToken);
        return proc.ExitCode == 0;
    }

    /// <summary>
    /// Computes the total size in bytes of a directory tree (e.g., <c>%LOCALAPPDATA%\RaggieSoft\Cache</c>).
    /// </summary>
    public static long GetDirectorySizeBytes(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return 0L;
        }

        try
        {
            // `EnumerateFiles(..., SearchOption.AllDirectories)` lazily walks every nested subfolder
            // and `.Sum(f => f.Length)` totals up the byte size of every cached file.
            var dirInfo = new DirectoryInfo(directoryPath);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0L;
        }
    }

    /// <summary>
    /// Formats a raw byte count into a friendly human-readable <c>KB</c> / <c>MB</c> / <c>GB</c> string.
    /// </summary>
    public static string FormatByteSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 KB";
        }
        if (bytes < 1024 * 1024)
        {
            return $"{Math.Max(1, bytes / 1024.0):0.#} KB";
        }
        if (bytes < 1024L * 1024L * 1024L)
        {
            return $"{bytes / (1024.0 * 1024.0):0.0} MB";
        }
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.00} GB";
    }
}
