// ================================================================================
// FILE: MusicDataService.cs
// PROJECT: RaggieSoft.Core (Shared Architecture Library)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW FOR 100-LEVEL READERS:
//   What is this file?
//     This file is the **Music Catalog Loader, Chapter Soundtrack Resolver, and
//     Timed-Lyrics Parser** shared by **The Listening Room** (`ListeningRoom.exe`)
//     and **RaggieSoft Book Reader** (`BookReader.exe`).
//
//   What flat files on `https://assets.raggiesoft.com` (or `%LOCALAPPDATA%` cache) feed into it?
//     1. `/engine-room-records/catalog.json`
//        -> Complete album & track catalog + Apple Music, Spotify, Amazon Music,
//           and YouTube Music streaming IDs (`LoadCatalogAlbumsAsync`).
//     2. `/engine-room-records/json/master-catalog.json`
//        -> Master Engine Room Records registry with artist personas, production
//           types, and official ISRCs (`LoadArtistsAndAlbumsAsync`).
//     3. `/engine-room-records/playlists/<slug>/playlist.json`
//        -> Curated multi-song chapter soundtracks (`ResolveChapterAudioQueueAsync`).
//     4. `/engine-room-records/artists/<artist>/<album>/tracks.json`
//        -> Per-album song manifest (`LoadAlbumTracksAsync`).
//     5. `/engine-room-records/artists/<artist>/<album>/lyrics/<fileName>.vtt`
//        -> Synchronized timed lyric files (`LoadTrackVttCuesAsync` & `ParseWebVtt`).
//     6. `/engine-room-records/artists/<artist>/<album>/lyrics/<fileName>.md` &
//        `suno-prompts/<fileName>.md`
//        -> Full Markdown lyrics and open-culture song creation notes.
//
//   What visible features does it power?
//     - Populates The Listening Room's artist/album library, cover artwork, track
//       tables, lossless DSP links, liner notes, and live scrolling lyric lines.
//     - Resolves `audio:` and `playlist:` fields in any novel chapter's YAML
//       frontmatter into a ready-to-play `ChapterAudioQueue` inside Book Reader!
// ================================================================================

using System.Globalization;           // `CultureInfo.InvariantCulture` for locale-independent timestamp parsing.
using System.Text.Json;               // `JsonSerializer` & `JsonSerializerOptions`.
using System.Text.RegularExpressions; // `Regex` for parsing `.vtt` timing lines (`00:01:12.400 --> 00:01:16.900`).
using RaggieSoft.Core.Models;         // All music models (`ArtistSummary`, `AlbumSummary`, `TrackItem`, `VttCue`, etc.).

namespace RaggieSoft.Core.Services;

/// <summary>
/// Loads Engine Room Records catalogs, resolves chapter soundtrack queues, builds
/// lossless streaming service handoff links, and parses timed <c>.vtt</c> lyrics.
/// </summary>
public sealed class MusicDataService
{
    private readonly RaggieSoftWorkspaceLocator _locator;

    // In-memory cache ("memoization"): once `catalog.json` has been loaded and parsed once during
    // an app session, subsequent chapter navigations reuse `_cachedCatalogAlbums` instantaneously!
    private List<CatalogAlbumEntry>? _cachedCatalogAlbums;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Matches WebVTT timestamp cue arrows in either `MM:SS.mmm --> MM:SS.mmm` OR
    // `HH:MM:SS.mmm --> HH:MM:SS.mmm` format, capturing `(?<start>...)` and `(?<end>...)`.
    private static readonly Regex VttTimingRegex = new(
        @"(?<start>(?:\d{2}:)?\d{2}:\d{2}\.\d{3})\s*-->\s*(?<end>(?:\d{2}:)?\d{2}:\d{2}\.\d{3})",
        RegexOptions.Compiled);

    public MusicDataService(RaggieSoftWorkspaceLocator locator)
    {
        _locator = locator;
    }

