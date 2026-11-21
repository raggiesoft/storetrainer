// ================================================================================
// FILE: MusicModels.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file defines all C# data models for RaggieSoft's music ecosystem on
//     Windows—powering **The Listening Room** (`ListeningRoom.exe`, the desktop
//     counterpart to `https://music.raggiesoft.com`) AND the built-in Chapter
//     Soundtrack Preview Player inside **RaggieSoft Book Reader** (`BookReader.exe`).
//
//   What JSON and text files feed into these models?
//     Like our web apps, the Windows desktop suite uses a **Zero-Database Flat-File**
//     architecture—there is no MySQL or SQLite server to configure or corrupt.
//     Instead, `MusicDataService` deserializes static files served from
//     `https://assets.raggiesoft.com` (or cached offline in `%LOCALAPPDATA%\RaggieSoft\Cache`):
//       1. `/engine-room-records/catalog.json`
//          -> `CatalogAlbumEntry` & `CatalogTrackEntry` (complete album/song tree
//             plus Apple Music, Spotify, Amazon Music, and YouTube Music IDs).
//       2. `/engine-room-records/playlists/<slug>/playlist.json`
//          -> `PlaylistManifest` & `PlaylistTrackRef` (curated multi-song chapter
//             soundtracks referenced by `playlist:` in a novel's YAML frontmatter).
//       3. `/engine-room-records/json/master-catalog.json`
//          -> `MasterCatalogEntry` (official US ISRC codes, UPCs, release dates,
//             and artist personas across Engine Room Records).
//       4. `/engine-room-records/artists/<artist>/<album>/tracks.json`
//          -> `AlbumTracksManifest` & `TrackItem` (per-album tracklist manifest).
//       5. `/engine-room-records/artists/<artist>/<album>/lyrics/<track>.vtt`
//          -> `VttCue` (karaoke-style start/end timestamps and lyric lines).
//
//   What visible features does it power?
//     - The Artist & Album browser sidebar, album cover grid, tracklist table, and
//       live scrolling timed-lyrics display in `ListeningRoom.exe`.
//     - Book Reader's top/bottom chapter soundtrack bar, multi-track scene queues,
//       and "Listen in Lossless Quality" handoff menu (opening `ListeningRoom.exe`
//       via Named Pipe IPC or launching Apple Music / Spotify / Amazon / YouTube).
// ================================================================================

using System.Globalization;           // `CultureInfo.InvariantCulture` so numbers format with `.` regardless of OS locale.
using System.Text.Json;               // `Utf8JsonReader`, `Utf8JsonWriter`, and `JsonTokenType`.
using System.Text.Json.Serialization; // `[JsonPropertyName]`, `[JsonConverter]`, `[JsonIgnore]`, and `JsonConverter<T>`.

namespace RaggieSoft.Core.Models;

/// <summary>
/// Tolerant JSON converter that reads a JSON String, Number, or Boolean as a clean C# <see cref="string"/>
/// (e.g., handling <c>"year": "1978"</c> or <c>"year": 1978</c>, or numeric byte sizes vs formatted strings,
/// seamlessly without throwing a type-mismatch exception).
/// </summary>
// WHY THIS MATTERS FOR BEGINNERS COMING FROM PHP/JAVASCRIPT:
//   In PHP or JS, `"1978"` and `1978` coerce to strings automatically when echoed.
//   In strongly typed C#, `System.Text.Json` is strict by default: if a property is `string Year`
//   and a JSON file has `"year": 1978` (without quotes), default deserialization throws `JsonException`!
//   Attaching `[JsonConverter(typeof(FlexibleStringJsonConverter))]` makes that property accept
//   strings, integers, decimals, booleans, or `null` gracefully.
public sealed class FlexibleStringJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // This is a modern C# **switch expression** (`value switch { pattern => result, ... }`—very
        // similar to PHP 8's `match ($tokenType) { ... }` expression!):
        return reader.TokenType switch
        {
            // Already a JSON string (`"1978"`): read it, falling back to `""` if null.
            JsonTokenType.String => reader.GetString() ?? string.Empty,

            // Unquoted JSON number (`1978` or `14.5`): format using `CultureInfo.InvariantCulture`
            // so decimal points are always `"."` even on European Windows PCs that use `","`.
            // (`out var l` declares `l` inline and fills it if the number fits in a 64-bit integer.)
            JsonTokenType.Number => reader.TryGetInt64(out var l)
                ? l.ToString(CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture),

            // JSON booleans (`true` / `false`) and `null`:
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => string.Empty,

            // `_` is the wildcard ("default") arm: if JSON unexpectedly had `{...}` or `[...]`,
            // advance past the whole subtree and return `""` instead of crashing.
            _ => ReadAndSkipFallback(ref reader)
        };
    }

    // Helper called by the wildcard `_` arm above because `reader.Skip()` mutates the `ref` struct
    // before returning `string.Empty`.
    private static string ReadAndSkipFallback(ref Utf8JsonReader reader)
    {
        reader.Skip();
        return string.Empty;
    }

    // Serializes the C# string back out as a standard quoted JSON string.
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

