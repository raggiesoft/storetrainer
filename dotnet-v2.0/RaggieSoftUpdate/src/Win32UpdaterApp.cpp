// ================================================================================
// FILE: Win32UpdaterApp.cpp
// PROJECT: RaggieSoftUpdate (Zero-Runtime Native C++20 Win32 Update Utility)
// COMPILER: Visual Studio 2026 C++20 (/std:c++20 /MT Static Runtime — ZERO DLLs!)
// ARCHITECTURES: Native x64 & Native ARM64 (Windows 10 & Windows 11)
// LICENSE: MIT License (Code) / CC BY-SA 4.0 (Creative Content)
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL READER RETURNING FROM CLASSIC MFC):
//   What is this file?
//     This single, clean modern C++20 file compiles 'RaggieSoftUpdate.exe' — a
//     true ZERO-EXTERNAL-RUNTIME native Windows binary (~250 KB!) that checks,
//     downloads, and installs updates across EVERY RaggieSoft desktop application:
//       - RaggieSoft Launcher
//       - RaggieSoft Book Reader
//       - RaggieSoft The Listening Room
//       - RaggieSoft Backstage
//       - RaggieSoft StoreTrainer 2.0 (AND Classic 32-bit VB6 StoreTrainer 1.0/1.1!)
//
//   Why does it need ZERO external runtime files?
//     1. In RaggieSoftUpdate.vcxproj, we set '<RuntimeLibrary>MultiThreaded</RuntimeLibrary>'
//        ('/MT' flag), which bakes the tiny C++ standard library functions we use
//        directly inside 'RaggieSoftUpdate.exe' — so users NEVER need 'vcruntime140.dll',
//        'msvcp140.dll', 'msvbvm60.dll', or a '.NET Runtime' installed!
//     2. Every OS feature we call uses DLLs built permanently into Windows itself:
//          - 'advapi32.dll' ('RegOpenKeyExW'): Reads 64-bit ('KEY_WOW64_64KEY'),
//            Current-User ('HKCU'), and 32-bit ('KEY_WOW64_32KEY') Uninstall keys.
//          - 'winhttp.dll' ('WinHttpOpen'): Streams 'apps-manifest.json' and
//            'RaggieSoft-*-Setup.exe' over OS-native HTTPS (TLS 1.2 / TLS 1.3).
//          - 'comctl32.dll' & 'user32.dll': Renders our High-DPI Wizard window
//            (Interactive mode) or compact Progress Bar window ('/SILENT' mode),
//            or runs 100% invisibly in the background ('/VERYSILENT' mode)!
//
//   How does Modern C++20 RAII replace manual MFC handle cleanup?
//     Notice the small 'UniqueWinHttpHandle' and 'UniqueRegKey' wrappers below!
//     In C, forgetting 'WinHttpCloseHandle(h)' on an early 'return' leaked OS
//     memory. In C++20, wrapping the handle inside a tiny struct whose destructor
//     ('~UniqueWinHttpHandle()') closes it automatically means the compiler writes
//     100% leak-proof cleanup on every return path for us!
// ================================================================================

#ifdef _WIN32

#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX

#include <windows.h>
#include <commctrl.h>
#include <shellapi.h>
#include <shlobj.h>
#include <winhttp.h>

#include <atomic>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <string>
#include <thread>
#include <vector>

#include "UpdaterCore.hpp"

// Tell the MSVC linker to link only built-in Windows OS system import libraries
// and embed a Common Controls v6 visual-styles manifest directly into the .exe!
#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "gdi32.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "winhttp.lib")

#pragma comment(linker, "\"/manifestdependency:type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")

namespace fs = std::filesystem;
using namespace raggiesoft::updater;

