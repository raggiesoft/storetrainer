// ================================================================================
// FILE: RaggieSoftWorkspaceLocator.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file is the **Cloud CDN Client & Local Offline Cache Manager** shared by
//     every RaggieSoft Windows desktop app (`BookReader`, `ListeningRoom`, `Backstage`,
//     and `RaggieSoftLauncher`).
//
//   Why does this file exist, and how does Windows app storage work?
//     A distributed Windows `.exe` installed on a reader's laptop obviously doesn't
//     have the author's `~/Developer` workspace on disk! Instead, our apps work like
//     a hybrid streaming + offline e-reader/music player:
//       1. **Cloud Streaming (`https://assets.raggiesoft.com`)**:
//          Whenever an app needs a catalog JSON, Markdown chapter (`pXXX.md`), CSS
//          theme, `.vtt` timed lyrics file, cover image, or `.mp3` song, it checks
//          the user's local cache folder first. If the file isn't cached yet,
//          `SharedHttpClient` downloads it over HTTPS and saves a local copy.
//       2. **Two Standard Windows User Folders (`%LOCALAPPDATA%` vs `%APPDATA%`)**:
//          Windows gives every user account two separate hidden app-data folders:
//            - `LocalCacheDirectory` (`C:\Users\<You>\AppData\Local\RaggieSoft\Cache`,
//              known as `%LOCALAPPDATA%`): For large downloaded files (illustrations,
//              MP3s, offline `.7z` book packages, and `.zip` album archives) that stay
//              strictly on THIS computer so they never slow down domain sign-in.
//            - `RoamingSettingsDirectory` (`C:\Users\<You>\AppData\Roaming\RaggieSoft`,
//              known as `%APPDATA%`): For tiny kilobyte-sized JSON user preferences
//              (font choice, text size, appearance mode, and the one-time Work of
//              Fiction Notice acknowledgement).
//       3. **One-Click Offline Archives (`7za.exe` / `tar.exe` / `ZipFile`)**:
//          Before boarding a flight or subway without Wi-Fi, a user can click
//          "Save Book Offline" or "Save Album Offline". This service downloads the
//          compressed `<slug>-package.7z` or `<album>-free-archive.zip` bundle and
//          extracts every chapter, illustration, MP3, and lyric file into
//          `%LOCALAPPDATA%\RaggieSoft\Cache` for instant 100% offline reading & listening!
// ================================================================================

using System.Diagnostics;    // `Process` & `ProcessStartInfo` for running bundled `7za.exe` or `tar.exe`.
using System.IO.Compression; // `ZipFile.ExtractToDirectory` built-in .NET `.zip` extractor fallback.
using System.Net.Http;       // `HttpClient` & `HttpCompletionOption` for HTTPS downloads (like `curl` in PHP).
using System.Text.Json;      // `JsonSerializer` for reading/writing `legal-acknowledgement.json`.

namespace RaggieSoft.Core.Services;

/// <summary>
/// Manages cloud asset fetching from <c>https://assets.raggiesoft.com</c>, local flat-file caching
/// in <c>%LOCALAPPDATA%\RaggieSoft\Cache</c>, user settings storage in <c>%APPDATA%\RaggieSoft</c>,
/// and offline archive extraction.
/// </summary>
public sealed class RaggieSoftWorkspaceLocator
{
    /// <summary>
    /// Canonical production CDN root for all public RaggieSoft books, music, and Backstage assets.
    /// </summary>
    public const string ProductionAssetsCdnBaseUrl = "https://assets.raggiesoft.com";

    /// <summary>
    /// Shared <see cref="HttpClient"/> instance used for asset downloads across the entire process.
    /// </summary>
    // IMPORTANT .NET BEST PRACTICE FOR BEGINNERS:
    //   In PHP, each `curl_init()` lives for one single page request. In a long-running desktop
    //   app, creating a brand-new `new HttpClient()` on every image/song download would exhaust
    //   Windows TCP network sockets! Making `SharedHttpClient` `static readonly` shares ONE
    //   connection-pooled HTTP/2 client safely across every service and thread in the app.
    public static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    // Private helper model serialized to `%APPDATA%\RaggieSoft\legal-acknowledgement.json`
    // once the user clicks "I Understand — Continue" on first launch.
    private sealed class LegalAcknowledgementRecord
    {
        public bool Acknowledged { get; set; }
        public string AcknowledgedAtUtc { get; set; } = string.Empty;
        public string NoticeVersion { get; set; } = "2026.11";
    }