/// <summary>
/// Represents an album object inside <c>/engine-room-records/catalog.json</c>, including
/// commercial streaming platform IDs for higher-quality lossless listening handoffs.
/// </summary>
public sealed class CatalogAlbumEntry
{
    // Folder slug for the album, e.g. `"1997-hard-reset"`.
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    // Display name of the recording artist or band, e.g. `"The Stardust Engine"`.
    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    // URL/folder slug for the artist, e.g. `"the-stardust-engine"`.
    [JsonPropertyName("artistSlug")]
    public string ArtistSlug { get; set; } = string.Empty;

    // Display title of the album, e.g. `"Hard Reset"`.
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    // Release year (accepts either `"1997"` or `1997` in JSON thanks to `FlexibleStringJsonConverter`).
    [JsonPropertyName("year")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string Year { get; set; } = string.Empty;

    // Release classification, e.g. `"Studio Album"`, `"Single"`, `"Live Album"`, or `"Soundtrack"`.
    [JsonPropertyName("productionType")]
    public string ProductionType { get; set; } = string.Empty;

    // Relative CDN path to the square album cover image (`album-art.jpg`).
    [JsonPropertyName("artwork")]
    public string Artwork { get; set; } = string.Empty;

    // Editorial liner-note summary describing the album's place in story canon.
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // --- Commercial Streaming Platform IDs (distributed via DistroKid) ---
    // Used by `MusicDataService` to build direct one-click links to Apple Music (Lossless),
    // Spotify, Amazon Music, and YouTube Music.
    [JsonPropertyName("spotifyId")]
    public string SpotifyId { get; set; } = string.Empty;

    [JsonPropertyName("appleId")]
    public string AppleId { get; set; } = string.Empty;

    [JsonPropertyName("amazonId")]
    public string AmazonId { get; set; } = string.Empty;

    [JsonPropertyName("youtubeId")]
    public string YoutubeId { get; set; } = string.Empty;

    // Ordered list of songs on this album.
    [JsonPropertyName("tracks")]
    public List<CatalogTrackEntry> Tracks { get; set; } = [];
}

/// <summary>
/// Represents an individual song entry nested inside <see cref="CatalogAlbumEntry.Tracks"/>
/// in <c>/engine-room-records/catalog.json</c>.
/// </summary>
public sealed class CatalogTrackEntry
{
    // URL-friendly track identifier (`Slug`) and exact audio filename stem (`FileName`, without `.mp3`).
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    // Disc number (defaults to `1`; `2+` for double/triple albums) and 1-based track number.
    [JsonPropertyName("disc")]
    public int Disc { get; set; } = 1;

    [JsonPropertyName("track")]
    public int Track { get; set; } = 1;

    // Song title and formatted duration string (e.g. `"04:38"`).
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    // Official International Standard Recording Code (e.g. `QTAHP...` or DistroKid-assigned ISRC).
    [JsonPropertyName("isrc")]
    public string Isrc { get; set; } = string.Empty;

    // Optional multi-movement suite grouping name and story context note (`LoreNote`).
    [JsonPropertyName("suiteName")]
    public string SuiteName { get; set; } = string.Empty;

    [JsonPropertyName("loreNote")]
    public string LoreNote { get; set; } = string.Empty;