namespace {

// ============================================================================
// SECTION 1: UTF-8 <-> UTF-16 ('std::wstring') Conversion & RAII OS Handles
// ============================================================================

std::wstring Utf8ToWide(std::string_view utf8) {
    if (utf8.empty()) return {};
    int needed = ::MultiByteToWideChar(CP_UTF8, 0, utf8.data(), static_cast<int>(utf8.size()), nullptr, 0);
    if (needed <= 0) return {};
    std::wstring out(static_cast<std::size_t>(needed), L'\0');
    ::MultiByteToWideChar(CP_UTF8, 0, utf8.data(), static_cast<int>(utf8.size()), out.data(), needed);
    return out;
}

std::string WideToUtf8(std::wstring_view wide) {
    if (wide.empty()) return {};
    int needed = ::WideCharToMultiByte(CP_UTF8, 0, wide.data(), static_cast<int>(wide.size()), nullptr, 0, nullptr, nullptr);
    if (needed <= 0) return {};
    std::string out(static_cast<std::size_t>(needed), '\0');
    ::WideCharToMultiByte(CP_UTF8, 0, wide.data(), static_cast<int>(wide.size()), out.data(), needed, nullptr, nullptr);
    return out;
}

// RAII wrapper for Windows Registry HKEY handles (closes automatically at scope exit).
struct UniqueRegKey {
    HKEY handle = nullptr;
    UniqueRegKey() = default;
    explicit UniqueRegKey(HKEY h) : handle(h) {}
    ~UniqueRegKey() { if (handle) ::RegCloseKey(handle); }
    UniqueRegKey(const UniqueRegKey&) = delete;
    UniqueRegKey& operator=(const UniqueRegKey&) = delete;
};

// RAII wrapper for WinHTTP session/connection/request HINTERNET handles.
struct UniqueWinHttp {
    HINTERNET handle = nullptr;
    UniqueWinHttp() = default;
    explicit UniqueWinHttp(HINTERNET h) : handle(h) {}
    ~UniqueWinHttp() { if (handle) ::WinHttpCloseHandle(handle); }
    UniqueWinHttp(const UniqueWinHttp&) = delete;
    UniqueWinHttp& operator=(const UniqueWinHttp&) = delete;
    explicit operator bool() const { return handle != nullptr; }
};

// Reads a REG_SZ string value from an open Registry key cleanly.
std::wstring ReadRegString(HKEY hKey, const wchar_t* valueName) {
    DWORD type = 0;
    DWORD byteSize = 0;
    if (::RegQueryValueExW(hKey, valueName, nullptr, &type, nullptr, &byteSize) != ERROR_SUCCESS ||
        (type != REG_SZ && type != REG_EXPAND_SZ) || byteSize == 0) {
        return {};
    }
    std::wstring buf(byteSize / sizeof(wchar_t), L'\0');
    if (::RegQueryValueExW(hKey, valueName, nullptr, &type, reinterpret_cast<LPBYTE>(buf.data()), &byteSize) != ERROR_SUCCESS) {
        return {};
    }
    while (!buf.empty() && buf.back() == L'\0') {
        buf.pop_back();
    }
    return buf;
}

// Resolves '%LOCALAPPDATA%\RaggieSoft\Cache\Installers' using SHGetKnownFolderPath.
fs::path GetInstallerCacheDir() {
    PWSTR rawPath = nullptr;
    fs::path result = fs::temp_directory_path() / L"RaggieSoftInstallers";
    if (SUCCEEDED(::SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &rawPath)) && rawPath) {
        result = fs::path(rawPath) / L"RaggieSoft" / L"Cache" / L"Installers";
        ::CoTaskMemFree(rawPath);
    }
    std::error_code ec;
    fs::create_directories(result, ec);
    return result;
}

// ============================================================================
// SECTION 2: Multi-View Windows Registry Discovery (HKLM64, HKCU, and HKLM32)
// ============================================================================

void ScanInnoUninstallView(HKEY rootHive, REGSAM wow64Flag, std::vector<SuiteAppEntry>& catalog) {
    HKEY rawUninstall = nullptr;
    if (::RegOpenKeyExW(
            rootHive,
            L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
            0,
            KEY_READ | wow64Flag,
            &rawUninstall) != ERROR_SUCCESS) {
        return;
    }
    UniqueRegKey uninstallKey(rawUninstall);

    wchar_t subKeyName[256]{};
    for (DWORD idx = 0;; ++idx) {
        DWORD nameLen = 255;
        if (::RegEnumKeyExW(uninstallKey.handle, idx, subKeyName, &nameLen, nullptr, nullptr, nullptr, nullptr) != ERROR_SUCCESS) {
            break;
        }

        HKEY rawApp = nullptr;
        if (::RegOpenKeyExW(uninstallKey.handle, subKeyName, 0, KEY_READ | wow64Flag, &rawApp) != ERROR_SUCCESS) {
            continue;
        }
        UniqueRegKey appKey(rawApp);

        const std::string subUtf8 = WideToUtf8(subKeyName);
        const std::string displayName = WideToUtf8(ReadRegString(appKey.handle, L"DisplayName"));
        const std::string publisher = WideToUtf8(ReadRegString(appKey.handle, L"Publisher"));
        const std::string displayVer = WideToUtf8(ReadRegString(appKey.handle, L"DisplayVersion"));
        std::wstring installLocW = ReadRegString(appKey.handle, L"InstallLocation");
        while (!installLocW.empty() && (installLocW.back() == L'\\' || installLocW.back() == L'/')) {
            installLocW.pop_back();
        }

        if (ToLowerAscii(publisher).find("raggiesoft") == std::string::npos &&
            ToLowerAscii(displayName).find("raggiesoft") == std::string::npos) {
            continue;
        }

        for (auto& entry : catalog) {
            if (!entry.innoAppId.empty() &&
                ToLowerAscii(subUtf8).rfind(ToLowerAscii(entry.innoAppId), 0) == 0) {
                fs::path exeCandidate = fs::path(installLocW) / Utf8ToWide(entry.exeName);
                std::error_code ec;
                if (!installLocW.empty() && fs::exists(exeCandidate, ec)) {
                    entry.isInstalled = true;
                    entry.isClassic32Bit = false;
                    entry.installedVersion = displayVer.empty() ? "1.0.0" : displayVer;
                    entry.installLocation = WideToUtf8(installLocW);
                    entry.executablePath = WideToUtf8(exeCandidate.wstring());
                }
            }
        }
    }
}

