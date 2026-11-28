# RaggieSoft StoreTrainer — Multi-Generation Desktop Repository (`v1.0` Pre-AI Archive • `v1.1` Retro VB6 • `v2.0` C# / .NET 10 + C++20 Updater)

[![License: MIT](https://img.shields.io/badge/Code_License-MIT-0ea5e9.svg)](./LICENSE)
[![Website](https://img.shields.io/badge/Portal-storetrainer.raggiesoft.com-10b981.svg)](https://storetrainer.raggiesoft.com)

**RaggieSoft StoreTrainer** is a Windows desktop utility authored by **Michael P. Ragsdale** to organize and launch classic and modern PC games alongside their companion **game trainers**, ZIP archives, save-game backups, and shortcut cheat sheets.

This repository preserves the **untouched pre-AI historical codebase (`2018–2022`)** side-by-side with its **Classic Visual Basic 6.0 `v1.1` Retro Edition** (for Windows 95 through Windows 11), its modern **C# / `.NET 10` WPF `v2.0` Edition** (`.stfx` structured JSON profiles in `Documents\StoreFiles\`), and our **Zero-Runtime Native C++20 Win32 Software Update Utility (`RaggieSoftUpdate.exe`)**.

---

## Repository Layout Across Three Generations

| Directory / Ref | Generation | Stack & Installer | Target OS Range |
| :--- | :--- | :--- | :--- |
| **[`archive/pre-ai-vb6/`](./archive/pre-ai-vb6/)**<br>*(Tag `v1.0.0-pre-ai` & branch `archive/pre-ai-2018-2022`)* | **StoreTrainer 1.0 (Frozen Pre-AI Record)** | Visual Basic 6.0 (`TStore.vbp`), Win32 APIs, NSIS Wizard95, plus the untouched May 15, 2022 VS .NET 2003 `_UpgradeReport.htm` artifact | Historical Archive (`2018–2022`) |
| **[`vb6-v1.1/`](./vb6-v1.1/)** | **StoreTrainer 1.1 (Classic Retro Edition)** | Visual Basic 6.0 SP6 (`TStore.exe`) + ANSI NSIS (`Unicode false` in `TSSetup.nsi` & `TSSetup-Patch.nsi`) | **Windows 95, 98, ME, NT 4.0 SP3+, 2000 SP4, XP, Vista, 7, 8.1, 10 & 11** |
| **[`dotnet-v2.0/`](./dotnet-v2.0/)** | **StoreTrainer 2.0 + `RaggieSoftUpdate` (Modern Desktop Edition)** | C# 13 / `.NET 10` WPF (`StoreTrainer.exe`) + Native C++20 `/MT` (`RaggieSoftUpdate.exe`) + **Inno Setup 7.1.1** (`StoreTrainer.iss` + `SharedInno\RaggieSoftWizardCommon.iss`) | **Windows 10 & Windows 11 (`x64` & `ARM64`)** |

---

## Architectural Highlights

### 1. Frozen Pre-AI Provenance (`archive/pre-ai-vb6/` & `v1.0.0-pre-ai`)
- Built by hand in **Visual Basic 6.0** inside a **Windows 2000 Professional SP4** VirtualBox VM (`2018–2020`).
- Includes [`archive/ARCHIVE-PROVENANCE.md`](./archive/ARCHIVE-PROVENANCE.md) documenting both the original VB6 modules (`DetectOSBits.bas` dynamic `GetProcAddress` probing for `IsWow64Process` so `TStore.exe` runs on Windows 95 without missing kernel exports) and the exact history of `TStore.NET/_UpgradeReport.htm` (generated automatically on `2022-05-15 19:16` when `TStore.vbp` was opened once in Visual Studio .NET 2003).

### 2. StoreTrainer v1.1 — Classic VB6 + Word 97 ↔ Word 2000 `.stf` Forward Compatibility (`vb6-v1.1/`)
- **Full `.stf` Forward & Backward Compatibility (`STF.Version=1.0`)**: Like opening a Word 2000 `.doc` inside Word 97, StoreTrainer `v1.1` continues writing `STF.Version=1.0` inside `Documents\My StoreFiles\*.stf` so profiles saved in `v1.1` open cleanly in `v1.0` (which simply ignores `v1.1`'s optional `LaunchDelay` key).
- **Completed Command-Line & Explorer Integration**: Implements `CheckCommandLine()` in `TStore.bas` supporting `/l "<file>.stf"` (silent launch & exit), `/e "<file>.stf"` (open in editor), `File -> Recent` MRU history, parent-folder working directory resolution, and `Help -> Check for Updates...` (which delegates to `RaggieSoftUpdate.exe` on Windows 10/11 and **automatically disables itself** on offline retro Windows 95–8.1 machines!).
- **Windows 95 ANSI NSIS (`TSSetup.nsi` & `TSSetup-Patch.nsi`)**:
  - Compiles in explicit ANSI mode (`Unicode false`) for Windows 95 compatibility, patches `v1.0` -> `v1.1` in place, omits source-code bundling (now hosted here on GitHub), and packages classic Microsoft VB6 SP6 / `DCOM98` / `WSH56` redistributables from `vb6-v1.1/Installer/Microsoft Redist/` (gitignored binary redistributable staging folder) for bare-metal Windows 95 / NT 4.0 systems.

### 3. StoreTrainer 2.0 — `.stfx` JSON, Trainer `.ZIP` UAC Extractor & 6 Modern Features (`dotnet-v2.0/StoreTrainer/`)
- **Modern `.stfx` JSON Format (`Documents\StoreFiles\*.stfx`) + 3-Step StoreFile Upgrade Wizard**:
  - Evolves Win9x `My Documents\My StoreFiles\*.stf` INI files into structured `.stfx` JSON profiles inside `Documents\StoreFiles\` while leaving original `.stf` files **100% untouched**, complete with a guided 3-step **StoreFile Upgrade Wizard** and Upgrade Summary Report.
- **Trainer `.ZIP` Archive Extractor with Windows UAC Elevation (`--elevated-copy`)**:
  - Inspects downloaded `.zip` trainer archives (guarding against Zip-Slip paths) and extracts either into `Documents\StoreFiles\Trainers\<Game>\` or **directly into the game's installation directory**—automatically invoking an elevated copy helper (`Verb = "runas"`) so Windows prompts for UAC administrator permission when a game lives inside `C:\Program Files (x86)\...`!
- **Complete Gamer & Preservationist Feature Suite**:
  1. **Multiple Companion Trainers per Game** + **"▶ Launch with Trainer"** AND **"▶ Launch Game Only (No Trainer)"** + native Windows game `.exe` icon extraction (`ExtractAssociatedIconW`).
  2. **Pre-Launch Save-Game Backup & 1-Click Rollback ("Trainer Insurance")** (rolling `.zip` snapshots before launching with a trainer).
  3. **Auto-Close Companion Trainer on Game Exit** + automatic *Total Playtime* & *Last Played* session tracking.
  4. **Classic IBM PC CP437 `.NFO` / `FILE_ID.DIZ` / `README` Viewer** (authentic MS-DOS box-drawing ASCII art decoder + 1-click shortcut key importer).
  5. **Floating Always-On-Top Hotkey Cheat Card** window for second monitors or borderless play.
  6. **Classic PC CPU Core Affinity Limiter** (`All Cores` / `Single Core` / `Dual Core` `ProcessorAffinity` for 1990s/2000s games) + **1-Click Desktop Game Shortcut (`.lnk`)** + **SHA-256 Trusted Trainer Snapshot**.

### 4. Zero-Runtime Native C++20 Updater (`RaggieSoftUpdate.exe`) & Express/Custom Inno Setup (`SharedInno/`)
- **`RaggieSoftUpdate/` (`C++20` + `/MT` Static CRT -> Zero External Runtime DLLs)**:
  - Standalone Win32 + Common Controls + WinHTTP TLS 1.2/1.3 + Windows CNG (`bcrypt.dll`) SHA-256 update client that discovers every installed RaggieSoft application across `HKLM64`, `HKCU`, and `HKLM32` (including **RaggieSoft Launcher**, Book Reader, The Listening Room, Backstage, StoreTrainer 2.0, and Classic StoreTrainer 1.1) and runs in **Interactive Wizard** (`/WIZARD`), **`/SILENT`** (progress bar only), or **`/VERYSILENT`** (`--check-only` exit code `10` when updates exist) mode.
- **Express vs. Custom Inno Setup 7 Architecture (`SharedInno/RaggieSoftWizardCommon.iss` & `StoreTrainer.iss`)**:
  - Adds **Express vs. Custom** setup modes, custom `.stfx` double-click verb selection (*Launch* `/l` vs. *Edit* `/e*), surgical 32-bit NSIS takeover, and **Windows `Active Setup` multi-user `HKCU\Software\Classes` cleanup** on Admin uninstall so other user accounts on a shared PC never need an external removal tool.

---

## Licensing & Fictitious Entities Notice

- **Source Code License**: Released under the **[MIT License](./LICENSE)**.
- **Documentation & Visual Assets**: Licensed under **[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)**.
- **Fictitious Persons & Entities Disclaimer**: While **RaggieSoft StoreTrainer** is a real-world desktop utility authored by **Michael P. Ragsdale**, all narrative characters, bands, biographies, universities (including *Commonwealth Polytechnic Institute / CPI*, *Virginia Dominion University / VDU*, *The Forgers*, and *The Crucible*), corporations (including *OmniCorp*, *Apex Records*, *Prairie Coachworks*, and *Sainte-Claire*), transit agencies (*Blue Ridge Transit*, *CVTA*), in-universe legal filings, and any entity with **LLC**, **Trust**, or **Holdings** in its name across the wider RaggieSoft creative network are works of fiction. Any resemblance to actual persons (living or dead), businesses, institutions, or events is purely coincidental.
