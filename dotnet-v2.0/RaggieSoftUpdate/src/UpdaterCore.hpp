// ================================================================================
// FILE: UpdaterCore.hpp
// PROJECT: RaggieSoftUpdate (Zero-Runtime Native C++20 Win32 Update Utility)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL READER RETURNING FROM CLASSIC MFC):
//   Welcome back to C++! If the last C++ you touched was 1990s/2000s MFC (or QFC
//   in the RaggieSoft novels!), modern C++20 feels like a breath of fresh air:
//     - Instead of 'CString', 'TCHAR*', and manual 'new'/'delete' memory leaks,
//       modern C++ uses 'std::string' (UTF-8), 'std::wstring' (UTF-16 wide text
//       for Win32 '*W' APIs), and 'std::vector<T>' (resizable arrays that clean
//       up their own memory automatically the instant they go out of scope — a
//       core C++ superpower called **RAII**: Resource Acquisition Is Initialization).
//     - Instead of giant 'AFX_MSG' macro tables, clean structs and standard
//       functions keep data parsing 100% separate from OS window code.
//
//   Why is this header written in pure, OS-independent C++20?
//     By keeping command-line parsing ('ParseUpdaterArgs'), dotted version math
//     ('CompareVersionStrings'), and 'apps-manifest.json' parsing ('ParseManifestJson')
//     in standard C++20 without '<windows.h>', we can compile and unit-test this
//     exact header on macOS/Linux ('clang++ -std=c++20') AND in Visual Studio 2026
//     on Windows ('/std:c++20 /MT') with zero external JSON libraries!
// ================================================================================

#pragma once

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <string>
#include <string_view>
#include <vector>

namespace raggiesoft::updater {

// Execution UI modes modeled directly on Inno Setup's command-line conventions:
//   - InteractiveWizard: Full step-by-step GUI wizard (default).
//   - SilentProgress:    '/SILENT' — non-interactive compact progress bar window only.
//   - VerySilent:        '/VERYSILENT' — 100% invisible background execution.
enum class UiMode {
    InteractiveWizard,
    SilentProgress,
    VerySilent
};

// Deterministic process exit codes so calling apps (Launcher, StoreTrainer, etc.)
// can run 'RaggieSoftUpdate.exe --check-only /VERYSILENT' and inspect the exit code!
namespace ExitCodes {
    inline constexpr int UpToDateOrSuccess = 0;
    inline constexpr int UpdatesAvailable  = 10;
    inline constexpr int NetworkError      = 2;
    inline constexpr int ManifestError     = 3;
    inline constexpr int InstallError      = 4;
    inline constexpr int UserCanceled      = 5;
}

// Parsed command-line options passed to RaggieSoftUpdate.exe.
struct UpdaterArgs {
    UiMode mode = UiMode::InteractiveWizard;
    bool checkOnly = false;       // '--check-only' / '/CHECKONLY': don't download/install; exit 10 if updates exist.
    std::string focusAppId;       // Optional '--app storetrainer' / '/APP=book-reader' caller hint.
    std::string manifestUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps-manifest.json";
};

// Metadata for one RaggieSoft desktop application parsed from 'apps-manifest.json'
// and enriched with live local Windows Registry installation state.
struct SuiteAppEntry {
    std::string id;
    std::string innoAppId;
    std::string title;
    std::string imprint;
    std::string category;
    std::string description;
    std::string latestVersion;
    std::string exeName;
    std::string installerFileName;
    std::string installerDownloadUrl;
    std::string webUrl;

    // Optional Classic VB6 1.x patch metadata (used when StoreTrainer 1.0 is detected!)
    std::string latestClassicVersion;
    std::string classicPatchFileName;
    std::string classicPatchDownloadUrl;

    // --- Live Local Machine State (Populated by Windows Registry Scanner) ---
    bool isInstalled = false;
    bool isClassic32Bit = false;  // True when 32-bit VB6 'TStore.exe' was found in HKLM32!
    std::string installedVersion;
    std::string installLocation;
    std::string executablePath;
    bool selectedForUpdate = false;