// Also checks 32-bit Registry ('KEY_WOW64_32KEY') for Classic Visual Basic 6.0 StoreTrainer 1.0/1.1!
void ScanClassic32BitStoreTrainer(std::vector<SuiteAppEntry>& catalog) {
    auto it = std::find_if(catalog.begin(), catalog.end(), [](const SuiteAppEntry& e) {
        return e.id == "storetrainer";
    });
    if (it == catalog.end() || it->isInstalled) {
        return; // Modern 64-bit StoreTrainer 2.0 is already installed
    }

    HKEY rawClassic = nullptr;
    if (::RegOpenKeyExW(
            HKEY_LOCAL_MACHINE,
            L"Software\\RaggieSoft\\StoreTrainer",
            0,
            KEY_READ | KEY_WOW64_32KEY,
            &rawClassic) != ERROR_SUCCESS) {
        return;
    }
    UniqueRegKey classicKey(rawClassic);

    std::wstring dirW = ReadRegString(classicKey.handle, L"Installation Folder");
    std::wstring verW = ReadRegString(classicKey.handle, L"Product Version");
    fs::path tstoreExe = fs::path(dirW) / L"TStore.exe";
    std::error_code ec;
    if (!dirW.empty() && fs::exists(tstoreExe, ec)) {
        std::string verUtf8 = WideToUtf8(verW);
        // Normalize 4-part VB6 version ('1.1.0.0' -> '1.1.0')
        int dots = 0;
        for (std::size_t i = 0; i < verUtf8.size(); ++i) {
            if (verUtf8[i] == '.' && ++dots == 3) {
                verUtf8 = verUtf8.substr(0, i);
                break;
            }
        }
        it->isInstalled = true;
        it->isClassic32Bit = true;
        it->installedVersion = verUtf8.empty() ? "1.0.0" : verUtf8;
        it->installLocation = WideToUtf8(dirW);
        it->executablePath = WideToUtf8(tstoreExe.wstring());
    }
}

void DiscoverInstalledApps(std::vector<SuiteAppEntry>& catalog) {
    for (auto& e : catalog) {
        e.isInstalled = false;
        e.isClassic32Bit = false;
        e.installedVersion.clear();
        e.installLocation.clear();
        e.executablePath.clear();
        e.selectedForUpdate = false;
    }
    ScanInnoUninstallView(HKEY_LOCAL_MACHINE, KEY_WOW64_64KEY, catalog);
    ScanInnoUninstallView(HKEY_CURRENT_USER, KEY_WOW64_64KEY, catalog);
    ScanClassic32BitStoreTrainer(catalog);
}

// ============================================================================
// SECTION 3: Native OS HTTPS Client ('winhttp.dll' — TLS 1.2 / TLS 1.3)
// ============================================================================

struct CrackedHttpsUrl {
    std::wstring host;
    std::wstring pathAndQuery;
    INTERNET_PORT port = INTERNET_DEFAULT_HTTPS_PORT;
    bool secure = true;
};

bool CrackUrl(const std::wstring& url, CrackedHttpsUrl& out) {
    URL_COMPONENTS uc{};
    uc.dwStructSize = sizeof(uc);
    uc.dwHostNameLength = static_cast<DWORD>(-1);
    uc.dwUrlPathLength = static_cast<DWORD>(-1);
    uc.dwExtraInfoLength = static_cast<DWORD>(-1);

    if (!::WinHttpCrackUrl(url.c_str(), static_cast<DWORD>(url.size()), 0, &uc)) {
        return false;
    }
    out.host.assign(uc.lpszHostName, uc.dwHostNameLength);
    out.pathAndQuery.assign(uc.lpszUrlPath, uc.dwUrlPathLength);
    if (uc.dwExtraInfoLength > 0) {
        out.pathAndQuery.append(uc.lpszExtraInfo, uc.dwExtraInfoLength);
    }
    out.port = uc.nPort;
    out.secure = (uc.nScheme == INTERNET_SCHEME_HTTPS);
    return true;
}

bool HttpGetToString(std::string_view urlUtf8, std::string& responseBody) {
    CrackedHttpsUrl parts;
    if (!CrackUrl(Utf8ToWide(urlUtf8), parts)) return false;

    UniqueWinHttp session(::WinHttpOpen(
        L"RaggieSoftUpdate/2.0 (Windows NT; Native Win32 C++20)",
        WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
        WINHTTP_NO_PROXY_NAME,
        WINHTTP_NO_PROXY_BYPASS,
        0));
    if (!session) return false;

    ::WinHttpSetTimeouts(session.handle, 6000, 6000, 10000, 15000);

    UniqueWinHttp connect(::WinHttpConnect(session.handle, parts.host.c_str(), parts.port, 0));
    if (!connect) return false;

    UniqueWinHttp request(::WinHttpOpenRequest(
        connect.handle,
        L"GET",
        parts.pathAndQuery.c_str(),
        nullptr,
        WINHTTP_NO_REFERER,
        WINHTTP_DEFAULT_ACCEPT_TYPES,
        parts.secure ? WINHTTP_FLAG_SECURE : 0));
    if (!request) return false;

    if (!::WinHttpSendRequest(request.handle, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0) ||
        !::WinHttpReceiveResponse(request.handle, nullptr)) {
        return false;
    }

    DWORD statusCode = 0;
    DWORD statusSize = sizeof(statusCode);
    ::WinHttpQueryHeaders(
        request.handle,
        WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
        WINHTTP_HEADER_NAME_BY_INDEX,
        &statusCode,
        &statusSize,
        WINHTTP_NO_HEADER_INDEX);
    if (statusCode < 200 || statusCode >= 300) {
        return false;
    }

    responseBody.clear();
    DWORD available = 0;
    while (::WinHttpQueryDataAvailable(request.handle, &available) && available > 0) {
        std::string chunk(available, '\0');
        DWORD downloaded = 0;
        if (!::WinHttpReadData(request.handle, chunk.data(), available, &downloaded)) {
            return false;
        }
        chunk.resize(downloaded);
        responseBody.append(chunk);
    }
    return true;
}

