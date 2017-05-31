set TGTDIR=%DROPBOX%\rel\journal
mkdir %TGTDIR%
xcopy .\Release %TGTDIR%\ /I /E /C /G /R /Y /Q
