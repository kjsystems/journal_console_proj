set tgtdir=C:\src\journal_console_proj\Release
set appname=journal08.exe

signtool.exe sign /f C:\Dropbox\doc\コードサイニング-COMODO\20200910\kjsystems-sign.pfx /p tama0609 %tgtdir%\%appname%
signtool.exe verify /pa /v %tgtdir%\%appname%

