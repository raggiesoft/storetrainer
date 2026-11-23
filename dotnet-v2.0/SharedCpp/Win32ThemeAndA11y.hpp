// ================================================================================
// FILE: SharedCpp/Win32ThemeAndA11y.hpp
// PURPOSE: Shared Zero-Dependency Win32 C++20 Dark/Light Mode, High-Contrast Theme,
//          Accessibility Text-Scale, Per-Monitor V2 DPI, Keyboard Focus, and
//          Multi-Resolution Application Icon Engine for Pure C++ GUI Apps!
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL READER):
//   Why doesn't a raw Win32 C++ window automatically switch to Dark Mode like a
//   modern web browser (`prefers-color-scheme: dark`) or WPF / WinUI 3 app?
//     Classic Win32 (`user32.dll` + `comctl32.dll`, designed in the Windows 95/XP era)
//     defaults to bright white `COLOR_WINDOW` backgrounds even when the user enables
//     Dark Mode in Windows 11 Settings -> Personalization -> Colors!
//
//   How does this shared header make pure C++20 Win32 apps feel 100% native,
//   accessible, and theme-aware with ZERO external libraries?
//     1. **Windows Contrast Themes / High Contrast First (`SPI_GETHIGHCONTRAST`)**:
//        Before applying custom Dark or Light colors, we ALWAYS check whether the
//        user has enabled a Windows Accessibility Contrast Theme (*Aquatic*, *Desert*,
//        *Dusk*, *Night Sky*, or classic `Alt + Left Shift + Print Screen`). When
//        High Contrast is active, we strictly use the user's exact OS `GetSysColor(...)`
//        palette so custom colors NEVER override assistive contrast settings!
//     2. **System Dark Mode vs. Light Mode (`AppsUseLightTheme`)**:
//        Reads `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`
//        (`AppsUseLightTheme == 0` -> Dark Mode; `1` -> Light Mode), flips the
//        Windows 11 DWM title bar (`DWMWA_USE_IMMERSIVE_DARK_MODE = 20`), unlocks
//        `uxtheme.dll` dark scrollbars (`DarkMode_Explorer`), and builds WCAG AAA
//        (> 10:1 contrast) GDI brushes for labels, text boxes, list views, and frames—
//        updating LIVE on `WM_SETTINGCHANGE` / `WM_SYSCOLORCHANGE` without restarting!
//     3. **Windows Accessibility Text Size Slider (`TextScaleFactor`) + HiDPI**:
//        Reads `HKCU\Software\Microsoft\Accessibility\TextScaleFactor` (`100%..225%`,
//        configured in Windows Settings -> Accessibility -> Text size) and multiplies
//        it with Per-Monitor V2 DPI (`GetDpiForWindow` + `WM_DPICHANGED`) so users
//        who enlarge system text get proportionately larger, unclipped fonts & rows!
//     4. **Reduced Motion / Animation Preference (`SPI_GETCLIENTAREAANIMATION`)**:
//        Queries Windows Settings -> Accessibility -> Visual effects -> Animation
//        effects so animations can be toned down when turned off.
//     5. **Keyboard Focus Indicators & Screen-Reader Clarity (`UISF_HIDEFOCUS`)**:
//        Clears `UISF_HIDEFOCUS` / `UISF_HIDEACCEL` on window startup so keyboard-only
//        users immediately see crisp dotted/outlined focus rectangles and `Alt` key
//        underlines without having to press `Tab` or `Alt` blindly first!
// ================================================================================

#pragma once

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
#include <dwmapi.h>
#include <uxtheme.h>
#include <algorithm>
#include <vector>

#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "dwmapi.lib")
#pragma comment(lib, "gdi32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "uxtheme.lib")

// Standard Win32 resource ID compiled into every pure C++ `.rc` script for the app icon.
#ifndef IDI_RAGGIESOFT_APP_ICON
#define IDI_RAGGIESOFT_APP_ICON 101
#endif

// Windows 10 20H1+ / Windows 11 DWM Dark Titlebar attribute constant (`20`).
#ifndef DWMWA_USE_IMMERSIVE_DARK_MODE
#define DWMWA_USE_IMMERSIVE_DARK_MODE 20
#endif

namespace raggiesoft::win32a11y {

/// Holds the resolved color palette, GDI background brushes, and accessibility flags
/// for the current OS theme (High Contrast, Dark Mode, or Light Mode).
struct ThemePalette {
    bool isHighContrast   = false;
    bool isDarkMode       = false;
    bool animationsOn     = true;
    int  textScalePercent = 100; // 100..225 from Windows Accessibility -> Text size

