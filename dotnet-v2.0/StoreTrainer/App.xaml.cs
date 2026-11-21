// ================================================================================
// FILE: App.xaml.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Documentation & Creative Content)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is this file?
//     This is the C# startup coordinator that runs the instant 'StoreTrainer.exe'
//     starts on Windows — before any window appears on screen.
//
//   What four important jobs happen here?
//     1. UAC-Elevated Trainer File Copy Helper ('--elevated-copy "<src>" "<dest>"'):
//        When a user extracts a '.zip' trainer directly into a game's installation
//        folder inside 'C:\Program Files (x86)\...', Windows requires administrator
//        permission. StoreTrainer re-launches itself briefly with 'Verb = "runas"'
//        and '--elevated-copy', copies the staged trainer files into the game folder,
//        and exits immediately with code 0!
//     2. Named OS Mutex ('RaggieSoft_StoreTrainer_Mutex'):
//        Uses the exact same Mutex name in BOTH classic VB6 StoreTrainer v1.1 AND
//        modern StoreTrainer 2.0 so Inno Setup, NSIS, and RaggieSoftUpdate.exe all
//        know if StoreTrainer is open during an upgrade or uninstall.
//     3. Command-Line Switch Routing ('/l', '/e', '.stfx', and classic '.stf'):
//        - '/l "Game.stfx"' (or double-clicking a '.stfx'): Launches the game and
//          companion trainer immediately without opening the editor window, waiting
//          quietly in the background if Auto-Close Trainer or Playtime tracking is active!
//        - '/e "Game.stfx"': Opens MainWindow with that profile selected for editing.
//        - Any classic '.stf' file (via '/l', '/e', or double-click): Opens MainWindow
//          and immediately launches the 3-step **StoreFile Upgrade Wizard**!
//     4. Normal Interactive Launch:
//        Opens 'MainWindow' when started from Start Menu, Desktop, or RaggieSoft Launcher.
// ================================================================================

using System.IO;
using System.Threading;
using System.Windows;
using StoreTrainer.Services;

namespace StoreTrainer;

public partial class App : Application
{
    // Shared Win32 Mutex name watched by NSIS (v1.1), Inno Setup 7 (v2.0), and RaggieSoftUpdate.exe.
    public const string AppMutexName = "RaggieSoft_StoreTrainer_Mutex";

    private Mutex? _instanceMutex;

    // Optional '.stfx' file path passed via '/e "file.stfx"' so MainWindow opens it in the editor.
    public static string? StartupEditProfilePath { get; private set; }

