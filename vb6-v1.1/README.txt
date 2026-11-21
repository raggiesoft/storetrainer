RAGGIESOFT STORETRAINER (CLASSIC RETRO EDITION v1.1)
----------------------------------------------------

StoreTrainer is a lightweight utility for storing and launching PC games alongside their companion game trainers and command-line switches.

* For Windows 10 & Windows 11 (x64 & ARM64): Use RaggieSoft StoreTrainer 2.0 (available standalone or through RaggieSoft Launcher).
* For Vintage & Retro Gaming PCs (Windows 95, 98, ME, NT 4.0 SP3+, 2000 SP4, XP, Vista, 7, 8.1): Use this Classic Visual Basic 6.0 Edition (v1.1), which also integrates cleanly with RaggieSoft Launcher on modern PCs!

COMMAND-LINE USAGE & EXPLORER INTEGRATION
-----------------------------------------
  TStore.exe /l "C:\Path\To\Game.stf"   -> Launches the game & trainer immediately and exits
  TStore.exe /e "C:\Path\To\Game.stf"   -> Opens the StoreFile inside the StoreTrainer Editor
  Double-clicking any .stf file         -> Launches the game & trainer automatically

VERSION HISTORY (CLASSIC VB6 LINE)
----------------------------------
Version 1.1.0
 * Completed CheckCommandLine() supporting /l (silent launch), /e (edit), and Explorer .stf double-click
 * Fixed working directory resolution (ChDir ParentFolder) preventing VB6 Error 76 on launch
 * Preserved multi-flag command-line switches (-windowed -novid) without wrapping extra quotes
 * Made "Trainer Launches Game" and "Launch Game First" mutually exclusive
 * Added configurable Launch Delay (0-30 seconds) and File -> Recent StoreFiles menu
 * Added named Win32 Mutex (RaggieSoft_StoreTrainer_Mutex) & RaggieSoft Launcher Registry integration
 * Explicit ANSI NSIS installer mode (Unicode false) preserving Windows 95 compatibility

Version 1.0.0 (2018-2020, Pre-AI Release)
 * Initial release authored in Visual Basic 6.0 inside a Windows 2000 Professional SP4 VM
