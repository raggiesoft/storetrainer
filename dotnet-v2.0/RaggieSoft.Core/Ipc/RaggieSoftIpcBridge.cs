// ================================================================================
// FILE: RaggieSoftIpcBridge.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file builds a private, two-way "walkie-talkie" channel between the
//     separate Windows desktop programs in the RaggieSoft suite:
//       - RaggieSoft Book Reader (BookReader.exe)
//       - RaggieSoft The Listening Room (ListeningRoom.exe)
//       - RaggieSoft Backstage (Backstage.exe)
//
//   Why does this file exist?
//     On a website (HTML/PHP), jumping from a book page to a song player is just
//     an `<a href="...">` link inside one web browser. On Windows, however, each
//     `.exe` app runs inside its own isolated "process" (its own walled-off slice
//     of computer memory). Windows intentionally blocks one `.exe` from reaching
//     directly into another `.exe`'s variables so a buggy program can't crash its
//     neighbors.
//
//     To let two desktop apps cooperate safely, operating systems provide
//     **Inter-Process Communication (IPC)**. Specifically, this file uses
//     **Windows Named Pipes** (`System.IO.Pipes`):
//       - Think of a Named Pipe like a private pneumatic tube inside the Windows
//         operating system kernel, labeled with a name like
//         `"RaggieSoft.ListeningRoom.Ipc"`.
//       - Unlike an HTTP web server (`localhost:8080`), a local Named Pipe lives
//         purely in local OS memory (`"."`). It NEVER opens a TCP/IP network port,
//         NEVER touches Wi-Fi or the internet, and NEVER triggers scary Windows
//         Defender Firewall pop-ups for the user!
//
//   What data flows through these pipes, and what visible features does it power?
//     Messages are tiny JSON strings (serialized from `RaggieSoftIpcMessage`):
//       1. BookReader -> ListeningRoom ("PLAY_TRACK" / "TOGGLE_PLAY_PAUSE"):
//          When a reader clicks a song link or "Open in The Listening Room" while
//          reading a chapter, BookReader sends a JSON message down ListeningRoom's
//          pipe. If ListeningRoom isn't open yet, `SendOrLaunchSiblingAsync` finds
//          `ListeningRoom.exe` on disk, launches it automatically, waits for its
//          pipe to wake up, and then hands off the song!
//       2. ListeningRoom -> BookReader ("NOW_PLAYING"):
//          As ListeningRoom plays music and steps through timed `.vtt` lyrics, it
//          broadcasts the current song title, artist, timestamp, and active lyric
//          line back to BookReader so BookReader's bottom Companion Dock updates
//          live in real time.
//       3. ListeningRoom / Backstage -> BookReader ("OPEN_BOOK") &
//          BookReader -> Backstage ("OPEN_BACKSTAGE"):
//          Clicking a companion chapter link in Backstage or ListeningRoom jumps
//          BookReader straight to that exact novel, chapter part, or Story Arc;
//          clicking "Open in RaggieSoft Backstage" in BookReader opens the matching
//          behind-the-scenes dossier in `Backstage.exe`.
// ================================================================================

// `using` directives at the top of a C# file work like `use` or `require_once` in PHP:
// they import built-in .NET namespaces (toolkits) so we can write `StreamReader` or
// `JsonSerializer` directly instead of spelling out `System.Text.Json.JsonSerializer`
// every single time.
// (Note: `RaggieSoft.Core.csproj` also enables `<ImplicitUsings>enable</ImplicitUsings>`,
// which automatically imports everyday namespaces like `System`, `System.IO`,
// `System.Collections.Generic`, `System.Linq`, and `System.Threading.Tasks`.)
using System.Diagnostics; // Gives us `Process` and `ProcessStartInfo` to launch sibling `.exe` apps.
using System.IO.Pipes;    // Gives us `NamedPipeServerStream` and `NamedPipeClientStream` for local IPC.
using System.Text;        // Gives us `Encoding.UTF8` to convert JSON text to/from raw bytes.
using System.Text.Json;   // Gives us `JsonSerializer` (C#'s fast, built-in `json_encode` / `json_decode`).