    COLORREF windowBg     = RGB(248, 250, 252);
    COLORREF surfaceBg    = RGB(255, 255, 255);
    COLORREF textPrimary  = RGB(15, 23, 42);
    COLORREF textMuted    = RGB(51, 65, 85);
    COLORREF borderSubtle = RGB(203, 213, 225);
    COLORREF accentColor  = RGB(2, 132, 199);

    HBRUSH hbrWindowBg    = nullptr;
    HBRUSH hbrSurfaceBg   = nullptr;

    ~ThemePalette() {
        ReleaseBrushes();
    }

    void ReleaseBrushes() {
        if (hbrWindowBg)  { ::DeleteObject(hbrWindowBg);  hbrWindowBg  = nullptr; }
        if (hbrSurfaceBg) { ::DeleteObject(hbrSurfaceBg); hbrSurfaceBg = nullptr; }
    }

    /// Queries live Windows Accessibility + Personalization settings and rebuilds GDI brushes.
    void RefreshFromSystem() {
        ReleaseBrushes();

        // 1. Check Windows Contrast Themes / High Contrast Mode FIRST (highest priority!)
        HIGHCONTRASTW hc{};
        hc.cbSize = sizeof(hc);
        if (::SystemParametersInfoW(SPI_GETHIGHCONTRAST, sizeof(hc), &hc, 0)) {
            isHighContrast = (hc.dwFlags & HCF_HIGHCONTRASTON) != 0;
        } else {
            isHighContrast = false;
        }

        // 2. Check Windows Animation Effects accessibility toggle
        BOOL anim = TRUE;
        if (::SystemParametersInfoW(SPI_GETCLIENTAREAANIMATION, 0, &anim, 0)) {
            animationsOn = (anim != FALSE);
        } else {
            animationsOn = true;
        }

        // 3. Check Windows Accessibility -> Text size slider (`100` to `225` percent)
        textScalePercent = 100;
        HKEY hA11y = nullptr;
        if (::RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\Microsoft\\Accessibility", 0, KEY_READ, &hA11y) == ERROR_SUCCESS) {
            DWORD val = 100, cb = sizeof(val), type = 0;
            if (::RegQueryValueExW(hA11y, L"TextScaleFactor", nullptr, &type, reinterpret_cast<LPBYTE>(&val), &cb) == ERROR_SUCCESS &&
                type == REG_DWORD) {
                textScalePercent = std::clamp(static_cast<int>(val), 100, 200);
            }
            ::RegCloseKey(hA11y);
        }

        // 4. Resolve semantic colors:
        //    - High Contrast: 100% OS `GetSysColor` tokens (never custom hex!).
        //    - Dark Mode:     WCAG AAA (> 11:1) slate surfaces (`#121620` / `#1B2230`) & crisp white/ice text.
        //    - Light Mode:    WCAG AAA (> 12:1) daylight surfaces (`#F8FAFC` / `#FFFFFF`) & deep navy text.
        if (isHighContrast) {
            windowBg     = ::GetSysColor(COLOR_WINDOW);
            surfaceBg    = ::GetSysColor(COLOR_WINDOW);
            textPrimary  = ::GetSysColor(COLOR_WINDOWTEXT);
            textMuted    = ::GetSysColor(COLOR_WINDOWTEXT);
            borderSubtle = ::GetSysColor(COLOR_WINDOWFRAME);
            accentColor  = ::GetSysColor(COLOR_HIGHLIGHT);
            // Determine whether this High Contrast theme is dark-on-light or light-on-dark for titlebar
            const int lum = (GetRValue(windowBg) * 299 + GetGValue(windowBg) * 587 + GetBValue(windowBg) * 114) / 1000;
            isDarkMode   = (lum < 128);
        } else {
            DWORD appsUseLight = 1;
            HKEY hPers = nullptr;
            if (::RegOpenKeyExW(
                    HKEY_CURRENT_USER,
                    L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
                    0,
                    KEY_READ,
                    &hPers) == ERROR_SUCCESS) {
                DWORD cb = sizeof(appsUseLight), type = 0;
                ::RegQueryValueExW(hPers, L"AppsUseLightTheme", nullptr, &type, reinterpret_cast<LPBYTE>(&appsUseLight), &cb);
                ::RegCloseKey(hPers);
            }
            isDarkMode = (appsUseLight == 0);

            if (isDarkMode) {
                windowBg     = RGB(18, 22, 32);    // #121620 Deep Slate Window Frame
                surfaceBg    = RGB(27, 34, 48);    // #1B2230 Elevated ListView / Log Card
                textPrimary  = RGB(241, 245, 249); // #F1F5F9 Crisp Ice White (15.4:1 contrast)
                textMuted    = RGB(203, 213, 225); // #CBD5E1 Soft Slate (10.8:1 contrast)
                borderSubtle = RGB(51, 65, 85);    // #334155 Subtle Divider
                accentColor  = RGB(56, 189, 248);  // #38BDF8 Sky Cyan Accent
            } else {
                windowBg     = RGB(244, 247, 251); // #F4F7FB Clean Daylight Window Frame
                surfaceBg    = RGB(255, 255, 255); // #FFFFFF Pure Card Surface
                textPrimary  = RGB(15, 23, 42);    // #0F172A Deep Ink Navy (16.1:1 contrast)
                textMuted    = RGB(51, 65, 85);    // #334155 Slate Subtitle (9.3:1 contrast)
                borderSubtle = RGB(203, 213, 225); // #CBD5E1 Crisp Border
                accentColor  = RGB(2, 132, 199);   // #0284C7 Ocean Blue Accent
            }
        }

        hbrWindowBg  = ::CreateSolidBrush(windowBg);
        hbrSurfaceBg = ::CreateSolidBrush(surfaceBg);
    }
};

/// Opts the current Win32 process into `uxtheme.dll`'s Dark Mode scrollbar & control support
/// on Windows 10 (1903+) and Windows 11 using safe dynamic ordinal lookup (`GetProcAddress`).
inline void EnableProcessDarkModeSupport() {
    HMODULE hUx = ::LoadLibraryExW(L"uxtheme.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!hUx) return;

    // Ordinal 135 on Windows 10 1903+ / Windows 11: `SetPreferredAppMode(PreferredAppMode)`
    // where `1` == `AllowDark` (follows OS Light/Dark mode dynamically!).
    using SetPreferredAppModeFn = int(WINAPI*)(int appMode);
    if (auto fn = reinterpret_cast<SetPreferredAppModeFn>(::GetProcAddress(hUx, MAKEINTRESOURCEA(135)))) {
        fn(1 /* AllowDark */);
    }
    using FlushMenuThemesFn = void(WINAPI*)();
    if (auto flushFn = reinterpret_cast<FlushMenuThemesFn>(::GetProcAddress(hUx, MAKEINTRESOURCEA(136)))) {
        flushFn();
    }
}

/// Applies Dark/Light DWM titlebar + `uxtheme.dll` scrollbar/control visual styles to `hwnd`
/// and its child controls (`ListView`, `Edit`, `Button`, `CheckBox`, `ProgressBar`).
inline void ApplyWindowAndControlThemes(
    HWND hwndMain,
    const ThemePalette& theme,
    const std::vector<HWND>& scrollableOrButtonCtrls,
    const std::vector<HWND>& checkBoxCtrls,
    HWND listViewHwnd = nullptr)
{
    HMODULE hUx = ::GetModuleHandleW(L"uxtheme.dll");
    using AllowDarkModeForWindowFn = BOOL(WINAPI*)(HWND, BOOL);
    auto allowDarkWnd = hUx
        ? reinterpret_cast<AllowDarkModeForWindowFn>(::GetProcAddress(hUx, MAKEINTRESOURCEA(133)))
        : nullptr;

    const BOOL darkBool = (!theme.isHighContrast && theme.isDarkMode) ? TRUE : FALSE;
    ::DwmSetWindowAttribute(hwndMain, DWMWA_USE_IMMERSIVE_DARK_MODE, &darkBool, sizeof(darkBool));
    if (allowDarkWnd) {
        allowDarkWnd(hwndMain, darkBool);
    }

    const wchar_t* explorerTheme = (!theme.isHighContrast && theme.isDarkMode)
        ? L"DarkMode_Explorer"
        : L"Explorer";

    for (HWND ctrl : scrollableOrButtonCtrls) {
        if (!ctrl) continue;
        if (allowDarkWnd) allowDarkWnd(ctrl, darkBool);
        ::SetWindowTheme(ctrl, explorerTheme, nullptr);
    }

    // Classic Win32 BS_AUTOCHECKBOX ignores `WM_CTLCOLORSTATIC` text color when `uxtheme` visual
    // styles paint dark black glyph text over a dark window! Disabling visual theme (`L"", L""`) on
    // checkboxes in Dark Mode lets `WM_CTLCOLORSTATIC` / `WM_CTLCOLORBTN` render crisp `#F1F5F9`
    // white checkbox labels while keeping native visual styles in Light Mode!
    for (HWND chk : checkBoxCtrls) {
        if (!chk) continue;
        if (!theme.isHighContrast && theme.isDarkMode) {
            ::SetWindowTheme(chk, L"", L"");
        } else {
            ::SetWindowTheme(chk, L"Explorer", nullptr);
        }
    }

    if (listViewHwnd) {
        if (allowDarkWnd) allowDarkWnd(listViewHwnd, darkBool);
        ::SetWindowTheme(listViewHwnd, explorerTheme, nullptr);
        ListView_SetBkColor(listViewHwnd, theme.surfaceBg);
        ListView_SetTextBkColor(listViewHwnd, theme.surfaceBg);
        ListView_SetTextColor(listViewHwnd, theme.textPrimary);
        if (HWND hdr = ListView_GetHeader(listViewHwnd)) {
            if (allowDarkWnd) allowDarkWnd(hdr, darkBool);
            ::SetWindowTheme(hdr, (!theme.isHighContrast && theme.isDarkMode) ? L"ItemsView" : L"Explorer", nullptr);
        }
    }

    // Ensure keyboard focus rectangles and Alt-mnemonic underlines are ALWAYS visible
    // for keyboard & assistive-technology users right from launch!
    ::SendMessageW(hwndMain, WM_CHANGEUISTATE, MAKEWPARAM(UIS_CLEAR, UISF_HIDEFOCUS | UISF_HIDEACCEL), 0);

    ::RedrawWindow(hwndMain, nullptr, nullptr, RDW_ERASE | RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_FRAME);
}

/// Scales a logical 96-DPI pixel size by monitor DPI, and optionally by Windows'
/// Accessibility *Text size* multiplier (`textScalePercent`, capped sensibly so controls fit).
inline int ScaleLayoutPx(int px, UINT dpi, int textScalePercent = 100) {
    const int effectiveDpi = static_cast<int>(dpi > 0 ? dpi : 96);
    // Grow layout geometry gently (50% of text growth) when TextScaleFactor > 100% so larger fonts never clip!
    const int layoutFactor = 100 + ((std::max(100, textScalePercent) - 100) / 2);
    return ::MulDiv(px, effectiveDpi * layoutFactor, 96 * 100);
}

inline int ScaleFontPx(int px, UINT dpi, int textScalePercent = 100) {
    const int effectiveDpi = static_cast<int>(dpi > 0 ? dpi : 96);
    const int clampedScale = std::clamp(textScalePercent, 100, 175);
    return ::MulDiv(px, effectiveDpi * clampedScale, 96 * 100);
}

/// Loads both the large (`SM_CXICON`, Alt+Tab / Taskbar) and small (`SM_CXSMICON`, Titlebar)
/// multi-resolution icons from resource `IDI_RAGGIESOFT_APP_ICON` at the exact monitor DPI!
inline void ApplyDpiWindowIcons(HWND hwnd, HINSTANCE hInstance, UINT dpi) {
    const int bigW = ::GetSystemMetricsForDpi(SM_CXICON, dpi);
    const int bigH = ::GetSystemMetricsForDpi(SM_CYICON, dpi);
    const int smW  = ::GetSystemMetricsForDpi(SM_CXSMICON, dpi);
    const int smH  = ::GetSystemMetricsForDpi(SM_CYSMICON, dpi);

    HICON hBig = static_cast<HICON>(::LoadImageW(
        hInstance, MAKEINTRESOURCEW(IDI_RAGGIESOFT_APP_ICON), IMAGE_ICON, bigW, bigH, LR_SHARED));
    HICON hSm  = static_cast<HICON>(::LoadImageW(
        hInstance, MAKEINTRESOURCEW(IDI_RAGGIESOFT_APP_ICON), IMAGE_ICON, smW, smH, LR_SHARED));

    if (hBig) ::SendMessageW(hwnd, WM_SETICON, ICON_BIG,   reinterpret_cast<LPARAM>(hBig));
    if (hSm)  ::SendMessageW(hwnd, WM_SETICON, ICON_SMALL, reinterpret_cast<LPARAM>(hSm));
}

} // namespace raggiesoft::win32a11y

#endif // _WIN32