    /// <summary>
    /// End-user local flat-file cache and offline archive extraction directory
    /// (<c>%LOCALAPPDATA%\RaggieSoft\Cache</c>). Strictly used for high-volume downloaded
    /// book packages, illustrations, theme stylesheets, Backstage dossiers, and
    /// music archives so media caches never bloat Windows roaming profiles.
    /// </summary>
    // `{ get; }` (without `set;`) is a read-only property: set once in the constructor below!
    public string LocalCacheDirectory { get; }

    /// <summary>
    /// End-user roaming application settings directory (<c>%APPDATA%\RaggieSoft</c>).
    /// Strictly used for lightweight JSON user preferences (font selection, font size,
    /// appearance mode, line spacing, audio volume, and first-run notice state).
    /// </summary>
    public string RoamingSettingsDirectory { get; }

    /// <summary>
    /// Path to the bundled 7-Zip standalone executable (<c>Tools\7zip\7za.exe</c> on Windows),
    /// or <c>null</c> if not present in the current folder.
    /// </summary>
    public string? BundledSevenZipPath { get; }

    /// <summary>
    /// Initializes standard Windows <c>%LOCALAPPDATA%</c> cache and <c>%APPDATA%</c> settings
    /// directories and locates the bundled <c>7za.exe</c> archive utility.
    /// </summary>
    public RaggieSoftWorkspaceLocator()
    {
        // Ask .NET for the OS-specific LocalAppData folder (`C:\Users\<User>\AppData\Local` on Windows)
        // and ensure `RaggieSoft\Cache` exists (`Directory.CreateDirectory` is a safe no-op if it already exists).
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        LocalCacheDirectory = Path.Combine(localAppData, "RaggieSoft", "Cache");
        Directory.CreateDirectory(LocalCacheDirectory);

        // Ask .NET for Roaming AppData (`C:\Users\<User>\AppData\Roaming` on Windows).
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        RoamingSettingsDirectory = Path.Combine(roamingAppData, "RaggieSoft");
        Directory.CreateDirectory(RoamingSettingsDirectory);

        BundledSevenZipPath = ResolveBundledSevenZipPath();
    }

    /// <summary>
    /// Returns <c>true</c> if the user has already acknowledged the one-time Work of Fiction Notice
    /// across any RaggieSoft desktop application on this Windows user account.
    /// </summary>
    public bool HasAcknowledgedFictionNotice()
    {
        try
        {
            var path = Path.Combine(RoamingSettingsDirectory, "legal-acknowledgement.json");
            if (!File.Exists(path))
            {
                return false;
            }

            var record = JsonSerializer.Deserialize<LegalAcknowledgementRecord>(File.ReadAllText(path));
            return record?.Acknowledged == true;
        }
        catch
        {
            // If the JSON file was unreadable or corrupted, return `false` so the notice shows cleanly.
            return false;
        }
    }

    /// <summary>
    /// Records that the user has acknowledged the one-time Work of Fiction Notice so
    /// RaggieSoft desktop apps never need to pin a permanent multi-line bottom bar.
    /// </summary>
    public void MarkFictionNoticeAcknowledged()
    {
        try
        {
            var path = Path.Combine(RoamingSettingsDirectory, "legal-acknowledgement.json");
            var record = new LegalAcknowledgementRecord
            {
                Acknowledged = true,
                // `"O"` is .NET's ISO-8601 "round-trip" timestamp format, e.g. `"2026-11-20T06:00:00.0000000Z"`.
                AcknowledgedAtUtc = DateTime.UtcNow.ToString("O"),
                NoticeVersion = "2026.11"
            };
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Ignore transient settings write error (e.g., read-only disk) so app startup never crashes.
        }
    }

    /// <summary>
    /// Locates the bundled <c>7za.exe</c> inside the application's installation folder
    /// (<c>Tools\7zip\7za.exe</c>, copied automatically during build by <c>RaggieSoft.Core.csproj</c>).
    /// </summary>
    private static string? ResolveBundledSevenZipPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var winCandidate = Path.Combine(baseDir, "Tools", "7zip", "7za.exe");
        if (File.Exists(winCandidate))
        {
            return winCandidate;
        }

        var directCandidate = Path.Combine(baseDir, "7za.exe");
        if (File.Exists(directCandidate))
        {
            return directCandidate;
        }