    // Inline full lyrics Markdown string (if bundled directly inside `catalog.json`).
    [JsonPropertyName("lyrics")]
    public string Lyrics { get; set; } = string.Empty;
}

/// <summary>
/// Represents <c>/engine-room-records/playlists/&lt;slug&gt;/playlist.json</c>, referenced from a
/// novel chapter's YAML frontmatter via <c>playlist: playlists/&lt;slug&gt;</c>.
/// </summary>
public sealed class PlaylistManifest
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // Ordered sequence of songs that play across the chapter or multi-act scene.
    [JsonPropertyName("tracks")]
    public List<PlaylistTrackRef> Tracks { get; set; } = [];
}

/// <summary>
/// Single track reference inside a curated <c>playlist.json</c> manifest.
/// </summary>
public sealed class PlaylistTrackRef
{
    // Relative path to the track in Engine Room Records (e.g. `"the-stardust-engine/1997-hard-reset/01-hard-reset"`).
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    // Optional narrative cue label (e.g. `"Act I — Arrival"` or `"Closing Credits"`).
    [JsonPropertyName("actLabel")]
    public string ActLabel { get; set; } = string.Empty;
}

/// <summary>
/// Represents a higher-quality listening destination (either launching the local
/// <c>ListeningRoom.exe</c> companion app via IPC, or opening a commercial lossless
/// streaming service in the browser) exposed in Book Reader and The Listening Room.
/// </summary>
public sealed class StreamingServiceDestination
{
    // Human-readable menu label, e.g. `"Apple Music (Lossless)"` or `"The Listening Room (Synced Lyrics & Liner Notes)"`.
    public string ServiceName { get; set; } = string.Empty;

    // Web fallback URL (`https://music.apple.com/...`, `https://open.spotify.com/...`, or `https://music.raggiesoft.com/...`).
    public string Url { get; set; } = string.Empty;

    // When `true`, clicking this menu item in Book Reader first tries `RaggieSoftIpcBridge.SendOrLaunchSiblingAsync`
    // to play the song natively in `ListeningRoom.exe` with live synchronized lyrics!
    public bool IsListeningRoomAppHandoff { get; set; }
    public string ArtistSlug { get; set; } = string.Empty;
    public string AlbumSlug { get; set; } = string.Empty;
    public string TrackSlug { get; set; } = string.Empty;
}

/// <summary>
/// First-class resolved chapter audio track for RaggieSoft Book Reader's native
/// 128 kbps preview player + higher-quality streaming handoff menu.
/// </summary>
public sealed class ChapterAudioTrack
{
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public string ArtistSlug { get; set; } = string.Empty;
    public string AlbumSlug { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DurationLabel { get; set; } = string.Empty;
    public string ActLabel { get; set; } = string.Empty;

    // Local cached `.mp3` path or `https://assets.raggiesoft.com/.../web-mp3/<fileName>.mp3` URL,
    // plus resolved square album cover artwork URI.
    public string PreviewMp3Uri { get; set; } = string.Empty;
    public string CoverArtUri { get; set; } = string.Empty;
    public string InlineLyricsMarkdown { get; set; } = string.Empty;

    // Menu options letting the reader jump to The Listening Room, Apple Music, Spotify, Amazon, or YouTube.
    public List<StreamingServiceDestination> HighQualityDestinations { get; set; } = [];

    // Formats `"Artist • Album • Act Label"` cleanly for Book Reader's compact audio bar,
    // omitting any empty fields so stray `" • "` separators never appear.
    public string SubtitleLine
    {
        get
        {
            var pieces = new List<string>();
            if (!string.IsNullOrWhiteSpace(ArtistName)) pieces.Add(ArtistName);
            if (!string.IsNullOrWhiteSpace(AlbumTitle)) pieces.Add(AlbumTitle);
            if (!string.IsNullOrWhiteSpace(ActLabel)) pieces.Add(ActLabel);
            return string.Join(" • ", pieces);
        }
    }
}

/// <summary>
/// Resolved chapter soundtrack or multi-track playlist queue for a book Part.
/// </summary>
public sealed class ChapterAudioQueue
{
    // Optional custom queue banner title from YAML `audio_playlist_title:` or `playlist.json`.
    public string QueueTitle { get; set; } = string.Empty;

