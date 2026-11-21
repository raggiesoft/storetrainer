Option Strict Off
Option Explicit On
Module OSVersionInfo
	' NOTE: in order for calls to GetVersionEx to function properly,
	' you must include a manifest file with correct supportedOS entries
	'
	' Otherwise, calls will always return 6.2 at most on VER_PLATFORM_WIN32_NT
	
	'UPGRADE_WARNING: Structure OSVERSIONINFO may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
	Private Declare Function GetVersionEx Lib "kernel32"  Alias "GetVersionExA"(ByRef lpVersionInformation As OSVERSIONINFO) As Integer
	
	Private Structure OSVERSIONINFO
		Dim OSVSize As Integer
		Dim dwVerMajor As Integer
		Dim dwVerMinor As Integer
		Dim dwBuildNumber As Integer
		Dim PlatformID As Integer
		<VBFixedString(128),System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr,SizeConst:=128)> Public szCSDVersion As String
	End Structure
	
	Public Enum osArch
		VER_PLATFORM_WIN32s = 0 'Windows 3.x with Win32s Library
		VER_PLATFORM_WIN32_WINDOWS = 1 'Windows 9x
		VER_PLATFORM_WIN32_NT = 2 'Windows NT
	End Enum
	' Let's provide a public way to access Version Information
	Public Structure osInfo
		Dim osArchitecture As osArch
		Dim osVersionMajor As Integer
		Dim osVersionMinor As Integer
		Dim osVersionBuild As Integer
		Dim osServicePack As Integer
	End Structure
	
	Public Function GetWinVer() As osInfo
		Dim osv As OSVERSIONINFO
		osv.OSVSize = Len(osv)
		If GetVersionEx(osv) = 1 Then
			GetWinVer.osVersionMajor = osv.dwVerMajor
			GetWinVer.osVersionMinor = osv.dwVerMinor
			GetWinVer.osVersionBuild = osv.dwBuildNumber
			GetWinVer.osServicePack = -1 'Trap for no service pack (Windows 9x?)
			'UPGRADE_WARNING: Untranslated statement in GetWinVer. Please check source code.
			
		End If
		
	End Function
	
	' Returns the version of Windows that the user is running
	Public Function GetWindowsVersion() As String
		Dim osv As OSVERSIONINFO
		osv.OSVSize = Len(osv)
		
		If GetVersionEx(osv) = 1 Then
			'UPGRADE_WARNING: Untranslated statement in GetWindowsVersion. Please check source code.
		Else
			GetWindowsVersion = "Unable to identify your version of Windows."
		End If
	End Function
End Module