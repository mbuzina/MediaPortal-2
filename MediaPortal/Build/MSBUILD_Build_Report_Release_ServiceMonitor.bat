rem @ECHO OFF


"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Tools\BuildReport\BuildReport.sln /target:Rebuild  /property:Configuration=Release


xcopy /Y ..\Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\Tools\BuildReport\images\*.* .\images\

"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" ..\Source\MP2-ServiceMonitor.sln /target:Rebuild /property:Configuration=Release;Platform=x86 > MSBUILD_Build_Report_ServiceMonitor_Release_x86.txt

..\Tools\BuildReport\bin\x86\Release\BuildReport.exe --Input=MSBUILD_Build_Report_ServiceMonitor_Release_x86.txt --Output=MSBUILD_Build_Report_ServiceMonitor_Release_x86.html --Solution=MP2-ServiceMonitor.sln --Title="MediaPortal 2 ServiceMonitor - Build Report"