    /// <summary>
    /// Loads and caches <c>/engine-room-records/catalog.json</c> for fast chapter audio lookup
    /// and lossless streaming service link enrichment.
    /// </summary>
    public async Task<List<CatalogAlbumEntry>> LoadCatalogAlbumsAsync()
    {
        // Return the already-parsed list immediately if we loaded it earlier in this session.
        if (_cachedCatalogAlbums is not null)
        {
            return _cachedCatalogAlbums;
        }

        try
        {
            var json = await _locator.ReadAssetTextAsync("engine-room-records/catalog.json");
            _cachedCatalogAlbums = JsonSerializer.Deserialize<List<CatalogAlbumEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            _cachedCatalogAlbums = [];
        }

        return _cachedCatalogAlbums;
    }

    /// <summary>
    /// Loads the complete Engine Room Records catalog and groups it into Artists and Albums,
    /// enriching each album with lossless streaming destinations (Apple Music, Spotify,
    /// Amazon Music, YouTube Music) from <c>catalog.json</c>.
    /// </summary>
    public async Task<List<ArtistSummary>> LoadArtistsAndAlbumsAsync()
    {
        var catalogAlbums = await LoadCatalogAlbumsAsync();

        // Build an O(1) dictionary keyed by `"<artistSlug>/<albumSlug>"` so we can enrich
        // `master-catalog.json` rows with artwork, descriptions, and streaming IDs from `catalog.json`.
        var catalogLookup = catalogAlbums
            .Where(a => !string.IsNullOrWhiteSpace(a.Slug))
            .GroupBy(a => $"{a.ArtistSlug}/{a.Slug}".ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        List<MasterCatalogEntry> entries = [];
        try
        {
            var json = await _locator.ReadAssetTextAsync("engine-room-records/json/master-catalog.json");
            entries = JsonSerializer.Deserialize<List<MasterCatalogEntry>>(json, JsonOptions) ?? [];
        }
        catch
        {
            // Fall back to catalog.json directly if master-catalog.json is unavailable offline
        }

        // Primary path: group flat `MasterCatalogEntry` rows by Artist (`GroupBy(ArtistSlug)`),
        // then nest a second `GroupBy(AlbumSlug)` inside each artist—similar to SQL `GROUP BY`!
        if (entries.Count > 0)
        {
            return entries
                .Where(e => !string.IsNullOrWhiteSpace(e.ArtistSlug) && !string.IsNullOrWhiteSpace(e.AlbumSlug))
                .GroupBy(e => e.ArtistSlug, StringComparer.OrdinalIgnoreCase)
                .Select(artistGroup =>
                {
                    var firstArtist = artistGroup.First();
                    var albums = artistGroup
                        .GroupBy(a => a.AlbumSlug, StringComparer.OrdinalIgnoreCase)
                        .Select(albumGroup =>
                        {
                            var firstAlbum = albumGroup.First();
                            var lookupKey = $"{firstArtist.ArtistSlug}/{firstAlbum.AlbumSlug}".ToLowerInvariant();
                            catalogLookup.TryGetValue(lookupKey, out var richAlbum);

                            var relCover = !string.IsNullOrWhiteSpace(richAlbum?.Artwork)
                                ? richAlbum.Artwork
                                : $"engine-room-records/artists/{firstArtist.ArtistSlug}/{firstAlbum.AlbumSlug}/album-art.jpg";

                            var summary = new AlbumSummary
                            {
                                ArtistSlug = firstArtist.ArtistSlug,
                                ArtistName = firstArtist.ArtistPersona,
                                AlbumSlug = firstAlbum.AlbumSlug,
                                AlbumTitle = firstAlbum.AlbumTitle,
                                ProductionType = !string.IsNullOrWhiteSpace(firstAlbum.AlbumProductionType)
                                    ? firstAlbum.AlbumProductionType
                                    : (richAlbum?.ProductionType ?? "Studio Release"),
                                ExplicitYear = richAlbum?.Year ?? string.Empty,
                                Description = richAlbum?.Description ?? string.Empty,
                                CoverArtUri = _locator.ResolveAssetUri(relCover),
                                TrackCount = albumGroup.Count()
                            };

                            PopulateAlbumStreamingLinks(summary, richAlbum);
                            return summary;
                        })
                        .OrderBy(a => a.AlbumSlug, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new ArtistSummary
                    {
                        ArtistSlug = firstArtist.ArtistSlug,
                        ArtistName = firstArtist.ArtistPersona,
                        Albums = albums
                    };
                })
                .OrderBy(a => a.ArtistName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Fallback path: build the Artist -> Album tree directly from `catalog.json` if `master-catalog.json` wasn't cached.
        return catalogAlbums
            .Where(a => !string.IsNullOrWhiteSpace(a.ArtistSlug) && !string.IsNullOrWhiteSpace(a.Slug))
            .GroupBy(a => a.ArtistSlug, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var albums = group.Select(alb =>
                {
                    var relCover = !string.IsNullOrWhiteSpace(alb.Artwork)
                        ? alb.Artwork
                        : $"engine-room-records/artists/{alb.ArtistSlug}/{alb.Slug}/album-art.jpg";
                    var summary = new AlbumSummary
                    {
                        ArtistSlug = alb.ArtistSlug,
                        ArtistName = alb.Artist,
                        AlbumSlug = alb.Slug,
                        AlbumTitle = alb.Title,
                        ProductionType = string.IsNullOrWhiteSpace(alb.ProductionType) ? "Studio Release" : alb.ProductionType,
                        ExplicitYear = alb.Year,
                        Description = alb.Description,
                        CoverArtUri = _locator.ResolveAssetUri(relCover),
                        TrackCount = alb.Tracks.Count
                    };
                    PopulateAlbumStreamingLinks(summary, alb);
                    return summary;
                })
                .OrderBy(a => a.AlbumSlug, StringComparer.OrdinalIgnoreCase)
                .ToList();

                return new ArtistSummary
                {
                    ArtistSlug = first.ArtistSlug,
                    ArtistName = first.Artist,
                    Albums = albums
                };
            })
            .OrderBy(a => a.ArtistName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Populates commercial DSP links (`Apple Music`, `Spotify`, `Amazon Music`, `YouTube Music`)
    // accepting either bare platform IDs (`"1792345678"`) or full `https://...` URLs.
    private static void PopulateAlbumStreamingLinks(AlbumSummary target, CatalogAlbumEntry? richAlbum)
    {
        target.StreamingDestinations.Clear();

        if (richAlbum is not null)
        {
            if (!string.IsNullOrWhiteSpace(richAlbum.AppleId))
            {
                target.AppleMusicUrl = richAlbum.AppleId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? richAlbum.AppleId
                    : $"https://music.apple.com/us/album/{richAlbum.AppleId}";
                target.StreamingDestinations.Add(new StreamingServiceDestination
                {
                    ServiceName = "Apple Music (Lossless)",
                    Url = target.AppleMusicUrl
                });
            }

            if (!string.IsNullOrWhiteSpace(richAlbum.SpotifyId))
            {
                target.SpotifyUrl = richAlbum.SpotifyId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? richAlbum.SpotifyId
                    : $"https://open.spotify.com/album/{richAlbum.SpotifyId}";
                target.StreamingDestinations.Add(new StreamingServiceDestination
                {
                    ServiceName = "Spotify",
                    Url = target.SpotifyUrl
                });
            }

            if (!string.IsNullOrWhiteSpace(richAlbum.AmazonId))
            {
                target.AmazonMusicUrl = richAlbum.AmazonId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? richAlbum.AmazonId
                    : $"https://music.amazon.com/albums/{richAlbum.AmazonId}";
                target.StreamingDestinations.Add(new StreamingServiceDestination
                {
                    ServiceName = "Amazon Music",
                    Url = target.AmazonMusicUrl
                });
            }

            if (!string.IsNullOrWhiteSpace(richAlbum.YoutubeId))
            {
                target.YoutubeMusicUrl = richAlbum.YoutubeId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? richAlbum.YoutubeId
                    : $"https://music.youtube.com/playlist?list={richAlbum.YoutubeId}";
                target.StreamingDestinations.Add(new StreamingServiceDestination
                {
                    ServiceName = "YouTube Music",
                    Url = target.YoutubeMusicUrl
                });
            }
        }

        // Always include the canonical web Listening Room album page as a universal option
        target.StreamingDestinations.Add(new StreamingServiceDestination
        {
            ServiceName = "Engine Room Records Web Player",
            Url = $"https://music.raggiesoft.com/{target.ArtistSlug}/{target.AlbumSlug}"
        });
    }

    /// <summary>
    /// Resolves YAML frontmatter <c>audio:</c>, <c>playlist:</c>, <c>audio_playlist_title:</c>, and <c>audio_start:</c>
    /// into a native <see cref="ChapterAudioQueue"/> for RaggieSoft Book Reader's 128 kbps preview player
    /// and high-quality streaming service handoff menu.
    /// </summary>
    public async Task<ChapterAudioQueue> ResolveChapterAudioQueueAsync(
        ParsedChapterPart parsedPart,
        BookPart? tocFallback = null)
    {
        var queue = new ChapterAudioQueue
        {
            QueueTitle = parsedPart.AudioPlaylistTitle,
            StartOffsetSeconds = parsedPart.AudioStartSeconds
        };

        // Prefer YAML frontmatter specs on the loaded `pXXX.md` part, falling back to `toc.json` metadata if needed.
        var playlistSpec = !string.IsNullOrWhiteSpace(parsedPart.Playlist)
            ? parsedPart.Playlist
            : (tocFallback?.Playlist ?? string.Empty);

        var audioSpecs = parsedPart.AudioSpecs.Count > 0
            ? parsedPart.AudioSpecs
            : (tocFallback?.AudioSpecs ?? []);

        if (string.IsNullOrWhiteSpace(playlistSpec) && audioSpecs.Count == 0 && !string.IsNullOrWhiteSpace(parsedPart.AudioRelativePath))
        {
            audioSpecs = [parsedPart.AudioRelativePath];
        }

        // If this chapter has no soundtrack configured at all, return the empty queue (`HasAudio == false`).
        if (string.IsNullOrWhiteSpace(playlistSpec) && audioSpecs.Count == 0)
        {
            return queue;
        }

        var catalogAlbums = await LoadCatalogAlbumsAsync();

        // 1. Resolve curated multi-track playlist (e.g., `playlist: playlists/<slug>`).
        if (!string.IsNullOrWhiteSpace(playlistSpec))
        {
            var cleanPlaylist = playlistSpec.Trim().Trim('/');
            if (cleanPlaylist.StartsWith("playlists/", StringComparison.OrdinalIgnoreCase))
            {
                cleanPlaylist = cleanPlaylist["playlists/".Length..];
            }

            try
            {
                var playlistJson = await _locator.ReadAssetTextAsync(
                    $"engine-room-records/playlists/{cleanPlaylist}/playlist.json");
                var manifest = JsonSerializer.Deserialize<PlaylistManifest>(playlistJson, JsonOptions);
                if (manifest is not null)
                {
                    if (string.IsNullOrWhiteSpace(queue.QueueTitle) && !string.IsNullOrWhiteSpace(manifest.Title))
                    {
                        queue.QueueTitle = manifest.Title;
                    }

                    foreach (var item in manifest.Tracks)
                    {
                        var resolved = ResolveAudioSpecAgainstCatalog(item.Path, item.ActLabel, catalogAlbums);
                        queue.Tracks.AddRange(resolved);
                    }
                }
            }
            catch
            {
                // Fall through to any explicit audioSpecs if playlist.json could not be fetched
            }
        }

        // 2. Resolve explicit YAML `audio:` spec(s) (single MP3, array of MP3s, or full album slug).
        if (queue.Tracks.Count == 0 && audioSpecs.Count > 0)
        {
            foreach (var spec in audioSpecs)
            {
                var resolved = ResolveAudioSpecAgainstCatalog(spec, string.Empty, catalogAlbums);
                queue.Tracks.AddRange(resolved);
            }
        }

        return queue;
    }

    // Resolves any flexible `audio:` reference format written in chapter YAML frontmatter:
    //   - Case A (`1997-hard-reset`): Queues every track on that album
    //   - Case B (`the-stardust-engine/1997-hard-reset`): Queues every track on that artist's album
    //   - Case C (`the-stardust-engine/1997-hard-reset/web-mp3/01-hard-reset.mp3`): Queues that specific song
    private List<ChapterAudioTrack> ResolveAudioSpecAgainstCatalog(
        string rawSpec,
        string actLabel,
        List<CatalogAlbumEntry> catalogAlbums)
    {
        var results = new List<ChapterAudioTrack>();
        if (string.IsNullOrWhiteSpace(rawSpec))
        {
            return results;
        }

        // Strip optional `{{CDN}}`, `https://assets.raggiesoft.com`, `/engine-room-records/`, and `/artists/` prefixes.
        var cleaned = rawSpec.Trim()
            .Replace("{{CDN}}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("https://assets.raggiesoft.com", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim('/');

        if (cleaned.StartsWith("engine-room-records/", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["engine-room-records/".Length..];
        }

        if (cleaned.StartsWith("artists/", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["artists/".Length..];
        }

        var segments = cleaned.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return results;
        }

        // Case A: Single bare album slug ("1997-hard-reset")
        if (segments.Length == 1)
        {
            var matchedAlbum = catalogAlbums.FirstOrDefault(a =>
                string.Equals(a.Slug, segments[0], StringComparison.OrdinalIgnoreCase));
            if (matchedAlbum is not null)
            {
                foreach (var t in matchedAlbum.Tracks)
                {
                    results.Add(BuildChapterAudioTrack(matchedAlbum, t, actLabel));
                }
            }
            return results;
        }

        var artistSlug = segments[0];
        var albumSlug = segments[1];
        var albumMatch = catalogAlbums.FirstOrDefault(a =>
            string.Equals(a.ArtistSlug, artistSlug, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Slug, albumSlug, StringComparison.OrdinalIgnoreCase))
            ?? catalogAlbums.FirstOrDefault(a =>
                string.Equals(a.Slug, albumSlug, StringComparison.OrdinalIgnoreCase));

        // Case B: Artist + Album ("the-stardust-engine/1997-hard-reset") -> full album queue
        if (segments.Length == 2 && !segments[1].EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            if (albumMatch is not null)
            {
                foreach (var t in albumMatch.Tracks)
                {
                    results.Add(BuildChapterAudioTrack(albumMatch, t, actLabel));
                }
            }
            return results;
        }

        // Case C: Specific track path ("<artist>/<album>/web-mp3/<fileName>.mp3" or "<artist>/<album>/<fileName>").
        // Notice C# index-from-end syntax:
        //   - `segments[^1]` grabs the LAST element of the array (like `$segments[count($segments) - 1]` in PHP!)
        //   - `lastSegment[..^4]` slices off the trailing 4 characters (`".mp3"`).
        var lastSegment = segments[^1];
        var fileStem = lastSegment.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
            ? lastSegment[..^4]
            : lastSegment;

        CatalogTrackEntry? trackMatch = null;
        if (albumMatch is not null)
        {
            trackMatch = albumMatch.Tracks.FirstOrDefault(t =>
                string.Equals(t.FileName, fileStem, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Slug, fileStem, StringComparison.OrdinalIgnoreCase) ||
                t.FileName.EndsWith(fileStem, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // Search across all albums if artist/album slug varied slightly
            foreach (var alb in catalogAlbums)
            {
                var candidate = alb.Tracks.FirstOrDefault(t =>
                    string.Equals(t.FileName, fileStem, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Slug, fileStem, StringComparison.OrdinalIgnoreCase));
                if (candidate is not null)
                {
                    albumMatch = alb;
                    trackMatch = candidate;
                    break;
                }
            }
        }

        if (albumMatch is not null && trackMatch is not null)
        {
            results.Add(BuildChapterAudioTrack(albumMatch, trackMatch, actLabel));
            return results;
        }

        // Fallback if `catalog.json` wasn't cached offline yet: synthesize playable metadata directly from path segments!
        var resolvedMp3Rel = $"engine-room-records/artists/{artistSlug}/{albumSlug}/web-mp3/{fileStem}.mp3";
        var resolvedCoverRel = $"engine-room-records/artists/{artistSlug}/{albumSlug}/album-art.jpg";
        var humanizedTitle = HumanizeSlug(fileStem);
        var humanizedArtist = albumMatch?.Artist ?? HumanizeSlug(artistSlug);
        var humanizedAlbum = albumMatch?.Title ?? HumanizeSlug(albumSlug);

        var fallbackTrack = new ChapterAudioTrack
        {
            Title = humanizedTitle,
            ArtistName = humanizedArtist,
            AlbumTitle = humanizedAlbum,
            ArtistSlug = artistSlug,
            AlbumSlug = albumSlug,
            FileName = fileStem,
            ActLabel = actLabel,
            PreviewMp3Uri = _locator.ResolveAssetUri(resolvedMp3Rel),
            CoverArtUri = _locator.ResolveAssetUri(resolvedCoverRel),
            HighQualityDestinations = BuildTrackHighQualityDestinations(
                artistSlug,
                albumSlug,
                fileStem,
                humanizedArtist,
                humanizedTitle,
                albumMatch)
        };
        results.Add(fallbackTrack);
        return results;
    }

    // Converts a matched `CatalogAlbumEntry` + `CatalogTrackEntry` into a `ChapterAudioTrack`.
    private ChapterAudioTrack BuildChapterAudioTrack(
        CatalogAlbumEntry album,
        CatalogTrackEntry track,
        string actLabel)
    {
        var fileStem = !string.IsNullOrWhiteSpace(track.FileName) ? track.FileName : track.Slug;
        var mp3Rel = $"engine-room-records/artists/{album.ArtistSlug}/{album.Slug}/web-mp3/{fileStem}.mp3";
        var coverRel = !string.IsNullOrWhiteSpace(album.Artwork)
            ? album.Artwork
            : $"engine-room-records/artists/{album.ArtistSlug}/{album.Slug}/album-art.jpg";

        return new ChapterAudioTrack
        {
            Title = track.Title,
            ArtistName = album.Artist,
            AlbumTitle = album.Title,
            ArtistSlug = album.ArtistSlug,
            AlbumSlug = album.Slug,
            FileName = fileStem,
            DurationLabel = track.Duration,
            ActLabel = actLabel,
            PreviewMp3Uri = _locator.ResolveAssetUri(mp3Rel),
            CoverArtUri = _locator.ResolveAssetUri(coverRel),
            InlineLyricsMarkdown = track.Lyrics,
            HighQualityDestinations = BuildTrackHighQualityDestinations(
                album.ArtistSlug,
                album.Slug,
                fileStem,
                album.Artist,
                track.Title,
                album)
        };
    }

    // Builds the "Listen in Higher Quality" dropdown menu for a chapter track:
    //   1. First item (`IsListeningRoomAppHandoff = true`): launches or messages `ListeningRoom.exe` via Named Pipe IPC!
    //   2. Apple Music, Spotify, Amazon Music, and YouTube Music (using exact album IDs when present, or search URLs as fallback).
    private static List<StreamingServiceDestination> BuildTrackHighQualityDestinations(
        string artistSlug,
        string albumSlug,
        string fileStem,
        string artistName,
        string trackTitle,
        CatalogAlbumEntry? album)
    {
        var destinations = new List<StreamingServiceDestination>
        {
            new()
            {
                ServiceName = "The Listening Room (Synced Lyrics & Liner Notes)",
                Url = $"https://music.raggiesoft.com/{artistSlug}/{albumSlug}?track={fileStem}",
                IsListeningRoomAppHandoff = true,
                ArtistSlug = artistSlug,
                AlbumSlug = albumSlug,
                TrackSlug = fileStem
            }
        };

        var searchQuery = Uri.EscapeDataString($"{artistName} {trackTitle}".Trim());

        var appleUrl = !string.IsNullOrWhiteSpace(album?.AppleId)
            ? (album.AppleId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? album.AppleId
                : $"https://music.apple.com/us/album/{album.AppleId}")
            : $"https://music.apple.com/us/search?term={searchQuery}";
        destinations.Add(new StreamingServiceDestination
        {
            ServiceName = "Apple Music (Lossless)",
            Url = appleUrl
        });

        var spotifyUrl = !string.IsNullOrWhiteSpace(album?.SpotifyId)
            ? (album.SpotifyId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? album.SpotifyId
                : $"https://open.spotify.com/album/{album.SpotifyId}")
            : $"https://open.spotify.com/search/{searchQuery}";
        destinations.Add(new StreamingServiceDestination
        {
            ServiceName = "Spotify",
            Url = spotifyUrl
        });

        var amazonUrl = !string.IsNullOrWhiteSpace(album?.AmazonId)
            ? (album.AmazonId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? album.AmazonId
                : $"https://music.amazon.com/albums/{album.AmazonId}")
            : $"https://music.amazon.com/search/{searchQuery}";
        destinations.Add(new StreamingServiceDestination
        {
            ServiceName = "Amazon Music",
            Url = amazonUrl
        });

        var ytUrl = !string.IsNullOrWhiteSpace(album?.YoutubeId)
            ? (album.YoutubeId.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? album.YoutubeId
                : $"https://music.youtube.com/playlist?list={album.YoutubeId}")
            : $"https://music.youtube.com/search?q={searchQuery}";
        destinations.Add(new StreamingServiceDestination
        {
            ServiceName = "YouTube Music",
            Url = ytUrl
        });

        return destinations;
    }

    // Turns a hyphenated slug like `"01-hard-reset"` into Title Case `"Hard Reset"`.
    private static string HumanizeSlug(string slug)
    {
        var stripped = Regex.Replace(slug, @"^(?:\d+-)+", string.Empty);
        var words = stripped.Split('-', '_', ' ')
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..] : string.Empty));
        var result = string.Join(" ", words);
        return string.IsNullOrWhiteSpace(result) ? slug : result;
    }

    /// <summary>
    /// Loads the full track manifest (<c>tracks.json</c>) for a specific artist and album.
    /// </summary>
    public async Task<List<TrackItem>> LoadAlbumTracksAsync(AlbumSummary album)
    {
        var relJson = $"engine-room-records/artists/{album.ArtistSlug}/{album.AlbumSlug}/tracks.json";
        var json = await _locator.ReadAssetTextAsync(relJson);
        var manifest = JsonSerializer.Deserialize<AlbumTracksManifest>(json, JsonOptions) ?? new AlbumTracksManifest();

        // Enrich each `TrackItem` with its parent artist/album context and resolved `.mp3` URI.
        foreach (var track in manifest.Tracks)
        {
            track.ArtistSlug = album.ArtistSlug;
            track.ArtistName = album.ArtistName;
            track.AlbumSlug = album.AlbumSlug;
            track.AlbumTitle = album.AlbumTitle;
            track.CoverArtUri = album.CoverArtUri;
            track.AudioUri = _locator.ResolveAssetUri(
                $"engine-room-records/artists/{album.ArtistSlug}/{album.AlbumSlug}/web-mp3/{track.FileName}.mp3");
        }

        return manifest.Tracks;
    }

    /// <summary>
    /// Loads and parses timed lyrics for a track. Returns an empty list if instrumental.
    /// </summary>
    public async Task<List<VttCue>> LoadTrackVttCuesAsync(TrackItem track)
    {
        try
        {
            var relVtt = $"engine-room-records/artists/{track.ArtistSlug}/{track.AlbumSlug}/lyrics/{track.FileName}.vtt";
            var vttText = await _locator.ReadAssetTextAsync(relVtt);
            return ParseWebVtt(vttText);
        }
        catch
        {
            // Instrumental tracks have no `.vtt` file -> return an empty cue list cleanly.
            return [];
        }
    }

    /// <summary>
    /// Loads the full lyrics file (<c>lyrics/&lt;fileName&gt;.md</c>) for a <see cref="TrackItem"/>.
    /// </summary>
    public async Task<string> LoadTrackMarkdownLyricsAsync(TrackItem track)
    {
        return await LoadTrackMarkdownLyricsBySlugAsync(track.ArtistSlug, track.AlbumSlug, track.FileName);
    }

    /// <summary>
    /// Loads the full lyrics file (<c>lyrics/&lt;fileName&gt;.md</c>) by artist, album, and track filename stem.
    /// </summary>
    public async Task<string> LoadTrackMarkdownLyricsBySlugAsync(string artistSlug, string albumSlug, string fileName)
    {
        try
        {
            var relMd = $"engine-room-records/artists/{artistSlug}/{albumSlug}/lyrics/{fileName}.md";
            return await _locator.ReadAssetTextAsync(relMd);
        }
        catch
        {
            return "Instrumental or full lyrics not available for this song.";
        }
    }

    /// <summary>
    /// Loads the open-culture song creation &amp; liner notes (<c>suno-prompts/&lt;fileName&gt;.md</c>) for a track.
    /// </summary>
    public async Task<string> LoadTrackSunoPromptAsync(TrackItem track)
    {
        try
        {
            var relPrompt = $"engine-room-records/artists/{track.ArtistSlug}/{track.AlbumSlug}/suno-prompts/{track.FileName}.md";
            return await _locator.ReadAssetTextAsync(relPrompt);
        }
        catch
        {
            return "Song creation notes are not available for this song.";
        }
    }

    /// <summary>
    /// Parses timed lyric cues from a <c>.vtt</c> text file into chronological <see cref="VttCue"/> objects.
    /// </summary>
    // HOW TIMED LYRICS FILES WORK:
    //   A timed lyrics file consists of blocks separated by blank lines:
    //     WEBVTT
    //
    //     00:14.200 --> 00:18.650
    //     First sung line of the verse
    //
    //   `ParseWebVtt` splits on blank lines (`\n\s*\n`), skips header/comment blocks that lack a
    //   `-->` timing arrow, parses `start` and `end` into .NET `TimeSpan` values, and joins the
    //   remaining lines of the block as the sung `Text`!
    public static List<VttCue> ParseWebVtt(string vttContent)
    {
        var cues = new List<VttCue>();
        if (string.IsNullOrWhiteSpace(vttContent))
        {
            return cues;
        }

        var normalized = vttContent.Replace("\r\n", "\n");
        var blocks = Regex.Split(normalized, @"\n\s*\n");

        foreach (var rawBlock in blocks)
        {
            var lines = rawBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                continue;
            }

            // Locate the line containing `00:00.000 --> 00:04.000` (allows an optional numeric cue ID on line 0).
            var timingLineIndex = -1;
            Match? timingMatch = null;
            for (var i = 0; i < lines.Length; i++)
            {
                var m = VttTimingRegex.Match(lines[i]);
                if (m.Success)
                {
                    timingLineIndex = i;
                    timingMatch = m;
                    break;
                }
            }

            // Skip `WEBVTT` file headers and `NOTE ...` metadata blocks that have no `-->` arrow.
            if (timingLineIndex < 0 || timingMatch is null)
            {
                continue;
            }

            var start = ParseVttTimestamp(timingMatch.Groups["start"].Value);
            var end = ParseVttTimestamp(timingMatch.Groups["end"].Value);
            var textLines = lines.Skip(timingLineIndex + 1).ToList();
            var cueText = string.Join(" ", textLines).Trim();

            if (!string.IsNullOrWhiteSpace(cueText))
            {
                cues.Add(new VttCue
                {
                    StartTime = start,
                    EndTime = end,
                    Text = cueText
                });
            }
        }

        return cues;
    }

    // Converts `"01:23.450"` (`MM:SS.mmm`) or `"00:01:23.450"` (`HH:MM:SS.mmm`) into a .NET `TimeSpan`.
    private static TimeSpan ParseVttTimestamp(string raw)
    {
        var parts = raw.Split(':');
        if (parts.Length == 3)
        {
            var hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var seconds = double.Parse(parts[2], CultureInfo.InvariantCulture);
            return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        if (parts.Length == 2)
        {
            var minutes = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var seconds = double.Parse(parts[1], CultureInfo.InvariantCulture);
            return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.Zero;
    }
}
