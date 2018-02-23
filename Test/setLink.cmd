ECHO OFF
for /f "tokens=2,*" %%i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v Personal') do set s=%%j
set s="%s%\Visual Studio 2017\Projects\SE\SE\Class1.cs"
set tc= "%~dp0Script.cs"

ECHO ON
del %s%
mklink %s% %tc%
pause