    // Optional starting timestamp in seconds from YAML `audio_start:` (e.g. `92.5` for `"1:32.5"`),
    // allowing a chapter's soundtrack button to cue straight into a specific chorus or bridge!
    public double StartOffsetSeconds { get; set; }

    public List<ChapterAudioTrack> Tracks { get; set; } = [];

    // Convenience boolean flags used by Book Reader's UI to show/hide the soundtrack bar
    // (`HasAudio`) and Previous/Next Track buttons (`IsMultiTrack`).
    public bool HasAudio => Tracks.Count > 0;
    public bool IsMultiTrack => Tracks.Count > 1;
}

/// <summary>
/// Represents a single authoritative track record in <c>engine-room-records/json/master-catalog.json</c>.
/// Each row corresponds to one mastered recording in the Engine Room Records catalog.
/// </summary>
public sealed class MasterCatalogEntry
{
    // Internal Engine Room Records catalog ID and official US ISRC code.
    [JsonPropertyName("err_id")]
    public string ErrId { get; set; } = string.Empty;

    [JsonPropertyName("isrc")]
    public string Isrc { get; set; } = string.Empty;

    // Universal Product Code (barcode) assigned to the parent commercial album release.
    [JsonPropertyName("albumUPC")]
    public string AlbumUpc { get; set; } = string.Empty;

    [JsonPropertyName("trackTitle")]
    public string TrackTitle { get; set; } = string.Empty;

    [JsonPropertyName("trackVersion")]
    public string TrackVersion { get; set; } = string.Empty;

    [JsonPropertyName("taggedTitle")]
    public string TaggedTitle { get; set; } = string.Empty;

    [JsonPropertyName("albumTitle")]
    public string AlbumTitle { get; set; } = string.Empty;

    [JsonPropertyName("albumProductionType")]
    public string AlbumProductionType { get; set; } = string.Empty;

    // Performing artist/band name (`artistPersona`) and URL/directory slugs.
    [JsonPropertyName("artistPersona")]
    public string ArtistPersona { get; set; } = string.Empty;

    [JsonPropertyName("artistSlug")]
    public string ArtistSlug { get; set; } = string.Empty;

    [JsonPropertyName("albumSlug")]
    public string AlbumSlug { get; set; } = string.Empty;

    [JsonPropertyName("trackSlug")]
    public string TrackSlug { get; set; } = string.Empty;

    // Real-world copyright holder, DSP distributor (`DistroKid`), release date, and publication status.
    [JsonPropertyName("legalOwner")]
    public string LegalOwner { get; set; } = string.Empty;

    [JsonPropertyName("distributor")]
    public string Distributor { get; set; } = string.Empty;