        return null;
    }

    /// <summary>
    /// Checks whether a novel's offline package has been downloaded and extracted
    /// into the local user cache (<c>%LOCALAPPDATA%\RaggieSoft\Cache\raggiesoft-books\books\&lt;slug&gt;</c>).
    /// </summary>
    public bool IsBookPackageInstalledOffline(string bookSlug)
    {
        // After extracting `<slug>-package.7z`, `DownloadAndExtractBookPackage7zAsync` writes a small
        // sentinel file named `.offline_7z_installed` so checking offline status takes < 0.1 ms!
        var markerFile = Path.Combine(
            LocalCacheDirectory,
            "raggiesoft-books",
            "books",
            bookSlug,
            ".offline_7z_installed");
        return File.Exists(markerFile);
    }

    /// <summary>
    /// Checks whether an album's offline audio &amp; lyrics archive has been downloaded into the
    /// local user cache (<c>%LOCALAPPDATA%\RaggieSoft\Cache\engine-room-records\artists\&lt;artist&gt;\&lt;album&gt;</c>).
    /// </summary>
    public bool IsAlbumInstalledOffline(string artistSlug, string albumSlug)
    {
        var markerFile = Path.Combine(
            LocalCacheDirectory,
            "engine-room-records",
            "artists",
            artistSlug,
            albumSlug,
            ".offline_album_installed");
        return File.Exists(markerFile);
    }

    /// <summary>
    /// Checks whether the RaggieSoft Backstage dossier archive has been downloaded
    /// for offline reading (<c>%LOCALAPPDATA%\RaggieSoft\Cache\raggiesoft-backstage</c>).
    /// </summary>
    public bool IsBackstageArchiveInstalledOffline()
    {
        var markerFile = Path.Combine(
            LocalCacheDirectory,
            "raggiesoft-backstage",
            ".offline_backstage_installed");
        return File.Exists(markerFile);
    }

    /// <summary>
    /// Resolves an asset path starting with <c>"/raggiesoft-books/..."</c>,
    /// <c>"/engine-room-records/..."</c>, <c>"/raggiesoft-backstage/..."</c>, or <c>"{{CDN}}/..."</c>
    /// into a local cached file path (if already downloaded/extracted offline)
    /// or an <c>https://assets.raggiesoft.com</c> URL.
    /// </summary>
    public string ResolveAssetUri(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return string.Empty;
        }

        // Strip Markdown layout fragment suffixes such as `#fullwidth` or `#inline` before resolving
        // (`rawPath[..hashIdx]` slices everything before the `#` character).
        var hashIdx = rawPath.IndexOf('#');
        var cleanRaw = hashIdx > 0 ? rawPath[..hashIdx] : rawPath;

        // Convert web forward-slashes (`/`) into OS directory separators (`\` on Windows) and check
        // whether that exact relative path already exists inside `%LOCALAPPDATA%\RaggieSoft\Cache`!
        var normalized = NormalizeRelativeAssetPath(cleanRaw);
        var cachedFile = Path.Combine(LocalCacheDirectory, normalized.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(cachedFile))
        {
            return cachedFile;
        }

        // Not cached on disk yet -> return the full HTTPS CDN address.
        return $"{ProductionAssetsCdnBaseUrl}/{normalized}";
    }

    /// <summary>
    /// Ensures a binary asset (such as a <c>/web-mp3/*.mp3</c> audio track or <c>*.jpg</c> image)
    /// is downloaded into <c>%LOCALAPPDATA%\RaggieSoft\Cache</c> and returns its absolute
    /// local file system path on disk.
    /// Passing a local file URI to WPF's <c>MediaPlayer</c> and <c>BitmapImage</c> avoids legacy
    /// WinInet HTTPS streaming stalls and enables instant offline replay.
    /// </summary>
    public async Task<string> EnsureAssetCachedLocallyAsync(
        string relativeOrFullAssetPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeOrFullAssetPath))
        {
            return string.Empty;
        }

        // Strip any URL fragment (`#fullwidth` / `#inline`).
        var hashIdx = relativeOrFullAssetPath.IndexOf('#');
        var withoutFragment = hashIdx > 0 ? relativeOrFullAssetPath[..hashIdx] : relativeOrFullAssetPath;

        // If the caller already passed a full local path (`C:\Users\...`) that exists on disk, return it immediately!
        if (Path.IsPathRooted(withoutFragment) && File.Exists(withoutFragment))
        {
            return withoutFragment;
        }

        // Map the relative CDN path into `%LOCALAPPDATA%\RaggieSoft\Cache\<relative-path>`.
        // If a non-empty cached file already exists, reuse it with zero network traffic!
        var cleanRel = NormalizeRelativeAssetPath(withoutFragment);
        var localPath = Path.Combine(LocalCacheDirectory, cleanRel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(localPath))
        {
            var existingInfo = new FileInfo(localPath);
            if (existingInfo.Length > 0)
            {
                return localPath;
            }
        }

        // Ensure parent subdirectories exist before writing.
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var cdnUrl = $"{ProductionAssetsCdnBaseUrl}/{cleanRel}";

        // ATOMIC TEMP-FILE DOWNLOAD PATTERN:
        //   Never stream directly into `localPath`! If the user's Wi-Fi drops halfway through an MP3,
        //   streaming straight into `localPath` would leave a half-written, broken `.mp3` on disk.
        //   Instead, we stream into a unique `.tmp-<guid>` file first, and ONLY rename (`File.Move`)
        //   it to `localPath` once 100% of the bytes have arrived safely!
        var tempPath = $"{localPath}.tmp-{Guid.NewGuid():N}";

        try
        {
            using var response = await SharedHttpClient.GetAsync(
                cdnUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            File.Move(tempPath, localPath, overwrite: true);
            return localPath;
        }
        catch
        {
            // Clean up any partial `.tmp-*` file if the download failed or was canceled, then re-throw (`throw;`)
            // so the caller knows the asset couldn't be fetched while offline.
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore temp file cleanup error
            }
            throw;
        }
    }

    /// <summary>
    /// Reads a UTF-8 text file (JSON, Markdown, CSS, or timed lyrics) from the user's local
    /// offline cache first, or downloads it from <c>https://assets.raggiesoft.com</c> and caches it.
    /// </summary>
    public async Task<string> ReadAssetTextAsync(string relativeAssetPath, string? unusedLegacyParam = null)
    {
        var cleanRel = NormalizeRelativeAssetPath(relativeAssetPath);
        var cacheFile = Path.Combine(LocalCacheDirectory, cleanRel.Replace('/', Path.DirectorySeparatorChar));

        // 1. Serve immediately from local offline cache if already present
        if (File.Exists(cacheFile))
        {
            return await File.ReadAllTextAsync(cacheFile);
        }

        // 2. Download from https://assets.raggiesoft.com and persist in local cache
        var cdnUrl = $"{ProductionAssetsCdnBaseUrl}/{cleanRel}";
        var content = await SharedHttpClient.GetStringAsync(cdnUrl);
        var cacheDir = Path.GetDirectoryName(cacheFile);
        if (!string.IsNullOrWhiteSpace(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
            await File.WriteAllTextAsync(cacheFile, content);
        }

        return content;
    }

    /// <summary>
    /// Downloads <c>&lt;slug&gt;-package.7z</c> from <c>https://assets.raggiesoft.com/raggiesoft-books/books/&lt;slug&gt;/&lt;slug&gt;-package.7z</c>
    /// and extracts it into <c>%LOCALAPPDATA%\RaggieSoft\Cache\raggiesoft-books\books\&lt;slug&gt;\</c>.
    /// </summary>
    // `IProgress<string>? progress` is .NET's thread-safe status callback interface: calling
    // `progress?.Report("...")` automatically marshals the status message back onto WPF's UI thread
    // to update the window's status bar!
    public async Task<bool> DownloadAndExtractBookPackage7zAsync(
        string bookSlug,
        IProgress<string>? progress = null,
        IEnumerable<string>? referencedThemes = null)
    {
        var bookCacheDir = Path.Combine(LocalCacheDirectory, "raggiesoft-books", "books", bookSlug);
        Directory.CreateDirectory(bookCacheDir);

        var archiveName = $"{bookSlug}-package.7z";
        var archiveUrl = $"{ProductionAssetsCdnBaseUrl}/raggiesoft-books/books/{bookSlug}/{archiveName}";
        var localArchivePath = Path.Combine(bookCacheDir, archiveName);

        // Download the compressed `.7z` package containing all Markdown parts + illustrations for this novel.
        progress?.Report("Downloading book for offline reading...");
        var bytes = await SharedHttpClient.GetByteArrayAsync(archiveUrl);
        await File.WriteAllBytesAsync(localArchivePath, bytes);

        // Extract into `bookCacheDir` and remove the temporary `.7z` archive file to save disk space.
        progress?.Report("Preparing offline book & illustrations...");
        var extracted = await ExtractArchiveWithSevenZipAsync(localArchivePath, bookCacheDir);
        try
        {
            File.Delete(localArchivePath);
        }
        catch
        {
            // Ignore temp archive cleanup error
        }

        // Pre-cache any custom narrative theme stylesheets (`/raggiesoft-books/css/themes/<theme>.css`)
        // used by chapters in this novel so custom chapter colors also work 100% offline!
        if (referencedThemes is not null)
        {
            foreach (var themeSlug in referencedThemes.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var cleanTheme = themeSlug.Trim().EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                    ? themeSlug.Trim()
                    : $"{themeSlug.Trim()}.css";
                await TryDownloadFileToCacheAsync($"raggiesoft-books/css/themes/{cleanTheme}");
            }
        }

        // Drop the `.offline_7z_installed` marker file once extraction succeeds.
        if (extracted)
        {
            await File.WriteAllTextAsync(
                Path.Combine(bookCacheDir, ".offline_7z_installed"),
                DateTime.UtcNow.ToString("O"));
            progress?.Report("Book saved for offline reading.");
        }

        return extracted;
    }

    /// <summary>
    /// Downloads the free album archive or individual <c>/web-mp3/</c> tracks + lyrics + creation notes
    /// from <c>https://assets.raggiesoft.com/engine-room-records/artists/&lt;artist&gt;/&lt;album&gt;/</c> and
    /// stores them in <c>%LOCALAPPDATA%\RaggieSoft\Cache</c> for 100% offline playback.
    /// </summary>
    public async Task<bool> DownloadAlbumOfflineAsync(
        string artistSlug,
        string albumSlug,
        IEnumerable<string> trackFileNames,
        IProgress<string>? progress = null)
    {
        var albumCacheDir = Path.Combine(LocalCacheDirectory, "engine-room-records", "artists", artistSlug, albumSlug);
        var webMp3Dir = Path.Combine(albumCacheDir, "web-mp3");
        var lyricsDir = Path.Combine(albumCacheDir, "lyrics");
        var promptsDir = Path.Combine(albumCacheDir, "suno-prompts");

        Directory.CreateDirectory(webMp3Dir);
        Directory.CreateDirectory(lyricsDir);
        Directory.CreateDirectory(promptsDir);

        // 1. Download square album cover art (`album-art.jpg`).
        await TryDownloadFileToCacheAsync(
            $"engine-room-records/artists/{artistSlug}/{albumSlug}/album-art.jpg");

        // 2. Try downloading Harper's pre-packed `*-free-archive.zip` bundle first if available on CDN
        //    (downloading one `.zip` is much faster than making a dozen separate HTTP requests!).
        var freeZipUrl = $"{ProductionAssetsCdnBaseUrl}/engine-room-records/artists/{artistSlug}/{albumSlug}/web-mp3/{albumSlug}-free-archive.zip";
        var localZipPath = Path.Combine(webMp3Dir, $"{albumSlug}-free-archive.zip");
        var extractedFromZip = false;

        try
        {
            progress?.Report("Downloading album for offline listening...");
            using var response = await SharedHttpClient.GetAsync(freeZipUrl);
            if (response.IsSuccessStatusCode)
            {
                var zipBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(localZipPath, zipBytes);

                progress?.Report("Preparing offline album & lyrics...");
                extractedFromZip = await ExtractArchiveWithSevenZipAsync(localZipPath, webMp3Dir);
                File.Delete(localZipPath);

                // Copy any `lyrics/` and `suno-prompts/` subfolders unpacked from the archive up into `albumCacheDir`.
                var extractedLyrics = Path.Combine(webMp3Dir, "lyrics");
                if (Directory.Exists(extractedLyrics))
                {
                    foreach (var file in Directory.GetFiles(extractedLyrics))
                    {
                        File.Copy(file, Path.Combine(lyricsDir, Path.GetFileName(file)), overwrite: true);
                    }
                }

                var extractedPrompts = Path.Combine(webMp3Dir, "suno-prompts");
                if (Directory.Exists(extractedPrompts))
                {
                    foreach (var file in Directory.GetFiles(extractedPrompts))
                    {
                        File.Copy(file, Path.Combine(promptsDir, Path.GetFileName(file)), overwrite: true);
                    }
                }
            }
        }
        catch
        {
            // Fall back to per-track /web-mp3/ download below
        }

        // 3. Ensure every track's audio, timed `.vtt` lyrics, full `.md` lyrics, and `suno-prompts/*.md`
        //    creation notes are cached locally.
        var trackList = trackFileNames.ToList();
        for (var i = 0; i < trackList.Count; i++)
        {
            var fileName = trackList[i];
            progress?.Report($"Saving track {i + 1} of {trackList.Count}...");

            if (!extractedFromZip)
            {
                await TryDownloadFileToCacheAsync(
                    $"engine-room-records/artists/{artistSlug}/{albumSlug}/web-mp3/{fileName}.mp3");
            }

            await TryDownloadFileToCacheAsync(
                $"engine-room-records/artists/{artistSlug}/{albumSlug}/lyrics/{fileName}.vtt");
            await TryDownloadFileToCacheAsync(
                $"engine-room-records/artists/{artistSlug}/{albumSlug}/lyrics/{fileName}.md");
            await TryDownloadFileToCacheAsync(
                $"engine-room-records/artists/{artistSlug}/{albumSlug}/suno-prompts/{fileName}.md");
        }

        // Write the `.offline_album_installed` sentinel marker file.
        await File.WriteAllTextAsync(
            Path.Combine(albumCacheDir, ".offline_album_installed"),
            DateTime.UtcNow.ToString("O"));

        progress?.Report("Album saved for offline listening.");
        return true;
    }

    // Best-effort helper that downloads an optional companion file into `LocalCacheDirectory` if missing,
    // ignoring 404 errors (for example, an instrumental track that has no `.vtt` vocal lyric file).
    private async Task TryDownloadFileToCacheAsync(string relativeAssetPath)
    {
        var cleanRel = NormalizeRelativeAssetPath(relativeAssetPath);
        var localPath = Path.Combine(LocalCacheDirectory, cleanRel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(localPath))
        {
            return;
        }

        try
        {
            var bytes = await SharedHttpClient.GetByteArrayAsync($"{ProductionAssetsCdnBaseUrl}/{cleanRel}");
            var dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllBytesAsync(localPath, bytes);
        }
        catch
        {
            // Optional file (e.g., instrumental track without timed lyrics)
        }
    }

    /// <summary>
    /// Extracts an archive (<c>.7z</c> or <c>.zip</c>) into <paramref name="targetDirectory"/> using a
    /// resilient 3-tier fallback chain: bundled <c>7za.exe</c> -&gt; Windows <c>System32\tar.exe</c> -&gt; .NET <see cref="ZipFile"/>.
    /// </summary>
    public async Task<bool> ExtractArchiveWithSevenZipAsync(string archivePath, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        // Tier 1: Try bundled 7-Zip standalone CLI (`Tools\7zip\7za.exe`).
        // `x -y` extracts with full folder structure (`x`) and auto-answers Yes (`-y`) to overwrites;
        // `CreateNoWindow = true` runs it silently in the background with zero flashing black console box!
        if (!string.IsNullOrWhiteSpace(BundledSevenZipPath) && File.Exists(BundledSevenZipPath))
        {
            var psi = new ProcessStartInfo
            {
                FileName = BundledSevenZipPath,
                Arguments = $"x -y \"{archivePath}\" -o\"{targetDirectory}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            if (proc is not null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0)
                {
                    return true;
                }
            }
        }

        // Tier 2: Try modern Windows' built-in `C:\Windows\System32\tar.exe` (based on `libarchive`,
        // which natively unpacks `.zip`, `.tar`, and `.7z` archives!).
        var systemTar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
        if (File.Exists(systemTar))
        {
            var tarPsi = new ProcessStartInfo
            {
                FileName = systemTar,
                Arguments = $"-xf \"{archivePath}\" -C \"{targetDirectory}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var tarProc = Process.Start(tarPsi);
            if (tarProc is not null)
            {
                await tarProc.WaitForExitAsync();
                if (tarProc.ExitCode == 0)
                {
                    return true;
                }
            }
        }

        // Tier 3: Pure managed .NET fallback (`System.IO.Compression.ZipFile`) for standard `.zip` files.
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, targetDirectory, overwriteFiles: true);
            return true;
        }

        return false;
    }

    // Strips `{{CDN}}` template placeholders, full `https://assets.raggiesoft.com` prefixes,
    // and leading `/` slashes so every asset maps cleanly to a relative cache path like
    // `"raggiesoft-books/books/rachel/toc.json"`.
    private static string NormalizeRelativeAssetPath(string rawPath)
    {
        return rawPath.Trim()
            .Replace("{{CDN}}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(ProductionAssetsCdnBaseUrl, string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimStart('/');
    }
}
