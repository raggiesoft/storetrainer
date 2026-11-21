# Pre-AI Historical Archive Provenance (`2018–2022`)

Every file inside [`pre-ai-vb6/`](./pre-ai-vb6/) (and on the frozen Git branch `archive/pre-ai-2018-2022` / annotated tag `v1.0.0-pre-ai`) is preserved **100% byte-for-byte untouched** as an authentic historical record of hand-written desktop software authored by **Michael P. Ragsdale** before modern AI coding assistants existed.

## 1. Visual Basic 6.0 & NSIS Wizard95 (`2018–2020`)
- **Build Environment**: Authored in **Microsoft Visual Basic 6.0 SP6** (`TStore.vbp`) inside a **Windows 2000 Professional SP4** VirtualBox virtual machine and tested across Windows 2000 Professional SP4 and Windows 10 Professional.
- **Runtime Architecture**: Uses direct Win32 API calls (`kernel32.dll`, `advapi32.dll`, `shell32.dll`, `user32.dll`, `comctl32.dll`) including dynamic `GetProcAddress` probing for `IsWow64Process` (`DetectOSBits.bas`) so 32-bit `TStore.exe` avoids missing-export crashes on Windows 95, Windows 98, and Windows 2000.
- **Installer (`Installer/TSSetup.nsi` & `TSSetup-Patch.nsi`)**: Packaged with **Nullsoft Scriptable Install System (NSIS)** and the classic Wizard95 `ISUI.nsh` interface, conditionally deploying DCOM98 and Windows Script Host 5.6 on vintage 1990s systems while protecting modern Windows System File Protection (`InstallLib`).

## 2. True Provenance of the `TStore.NET/` Folder (`May 15, 2022`)
Inside `pre-ai-vb6/TStore.NET/`, you will find an incomplete Visual Basic .NET folder alongside `_UpgradeReport.htm`.
- **What Actually Happened**: As recorded inside `_UpgradeReport.htm`'s timestamp header (`2022-05-15 19:16`), `TStore.vbp` was opened once inside **Visual Studio .NET 2003**, which automatically ran Microsoft's legacy *Visual Basic Upgrade Wizard*.
- **Why It Never Shipped**: The wizard produced **7 compile errors and 13 upgrade warnings** (`UPGRADE_ISSUE` markers on `App.ProductName`, `As Any` Win32 P/Invoke marshaling in `INIFile.vb` and `Registry.vb`, and `SHGetPathFromIDList` PIDL pointer conversions in `SpecialFolder.vb`) and was set aside uncompiled.
- **Modern Successors in This Repository**:
  - **[`../vb6-v1.1/`](../vb6-v1.1/)**: Completes the classic Visual Basic 6.0 + ANSI NSIS (`Unicode false`) codebase for vintage Windows 95–Windows 11 systems and integrates with **RaggieSoft Launcher**.
  - **[`../dotnet-v2.0/`](../dotnet-v2.0/)**: Rebuilds **RaggieSoft StoreTrainer 2.0** cleanly from the ground up in modern **C# & .NET 10 WPF** (`x64` & `ARM64`) with **Inno Setup 7** (`StoreTrainer.iss`), performing a surgical in-place takeover of legacy 32-bit NSIS installations while preserving 100% compatibility with `.stf` StoreFile profiles.
