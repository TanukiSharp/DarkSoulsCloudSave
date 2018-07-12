@echo off

set SOURCE=Release
set BUILD=Build
set TARGET=%BUILD%\DarkSoulsCloudSave

rmdir /S /Q %BUILD%
mkdir %TARGET%
xcopy /Y %SOURCE%\*.exe %TARGET%\
xcopy /Y %SOURCE%\*.dll %TARGET%\
del %TARGET%\*.vshost.exe

pause