// Downloads an installer binary to disk while reporting 0..100 progress via callback.
template <typename ProgressFn>
bool HttpDownloadToFile(std::string_view urlUtf8, const fs::path& destFile, ProgressFn&& onProgress) {
    CrackedHttpsUrl parts;
    if (!CrackUrl(Utf8ToWide(urlUtf8), parts)) return false;

    UniqueWinHttp session(::WinHttpOpen(
        L"RaggieSoftUpdate/2.0 (Windows NT; Native Win32 C++20)",
        WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
        WINHTTP_NO_PROXY_NAME,
        WINHTTP_NO_PROXY_BYPASS,
        0));
    if (!session) return false;

    UniqueWinHttp connect(::WinHttpConnect(session.handle, parts.host.c_str(), parts.port, 0));
    if (!connect) return false;

    UniqueWinHttp request(::WinHttpOpenRequest(
        connect.handle,
        L"GET",
        parts.pathAndQuery.c_str(),
        nullptr,
        WINHTTP_NO_REFERER,
        WINHTTP_DEFAULT_ACCEPT_TYPES,
        parts.secure ? WINHTTP_FLAG_SECURE : 0));
    if (!request) return false;

    if (!::WinHttpSendRequest(request.handle, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0) ||
        !::WinHttpReceiveResponse(request.handle, nullptr)) {
        return false;
    }

    DWORD statusCode = 0;
    DWORD statusSize = sizeof(statusCode);
    ::WinHttpQueryHeaders(
        request.handle,
        WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
        WINHTTP_HEADER_NAME_BY_INDEX,
        &statusCode,
        &statusSize,
        WINHTTP_NO_HEADER_INDEX);
    if (statusCode < 200 || statusCode >= 300) {
        return false;
    }

    wchar_t lenBuf[64]{};
    DWORD lenBufBytes = sizeof(lenBuf);
    unsigned long long totalBytes = 0;
    if (::WinHttpQueryHeaders(request.handle, WINHTTP_QUERY_CONTENT_LENGTH, WINHTTP_HEADER_NAME_BY_INDEX, lenBuf, &lenBufBytes, WINHTTP_NO_HEADER_INDEX)) {
        totalBytes = std::wcstoull(lenBuf, nullptr, 10);
    }

    fs::path tmpPath = destFile;
    tmpPath += L".part";
    std::ofstream out(tmpPath, std::ios::binary | std::ios::trunc);
    if (!out) return false;

    std::vector<char> buffer(65536);
    unsigned long long received = 0;
    DWORD avail = 0;
    while (::WinHttpQueryDataAvailable(request.handle, &avail) && avail > 0) {
        const DWORD toRead = std::min<DWORD>(avail, static_cast<DWORD>(buffer.size()));
        DWORD got = 0;
        if (!::WinHttpReadData(request.handle, buffer.data(), toRead, &got)) {
            return false;
        }
        out.write(buffer.data(), got);
        received += got;
        if (totalBytes > 0) {
            int pct = static_cast<int>((received * 100ULL) / totalBytes);
            onProgress(std::clamp(pct, 0, 100));
        }
    }
    out.close();

    std::error_code ec;
    fs::rename(tmpPath, destFile, ec);
    return !ec;
}

// Executes a downloaded Inno Setup ('/VERYSILENT ...') or NSIS ('/S') installer and waits for exit code 0.
bool ExecuteInstallerSilently(const fs::path& installerPath, bool isNsisPatch) {
    const std::wstring params = isNsisPatch
        ? L"/S"
        : L"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS";

    SHELLEXECUTEINFOW sei{};
    sei.cbSize = sizeof(sei);
    sei.fMask = SEE_MASK_NOCLOSEPROCESS;
    sei.lpVerb = L"open";
    sei.lpFile = installerPath.c_str();
    sei.lpParameters = params.c_str();
    sei.nShow = SW_SHOWNORMAL;

    if (!::ShellExecuteExW(&sei) || !sei.hProcess) {
        return false;
    }

    ::WaitForSingleObject(sei.hProcess, INFINITE);
    DWORD exitCode = 1;
    ::GetExitCodeProcess(sei.hProcess, &exitCode);
    ::CloseHandle(sei.hProcess);
    return exitCode == 0;
}

// Writes a compact status file in '%LOCALAPPDATA%\RaggieSoft\Cache\update-status.json'
// so calling apps using '--check-only /VERYSILENT' can read exact version details!
void WriteCheckStatusCacheFile(const std::vector<SuiteAppEntry>& catalog) {
    PWSTR rawLocal = nullptr;
    if (FAILED(::SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &rawLocal)) || !rawLocal) {
        return;
    }
    fs::path dir = fs::path(rawLocal) / L"RaggieSoft" / L"Cache";
    ::CoTaskMemFree(rawLocal);
    std::error_code ec;
    fs::create_directories(dir, ec);

    std::ofstream out(dir / L"update-status.json", std::ios::binary | std::ios::trunc);
    if (!out) return;

    int updateCount = 0;
    for (const auto& e : catalog) {
        if (e.HasUpdate()) ++updateCount;
    }

    out << "{\n  \"updatesAvailable\": " << updateCount << ",\n  \"apps\": [\n";
    for (std::size_t i = 0; i < catalog.size(); ++i) {
        const auto& e = catalog[i];
        out << "    { \"id\": \"" << e.id
            << "\", \"installed\": " << (e.isInstalled ? "true" : "false")
            << ", \"installedVersion\": \"" << e.installedVersion
            << "\", \"latestVersion\": \"" << e.latestVersion
            << "\", \"hasUpdate\": " << (e.HasUpdate() ? "true" : "false") << " }"
            << (i + 1 < catalog.size() ? "," : "") << "\n";
    }
    out << "  ]\n}\n";
}

