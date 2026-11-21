Attribute VB_Name = "OSVersionInfo"
Option Explicit

' ==============================================================================
' FILE: OSVersionInfo.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Calls 'GetVersionExA' in 'kernel32.dll' to identify whether StoreTrainer is
'   running on a DOS-based Windows 9x kernel (Windows 95, 98, ME) or an NT kernel
'   (NT 4.0, 2000, XP, Vista, 7, 8.1, 10, 11).
' ==============================================================================

Private Declare Function GetVersionEx Lib "kernel32" Alias "GetVersionExA" _
    (lpVersionInformation As OSVERSIONINFO) As Long

Private Type OSVERSIONINFO
    OSVSize         As Long
    dwVerMajor      As Long
    dwVerMinor      As Long
    dwBuildNumber   As Long
    PlatformID      As Long
    szCSDVersion    As String * 128
End Type

Public Enum osArch
    VER_PLATFORM_WIN32s = 0
    VER_PLATFORM_WIN32_WINDOWS = 1
    VER_PLATFORM_WIN32_NT = 2
End Enum

Public Type osInfo
    osArchitecture As osArch
    osVersionMajor As Long
    osVersionMinor As Long
    osVersionBuild As Long
    osServicePack As Long
End Type

Public Function GetWinVer() As osInfo
    Dim osv As OSVERSIONINFO
    osv.OSVSize = Len(osv)
    If GetVersionEx(osv) = 1 Then
        GetWinVer.osVersionMajor = osv.dwVerMajor
        GetWinVer.osVersionMinor = osv.dwVerMinor
        GetWinVer.osVersionBuild = osv.dwBuildNumber
        GetWinVer.osServicePack = -1
        Select Case osv.PlatformID
            Case VER_PLATFORM_WIN32s
                GetWinVer.osArchitecture = VER_PLATFORM_WIN32s
            Case VER_PLATFORM_WIN32_NT
                GetWinVer.osArchitecture = VER_PLATFORM_WIN32_NT
                Dim pos As Integer
                pos = InStr(osv.szCSDVersion, Chr$(0))
                If pos > 1 Then
                    On Error GoTo NoSP
                    Dim sSPName As String
                    sSPName = Left$(osv.szCSDVersion, pos - 1)
                    GetWinVer.osServicePack = CLng(Val(Right$(sSPName, 1)))
                    GoTo FinishNT
NoSP:
                    GetWinVer.osServicePack = 0
                End If
FinishNT:
            Case VER_PLATFORM_WIN32_WINDOWS
                GetWinVer.osArchitecture = VER_PLATFORM_WIN32_WINDOWS
        End Select
    End If
End Function

Public Function GetWindowsVersion() As String
    Dim osv As OSVERSIONINFO
    osv.OSVSize = Len(osv)

    If GetVersionEx(osv) = 1 Then
        Select Case osv.PlatformID
            Case VER_PLATFORM_WIN32s
                GetWindowsVersion = "Win32s on Windows 3.1"
            Case VER_PLATFORM_WIN32_NT
                GetWindowsVersion = "Windows NT"
                Select Case osv.dwVerMajor
                    Case 3: GetWindowsVersion = "Windows NT 3.5"
                    Case 4: GetWindowsVersion = "Windows NT 4.0"
                    Case 5
                        Select Case osv.dwVerMinor
                            Case 0: GetWindowsVersion = "Windows 2000"
                            Case 1: GetWindowsVersion = "Windows XP"
                            Case 2: GetWindowsVersion = "Windows Server 2003"
                        End Select
                    Case 6
                        Select Case osv.dwVerMinor
                            Case 0: GetWindowsVersion = "Windows Vista"
                            Case 1: GetWindowsVersion = "Windows 7"
                            Case 2: GetWindowsVersion = "Windows 8"
                            Case 3: GetWindowsVersion = "Windows 8.1"
                        End Select
                    Case 10
                        If osv.dwBuildNumber >= 22000 Then
                            GetWindowsVersion = "Windows 11"
                        Else
                            GetWindowsVersion = "Windows 10"
                        End If
                End Select
            Case VER_PLATFORM_WIN32_WINDOWS
                Select Case osv.dwVerMinor
                    Case 0: GetWindowsVersion = "Windows 95"
                    Case 90: GetWindowsVersion = "Windows Me"
                    Case Else: GetWindowsVersion = "Windows 98"
                End Select
        End Select
    Else
        GetWindowsVersion = "Unable to identify your version of Windows."
    End If
End Function