// A file-scoped `namespace` (ending with a semicolon `;`, introduced in modern C#)
// groups every class in this file under the logical address `RaggieSoft.Core.Ipc`,
// preventing naming collisions across large projects—just like `namespace RaggieSoft\Core\Ipc;` in PHP.
namespace RaggieSoft.Core.Ipc;

/// <summary>
/// Wire payload exchanged over Named Pipes between BookReader, ListeningRoom, and Backstage.
/// Each message is serialized to a UTF-8 JSON object when sent through a pipe and
/// deserialized back into an instance of this C# class on the receiving side.
/// </summary>
// C# keywords on this line:
//   - `public`: Any project that references `RaggieSoft.Core` (such as `BookReader` or
//     `ListeningRoom`) is allowed to see and use this class.
//   - `sealed`: Means no other class can inherit from (`extend`) this class. Sealing
//     data classes keeps the design simple and lets the .NET 10 JIT compiler optimize calls.
//   - `class`: A blueprint for a reference-type object stored in memory (similar to a PHP `class`).
public sealed class RaggieSoftIpcMessage
{
    // In C#, `{ get; set; }` defines an **auto-property**—a variable attached to the object
    // with an automatic getter (`get`) and setter (`set`).
    // Setting `= string.Empty;` gives the property a safe default `""` (empty string) instead
    // of `null` so callers never crash with a `NullReferenceException` if a field wasn't
    // included in the incoming JSON payload!

    /// <summary>
    /// Command verb telling the receiving app what action to take:
    /// <c>"PLAY_TRACK"</c>, <c>"TOGGLE_PLAY_PAUSE"</c>, <c>"NOW_PLAYING"</c>,
    /// <c>"OPEN_BOOK"</c>, or <c>"OPEN_BACKSTAGE"</c>.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    // --- Music Target Coordinates (used by "PLAY_TRACK") ---
    // URL-friendly folder identifiers ("slugs"), e.g. ArtistSlug = "the-stardust-engine",
    // AlbumSlug = "1997-hard-reset", TrackSlug = "01-hard-reset".
    public string ArtistSlug { get; set; } = string.Empty;
    public string AlbumSlug { get; set; } = string.Empty;
    public string TrackSlug { get; set; } = string.Empty;

    // --- Book & Backstage Target Coordinates (used by "OPEN_BOOK" and "OPEN_BACKSTAGE") ---
    // Identifies which novel (`BookSlug`, e.g. "rachel"), chapter Markdown part (`BookPartPath`,
    // e.g. "e001/c001/p001.md"), curated reading playlist (`StoryArcSlug`), or Backstage
    // dossier (`BackstageSlug`, e.g. "books/rachel/...") to open.
    public string BookSlug { get; set; } = string.Empty;
    public string BookPartPath { get; set; } = string.Empty;
    public string StoryArcSlug { get; set; } = string.Empty;
    public string BackstageSlug { get; set; } = string.Empty;

