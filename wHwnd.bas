Attribute VB_NAME = "wHwnd"
Option Explicit
Declare Function GetForegroundWindow Lib "user32.dll" () As Long
Declare Function GetActiveWindow Lib "user32.dll" () As Long 