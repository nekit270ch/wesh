@echo off

set "csc64=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "csc86=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "out=bin\wesh"
set "script="
set "b64=1"
set "b86=1"

:argloop
	if "%~1"=="/c" set "csc=%~2" & shift /1
	if "%~1"=="/o" set "out=%~dpn2" & shift /1
	if "%~1"=="/s" set "script=%~2" & shift /1
	if "%~1"=="/only64" set "b86=0"
	if "%~1"=="/only86" set "b64=0"
	shift /1
if not "%~1"=="" goto :argloop

if not "%script%"=="" if not exist "%script%" (
	echo.%script%: not found
	exit /b 1
)

set "cstr=/nologo /nowarn:0618 /debug- /optimize+ /r:Microsoft.JScript.dll,System.IO.Compression.FileSystem.dll /platform:[platform] /out:"%out%[out_ext].exe""
if not "%script%"=="" set "cstr=%cstr% /res:"%script%",RsScript"
set "cstr=%cstr% src\*.cs"

if "%b64%"=="1" call :build64
if "%b86%"=="1" call :build86

exit /b

:build64
	set "cs64=%cstr:[platform]=x64%"
	set "cs64=%cs64:[out_ext]=%"
	"%csc64%" %cs64%
goto :eof

:build86
	set "cs86=%cstr:[platform]=x86%"
	set "cs86=%cs86:[out_ext]=_x86%"
	"%csc86%" %cs86%
goto :eof