    // --- Live Playback Telemetry (used by "NOW_PLAYING" broadcasts from ListeningRoom) ---
    // Lets BookReader's Companion Dock display the currently playing song title, artist,
    // album, synchronized `.vtt` lyric line, elapsed time ("01:42 / 04:15"), and play/pause
    // icon without BookReader needing to decode the audio file itself.
    public string TrackTitle { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public string ActiveLyricLine { get; set; } = string.Empty;
    public string PositionLabel { get; set; } = string.Empty;

    // `bool` is C#'s boolean type (`true` or `false`); its automatic default value is `false`.
    public bool IsPlaying { get; set; }
}

/// <summary>
/// Manages an application's incoming Named Pipe listener loop on a background thread
/// and provides static helper methods to send IPC commands (and auto-launch companion
/// executables when needed).
/// </summary>
// `: IDisposable` means this class implements .NET's standard cleanup interface (`IDisposable`).
// Because our bridge runs a continuous background listener loop tied to an operating-system
// `CancellationTokenSource`, implementing `Dispose()` lets `MainWindow` cleanly shut down
// the background loop and release OS handles the moment the user closes the window.
public sealed class RaggieSoftIpcBridge : IDisposable
{
    // `public const string` declares compile-time constants—fixed pipe addresses shared by
    // every app in the suite so each app knows the exact "phone number" of its siblings.
    // Under the hood on Windows, `"RaggieSoft.ListeningRoom.Ipc"` becomes the kernel object
    // `\\.\pipe\RaggieSoft.ListeningRoom.Ipc`.
    public const string ListeningRoomPipeName = "RaggieSoft.ListeningRoom.Ipc";
    public const string BookReaderPipeName = "RaggieSoft.BookReader.Ipc";
    public const string BackstagePipeName = "RaggieSoft.Backstage.Ipc";

    // `private readonly` fields are internal state variables that ONLY code inside this class
    // can touch (`private`), and they can ONLY be assigned once inside the constructor (`readonly`).
    // By convention in C#, private fields use `_camelCase` with a leading underscore.

    // The Named Pipe address THIS running app listens on (e.g., BookReader passes `BookReaderPipeName`).
    private readonly string _listenPipeName;

    // `CancellationTokenSource` is .NET's cooperative "stop button" for background tasks.
    // Calling `_cts.Cancel()` in `Dispose()` flips `_cts.Token.IsCancellationRequested` to `true`
    // and wakes up any `await` that is currently waiting for a pipe connection.
    // (`new()` is C#'s target-typed `new` shorthand—same as `new CancellationTokenSource()`.)
    private readonly CancellationTokenSource _cts = new();

    // `Action<RaggieSoftIpcMessage>` is a strongly typed callback function (similar to passing a
    // `callable` or closure in PHP) that takes a `RaggieSoftIpcMessage` and returns `void` (nothing).
    // Whenever a sibling app sends us a message, we invoke this callback so the app's window can react!
    private readonly Action<RaggieSoftIpcMessage> _onMessageReceived;

    /// <summary>
    /// Creates and immediately starts a background Named Pipe listener for the current application.
    /// </summary>
    /// <param name="listenPipeName">The pipe name this app owns (e.g., <see cref="BookReaderPipeName"/>).</param>
    /// <param name="onMessageReceived">Callback invoked whenever a valid JSON message arrives.</param>
    public RaggieSoftIpcBridge(string listenPipeName, Action<RaggieSoftIpcMessage> onMessageReceived)
    {
        // Save the pipe name and callback function into our `readonly` fields.
        _listenPipeName = listenPipeName;
        _onMessageReceived = onMessageReceived;

        // `Task.Run(...)` schedules `RunPipeServerLoopAsync` to run on a .NET background thread-pool
        // thread so waiting for incoming messages NEVER freezes the app's buttons, scrolling, or audio!
        // `() => ...` is a C# lambda expression (anonymous function, like `fn() => ...` in PHP).
        // `_ =` is a C# "discard": it explicitly tells the compiler "we intentionally launched this
        // long-running background loop as fire-and-forget; we will stop it later via `_cts.Cancel()`."
        _ = Task.Run(() => RunPipeServerLoopAsync(_cts.Token));
    }

