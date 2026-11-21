Attribute VB_Name = "VersionCompare"
Option Explicit

' ==============================================================================
' FILE: VersionCompare.bas
' PROJECT: RaggieSoft StoreTrainer v1.1 (Classic Visual Basic 6.0 Edition)
' LICENSE: MIT License
'
' PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
'   Compares dotted version strings ('1.0' vs '1.1', or PE binary versions
'   '1.0.0.0' vs '1.1.0.0') segment-by-segment as integers so '1.10' properly
'   compares as newer than '1.2' instead of falling into alphabetical string traps!
' ==============================================================================

Public Function CompareFileVersion(sFileName1 As String, sFileName2 As String) As Integer
    Dim fs As FileSystemObject
    Set fs = CreateObject("Scripting.FileSystemObject")
    CompareFileVersion = CheckVersion(fs.GetFileVersion(sFileName1), fs.GetFileVersion(sFileName2))
End Function

Public Function CheckVersion(ByVal par1 As String, ByVal par2 As String) As Integer
    Dim arr1() As String
    Dim arr2() As String
    Dim looper As Integer
    Dim returnValue As Integer
    Dim val1 As Long
    Dim val2 As Long

    arr1 = Split(par1, ".")
    arr2 = Split(par2, ".")

    For looper = 0 To Max(UBound(arr1), UBound(arr2))
        val1 = 0
        val2 = 0
        If UBound(arr1) >= looper Then val1 = CLng(Val(arr1(looper)))
        If UBound(arr2) >= looper Then val2 = CLng(Val(arr2(looper)))

        If val1 > val2 Then
            returnValue = 1
            Exit For
        ElseIf val1 < val2 Then
            returnValue = -1
            Exit For
        End If
    Next looper

    CheckVersion = returnValue
End Function

Public Function Max(ByVal par1 As Integer, ByVal par2 As Integer) As Integer
    If par1 > par2 Then
        Max = par1
    Else
        Max = par2
    End If
End Function
