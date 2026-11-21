Option Strict Off
Option Explicit On
Module Registry
	
	Structure SECURITY_ATTRIBUTES
		Dim nLength As Integer
		Dim lpSecurityDescriptor As Integer
		Dim bInheritHandle As Integer
	End Structure
	
	'UPGRADE_WARNING: Structure SECURITY_ATTRIBUTES may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1050"'
	Public Declare Function RegCreateKeyEx Lib "advapi32.dll"  Alias "RegCreateKeyExA"(ByVal hKey As Integer, ByVal lpSubKey As String, ByVal Reserved As Integer, ByVal lpClass As String, ByVal dwOptions As Integer, ByVal samDesired As Integer, ByRef lpSecurityAttributes As SECURITY_ATTRIBUTES, ByRef phkResult As Integer, ByRef lpdwDisposition As Integer) As Integer
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="vbup1016"'
	Public Declare Function RegSetValueEx Lib "advapi32.dll"  Alias "RegSetValueExA"(ByVal hKey As Integer, ByVal lpszValueName As String, ByVal dwRes As Integer, ByVal dwType As Integer, ByRef lpDataBuff As Any, ByVal nSize As Integer) As Integer
	
	
	Private Declare Function RegCloseKey Lib "advapi32.dll" (ByVal hKey As Integer) As Integer
	Private Declare Function RegOpenKeyExA Lib "advapi32.dll" (ByVal hKey As Integer, ByVal lpszSubKey As String, ByRef dwOptions As Integer, ByVal samDesired As Integer, ByRef lpHKey As Integer) As Integer
	Private Declare Function RegQueryValueExA Lib "advapi32.dll" (ByVal hKey As Integer, ByVal lpszValueName As String, ByVal lpdwRes As Integer, ByRef lpdwType As Integer, ByVal lpDataBuff As String, ByRef nSize As Integer) As Integer
	Private Declare Function RegQueryValueEx Lib "advapi32.dll"  Alias "RegQueryValueExA"(ByVal hKey As Integer, ByVal lpszValueName As String, ByVal lpdwRes As Integer, ByRef lpdwType As Integer, ByRef lpDataBuff As Integer, ByRef nSize As Integer) As Integer
	
	Public Const HKEY_CLASSES_ROOT As Integer = &H80000000
	Public Const HKEY_CURRENT_USER As Integer = &H80000001
	Public Const HKEY_LOCAL_MACHINE As Integer = &H80000002
	Public Const HKEY_USERS As Integer = &H80000003
	
	Public Const ERROR_SUCCESS As Short = 0
	Public Const REG_SZ As Short = 1 ' Unicode nul terminated string
	Public Const REG_DWORD As Short = 4 ' 32-bit number
	
	Public Const KEY_QUERY_VALUE As Integer = &H1
	Public Const KEY_SET_VALUE As Integer = &H2
	Public Const KEY_CREATE_SUB_KEY As Integer = &H4
	Public Const KEY_ENUMERATE_SUB_KEYS As Integer = &H8
	Public Const KEY_NOTIFY As Integer = &H10
	Public Const KEY_CREATE_LINK As Integer = &H20
	Public Const READ_CONTROL As Integer = &H20000
	Public Const WRITE_DAC As Integer = &H40000
	Public Const WRITE_OWNER As Integer = &H80000
	Public Const SYNCHRONIZE As Integer = &H100000
	Public Const STANDARD_RIGHTS_REQUIRED As Integer = &HF0000
	Public Const STANDARD_RIGHTS_READ As Integer = READ_CONTROL
	Public Const STANDARD_RIGHTS_WRITE As Integer = READ_CONTROL
	Public Const STANDARD_RIGHTS_EXECUTE As Integer = READ_CONTROL
	Public Const KEY_READ As Boolean = STANDARD_RIGHTS_READ Or KEY_QUERY_VALUE Or KEY_ENUMERATE_SUB_KEYS Or KEY_NOTIFY
	Public Const KEY_WRITE As Boolean = STANDARD_RIGHTS_WRITE Or KEY_SET_VALUE Or KEY_CREATE_SUB_KEY
	Public Const KEY_EXECUTE As Boolean = KEY_READ
	
	Public Function RegGetValue(ByRef MainKey As Integer, ByRef SubKey As String, ByRef value As String) As String
		' MainKey must be one of the Publicly declared HKEY constants.
		Dim sKeyType As Integer 'to return the key type.  This function expects REG_SZ or REG_DWORD
		Dim ret As Integer 'returned by registry functions, should be 0&
		Dim lpHKey As Integer 'return handle to opened key
		Dim lpcbData As Integer 'length of data in returned string
		Dim ReturnedString As String 'returned string value
		Dim ReturnedLong As Integer 'returned long value
		If MainKey >= &H80000000 And MainKey <= &H80000006 Then
			' Open key
			ret = RegOpenKeyExA(MainKey, SubKey, 0, KEY_READ, lpHKey)
			If ret <> ERROR_SUCCESS Then
				RegGetValue = ""
				Exit Function 'No key open, so leave
			End If
			
			' Set up buffer for data to be returned in.
			' Adjust next value for larger buffers.
			lpcbData = 255
			ReturnedString = Space(lpcbData)
			
			' Read key
			ret = RegQueryValueExA(lpHKey, value, 0, sKeyType, ReturnedString, lpcbData)
			If ret <> ERROR_SUCCESS Then
				RegGetValue = "" 'Value probably doesn't exist
			Else
				If sKeyType = REG_DWORD Then
					ret = RegQueryValueEx(lpHKey, value, 0, sKeyType, ReturnedLong, 4)
					If ret = ERROR_SUCCESS Then RegGetValue = CStr(ReturnedLong)
				Else
					RegGetValue = Left(ReturnedString, lpcbData - 1)
				End If
			End If
			' Always close opened keys.
			ret = RegCloseKey(lpHKey)
		End If
	End Function
	
	Public Sub RegSetValue(ByRef H_KEY As Integer, ByRef RSubKey As String, ByRef ValueName As String, ByRef RegValue As String)
		'H_KEY must be one of the Key Constants
		Dim lRtn As Integer 'returned by registry functions, should be 0&
		Dim hKey As Integer 'return handle to opened key
		Dim lpDisp As Integer
		Dim Sec_Att As SECURITY_ATTRIBUTES
		Sec_Att.nLength = 12
		Sec_Att.lpSecurityDescriptor = 0
		Sec_Att.bInheritHandle = False
		If RegValue = "" Then RegValue = " "
		
		lRtn = RegCreateKeyEx(H_KEY, RSubKey, 0, "", 0, KEY_WRITE, Sec_Att, hKey, lpDisp)
		If lRtn <> 0 Then
			Exit Sub 'No key open, so leave
		End If
		lRtn = RegSetValueEx(hKey, ValueName, 0, REG_SZ, RegValue, CInt(Len(RegValue) + 1))
		lRtn = RegCloseKey(hKey)
	End Sub
End Module