    // Returns true when this app is installed locally and a newer version exists in the manifest.
    [[nodiscard]] bool HasUpdate() const;
};

// Case-insensitive ASCII lowercase helper.
inline std::string ToLowerAscii(std::string_view input) {
    std::string out(input);
    std::transform(out.begin(), out.end(), out.begin(), [](unsigned char c) {
        return static_cast<char>(std::tolower(c));
    });
    return out;
}

// Trims leading and trailing whitespace from a string_view without copying.
inline std::string_view TrimAscii(std::string_view sv) {
    while (!sv.empty() && std::isspace(static_cast<unsigned char>(sv.front()))) {
        sv.remove_prefix(1);
    }
    while (!sv.empty() && std::isspace(static_cast<unsigned char>(sv.back()))) {
        sv.remove_suffix(1);
    }
    return sv;
}

// Compares two dotted version strings numerically segment-by-segment (e.g., "1.2.0" vs "1.10.0"):
//   Returns -1 if v1 < v2, 0 if v1 == v2, +1 if v1 > v2.
//   Strips any leading 'v'/'V' or '+buildMetadata' suffix automatically.
inline int CompareVersionStrings(std::string_view v1Raw, std::string_view v2Raw) {
    auto normalize = [](std::string_view v) -> std::vector<long> {
        v = TrimAscii(v);
        if (!v.empty() && (v.front() == 'v' || v.front() == 'V')) {
            v.remove_prefix(1);
        }
        if (auto plusPos = v.find('+'); plusPos != std::string_view::npos) {
            v = v.substr(0, plusPos);
        }
        if (auto dashPos = v.find('-'); dashPos != std::string_view::npos) {
            v = v.substr(0, dashPos);
        }

        std::vector<long> parts;
        std::size_t start = 0;
        while (start <= v.size()) {
            auto dot = v.find('.', start);
            auto token = (dot == std::string_view::npos)
                ? v.substr(start)
                : v.substr(start, dot - start);

            long num = 0;
            for (char ch : token) {
                if (ch >= '0' && ch <= '9') {
                    num = (num * 10) + (ch - '0');
                } else {
                    break;
                }
            }
            parts.push_back(num);
            if (dot == std::string_view::npos) {
                break;
            }
            start = dot + 1;
        }
        return parts;
    };

    const auto p1 = normalize(v1Raw);
    const auto p2 = normalize(v2Raw);
    const std::size_t maxLen = std::max(p1.size(), p2.size());

    for (std::size_t i = 0; i < maxLen; ++i) {
        const long a = (i < p1.size()) ? p1[i] : 0;
        const long b = (i < p2.size()) ? p2[i] : 0;
        if (a < b) return -1;
        if (a > b) return 1;
    }
    return 0;
}

inline bool SuiteAppEntry::HasUpdate() const {
    if (!isInstalled || installedVersion.empty() || latestVersion.empty()) {
        return false;
    }
    return CompareVersionStrings(installedVersion, latestVersion) < 0;
}

// Parses command-line arguments supporting both Inno Setup '/FLAG' style and POSIX '--flag' style.
inline UpdaterArgs ParseUpdaterArgs(const std::vector<std::string>& rawArgs) {
    UpdaterArgs parsed;
    for (std::size_t i = 0; i < rawArgs.size(); ++i) {
        const std::string arg = std::string(TrimAscii(rawArgs[i]));
        const std::string lower = ToLowerAscii(arg);

        if (lower == "/verysilent" || lower == "--very-silent" || lower == "--verysilent" || lower == "-q") {
            parsed.mode = UiMode::VerySilent;
        } else if (lower == "/silent" || lower == "--silent") {
            if (parsed.mode != UiMode::VerySilent) {
                parsed.mode = UiMode::SilentProgress;
            }
        } else if (lower == "/wizard" || lower == "--wizard") {
            parsed.mode = UiMode::InteractiveWizard;
        } else if (lower == "/checkonly" || lower == "--check-only" || lower == "--check") {
            parsed.checkOnly = true;
        } else if ((lower == "--app" || lower == "/app") && (i + 1 < rawArgs.size())) {
            parsed.focusAppId = ToLowerAscii(TrimAscii(rawArgs[++i]));
        } else if (lower.rfind("/app=", 0) == 0) {
            parsed.focusAppId = ToLowerAscii(TrimAscii(std::string_view(arg).substr(5)));
        } else if (lower.rfind("--app=", 0) == 0) {
            parsed.focusAppId = ToLowerAscii(TrimAscii(std::string_view(arg).substr(6)));
        } else if ((lower == "--manifest-url" || lower == "/manifesturl") && (i + 1 < rawArgs.size())) {
            parsed.manifestUrl = std::string(TrimAscii(rawArgs[++i]));
        }
    }
    return parsed;
}

// ================================================================================
// ZERO-DEPENDENCY JSON ARRAY-OF-OBJECTS PARSER FOR 'apps-manifest.json'
//   Why write a focused 120-line recursive-descent JSON reader here?
//   Because pulling in boost or external packages isn't needed to read a clean
//   array of flat JSON objects ('[ { "id": "...", "latestVersion": "..." }, ... ]')!
//   Handles standard JSON escapes (\" \\ \/ \n \r \t \uXXXX BMP) safely.
// ================================================================================
namespace detail {

struct JsonCursor {
    std::string_view src;
    std::size_t pos = 0;

