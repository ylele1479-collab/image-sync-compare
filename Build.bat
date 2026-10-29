@echo off
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
for %%I in ("%CSC%") do set "FRAMEWORK=%%~dpI"
"%CSC%" /nologo /target:winexe /out:ImageCompare.exe /r:System.dll /r:System.Core.dll /r:System.Xaml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:"%FRAMEWORK%WPF\WindowsBase.dll" /r:"%FRAMEWORK%WPF\PresentationCore.dll" src\ImageCompare.cs src\FastPreview.cs src\Sidebar.cs
if errorlevel 1 pause
