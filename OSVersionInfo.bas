Attribute VB_Name = "OSVersionInfo"
Option Explicit
' NOTE: in order for calls to GetVersionEx to function properly,
' you must include a manifest file with correct supportedOS entries
'
' Otherwise, calls will always return 6.2 at most on VER_PLATFORM_WIN32_NT

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
    VER_PLATFORM_WIN32s = 0 'Windows 3.x with Win32s Library
    VER_PLATFORM_WIN32_WINDOWS = 1 'Windows 9x
    VER_PLATFORM_WIN32_NT = 2 'Windows NT
End Enum
' Let's provide a public way to access Version Information
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
        GetWinVer.osServicePack = -1 'Trap for no service pack (Windows 9x?)
        Select Case osv.PlatformID
            Case VER_PLATFORM_WIN32s
                GetWinVer.osArchitecture = VER_PLATFORM_WIN32s
            Case VER_PLATFORM_WIN32_NT
                GetWinVer.osArchitecture = VER_PLATFORM_WIN32_NT
                
                ' Service Pack
                
                
                
                ' It's a pain in the rear to strip the number from the string, but here goes
                ' We're going from a String() - a Character Array - to a String to a Long
                Dim pos As Integer
                pos = InStr(osv.szCSDVersion, Chr$(0)) 'Get the string "Service Pack #" as an Array
                If pos Then
                    On Error GoTo NoSP 'Handle not having a Service Pack
                    Dim sSPName As String
                    sSPName = Left$(osv.szCSDVersion, pos - 1) 'Convert from a String() to a String
                    sSPName = Right$(sSPName, 1) 'Strip the Service Pack number from the string
                    Dim lSPNum As Long
                    lSPNum = CLng(sSPName) 'Convert the service pack number from a String to a Long
                    GetWinVer.osServicePack = lSPNum 'Offer it to the user
                    GoTo FinishNT 'We're done, if a Service Pack is installed
NoSP:
                    GetWinVer.osServicePack = 0 'No Service Pack installed (or we couldn't parse the Service Pack information)
                End If
FinishNT:
            Case VER_PLATFORM_WIN32_WINDOWS:
                GetWinVer.osArchitecture = VER_PLATFORM_WIN32_WINDOWS
        End Select

    End If

End Function

' Returns the version of Windows that the user is running
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
                    Case 3
                        GetWindowsVersion = "Windows NT 3.5"
                    Case 4
                        GetWindowsVersion = "Windows NT 4.0"
                    Case 5
                        Select Case osv.dwVerMinor
                            Case 0
                                GetWindowsVersion = "Windows 2000"
                            Case 1
                                GetWindowsVersion = "Windows XP"
                            Case 2
                                GetWindowsVersion = "Windows Server 2003"
                        End Select
                    Case 6
                        Select Case osv.dwVerMinor
                            Case 0
                                GetWindowsVersion = "Windows Vista/Server 2008"
                            Case 1
                                GetWindowsVersion = "Windows 7/Server 2008 R2"
                            Case 2
                                GetWindowsVersion = "Windows 8/Server 2012"
                            Case 3
                                GetWindowsVersion = "Windows 8.1/Server 2012 R2"
                        End Select
                    Case 10
                        GetWindowsVersion = "Windows 10"
                End Select

            Case VER_PLATFORM_WIN32_WINDOWS:
                Select Case osv.dwVerMinor
                    Case 0
                        GetWindowsVersion = "Windows 95"
                    Case 90
                        GetWindowsVersion = "Windows Me"
                    Case Else
                        GetWindowsVersion = "Windows 98"
                End Select
        End Select
    Else
        GetWindowsVersion = "Unable to identify your version of Windows."
    End If
End Function