    void SkipWs() {
        while (pos < src.size() && std::isspace(static_cast<unsigned char>(src[pos]))) {
            ++pos;
        }
    }

    bool Consume(char expected) {
        SkipWs();
        if (pos < src.size() && src[pos] == expected) {
            ++pos;
            return true;
        }
        return false;
    }

    bool ParseString(std::string& out) {
        SkipWs();
        if (pos >= src.size() || src[pos] != '"') {
            return false;
        }
        ++pos; // skip opening '"'
        out.clear();

        while (pos < src.size()) {
            char c = src[pos++];
            if (c == '"') {
                return true;
            }
            if (c == '\\') {
                if (pos >= src.size()) return false;
                char esc = src[pos++];
                switch (esc) {
                    case '"':  out.push_back('"'); break;
                    case '\\': out.push_back('\\'); break;
                    case '/':  out.push_back('/'); break;
                    case 'b':  out.push_back('\b'); break;
                    case 'f':  out.push_back('\f'); break;
                    case 'n':  out.push_back('\n'); break;
                    case 'r':  out.push_back('\r'); break;
                    case 't':  out.push_back('\t'); break;
                    case 'u': {
                        if (pos + 4 > src.size()) return false;
                        unsigned int code = 0;
                        for (int k = 0; k < 4; ++k) {
                            char hc = src[pos++];
                            code <<= 4;
                            if (hc >= '0' && hc <= '9') code |= static_cast<unsigned int>(hc - '0');
                            else if (hc >= 'a' && hc <= 'f') code |= static_cast<unsigned int>(10 + hc - 'a');
                            else if (hc >= 'A' && hc <= 'F') code |= static_cast<unsigned int>(10 + hc - 'A');
                        }
                        // Encode Basic Multilingual Plane codepoint as UTF-8
                        if (code <= 0x7F) {
                            out.push_back(static_cast<char>(code));
                        } else if (code <= 0x7FF) {
                            out.push_back(static_cast<char>(0xC0 | ((code >> 6) & 0x1F)));
                            out.push_back(static_cast<char>(0x80 | (code & 0x3F)));
                        } else {
                            out.push_back(static_cast<char>(0xE0 | ((code >> 12) & 0x0F)));
                            out.push_back(static_cast<char>(0x80 | ((code >> 6) & 0x3F)));
                            out.push_back(static_cast<char>(0x80 | (code & 0x3F)));
                        }
                        break;
                    }
                    default:
                        out.push_back(esc);
                        break;
                }
            } else {
                out.push_back(c);
            }
        }
        return false;
    }