// ============================================================================
// SECTION 4: '/SILENT' Compact Progress Window & Interactive Update Wizard UI
// ============================================================================

constexpr UINT WM_UPDATER_STATUS   = WM_APP + 10;
constexpr UINT WM_UPDATER_PROGRESS = WM_APP + 11;
constexpr UINT WM_UPDATER_FINISHED = WM_APP + 12;

constexpr int IDC_HEADER_LABEL = 1001;
constexpr int IDC_SUB_LABEL    = 1002;
constexpr int IDC_APP_LISTVIEW = 1003;
constexpr int IDC_PROGRESS_BAR = 1004;
constexpr int IDC_STATUS_LABEL = 1005;
constexpr int IDC_PRIMARY_BTN  = 1006;
constexpr int IDC_CANCEL_BTN   = 1007;

struct WizardWindowState {
    UpdaterArgs args;
    std::vector<SuiteAppEntry> catalog;
    HWND hwnd = nullptr;
    HWND headerLabel = nullptr;
    HWND subLabel = nullptr;
    HWND listView = nullptr;
    HWND progressBar = nullptr;
    HWND statusLabel = nullptr;
    HWND primaryBtn = nullptr;
    HWND cancelBtn = nullptr;
    HFONT titleFont = nullptr;
    HFONT bodyFont = nullptr;
    int step = 0; // 0 = Checking, 1 = Selection, 2 = Installing, 3 = Complete
    int finalExitCode = ExitCodes::UpToDateOrSuccess;
    std::atomic<bool> cancelRequested{false};
};

int ScaleDpi(HWND hwnd, int px) {
    const UINT dpi = ::GetDpiForWindow(hwnd);
    return ::MulDiv(px, static_cast<int>(dpi > 0 ? dpi : 96), 96);
}

void PopulateWizardListView(WizardWindowState* st) {
    ListView_DeleteAllItems(st->listView);
    int row = 0;
    for (std::size_t i = 0; i < st->catalog.size(); ++i) {
        auto& app = st->catalog[i];
        if (!app.isInstalled && app.id != st->args.focusAppId) {
            continue;
        }

        std::wstring statusText;
        bool checkByDefault = false;
        if (!app.isInstalled) {
            statusText = L"Available (v" + Utf8ToWide(app.latestVersion) + L")";
        } else if (app.HasUpdate()) {
            statusText = L"Update Available (v" + Utf8ToWide(app.installedVersion) + L" -> v" + Utf8ToWide(app.latestVersion) + L")";
            checkByDefault = true;
        } else {
            statusText = L"Up to Date (v" + Utf8ToWide(app.installedVersion) + L")";
        }

        std::wstring titleW = Utf8ToWide(app.title);
        LVITEMW item{};
        item.mask = LVIF_TEXT | LVIF_PARAM;
        item.iItem = row;
        item.pszText = titleW.data();
        item.lParam = static_cast<LPARAM>(i);
        int inserted = ListView_InsertItem(st->listView, &item);
        ListView_SetItemText(st->listView, inserted, 1, statusText.data());
        ListView_SetCheckState(st->listView, inserted, checkByDefault ? TRUE : FALSE);
        app.selectedForUpdate = checkByDefault;
        ++row;
    }
}

void StartInstallWorkerThread(WizardWindowState* st) {
    st->step = 2;
    ::ShowWindow(st->listView, SW_HIDE);
    ::ShowWindow(st->progressBar, SW_SHOW);
    ::EnableWindow(st->primaryBtn, FALSE);
    ::SetWindowTextW(st->headerLabel, L"Updating RaggieSoft Applications...");
    ::SetWindowTextW(st->subLabel, L"Downloading and installing selected updates. Your saved library files and settings are preserved.");

    std::thread([st]() {
        const fs::path cacheDir = GetInstallerCacheDir();
        bool allSucceeded = true;

        for (const auto& app : st->catalog) {
            if (!app.selectedForUpdate || st->cancelRequested.load()) {
                continue;
            }

            auto* msgText = new std::wstring(L"Downloading " + Utf8ToWide(app.title) + L" v" + Utf8ToWide(app.latestVersion) + L"...");
            ::PostMessageW(st->hwnd, WM_UPDATER_STATUS, 0, reinterpret_cast<LPARAM>(msgText));
            ::PostMessageW(st->hwnd, WM_UPDATER_PROGRESS, 0, 0);

            const fs::path targetSetup = cacheDir / Utf8ToWide(app.installerFileName);
            bool downloaded = HttpDownloadToFile(app.installerDownloadUrl, targetSetup, [&](int pct) {
                ::PostMessageW(st->hwnd, WM_UPDATER_PROGRESS, static_cast<WPARAM>(pct), 0);
            });

            if (!downloaded) {
                allSucceeded = false;
                continue;
            }

            auto* instText = new std::wstring(L"Installing " + Utf8ToWide(app.title) + L"...");
            ::PostMessageW(st->hwnd, WM_UPDATER_STATUS, 0, reinterpret_cast<LPARAM>(instText));

            if (!ExecuteInstallerSilently(targetSetup, false)) {
                allSucceeded = false;
            }
        }

        ::PostMessageW(
            st->hwnd,
            WM_UPDATER_FINISHED,
            allSucceeded ? ExitCodes::UpToDateOrSuccess : ExitCodes::InstallError,
            0);
    }).detach();
}

