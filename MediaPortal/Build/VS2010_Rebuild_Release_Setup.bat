@echo off

set ProgramDir=%ProgramFiles%
if exist "%ProgramFiles(x86)%" set ProgramDir=%ProgramFiles(x86)%

"%ProgramDir%\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com" /rebuild Release ..\Tools\MergeMSI\MergeMSI.sln

"%ProgramDir%\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com" ..\Setup\MP2-Setup.sln /Rebuild "Release|x86" /Out build_setup.log


..\Tools\MergeMSI\bin\Release\MergeMSI.exe --TargetDir=..\Setup\MP2-Setup\bin\Release\ --TargetFileName=MP2-Setup.msi >> build_setup.log