    // Skips any non-string JSON value (numbers, booleans, null, nested arrays/objects)
    bool SkipAnyValue() {
        SkipWs();
        if (pos >= src.size()) return false;
        char c = src[pos];
        if (c == '"') {
            std::string dummy;
            return ParseString(dummy);
        }
        if (c == '{' || c == '[') {
            char open = c;
            char close = (c == '{') ? '}' : ']';
            int depth = 0;
            while (pos < src.size()) {
                if (src[pos] == '"') {
                    std::string dummy;
                    if (!ParseString(dummy)) return false;
                    continue;
                }
                if (src[pos] == open) ++depth;
                else if (src[pos] == close) {
                    --depth;
                    if (depth == 0) {
                        ++pos;
                        return true;
                    }
                }
                ++pos;
            }
            return false;
        }
        // Primitive literal (true, false, null, number)
        while (pos < src.size()) {
            char ch = src[pos];
            if (ch == ',' || ch == '}' || ch == ']' || std::isspace(static_cast<unsigned char>(ch))) {
                break;
            }
            ++pos;
        }
        return true;
    }
};

} // namespace detail

// Built-in offline fallback catalog (covers all 6 RaggieSoft desktop apps in 100% lowercase kebab-case!).
inline std::vector<SuiteAppEntry> GetBuiltInSuiteCatalog() {
    return {
        SuiteAppEntry{
            .id = "launcher",
            .innoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B000}",
            .title = "RaggieSoft Launcher",
            .imprint = "RAGGIESOFT MEDIA",
            .category = "Suite Hub",
            .description = "Central application hub for discovering, installing, and managing all RaggieSoft desktop apps.",
            .latestVersion = "1.0.0",
            .exeName = "RaggieSoftLauncher.exe",
            .installerFileName = "raggiesoft-launcher-setup.exe",
            .installerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/launcher/raggiesoft-launcher-setup.exe",
            .webUrl = "https://launcher.raggiesoft.com"
        },
        SuiteAppEntry{
            .id = "book-reader",
            .innoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B001}",
            .title = "RaggieSoft Book Reader",
            .imprint = "OCEAN VIEW ARCHIVES",
            .category = "Books",
            .description = "Read all Ocean View Archives and Story Crossroads novels with scene illustrations and offline downloads.",
            .latestVersion = "1.0.0",
            .exeName = "BookReader.exe",
            .installerFileName = "raggiesoft-book-reader-setup.exe",
            .installerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/book-reader/raggiesoft-book-reader-setup.exe",
            .webUrl = "https://books.raggiesoft.com"
        },
        SuiteAppEntry{
            .id = "listening-room",
            .innoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B002}",
            .title = "RaggieSoft The Listening Room",
            .imprint = "ENGINE ROOM RECORDS",
            .category = "Music",
            .description = "Stream and download the Engine Room Records catalog with synchronized lyrics and liner notes.",
            .latestVersion = "1.0.0",
            .exeName = "ListeningRoom.exe",
            .installerFileName = "raggiesoft-listening-room-setup.exe",
            .installerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/listening-room/raggiesoft-listening-room-setup.exe",
            .webUrl = "https://music.raggiesoft.com"
        },
        SuiteAppEntry{
            .id = "backstage",
            .innoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B003}",
            .title = "RaggieSoft Backstage",
            .imprint = "RAGGIESOFT MEDIA",
            .category = "Bonus Features",
            .description = "Explore behind-the-scenes dossiers, character correspondence, and campus guides.",
            .latestVersion = "1.0.0",
            .exeName = "Backstage.exe",
            .installerFileName = "raggiesoft-backstage-setup.exe",
            .installerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/backstage/raggiesoft-backstage-setup.exe",
            .webUrl = "https://backstage.raggiesoft.com"
        },
        SuiteAppEntry{
            .id = "storetrainer",
            .innoAppId = "{8E92D4A1-63F0-4C8B-9B7A-10164500B004}",
            .title = "RaggieSoft StoreTrainer",
            .imprint = "RAGGIESOFT UTILITIES",
            .category = "Utilities",
            .description = "Organize and launch PC games with companion trainers, pre-launch save backups, .NFO cheat cards, and .stfx profiles.",
            .latestVersion = "2.0.0",
            .exeName = "StoreTrainer.exe",
            .installerFileName = "raggiesoft-storetrainer-setup.exe",
            .installerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/storetrainer/raggiesoft-storetrainer-setup.exe",
            .webUrl = "https://storetrainer.raggiesoft.com",
            .latestClassicVersion = "1.1.0",
            .classicPatchFileName = "tspatch110.exe",
            .classicPatchDownloadUrl = "https://assets.raggiesoft.com/storetrainer/tspatch110.exe"
        },
        SuiteAppEntry{
            .id = "accessibility-studio",
            .innoAppId = "{50F8BAE3-295E-4D24-BEC0-89D6ABBCDA9D}",
            .title = "RaggieSoft Accessibility Studio",
            .imprint = "RAGGIESOFT ENTERPRISE",
            .category = "Developer Tools",
            .description = "Enterprise WCAG 2.2 AA and Revised Section 508 source-code web IDE and SARIF 2.1.0 CLI scanner.",
            .latestVersion = "1.0.0",
            .exeName = "AccessibilityStudio.exe",
            .installerFileName = "raggiesoft-accessibility-studio-setup.exe",
            .installerDownloadUrl = "https://assets.raggiesoft.com/desktop-apps/windows/apps/accessibility-studio/raggiesoft-accessibility-studio-setup.exe",
            .webUrl = "https://a11y.raggiesoft.com"
        }
    };
}