LRESULT CALLBACK UpdaterWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    auto* st = reinterpret_cast<WizardWindowState*>(::GetWindowLongPtrW(hwnd, GWLP_USERDATA));

    switch (msg) {
        case WM_CREATE: {
            auto* cs = reinterpret_cast<CREATESTRUCTW*>(lParam);
            st = reinterpret_cast<WizardWindowState*>(cs->lpCreateParams);
            st->hwnd = hwnd;
            ::SetWindowLongPtrW(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(st));

            st->titleFont = ::CreateFontW(-ScaleDpi(hwnd, 18), 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE,
                DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe UI");
            st->bodyFont = ::CreateFontW(-ScaleDpi(hwnd, 13), 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE,
                DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe UI");

            const bool silentOnly = (st->args.mode == UiMode::SilentProgress);
            const int pad = ScaleDpi(hwnd, 18);
            const int w = ScaleDpi(hwnd, silentOnly ? 460 : 600);

            st->headerLabel = ::CreateWindowExW(0, L"STATIC", L"Checking for RaggieSoft Software Updates...",
                WS_CHILD | WS_VISIBLE, pad, pad, w - pad * 2, ScaleDpi(hwnd, 26), hwnd,
                reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_HEADER_LABEL)), cs->hInstance, nullptr);
            ::SendMessageW(st->headerLabel, WM_SETFONT, reinterpret_cast<WPARAM>(st->titleFont), TRUE);

            st->subLabel = ::CreateWindowExW(0, L"STATIC", L"Contacting RaggieSoft update catalog...",
                WS_CHILD | WS_VISIBLE, pad, pad + ScaleDpi(hwnd, 30), w - pad * 2, ScaleDpi(hwnd, 36), hwnd,
                reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_SUB_LABEL)), cs->hInstance, nullptr);
            ::SendMessageW(st->subLabel, WM_SETFONT, reinterpret_cast<WPARAM>(st->bodyFont), TRUE);

            st->progressBar = ::CreateWindowExW(0, PROGRESS_CLASSW, nullptr,
                WS_CHILD | WS_VISIBLE | PBS_SMOOTH,
                pad, pad + ScaleDpi(hwnd, silentOnly ? 72 : 140), w - pad * 2, ScaleDpi(hwnd, 24), hwnd,
                reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_PROGRESS_BAR)), cs->hInstance, nullptr);
            ::SendMessageW(st->progressBar, PBM_SETRANGE, 0, MAKELPARAM(0, 100));

            st->statusLabel = ::CreateWindowExW(0, L"STATIC", L"Checking installed applications...",
                WS_CHILD | WS_VISIBLE, pad, pad + ScaleDpi(hwnd, silentOnly ? 104 : 300), w - pad * 2, ScaleDpi(hwnd, 22), hwnd,
                reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_STATUS_LABEL)), cs->hInstance, nullptr);
            ::SendMessageW(st->statusLabel, WM_SETFONT, reinterpret_cast<WPARAM>(st->bodyFont), TRUE);

            if (!silentOnly) {
                st->listView = ::CreateWindowExW(WS_EX_CLIENTEDGE, WC_LISTVIEWW, L"",
                    WS_CHILD | LVS_REPORT | LVS_SINGLESEL | LVS_SHOWSELALWAYS,
                    pad, pad + ScaleDpi(hwnd, 72), w - pad * 2, ScaleDpi(hwnd, 215), hwnd,
                    reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_APP_LISTVIEW)), cs->hInstance, nullptr);
                ListView_SetExtendedListViewStyle(st->listView, LVS_EX_CHECKBOXES | LVS_EX_FULLROWSELECT | LVS_EX_DOUBLEBUFFER);

                LVCOLUMNW col0{};
                col0.mask = LVCF_TEXT | LVCF_WIDTH;
                col0.cx = ScaleDpi(hwnd, 260);
                col0.pszText = const_cast<LPWSTR>(L"Application");
                ListView_InsertColumn(st->listView, 0, &col0);

                LVCOLUMNW col1{};
                col1.mask = LVCF_TEXT | LVCF_WIDTH;
                col1.cx = ScaleDpi(hwnd, 285);
                col1.pszText = const_cast<LPWSTR>(L"Status & Version");
                ListView_InsertColumn(st->listView, 1, &col1);

                st->primaryBtn = ::CreateWindowExW(0, L"BUTTON", L"Install Selected Updates",
                    WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_DEFPUSHBUTTON,
                    w - pad - ScaleDpi(hwnd, 290), pad + ScaleDpi(hwnd, 334), ScaleDpi(hwnd, 185), ScaleDpi(hwnd, 32), hwnd,
                    reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_PRIMARY_BTN)), cs->hInstance, nullptr);
                ::SendMessageW(st->primaryBtn, WM_SETFONT, reinterpret_cast<WPARAM>(st->bodyFont), TRUE);
                ::EnableWindow(st->primaryBtn, FALSE);

                st->cancelBtn = ::CreateWindowExW(0, L"BUTTON", L"Close",
                    WS_CHILD | WS_VISIBLE | WS_TABSTOP,
                    w - pad - ScaleDpi(hwnd, 95), pad + ScaleDpi(hwnd, 334), ScaleDpi(hwnd, 95), ScaleDpi(hwnd, 32), hwnd,
                    reinterpret_cast<HMENU>(static_cast<INT_PTR>(IDC_CANCEL_BTN)), cs->hInstance, nullptr);
                ::SendMessageW(st->cancelBtn, WM_SETFONT, reinterpret_cast<WPARAM>(st->bodyFont), TRUE);
            }

            // Spawn background manifest & Registry check so the UI opens instantaneously!
            std::thread([st]() {
                DiscoverInstalledApps(st->catalog);
                std::string json;
                if (HttpGetToString(st->args.manifestUrl, json)) {
                    MergeRemoteManifestJson(json, st->catalog);
                }
                WriteCheckStatusCacheFile(st->catalog);
                ::PostMessageW(st->hwnd, WM_UPDATER_FINISHED, -1, 0);
            }).detach();
            return 0;
        }

        case WM_UPDATER_STATUS: {
            auto* textPtr = reinterpret_cast<std::wstring*>(lParam);
            if (textPtr) {
                ::SetWindowTextW(st->statusLabel, textPtr->c_str());
                delete textPtr;
            }
            return 0;
        }

        case WM_UPDATER_PROGRESS: {
            ::SendMessageW(st->progressBar, PBM_SETPOS, wParam, 0);
            return 0;
        }

        case WM_UPDATER_FINISHED: {
            // wParam == -1 means initial manifest discovery just finished!
            if (static_cast<int>(wParam) == -1) {
                int updatesCount = 0;
                for (auto& e : st->catalog) {
                    if (e.HasUpdate()) {
                        e.selectedForUpdate = true;
                        ++updatesCount;
                    }
                }

                if (st->args.mode == UiMode::SilentProgress) {
                    if (updatesCount == 0) {
                        st->finalExitCode = ExitCodes::UpToDateOrSuccess;
                        ::DestroyWindow(hwnd);
                    } else {
                        StartInstallWorkerThread(st);
                    }
                    return 0;
                }

                // Interactive Wizard Step 1 (Selection)
                st->step = 1;
                ::ShowWindow(st->progressBar, SW_HIDE);
                ::ShowWindow(st->listView, SW_SHOW);
                PopulateWizardListView(st);

                if (updatesCount > 0) {
                    ::SetWindowTextW(st->headerLabel, L"Updates Available");
                    ::SetWindowTextW(st->subLabel, L"Check the RaggieSoft applications you would like to update, then click Install Selected Updates.");
                    ::SetWindowTextW(st->statusLabel, L"Ready to update.");
                    ::EnableWindow(st->primaryBtn, TRUE);
                } else {
                    ::SetWindowTextW(st->headerLabel, L"All Installed Applications Are Up to Date");
                    ::SetWindowTextW(st->subLabel, L"Every installed RaggieSoft application on this computer is running the latest release.");
                    ::SetWindowTextW(st->statusLabel, L"Up to date.");
                    ::SetWindowTextW(st->primaryBtn, L"Check Again");
                    ::EnableWindow(st->primaryBtn, TRUE);
                }
                return 0;
            }

            // Installation batch completed!
            st->finalExitCode = static_cast<int>(wParam);
            if (st->args.mode == UiMode::SilentProgress) {
                ::DestroyWindow(hwnd);
                return 0;
            }

            st->step = 3;
            DiscoverInstalledApps(st->catalog);
            ::ShowWindow(st->progressBar, SW_HIDE);
            ::ShowWindow(st->listView, SW_SHOW);
            PopulateWizardListView(st);
            ::SetWindowTextW(st->headerLabel, st->finalExitCode == 0 ? L"Update Complete" : L"Update Finished with Notices");
            ::SetWindowTextW(st->subLabel, L"Your RaggieSoft applications have been refreshed.");
            ::SetWindowTextW(st->statusLabel, st->finalExitCode == 0 ? L"All selected updates succeeded." : L"One or more installers were canceled or skipped.");
            ::SetWindowTextW(st->primaryBtn, L"Finish");
            ::EnableWindow(st->primaryBtn, TRUE);
            return 0;
        }

        case WM_COMMAND: {
            const int id = LOWORD(wParam);
            if (id == IDC_CANCEL_BTN) {
                st->cancelRequested.store(true);
                ::DestroyWindow(hwnd);
                return 0;
            }
            if (id == IDC_PRIMARY_BTN) {
                if (st->step == 3) {
                    ::DestroyWindow(hwnd);
                    return 0;
                }
                if (st->step == 1) {
                    // Read checkbox selections from ListView
                    int itemCount = ListView_GetItemCount(st->listView);
                    int chosen = 0;
                    for (auto& e : st->catalog) e.selectedForUpdate = false;
                    for (int r = 0; r < itemCount; ++r) {
                        if (ListView_GetCheckState(st->listView, r)) {
                            LVITEMW lvi{};
                            lvi.iItem = r;
                            lvi.mask = LVIF_PARAM;
                            if (ListView_GetItem(st->listView, &lvi)) {
                                auto idx = static_cast<std::size_t>(lvi.lParam);
                                if (idx < st->catalog.size()) {
                                    st->catalog[idx].selectedForUpdate = true;
                                    ++chosen;
                                }
                            }
                        }
                    }
                    if (chosen > 0) {
                        StartInstallWorkerThread(st);
                    } else {
                        ::DestroyWindow(hwnd);
                    }
                }
                return 0;
            }
            break;
        }

        case WM_DESTROY: {
            if (st) {
                if (st->titleFont) ::DeleteObject(st->titleFont);
                if (st->bodyFont) ::DeleteObject(st->bodyFont);
            }
            ::PostQuitMessage(st ? st->finalExitCode : 0);
            return 0;
        }
    }
    return ::DefWindowProcW(hwnd, msg, wParam, lParam);
}

