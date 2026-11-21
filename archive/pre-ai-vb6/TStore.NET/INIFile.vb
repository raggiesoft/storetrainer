Option Strict Off
Option Explicit On
Module INIFile
	
	'API DECLARATIONS
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1016"'
	Declare Function GetPrivateProfileString Lib "kernel32"  Alias "GetPrivateProfileStringA"(ByVal lpApplicationName As String, ByVal lpKeyName As Any, ByVal lpDefault As String, ByVal lpReturnedString As String, ByVal nSize As Integer, ByVal lpFileName As String) As Integer
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1016"'
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1016"'
	Declare Function WritePrivateProfileString Lib "kernel32"  Alias "WritePrivateProfileStringA"(ByVal lpApplicationName As String, ByVal lpKeyName As Any, ByVal lpString As Any, ByVal lpFileName As String) As Integer
	
	Public Function IniGetValueString(ByRef sINIFile As String, ByRef sSection As String, ByRef sKey As String, ByRef sDefault As String) As String
		Dim sTemp As New VB6.FixedLengthString(256)
		Dim nLength As Short
		sTemp.Value = Space(256)
		nLength = GetPrivateProfileString(sSection, sKey, sDefault, sTemp.Value, 255, sINIFile)
		IniGetValueString = Left(sTemp.Value, nLength)
	End Function
	Public Sub IniWriteValueString(ByRef sINIFile As String, ByRef sSection As String, ByRef sKey As String, ByRef sValue As String)
		Dim n As Short
		Dim sTemp As String
		sTemp = sValue
		'Replace any CR/LF characters with spaces
		For n = 1 To Len(sValue)
			If Mid(sValue, n, 1) = vbCr Or Mid(sValue, n, 1) = vbLf Then Mid(sValue, n) = " "
		Next n
		n = WritePrivateProfileString(sSection, sKey, sTemp, sINIFile)
	End Sub
End Module