// Parses 'apps-manifest.json' (either a root JSON array '[...]' or '{ "apps": [...] }')
// and merges remote version numbers & download URLs onto the built-in catalog!
inline bool MergeRemoteManifestJson(std::string_view jsonText, std::vector<SuiteAppEntry>& catalog) {
    // Strip optional UTF-8 BOM if present
    if (jsonText.size() >= 3 &&
        static_cast<unsigned char>(jsonText[0]) == 0xEF &&
        static_cast<unsigned char>(jsonText[1]) == 0xBB &&
        static_cast<unsigned char>(jsonText[2]) == 0xBF) {
        jsonText.remove_prefix(3);
    }

    detail::JsonCursor cur{jsonText, 0};
    cur.SkipWs();
    if (cur.pos >= cur.src.size()) {
        return false;
    }

    // Allow either top-level '[ ... ]' OR '{ "apps": [ ... ] }'
    if (cur.src[cur.pos] == '{') {
        auto arrPos = cur.src.find('[');
        if (arrPos == std::string_view::npos) return false;
        cur.pos = arrPos;
    }

    if (!cur.Consume('[')) {
        return false;
    }

    cur.SkipWs();
    if (cur.Consume(']')) {
        return true; // Empty array is valid JSON
    }

    while (cur.pos < cur.src.size()) {
        if (!cur.Consume('{')) {
            return false;
        }

        SuiteAppEntry parsedItem;
        cur.SkipWs();
        if (!cur.Consume('}')) {
            while (cur.pos < cur.src.size()) {
                std::string key;
                if (!cur.ParseString(key)) return false;
                if (!cur.Consume(':')) return false;

                cur.SkipWs();
                if (cur.pos < cur.src.size() && cur.src[cur.pos] == '"') {
                    std::string val;
                    if (!cur.ParseString(val)) return false;
                    if (key == "id") parsedItem.id = val;
                    else if (key == "innoAppId") parsedItem.innoAppId = val;
                    else if (key == "title") parsedItem.title = val;
                    else if (key == "imprint") parsedItem.imprint = val;
                    else if (key == "category") parsedItem.category = val;
                    else if (key == "description") parsedItem.description = val;
                    else if (key == "latestVersion") parsedItem.latestVersion = val;
                    else if (key == "exeName") parsedItem.exeName = val;
                    else if (key == "installerFileName") parsedItem.installerFileName = val;
                    else if (key == "installerDownloadUrl") parsedItem.installerDownloadUrl = val;
                    else if (key == "webUrl") parsedItem.webUrl = val;
                    else if (key == "latestClassicVersion") parsedItem.latestClassicVersion = val;
                    else if (key == "classicPatchFileName") parsedItem.classicPatchFileName = val;
                    else if (key == "classicPatchDownloadUrl") parsedItem.classicPatchDownloadUrl = val;
                } else {
                    if (!cur.SkipAnyValue()) return false;
                }

                if (cur.Consume(',')) {
                    continue;
                }
                if (cur.Consume('}')) {
                    break;
                }
                return false;
            }
        }

        if (!parsedItem.id.empty() || !parsedItem.innoAppId.empty()) {
            auto it = std::find_if(catalog.begin(), catalog.end(), [&](const SuiteAppEntry& existing) {
                return (!parsedItem.id.empty() && ToLowerAscii(existing.id) == ToLowerAscii(parsedItem.id)) ||
                       (!parsedItem.innoAppId.empty() && ToLowerAscii(existing.innoAppId) == ToLowerAscii(parsedItem.innoAppId));
            });

            if (it != catalog.end()) {
                if (!parsedItem.latestVersion.empty()) it->latestVersion = parsedItem.latestVersion;
                if (!parsedItem.description.empty()) it->description = parsedItem.description;
                if (!parsedItem.installerFileName.empty()) it->installerFileName = parsedItem.installerFileName;
                if (!parsedItem.installerDownloadUrl.empty()) it->installerDownloadUrl = parsedItem.installerDownloadUrl;
                if (!parsedItem.latestClassicVersion.empty()) it->latestClassicVersion = parsedItem.latestClassicVersion;
                if (!parsedItem.classicPatchFileName.empty()) it->classicPatchFileName = parsedItem.classicPatchFileName;
                if (!parsedItem.classicPatchDownloadUrl.empty()) it->classicPatchDownloadUrl = parsedItem.classicPatchDownloadUrl;
            } else if (!parsedItem.title.empty() && !parsedItem.latestVersion.empty()) {
                catalog.push_back(std::move(parsedItem));
            }
        }

        if (cur.Consume(',')) {
            continue;
        }
        if (cur.Consume(']')) {
            return true;
        }
        return false;
    }

    return false;
}

} // namespace raggiesoft::updater
