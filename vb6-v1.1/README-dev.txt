RAGGIESOFT STORETRAINER v1.1 — DEVELOPER & RETRO VM BUILD NOTES
---------------------------------------------------------------

1. COMPILING TSTORE.EXE (VISUAL BASIC 6.0 SP6):
   - Open `vb6-v1.1\TStore.vbp` inside Visual Basic 6.0 SP6 (tested in Windows 2000 Professional SP4 VM).
   - Click `File -> Make TStore.exe...` and output to `vb6-v1.1\Build\TStore.exe` alongside `TStore.exe.manifest`.
   - Future 1.1.x maintenance releases: increment `RevisionVer` in `TStore.vbp` (e.g., `1.1.1`, `1.1.2`).

2. COMPILING THE ANSI NSIS INSTALLER & IN-PLACE PATCHER (WINDOWS 95 COMPATIBLE):
   - Both `Installer\TSSetup.nsi` (full setup -> `tssetup110.exe`) and `Installer\TSSetup-Patch.nsi`
     (lightweight patcher -> `tspatch110.exe`, upgrading v1.0.x -> v1.1.0 and leaving room for future
     `1.1.x` updates via `/DPATCH_OUTFILE=tspatch111.exe`) explicitly set `Unicode false` so NSIS 3.x
     generates genuine x86 ANSI stubs that boot on Windows 95, Windows 98, and Windows 2000 SP4!
   - Note on `Microsoft Redist\`: Place your local VB6 SP6 / DCOM98 / WSH 5.6 redistributables into
     `vb6-v1.1\Installer\Microsoft Redist\` before compiling `TSSetup.nsi` (`TSSetup-Patch.nsi` does
     not require `Microsoft Redist\`).

3. RAGGIESOFT LAUNCHER & STORETRAINER 2.0 UPGRADE INTEROPERABILITY:
   - `TSSetup.nsi` and `TSSetup-Patch.nsi` write `DisplayName`, `DisplayVersion`, `Publisher`,
     `InstallLocation`, `DisplayIcon`, and `UninstallString` to
     `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\RaggieSoft StoreTrainer`.
   - On modern 64-bit Windows, RaggieSoft Launcher automatically detects classic VB6 v1.0/v1.1 under
     `WOW6432Node`, lets you launch `TStore.exe`, and offers a seamless one-click upgrade to
     StoreTrainer 2.0 (`StoreTrainer.iss`), which surgically removes the legacy NSIS files while
     leaving `SysWOW64` VB6 runtimes and `My Documents\My StoreFiles\*.stf` 100% untouched!
