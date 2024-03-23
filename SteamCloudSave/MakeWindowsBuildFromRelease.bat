@echo off

set SOURCE=bin\Release\net8.0-windows
set BUILD=bin\Build
set TARGET=%BUILD%\DarkSoulsCloudSave

rmdir /S /Q %BUILD%
mkdir %TARGET%
xcopy /Y %SOURCE%\*.exe %TARGET%\
xcopy /Y %SOURCE%\*.dll %TARGET%\
xcopy /Y %SOURCE%\*.runtimeconfig.json %TARGET%\
del %TARGET%\*.vshost.exe

pause
