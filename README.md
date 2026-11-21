# RaggieSoft StoreTrainer — Multi-Generation Desktop Repository (`v1.0` Pre-AI Archive • `v1.1` Retro VB6 • `v2.0` C# / .NET 10)

[![License: MIT](https://img.shields.io/badge/Code_License-MIT-0ea5e9.svg)](./LICENSE)
[![Website](https://img.shields.io/badge/Portal-storetrainer.raggiesoft.com-10b981.svg)](https://storetrainer.raggiesoft.com)

**RaggieSoft StoreTrainer** is a lightweight Windows desktop utility authored by **Michael P. Ragsdale** to organize and launch classic and modern PC games alongside their companion **game trainers** and command-line flags using plain-text `.stf` (**StoreFile**) INI profiles.

This repository preserves the **untouched pre-AI historical codebase (`2018–2022`)** side-by-side with its **Classic Visual Basic 6.0 `v1.1` Retro Edition** (for Windows 95 through Windows 11) and its modern **C# / `.NET 10` WPF `v2.0` Edition** (with surgical NSIS-to-Inno Setup 7 in-place upgrade support and **RaggieSoft Launcher** integration).

---

## Repository Layout Across Three Generations

| Directory / Ref | Generation | Stack & Installer | Target OS Range |
| :--- | :--- | :--- | :--- |
| **[`archive/pre-ai-vb6/`](./archive/pre-ai-vb6/)**<br>*(Tag `v1.0.0-pre-ai` & branch `archive/pre-ai-2018-2022`)* | **StoreTrainer 1.0 (Frozen Pre-AI Record)** | Visual Basic 6.0 (`TStore.vbp`), Win32 APIs, NSIS Wizard95, plus the untouched May 15, 2022 VS .NET 2003 `_UpgradeReport.htm` artifact | Historical Archive (`2018–2022`) |
| **[`vb6-v1.1/`](./vb6-v1.1/)** | **StoreTrainer 1.1 (Classic Retro Edition)** | Visual Basic 6.0 SP6 (`TStore.exe`) + ANSI NSIS (`Unicode false` in `TSSetup.nsi` & `TSSetup-Patch.nsi`) | **Windows 95, 98, ME, NT 4.0 SP3+, 2000 SP4, XP, Vista, 7, 8.1, 10 & 11** |
| **[`dotnet-v2.0/`](./dotnet-v2.0/)** | **StoreTrainer 2.0 (Modern Desktop Edition)** | C# 13 / `.NET 10` WPF (`x64` & `ARM64` native `StoreTrainer.exe`) + **Inno Setup 7.1.1** (`StoreTrainer.iss`) | **Windows 10 & Windows 11 (`x64` & `ARM64`)** |

---

## Architectural Highlights

### 1. Frozen Pre-AI Provenance (`archive/pre-ai-vb6/` & `v1.0.0-pre-ai`)
- Built by hand in **Visual Basic 6.0** inside a **Windows 2000 Professional SP4** VirtualBox VM (`2018–2020`).
- Includes [`archive/ARCHIVE-PROVENANCE.md`](./archive/ARCHIVE-PROVENANCE.md) documenting both the original VB6 modules (`DetectOSBits.bas` dynamic `GetProcAddress` probing for `IsWow64Process` so `TStore.exe` runs on Windows 95 without missing kernel exports) and the exact history of `TStore.NET/_UpgradeReport.htm` (generated automatically on `2022-05-15 19:16` when `TStore.vbp` was opened once in Visual Studio .NET 2003).

### 2. StoreTrainer v1.1 — Completing Classic VB6 + ANSI NSIS (`vb6-v1.1/`)
- **Completed Command-Line & Explorer Integration**: Implements `CheckCommandLine()` in `TStore.bas` supporting `/l "<file>.stf"` (silent game + trainer launch & exit), `/e "<file>.stf"` (open in editor), and Windows Explorer `.stf` double-click associations.
- **Classic Win32 Bug Fixes & QoL**:
  - Resolves parent working directories via `FSO.GetParentFolderName(...)` before `ChDir` (preventing VB6 Run-time error `76`).
  - Preserves multi-flag switches (`-windowed -novid`) without wrapping extra quotes around arguments.
  - Enforces mutual exclusivity between *Trainer Launches Game* and *Launch Game First*, adds configurable **Launch Delay (`0–30s`)**, and adds **`File -> Recent`** MRU tracking in `TStore.ini`.
- **Windows 95 ANSI NSIS + In-Place Patching + Launcher Integration**:
  - Explicitly sets `Unicode false` in `TSSetup.nsi` (`tssetup110.exe`) and `TSSetup-Patch.nsi` (`tspatch110.exe`) so NSIS 3 outputs genuine x86 ANSI installers compatible with Windows 95, patches `v1.0` -> `v1.1` cleanly (leaving room for future `v1.1.x` updates), and writes full `Uninstall` Registry metadata discovered by **RaggieSoft Launcher**.

### 3. StoreTrainer 2.0 — Modern C# / `.NET 10` WPF & Surgical NSIS-to-Inno Setup Takeover (`dotnet-v2.0/`)
- **100% Backward-Compatible `.stf` Library**: Reads and writes the exact same `Documents\My StoreFiles\*.stf` INI format (`STF.Version=1.1`) and `%APPDATA%\RaggieSoft\StoreTrainer\TStore.ini` settings, while adding:
  - **Live Searchable Library Sidebar** with Category filters, ★ Favorites pinning, Duplicate Profile, and Drag-and-Drop (`.stf` & `.exe`).
  - **Smart Launch Sequencer** (`Process.WaitForInputIdle` window-readiness detection + `0–30s` launch delay + optional custom working directories).
  - **Trusted Trainer File Verification** (SHA-256 snapshot warning before launch if a trainer `.exe` changes unexpectedly on disk) and **Personal Hotkey Notes**.
  - **Windows 11 Fluent `ThemeMode`** (`System` / `Dark` / `Light`) and full `UIAutomation` screen-reader accessibility.
- **Surgical NSIS -> Inno Setup 7 Migration (`StoreTrainer.iss`)**:
  - Detects legacy 32-bit NSIS installations in `HKLM32\Software\RaggieSoft\StoreTrainer` (`C:\Program Files (x86)\RaggieSoft\StoreTrainer`), closes running instances gracefully via the shared Win32 Mutex (`RaggieSoft_StoreTrainer_Mutex`), and **surgically removes only StoreTrainer's own legacy x86 files, Start Menu `.lnk`, and NSIS Uninstall Registry key**—leaving `C:\Windows\SysWOW64\` VB6 runtime DLLs/OCXs and user `My StoreFiles` 100% untouched so Windows *Installed Apps* displays **one** clean entry owned by Inno Setup's `unins000.exe`.

---

## Licensing & Fictitious Entities Notice

- **Source Code License**: Released under the **[MIT License](./LICENSE)**.
- **Documentation & Visual Assets**: Licensed under **[CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)**.
- **Fictitious Persons & Entities Disclaimer**: While **RaggieSoft StoreTrainer** is a real-world desktop utility authored by **Michael P. Ragsdale**, all narrative characters, bands, biographies, universities (including *Commonwealth Polytechnic Institute / CPI*, *Virginia Dominion University / VDU*, *The Forgers*, and *The Crucible*), corporations (including *OmniCorp*, *Apex Records*, *Prairie Coachworks*, and *Sainte-Claire*), transit agencies (*Blue Ridge Transit*, *CVTA*), in-universe legal filings, and any entity with **LLC**, **Trust**, or **Holdings** in its name across the wider RaggieSoft creative network are works of fiction. Any resemblance to actual persons (living or dead), businesses, institutions, or events is purely coincidental.
