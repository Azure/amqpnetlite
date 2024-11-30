@ECHO OFF
SETLOCAL EnableExtensions EnableDelayedExpansion

CALL "%VS140COMNTOOLS%VsDevCmd.bat"

ECHO MSBuild paths
where MSBuild

MSBuild.exe amqp-vs2015.sln /t:%1 /nologo /p:Configuration=%2;Platform=%3 /verbosity:%4

ECHO Build other versions of the micro NETMF projects
set build_platform=%3
set build_platform=%build_platform: =%
FOR /L %%I IN (2,1,3) DO (
    MSBuild.exe .\netmf\Amqp.NetMF.csproj /t:%1 /nologo /p:Configuration=%2;Platform=%build_platform%;FrameworkVersionMajor=4;FrameworkVersionMinor=%%I /verbosity:%4
    IF ERRORLEVEL 1 EXIT /b 1
    MSBuild.exe .\netmf\Amqp.Micro.NetMF.csproj /t:%1 /nologo /p:Configuration=%2;Platform=%build_platform%;FrameworkVersionMajor=4;FrameworkVersionMinor=%%I /verbosity:%4
    IF ERRORLEVEL 1 EXIT /b 1
)

EXIT /b 0