// ================================================================================
// FILE: RaggieSoftUpdaterLauncher.cs
// PROJECT: RaggieSoft.Core (Shared Across All RaggieSoft .NET 10 Desktop Apps)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   What is this file?
//     Every RaggieSoft Windows desktop app (Launcher, Book Reader, The Listening
//     Room, Backstage, and StoreTrainer 2.0) can check for software updates by
//     calling our shared zero-runtime C++20 update utility ('RaggieSoftUpdate.exe')!
//
//   Where does this helper look for 'RaggieSoftUpdate.exe'?
//     1. '%ProgramFiles%\RaggieSoft\Update\RaggieSoftUpdate.exe' (All-Users install)
//     2. '%LOCALAPPDATA%\Programs\RaggieSoft\Update\RaggieSoftUpdate.exe' (Per-User)
//     3. Next to the running app's own '.exe' or inside the solution's
//        'RaggieSoftUpdate\bin\...\Release\' build output folder during development!
//     4. Graceful Fallback: If 'RaggieSoftUpdate.exe' hasn't been compiled locally
//        yet on a developer machine, it falls back seamlessly to 'RaggieSoftSuiteManager'
//        to check 'apps-manifest.json' directly so "Check for Updates" ALWAYS works!
// ================================================================================

using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace RaggieSoft.Core.Services;

/// <summary>
/// Locates and invokes the standalone <c>RaggieSoftUpdate.exe</c> utility in interactive
/// wizard, <c>/SILENT</c>, or <c>/VERYSILENT</c> mode across all RaggieSoft desktop apps.
/// </summary>
public static class RaggieSoftUpdaterLauncher
{
    /// <summary>
    /// Searches standard RaggieSoft installation paths and Registry keys for <c>RaggieSoftUpdate.exe</c>.
    /// </summary>
    public static string? FindUpdaterExecutable()
    {
        var candidates = new List<string>();

        // 1. Check Registry key written by our shared Inno Setup wizard
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var hkcu = Registry.CurrentUser.OpenSubKey(@"Software\RaggieSoft\Update", writable: false);
                if (hkcu?.GetValue("ExecutablePath") is string cuPath && !string.IsNullOrWhiteSpace(cuPath))
                {
                    candidates.Add(cuPath);
                }

                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"Software\RaggieSoft\Update", writable: false);
                if (hklm?.GetValue("ExecutablePath") is string lmPath && !string.IsNullOrWhiteSpace(lmPath))
                {
                    candidates.Add(lmPath);
                }
            }
            catch
            {
                // Ignore registry read issues
            }
        }

        // 2. Check standard All-Users and Per-User install folders
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(pf))
        {
            candidates.Add(Path.Combine(pf, "RaggieSoft", "Update", "RaggieSoftUpdate.exe"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            candidates.Add(Path.Combine(localAppData, "Programs", "RaggieSoft", "Update", "RaggieSoftUpdate.exe"));
        }

        // 3. Check sibling folder or Visual Studio build output tree
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "RaggieSoftUpdate.exe"));
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            candidates.Add(Path.Combine(dir.FullName, "RaggieSoftUpdate", "bin", "x64", "Release", "RaggieSoftUpdate.exe"));
            candidates.Add(Path.Combine(dir.FullName, "RaggieSoftUpdate", "bin", "ARM64", "Release", "RaggieSoftUpdate.exe"));
            dir = dir.Parent;
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Launches <c>RaggieSoftUpdate.exe</c> interactively (focusing <paramref name="appId"/>),
    /// or returns <c>false</c> if the standalone updater binary is not yet installed on disk.
    /// </summary>
    public static bool TryLaunchInteractiveWizard(string appId)
    {
        var exe = FindUpdaterExecutable();
        if (string.IsNullOrWhiteSpace(exe))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.IsNullOrWhiteSpace(appId) ? "/WIZARD" : $"/WIZARD --app {appId}",
                WorkingDirectory = Path.GetDirectoryName(exe) ?? string.Empty,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Runs <c>RaggieSoftUpdate.exe --check-only /VERYSILENT</c> invisibly in the background
    /// and returns <c>true</c> if exit code <c>10</c> (updates available) was reported.
    /// </summary>
    public static async Task<bool> CheckVerySilentlyInBackgroundAsync(string appId, CancellationToken cancellationToken = default)
    {
        var exe = FindUpdaterExecutable();
        if (string.IsNullOrWhiteSpace(exe))
        {
            return false;
        }

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.IsNullOrWhiteSpace(appId)
                    ? "/VERYSILENT --check-only"
                    : $"/VERYSILENT --check-only --app {appId}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            if (proc is null)
            {
                return false;
            }

            await proc.WaitForExitAsync(cancellationToken);
            return proc.ExitCode == 10;
        }
        catch
        {
            return false;
        }
    }
}
