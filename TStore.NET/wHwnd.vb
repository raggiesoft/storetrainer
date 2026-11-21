Option Strict Off
Option Explicit On
Module wHwnd
	Declare Function GetForegroundWindow Lib "user32.dll" () As Integer
	Declare Function GetActiveWindow Lib "user32.dll" () As Integer
End Module