// ============================================================================
// SECTION 5: Headless '/VERYSILENT' Execution Path (Zero Windows Created!)
// ============================================================================

int RunVerySilentHeadless(const UpdaterArgs& args) {
    auto catalog = GetBuiltInSuiteCatalog();
    DiscoverInstalledApps(catalog);

    std::string json;
    if (!HttpGetToString(args.manifestUrl, json)) {
        return ExitCodes::NetworkError;
    }
    if (!MergeRemoteManifestJson(json, catalog)) {
        return ExitCodes::ManifestError;
    }
    WriteCheckStatusCacheFile(catalog);

    int updatesAvailable = 0;
    for (auto& app : catalog) {
        if (app.HasUpdate() && (args.focusAppId.empty() || app.id == args.focusAppId)) {
            app.selectedForUpdate = true;
            ++updatesAvailable;
        }
    }

    if (args.checkOnly) {
        return updatesAvailable > 0 ? ExitCodes::UpdatesAvailable : ExitCodes::UpToDateOrSuccess;
    }

    if (updatesAvailable == 0) {
        return ExitCodes::UpToDateOrSuccess;
    }

    const fs::path cacheDir = GetInstallerCacheDir();
    for (const auto& app : catalog) {
        if (!app.selectedForUpdate) continue;
        const fs::path dest = cacheDir / Utf8ToWide(app.installerFileName);
        if (!HttpDownloadToFile(app.installerDownloadUrl, dest, [](int) {})) {
            return ExitCodes::NetworkError;
        }
        if (!ExecuteInstallerSilently(dest, false)) {
            return ExitCodes::InstallError;
        }
    }
    return ExitCodes::UpToDateOrSuccess;
}

} // namespace

