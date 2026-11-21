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
//   What three important jobs happen here?
//     1. Named OS Mutex ('RaggieSoft_StoreTrainer_Mutex'):
//        Creates a named Windows kernel flag while StoreTrainer is running. Notice
//        we use the exact same Mutex name in BOTH classic VB6 StoreTrainer v1.1 AND
//        modern StoreTrainer 2.0! That means Inno Setup ('StoreTrainer.iss') and
//        NSIS ('TSSetup-Patch.nsi') both know if ANY generation of StoreTrainer is
//        currently open during an upgrade or uninstall.
//     2. Command-Line Switch Routing ('/l', '/e', '--version', and '.stf' paths):
//        - 'StoreTrainer.exe /l "C:\...\Doom.stf"': Loads that profile, starts the
//          game and trainer in the right order, and exits with code 0 (no editor
//          window flashed on screen!).
//        - 'StoreTrainer.exe /e "C:\...\Doom.stf"' (or a 'raggiesoft-storetrainer://'
//          deep link): Opens MainWindow with that profile selected for editing.
//        - 'StoreTrainer.exe --version': Writes version info to a small dialog/stdout
//          and exits cleanly.
//     3. Normal Interactive Launch:
//        Constructs and displays 'MainWindow' when launched from the Start Menu,
//        Desktop shortcut, or RaggieSoft Launcher.
// ================================================================================

using System.IO;                // 'File.Exists' and 'Path' helpers for inspecting '.stf' paths.
using System.Threading;         // 'Mutex' for installer/uninstaller running-instance detection.
using System.Windows;           // WPF 'Application', 'StartupEventArgs', 'ExitEventArgs', 'MessageBox'.
using StoreTrainer.Services;    // 'StoreTrainerService' for loading & launching '.stf' profiles.

namespace StoreTrainer;

public partial class App : Application
{
    // Shared Win32 Mutex name watched by both NSIS (v1.1) and Inno Setup 7 (v2.0).
    // Note: Avoiding a 'Local\' backslash prefix keeps the name 100% identical to
    // classic Win9x-compatible VB6 'CreateMutexA(0&, 0&, "RaggieSoft_StoreTrainer_Mutex")'.
    public const string AppMutexName = "RaggieSoft_StoreTrainer_Mutex";

    // Keeps the Mutex alive in memory until OnExit runs so the .NET Garbage Collector
    // never closes the kernel handle while MainWindow is still open.
    private Mutex? _instanceMutex;

    // Optional '.stf' file path passed via '/e "file.stf"' so MainWindow can auto-select
    // and open that profile as soon as the window finishes loading.
    public static string? StartupEditProfilePath { get; private set; }

    // Fires when StoreTrainer.exe starts up.
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Step 1: Register the running-instance Mutex in Windows kernel memory.
        _instanceMutex = new Mutex(initiallyOwned: false, name: AppMutexName);

        base.OnStartup(e);

        // Step 2: Inspect command-line arguments ('e.Args' is everything typed after 'StoreTrainer.exe').
        if (e.Args.Length > 0)
        {
            var firstArg = e.Args[0].Trim();

            // Case A: '--version' diagnostic flag
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

            // Case B: '/l "path\to\profile.stf"' (or '-l') -> Fast-launch game & trainer, then exit!
            if ((string.Equals(firstArg, "/l", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(firstArg, "-l", StringComparison.OrdinalIgnoreCase)) &&
                e.Args.Length >= 2)
            {
                await LaunchProfileDirectlyAndShutdownAsync(e.Args[1]);
                return;
            }

            // Case C: '/e "path\to\profile.stf"' (or '-e') -> Open profile inside MainWindow editor!
            if ((string.Equals(firstArg, "/e", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(firstArg, "-e", StringComparison.OrdinalIgnoreCase)) &&
                e.Args.Length >= 2)
            {
                StartupEditProfilePath = e.Args[1].Trim('"');
            }
            // Case D: 'raggiesoft-storetrainer://edit?file=...' or 'raggiesoft-storetrainer://launch?file=...'
            else if (firstArg.StartsWith("raggiesoft-storetrainer://", StringComparison.OrdinalIgnoreCase))
            {
                HandleCustomProtocolUri(firstArg, out var launchImmediatelyPath);
                if (!string.IsNullOrWhiteSpace(launchImmediatelyPath))
                {
                    await LaunchProfileDirectlyAndShutdownAsync(launchImmediatelyPath);
                    return;
                }
            }
            // Case E: Bare '"C:\...\MyGame.stf"' passed directly (e.g., dragging a .stf onto StoreTrainer.exe)
            // -> Matches Windows Explorer double-click expectation: launch immediately!
            else if (firstArg.EndsWith(".stf", StringComparison.OrdinalIgnoreCase) &&
                     File.Exists(firstArg.Trim('"')))
            {
                await LaunchProfileDirectlyAndShutdownAsync(firstArg.Trim('"'));
                return;
            }
        }

        // Step 3: Standard interactive launch — construct and display MainWindow!
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    // Loads a '.stf' profile, launches its configured game and trainer, shows a friendly
    // message ONLY if something went wrong (e.g., moved game file), and exits cleanly.
    private async Task LaunchProfileDirectlyAndShutdownAsync(string rawPath)
    {
        var cleanPath = rawPath.Trim().Trim('"');
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

        var launchResult = await service.LaunchProfileAsync(loadResult.Profile, confirmFingerprintMismatch: prompt =>
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

        Shutdown(0);
    }

    // Parses 'raggiesoft-storetrainer://' deep links into either an edit target or immediate launch target.
    private static void HandleCustomProtocolUri(string rawUri, out string? launchImmediatelyPath)
    {
        launchImmediatelyPath = null;
        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            return;
        }

        // Parse simple '?file=...' query parameter without external NuGet packages.
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], "file", StringComparison.OrdinalIgnoreCase))
            {
                var decodedPath = Uri.UnescapeDataString(kv[1]).Trim('"');
                if (string.Equals(uri.Host, "launch", StringComparison.OrdinalIgnoreCase))
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

    // Releases the OS Mutex cleanly when StoreTrainer exits.
    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.Dispose();
        _instanceMutex = null;
        base.OnExit(e);
    }
}
