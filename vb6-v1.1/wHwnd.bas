Attribute VB_NAME = "wHwnd"
Option Explicit

' ==============================================================================
' FILE: wHwnd.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Exposes 'GetForegroundWindow' and 'GetActiveWindow' from 'user32.dll' so
'   'SpecialFolder.bas' can pass a valid owner window handle (HWND) to Shell32.
' ==============================================================================

Declare Function GetForegroundWindow Lib "user32.dll" () As Long
Declare Function GetActiveWindow Lib "user32.dll" () As Long