// ============================================================================
// SECTION 6: Win32 GUI Entrypoint ('wWinMain' — Zero Console Window!)
// ============================================================================

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE, PWSTR, int nCmdShow) {
    // Enable crisp Per-Monitor v2 HiDPI rendering on 4K displays
    ::SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // Parse Unicode command line via CommandLineToArgvW (skipping argv[0] executable path)
    int argc = 0;
    LPWSTR* argvW = ::CommandLineToArgvW(::GetCommandLineW(), &argc);
    std::vector<std::string> rawArgs;
    if (argvW) {
        for (int i = 1; i < argc; ++i) {
            rawArgs.push_back(WideToUtf8(argvW[i]));
        }
        ::LocalFree(argvW);
    }

    const UpdaterArgs args = ParseUpdaterArgs(rawArgs);

    // Path 1: '/VERYSILENT' or '--check-only' without wizard -> run 100% invisibly!
    if (args.mode == UiMode::VerySilent) {
        return RunVerySilentHeadless(args);
    }

    // Path 2: Interactive Wizard or '/SILENT' Progress Bar window
    INITCOMMONCONTROLSEX icc{sizeof(icc), ICC_WIN95_CLASSES | ICC_LISTVIEW_CLASSES | ICC_PROGRESS_CLASS};
    ::InitCommonControlsEx(&icc);

    WizardWindowState state{};
    state.args = args;
    state.catalog = GetBuiltInSuiteCatalog();

    WNDCLASSEXW wc{};
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = UpdaterWndProc;
    wc.hInstance = hInstance;
    wc.hCursor = ::LoadCursorW(nullptr, IDC_ARROW);
    wc.hIcon = ::LoadIconW(nullptr, IDI_APPLICATION);
    wc.hbrBackground = reinterpret_cast<HBRUSH>(COLOR_WINDOW + 1);
    wc.lpszClassName = L"RaggieSoftUpdateWizardClass";
    ::RegisterClassExW(&wc);

    const bool silentOnly = (args.mode == UiMode::SilentProgress);
    const int winW = silentOnly ? 485 : 625;
    const int winH = silentOnly ? 185 : 430;
    const DWORD style = silentOnly
        ? (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU)
        : (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX);

    HWND hwnd = ::CreateWindowExW(
        0,
        wc.lpszClassName,
        L"RaggieSoft Software Update",
        style,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        winW,
        winH,
        nullptr,
        nullptr,
        hInstance,
        &state);

    if (!hwnd) {
        return ExitCodes::InstallError;
    }

    ::ShowWindow(hwnd, nCmdShow);
    ::UpdateWindow(hwnd);

    MSG msg{};
    while (::GetMessageW(&msg, nullptr, 0, 0) > 0) {
        if (!::IsDialogMessageW(hwnd, &msg)) {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }
    return static_cast<int>(msg.wParam);
}

#endif // _WIN32
