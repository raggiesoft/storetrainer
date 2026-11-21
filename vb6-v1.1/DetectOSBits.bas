Attribute VB_NAME = "DetectOSBits"
Option Explicit

' ==============================================================================
' FILE: DetectOSBits.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Why probe 'IsWow64Process' dynamically via 'GetProcAddress' instead of
'   calling it directly?
'     On Windows 95, 98, ME, NT 4.0, and Windows 2000, the 'IsWow64Process'
'     export did not exist inside 'kernel32.dll' yet! Probing for it at runtime
'     with 'GetProcAddress' lets 32-bit 'TStore.exe' detect 64-bit Windows when
'     available without crashing on vintage 1990s operating systems.
' ==============================================================================

Private Declare Function GetProcAddress Lib "kernel32" _
    (ByVal hModule As Long, ByVal lpProcName As String) As Long

Private Declare Function GetModuleHandle Lib "kernel32" _
    Alias "GetModuleHandleA" (ByVal lpModuleName As String) As Long

Private Declare Function GetCurrentProcess Lib "kernel32" () As Long

Private Declare Function IsWow64Process Lib "kernel32" _
    (ByVal hProc As Long, ByRef bWow64Process As Boolean) As Long

Public Function IsHost64Bit() As Boolean
    Dim handle As Long
    Dim is64Bit As Boolean

    is64Bit = False
    handle = GetProcAddress(GetModuleHandle("kernel32"), "IsWow64Process")
    If handle <> 0 Then
        IsWow64Process GetCurrentProcess(), is64Bit
    End If

    IsHost64Bit = is64Bit
End Function