    /// <summary>
    /// Continuously listens for incoming IPC messages on this application's Named Pipe.
    /// Each message uses a short-lived single-connection pipe cycle: wait for a sender to
    /// connect, read its UTF-8 JSON message until the sender closes its stream, dispatch the
    /// parsed <see cref="RaggieSoftIpcMessage"/>, and immediately loop around to await the next message.
    /// </summary>
    // `async Task` marks an asynchronous method (like `async function` returning a `Promise` in JS).
    // Every `await` inside pauses ONLY this method without blocking any OS thread until data arrives!
    private async Task RunPipeServerLoopAsync(CancellationToken cancellationToken)
    {
        // Keep accepting messages one after another until the user closes the app (`_cts.Cancel()`).
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Open a brand-new OS Named Pipe server endpoint for the next incoming message.
                // `await using var` automatically closes and disposes the OS pipe handle at the end
                // of this `try` block—even if an exception occurs!
                // Let's break down the 5 arguments passed to `NamedPipeServerStream`:
                //   1. `_listenPipeName`: Which pipe name we are answering (e.g. "RaggieSoft.BookReader.Ipc").
                //   2. `PipeDirection.In`: This server stream only RECEIVES bytes; when our app wants
                //      to talk back, it uses `SendMessageAsync` to open a client stream to the other app.
                //   3. `MaxAllowedServerInstances`: Allows Windows to overlap/re-create pipe instances
                //      cleanly if rapid-fire updates (like live lyrics) arrive back-to-back.
                //   4. `PipeTransmissionMode.Byte`: Streams a continuous sequence of UTF-8 bytes.
                //   5. `PipeOptions.Asynchronous`: Enables non-blocking OS-level overlapped I/O so
                //      `WaitForConnectionAsync` consumes zero CPU while asleep waiting for a caller.
                await using var server = new NamedPipeServerStream(
                    _listenPipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // Pause asynchronously right here (using 0% CPU) until a sibling app connects to our pipe,
                // OR until `cancellationToken` fires when our app is closing.
                await server.WaitForConnectionAsync(cancellationToken);

                // Wrap the raw byte pipe in a `StreamReader` that decodes UTF-8 text (`Encoding.UTF8`),
                // then read the entire JSON string sent by the client until the client closes its stream.
                using var reader = new StreamReader(server, Encoding.UTF8);
                var payload = await reader.ReadToEndAsync(cancellationToken);

                // Guard against empty pings or whitespace-only connections.
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    // `JsonSerializer.Deserialize<RaggieSoftIpcMessage>(payload)` is C#'s generic
                    // equivalent of PHP's `json_decode($payload)`: the `<RaggieSoftIpcMessage>`
                    // angle-bracket syntax ("generics") tells .NET the exact C# class to build and populate!
                    var msg = JsonSerializer.Deserialize<RaggieSoftIpcMessage>(payload);

                    // `is not null` is modern C#'s null check (safe even if `==` were ever overloaded).
                    if (msg is not null)
                    {
                        // Hand the decoded message to the owning app's callback!
                        // Note for WPF beginners: because `RunPipeServerLoopAsync` runs on a background
                        // thread, the WPF window's callback uses `Dispatcher.InvokeAsync(...)` to hop
                        // back onto the main UI thread before updating on-screen labels or buttons.
                        _onMessageReceived(msg);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal, expected shutdown path! When the user closes the window, `Dispose()`
                // cancels `cancellationToken`, which throws `OperationCanceledException` to wake
                // `WaitForConnectionAsync` up immediately. We `break` out of the `while` loop cleanly.
                break;
            }
            catch
            {
                // Defensive recovery: if malformed JSON arrived or Windows briefly recycled a pipe
                // handle, wait 250 milliseconds (quarter of a second) before looping so an unexpected
                // OS error can never spin CPU cores at 100% in a tight infinite loop.
                await Task.Delay(250, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Sends a JSON IPC message to the target pipe (ListeningRoom, BookReader, or Backstage).
    /// Returns <c>true</c> if the sibling app was running and accepted the message within
    /// <paramref name="timeoutMs"/> milliseconds; returns <c>false</c> if the sibling app is not open.
    /// </summary>
    // `static` means you can call `RaggieSoftIpcBridge.SendMessageAsync(...)` directly without
    // needing an object instance—just like a `public static function` in PHP.
    // `Task<bool>` means this `async` method eventually produces a `true` or `false` result.
    // `int timeoutMs = 900` is an optional parameter: if the caller omits it, C# uses 900 ms (0.9s).
    public static async Task<bool> SendMessageAsync(
        string targetPipeName,
        RaggieSoftIpcMessage message,
        int timeoutMs = 900)
    {
        try
        {
            // Open an outgoing (`PipeDirection.Out`) Named Pipe client aimed at `"."` (Windows shorthand
            // for "this local computer ONLY—never search the local network").
            // `await using var` ensures the pipe disconnects and flushes cleanly as soon as we return,
            // signaling end-of-stream (`ReadToEndAsync`) to the receiving `NamedPipeServerStream`!
            await using var client = new NamedPipeClientStream(".", targetPipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            // Try connecting to the sibling app's pipe. If the sibling `.exe` isn't currently running,
            // `ConnectAsync` will time out after `timeoutMs` milliseconds and jump to `catch` -> `false`.
            await client.ConnectAsync(timeoutMs);

            // Convert our C# `RaggieSoftIpcMessage` object into a compact JSON string (like `json_encode`),
            // encode that string into UTF-8 bytes, write the bytes into the pipe, and flush!
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await client.WriteAsync(bytes);
            await client.FlushAsync();
            return true;
        }
        catch
        {
            // Reaching here simply means the target app isn't open right now (or timed out).
            // Returning `false` lets callers like `SendOrLaunchSiblingAsync` decide whether to
            // launch the companion `.exe` automatically or ignore a live-lyric broadcast.
            return false;
        }
    }

    /// <summary>
    /// Sends a message to the sibling application; if the sibling app is not currently
    /// running, attempts to locate and launch its executable from the sibling build/install
    /// folder and then delivers the IPC command once its pipe comes online.
    /// </summary>
    public static async Task<bool> SendOrLaunchSiblingAsync(
        string targetPipeName,
        string siblingExeName,
        RaggieSoftIpcMessage message)
    {
        // Fast path: try sending the message first (waiting up to 600 ms).
        // If the companion app (e.g. `ListeningRoom.exe`) is already open on the user's desktop,
        // it receives the message immediately and we're done!
        if (await SendMessageAsync(targetPipeName, message, 600))
        {
            return true;
        }

        // Slow path: the companion app wasn't running yet, so let's search disk for its `.exe`!
        var exePath = FindSiblingExecutable(siblingExeName);
        if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
        {
            try
            {
                // Ask the Windows Shell (`UseShellExecute = true`) to start the companion `.exe`
                // as an independent desktop window—just as if the user double-clicked its shortcut.
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                // A newly launched WPF `.exe` needs a moment to load its window and start its
                // `RaggieSoftIpcBridge` listener. Poll up to 8 times * 500 ms = 4.0 seconds total;
                // the instant its Named Pipe comes online and accepts our message, return `true`!
                for (var attempt = 0; attempt < 8; attempt++)
                {
                    await Task.Delay(500);
                    if (await SendMessageAsync(targetPipeName, message, 600))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore process launch failures (for example, if security policy blocked launching)
                // and fall back gracefully to `false` so the calling app can offer a web link instead.
            }
        }

        // Neither an open pipe nor a launchable local `.exe` succeeded.
        return false;
    }

    /// <summary>
    /// Searches sibling project build folders and installed Program Files / LocalAppData locations
    /// for the companion executable (<c>ListeningRoom.exe</c>, <c>BookReader.exe</c>, or <c>Backstage.exe</c>).
    /// </summary>
    // Notice the `?` in `string?`: with `<Nullable>enable</Nullable>` turned on in C#, a plain
    // `string` promises never to be `null`, whereas `string?` explicitly warns callers:
    // "this method returns a file path string if found, OR `null` if the companion app isn't installed."
    private static string? FindSiblingExecutable(string siblingExeName)
    {
        // `AppContext.BaseDirectory` is the folder where THIS currently running `.exe` lives.
        var baseDir = AppContext.BaseDirectory;

        // `List<string>` is C#'s dynamically resizable list of strings (like a sequential array in PHP).
        // First candidate: check right next to the current `.exe` (e.g. portable single-folder bundle).
        var candidates = new List<string>
        {
            Path.Combine(baseDir, siblingExeName)
        };

        // 1. Developer Build Probe:
        //    When developing inside Visual Studio (`RaggieSoftApps.slnx`), each app compiles into its own
        //    project folder (`BookReader\bin\...`, `ListeningRoom\bin\...`, `Backstage\bin\...`).
        //    Strip `.exe` (`"ListeningRoom.exe"` -> `"ListeningRoom"`) and walk upward (`dir = dir.Parent`)
        //    checking every standard .NET 10 Debug/Release and x64/ARM64 output combination.
        var projectStem = Path.GetFileNameWithoutExtension(siblingExeName);
        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            var siblingProjectDir = Path.Combine(dir.FullName, projectStem);
            if (Directory.Exists(siblingProjectDir))
            {
                // `["Debug", "Release"]` is C# 12+'s clean collection expression syntax for arrays!
                string[] configs = ["Debug", "Release"];
                string[] rids = ["", "win-x64", "win-arm64"];
                string[] platforms = ["", "x64", "ARM64"];

                foreach (var cfg in configs)
                {
                    foreach (var plat in platforms)
                    {
                        var baseBin = string.IsNullOrEmpty(plat)
                            ? Path.Combine(siblingProjectDir, "bin", cfg, "net10.0-windows")
                            : Path.Combine(siblingProjectDir, "bin", plat, cfg, "net10.0-windows");

                        foreach (var rid in rids)
                        {
                            var targetFolder = string.IsNullOrEmpty(rid) ? baseBin : Path.Combine(baseBin, rid);
                            candidates.Add(Path.Combine(targetFolder, siblingExeName));
                            candidates.Add(Path.Combine(targetFolder, "publish", siblingExeName));
                        }
                    }
                }
            }
            // Step one folder higher up the directory tree (`null` once we pass the drive root `C:\`).
            dir = dir.Parent;
        }

        // 2. End-User Inno Setup Installation Probe:
        //    When a reader installs RaggieSoft apps via our Inno Setup `.exe` installers, Inno Setup's
        //    `{autopf}` smart directory places files in one of two standard Windows locations:
        //      - Admin / All-Users Install (`Environment.SpecialFolder.ProgramFiles`):
        //        `C:\Program Files\RaggieSoft\Book Reader\BookReader.exe`
        //      - Standard / Per-User Install (`Environment.SpecialFolder.LocalApplicationData` + `\Programs`):
        //        `C:\Users\<Name>\AppData\Local\Programs\RaggieSoft\Book Reader\BookReader.exe`
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string[] subPaths =
        [
            Path.Combine("RaggieSoft", "Book Reader"),
            Path.Combine("RaggieSoft", "Listening Room"),
            Path.Combine("RaggieSoft", "Backstage"),
            Path.Combine("RaggieSoft", projectStem),
            $"RaggieSoft {projectStem}",
            "RaggieSoft Book Reader",
            "RaggieSoft ListeningRoom",
            "RaggieSoft The Listening Room",
            "RaggieSoft Backstage"
        ];
        foreach (var subPath in subPaths)
        {
            candidates.Add(Path.Combine(programFiles, subPath, siblingExeName));
            candidates.Add(Path.Combine(localAppData, "Programs", subPath, siblingExeName));
        }

        // LINQ's `.FirstOrDefault(File.Exists)` walks `candidates` in order and returns the VERY FIRST
        // path where `File.Exists(path)` is `true`—or returns `null` if none of the paths exist on disk.
        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Cancels the background Named Pipe server loop and releases the underlying
    /// <see cref="CancellationTokenSource"/> handle when the owning window closes.
    /// </summary>
    public void Dispose()
    {
        // Trigger cancellation so `WaitForConnectionAsync` wakes up and `RunPipeServerLoopAsync` exits,
        // then free the OS resources owned by `_cts`.
        _cts.Cancel();
        _cts.Dispose();
    }
}