    [JsonPropertyName("realReleaseDate")]
    public string RealReleaseDate { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Grouped artist summary with all of their albums for The Listening Room's left sidebar.
/// </summary>
public sealed class ArtistSummary
{
    public string ArtistSlug { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public List<AlbumSummary> Albums { get; set; } = [];

    // LINQ's `.Sum(a => a.TrackCount)` adds up `TrackCount` across every album in `Albums`
    // (like `array_sum(array_column($albums, 'TrackCount'))` in PHP!).
    public int TotalTrackCount => Albums.Sum(a => a.TrackCount);

    // Consumer-friendly subtitle shown underneath the artist's name in the sidebar.
    public string Subtitle => $"{Albums.Count} Albums • {TotalTrackCount} Songs";
}

/// <summary>
/// Grouped album summary with resolved cover art, lossless streaming platform links,
/// and track manifest metadata for The Listening Room.
/// </summary>
public sealed class AlbumSummary
{
    public string ArtistSlug { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumSlug { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public string ProductionType { get; set; } = string.Empty;
    public string ExplicitYear { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Uses `ExplicitYear` from `catalog.json` if present; otherwise slices the first 4 characters
    // of `AlbumSlug` (`AlbumSlug[..4]` turns `"1997-hard-reset"` into `"1997"`—C#'s range operator
    // `[..4]` is equivalent to PHP's `substr($albumSlug, 0, 4)`!).
    public string ReleaseYear =>
        !string.IsNullOrWhiteSpace(ExplicitYear)
            ? ExplicitYear
            : (AlbumSlug.Length >= 4 ? AlbumSlug[..4] : string.Empty);

    public string CoverArtUri { get; set; } = string.Empty;
    public int TrackCount { get; set; }
    public string SpotifyUrl { get; set; } = string.Empty;
    public string AppleMusicUrl { get; set; } = string.Empty;
    public string AmazonMusicUrl { get; set; } = string.Empty;
    public string YoutubeMusicUrl { get; set; } = string.Empty;
    public List<StreamingServiceDestination> StreamingDestinations { get; set; } = [];

    public string DisplaySubtitle => $"{ArtistName} • {ReleaseYear} • {TrackCount} Songs";
}

/// <summary>
/// Deserializes <c>artists/&lt;artist&gt;/&lt;album&gt;/tracks.json</c>.
/// </summary>
public sealed class AlbumTracksManifest
{
    [JsonPropertyName("tracks")]
    public List<TrackItem> Tracks { get; set; } = [];
}

/// <summary>
/// Represents a single song inside an album's <c>tracks.json</c> file, enriched at runtime
/// with resolved audio, cover art, and parent album metadata for playback in The Listening Room.
/// </summary>
public sealed class TrackItem
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isrc")]
    public string Isrc { get; set; } = string.Empty;

    [JsonPropertyName("disc")]
    public int Disc { get; set; } = 1;

    [JsonPropertyName("discName")]
    public string DiscName { get; set; } = string.Empty;

    [JsonPropertyName("track")]
    public int TrackNumber { get; set; } = 1;

    [JsonPropertyName("suiteName")]
    public string SuiteName { get; set; } = string.Empty;

    [JsonPropertyName("legacyTier")]
    public string LegacyTier { get; set; } = string.Empty;

    [JsonPropertyName("loreNote")]
    public string LoreNote { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    // --- Runtime-Populated Context Properties (`[JsonIgnore]`) ---
    // Filled in by `MusicDataService.LoadAlbumTracksAsync` after deserializing `tracks.json`
    // so every `TrackItem` is self-contained when queued into the audio player.
    [JsonIgnore]
    public string ArtistSlug { get; set; } = string.Empty;

    [JsonIgnore]
    public string ArtistName { get; set; } = string.Empty;

    [JsonIgnore]
    public string AlbumSlug { get; set; } = string.Empty;

    [JsonIgnore]
    public string AlbumTitle { get; set; } = string.Empty;

    [JsonIgnore]
    public string AudioUri { get; set; } = string.Empty;

    [JsonIgnore]
    public string CoverArtUri { get; set; } = string.Empty;

    // `:D2` inside `$"{TrackNumber:D2}"` formats an integer with at least 2 digits and a leading
    // zero (`1` -> `"01"`, `12` -> `"12"`, just like `sprintf('%02d', $track)` in PHP).
    // Multi-disc tracks display as `"2-01"`; single-disc tracks display as `"01"`.
    [JsonIgnore]
    public string DisplayNumber => Disc > 1 ? $"{Disc}-{TrackNumber:D2}" : $"{TrackNumber:D2}";

    // Combines song duration (`"04:12"`) with optional multi-part suite name (`"04:12 • Act II"`).
    [JsonIgnore]
    public string DisplayBadge =>
        string.IsNullOrWhiteSpace(SuiteName)
            ? Duration
            : $"{Duration} • {SuiteName}";
}

/// <summary>
/// Represents a single timed lyric cue parsed from a <c>.vtt</c> file for live synchronized
/// karaoke-style lyric highlighting in The Listening Room and Book Reader's Companion Dock.
/// </summary>
public sealed class VttCue
{
    // `TimeSpan` is .NET's built-in duration/timestamp struct (accurate down to ticks/milliseconds).
    // Whenever the audio player's current position is between `StartTime` and `EndTime`, this lyric
    // cue becomes the highlighted active line!
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // The sung lyric line(s) spoken or sung during `[StartTime, EndTime]`.
    public string Text { get; set; } = string.Empty;

    // Formats `StartTime` as `"MM:SS"` (e.g. `"02:15"`—casting `TotalMinutes` to `(int)` so songs
    // longer than 60 minutes would show `"65:00"` cleanly rather than wrapping back to `"05:00"`).
    public string TimestampLabel => $"{(int)StartTime.TotalMinutes:D2}:{StartTime.Seconds:D2}";
}
