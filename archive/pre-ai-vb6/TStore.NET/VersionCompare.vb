Option Strict Off
Option Explicit On
Module VersionCompare
	Public Function CompareFileVersion(ByRef sFileName1 As String, ByRef sFileName2 As String) As Short
		
		'// Return values:
		'// -1 = File Version 2 is greater than File Version 1
		'//  0 = Versions are the same
		'//  1 = File version 1 is greater than File Version 2
		Dim iResult As Short
		
		Dim sFileVersion1 As String
		Dim sFileVersion2 As String
		Dim arrFileVersion1() As String
		Dim arrFileVersion2() As String
		Dim lVersion1 As Integer
		Dim lVersion2 As Integer
		Dim fs As IWshRuntimeLibrary.FileSystemObject
		
		'// Let's initialize our result with 0
		iResult = 0
		
		'// Use FileSystemObject to get the file version
		fs = CreateObject("Scripting.FileSystemObject")
		sFileVersion1 = fs.GetFileVersion(sFileName1)
		sFileVersion2 = fs.GetFileVersion(sFileName2)
		
		'// Split the two supplied file versions by the "." character
		arrFileVersion1 = Split(sFileVersion1, ".")
		arrFileVersion2 = Split(sFileVersion2, ".")
		
		Dim i As Short
		For i = 0 To UBound(arrFileVersion1)
			'// Convert the string to a number so we don't get false positive ("2">"19")
			lVersion1 = CInt(arrFileVersion1(i))
			lVersion2 = CInt(arrFileVersion2(i))
			If lVersion1 > lVersion2 Then
				iResult = 1
			ElseIf lVersion1 < lVersion2 Then 
				iResult = -1
			End If
			
			'If we have found that the result is not > or <, no need to proceed
			If iResult <> 0 Then Exit For
			
		Next 
		
		If UBound(arrFileVersion2) > UBound(arrFileVersion1) And CDbl(arrFileVersion2(UBound(arrFileVersion2))) <> 0 Then iResult = -1
		
		CompareFileVersion = iResult
		
	End Function
	
	Public Function CheckVersion(ByVal par1 As String, ByVal par2 As String) As Short
		Dim arr1() As String
		Dim arr2() As String
		Dim looper As Short
		Dim returnValue As Short
		Dim val1 As Short
		Dim val2 As Short
		arr1 = Split(par1, ".")
		arr2 = Split(par2, ".")
		
		For looper = 0 To Max(UBound(arr1), UBound(arr2))
			val1 = 0
			val2 = 0
			If UBound(arr1) >= looper Then
				val1 = Val(arr1(looper))
			End If
			If UBound(arr2) >= looper Then
				val2 = Val(arr2(looper))
			End If
			
			If val1 > val2 Then
				returnValue = 1
				Exit For
			ElseIf val1 < val2 Then 
				returnValue = -1
				Exit For
			Else
				' same #, keep going
			End If
		Next 
		CheckVersion = returnValue
	End Function
	
	Public Function Max(ByVal par1 As Short, ByVal par2 As Short) As Short
		If par1 > par2 Then
			Max = par1
		Else
			Max = par2
		End If
	End Function
End Module