    // Optional classic '.stf' file path passed on startup so MainWindow opens the StoreFile Upgrade Wizard!
    public static string? StartupClassicStfUpgradePath { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Step 1: Check for UAC-elevated trainer copy helper BEFORE acquiring the app mutex!
        if (e.Args.Length >= 3 &&
            string.Equals(e.Args[0], "--elevated-copy", StringComparison.OrdinalIgnoreCase))
        {
            var exitCode = StoreTrainerService.PerformElevatedCopy(
                e.Args[1].Trim('"'),
                e.Args[2].Trim('"'));
            Shutdown(exitCode);
            return;
        }

        // Step 2: Register the running-instance Mutex in Windows kernel memory.
        _instanceMutex = new Mutex(initiallyOwned: false, name: AppMutexName);

        base.OnStartup(e);

        // Step 3: Inspect command-line switches ('/l', '/e', '--version', deep links, or file paths).
        if (e.Args.Length > 0)
        {
            var firstArg = e.Args[0].Trim();

            if (string.Equals(firstArg, "--version", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(firstArg, "/version", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "RaggieSoft StoreTrainer v2.0.0\nCopyright © Michael P. Ragsdale (MIT License)",
                    "RaggieSoft StoreTrainer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown(0);
                return;
            }

            if ((string.Equals(firstArg, "/l", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(firstArg, "-l", StringComparison.OrdinalIgnoreCase)) &&
                e.Args.Length >= 2)
            {
                var target = e.Args[1].Trim('"');
                if (target.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
                {
                    // Classic 1.0/1.1 .stf -> open StoreFile Upgrade Wizard in MainWindow!
                    StartupClassicStfUpgradePath = target;
                }
                else
                {
                    await LaunchProfileDirectlyAndShutdownAsync(target);
                    return;
                }
            }
            else if ((string.Equals(firstArg, "/e", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(firstArg, "-e", StringComparison.OrdinalIgnoreCase)) &&
                     e.Args.Length >= 2)
            {
                var target = e.Args[1].Trim('"');
                if (target.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
                {
                    StartupClassicStfUpgradePath = target;
                }
                else
                {
                    StartupEditProfilePath = target;
                }
            }
            else if (firstArg.StartsWith("raggiesoft-storetrainer://", StringComparison.OrdinalIgnoreCase))
            {
                HandleCustomProtocolUri(firstArg, out var launchImmediatelyPath);
                if (!string.IsNullOrWhiteSpace(launchImmediatelyPath))
                {
                    await LaunchProfileDirectlyAndShutdownAsync(launchImmediatelyPath);
                    return;
                }
            }
            else
            {
                var unquoted = firstArg.Trim('"');
                if (unquoted.EndsWith(".stfx", StringComparison.OrdinalIgnoreCase) && File.Exists(unquoted))
                {
                    await LaunchProfileDirectlyAndShutdownAsync(unquoted);
                    return;
                }
                if (unquoted.EndsWith(".stf", StringComparison.OrdinalIgnoreCase) && File.Exists(unquoted))
                {
                    StartupClassicStfUpgradePath = unquoted;
                }
            }
        }

        // Step 4: Standard interactive launch — construct and display MainWindow!
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private async Task LaunchProfileDirectlyAndShutdownAsync(string cleanPath)
    {
        // Keep the WPF process alive without any visible window while awaiting game exit!
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var service = new StoreTrainerService();
        var loadResult = service.LoadStoreFile(cleanPath);

        if (!loadResult.Success || loadResult.Profile is null)
        {
            MessageBox.Show(
                loadResult.ErrorMessage,
                "RaggieSoft StoreTrainer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }

        var launchResult = await service.LaunchProfileAsync(
            loadResult.Profile,
            launchWithTrainer: true,
            confirmFingerprintMismatch: prompt =>
            {
                var answer = MessageBox.Show(
                    prompt,
                    "Trainer File Changed — RaggieSoft StoreTrainer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);
                return answer == MessageBoxResult.Yes;
            });

        if (!launchResult.Success)
        {
            MessageBox.Show(
                launchResult.Message,
                "RaggieSoft StoreTrainer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(launchResult.ErrorCode);
            return;
        }

        // Wait invisibly for the game session to end so Auto-Close Trainer & Playtime tracking finish cleanly!
        if (launchResult.SessionMonitorTask is not null)
        {
            await launchResult.SessionMonitorTask;
        }

        Shutdown(0);
    }

    private static void HandleCustomProtocolUri(string rawUri, out string? launchImmediatelyPath)
    {
        launchImmediatelyPath = null;
        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            return;
        }

        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], "file", StringComparison.OrdinalIgnoreCase))
            {
                var decodedPath = Uri.UnescapeDataString(kv[1]).Trim('"');
                if (decodedPath.EndsWith(".stf", StringComparison.OrdinalIgnoreCase))
                {
                    StartupClassicStfUpgradePath = decodedPath;
                }
                else if (string.Equals(uri.Host, "launch", StringComparison.OrdinalIgnoreCase))
                {
                    launchImmediatelyPath = decodedPath;
                }
                else
                {
                    StartupEditProfilePath = decodedPath;
                }
                return;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.Dispose();
        _instanceMutex = null;
        base.OnExit(e);
    }
}
