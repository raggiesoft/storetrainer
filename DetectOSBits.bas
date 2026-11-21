Attribute VB_NAME = "DetectOSBits"
Option Explicit

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

    ' Assume initially that this is not a WOW64 process
    is64Bit = False

    ' Then try to prove that wrong by attempting to load the
    ' IsWow64Process function dynamically
    handle = GetProcAddress(GetModuleHandle("kernel32"), "IsWow64Process")

    ' The function exists, so call it
    If handle <> 0 Then
        IsWow64Process GetCurrentProcess(), is64Bit
    End If

    ' Return the value
    IsHost64Bit = is64Bit
End Function