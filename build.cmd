@ECHO OFF
SETLOCAL EnableExtensions EnableDelayedExpansion

ECHO Build Amqp.Net Lite
ECHO.

SET return-code=0

CALL :file-exists MSBuild exe
IF "%MSBuildPath%" == "" (
  ECHO MSBuild.exe does not exist or is not under PATH.
  ECHO This can be resolved by building from a VS developer command prompt.
  CALL :handle-error 1
  GOTO :exit
)

SET build-target=build
SET build-config=Debug
SET build-platform=Any CPU
SET build-verbosity=minimal
SET build-dotnet=true
SET build-test=true
SET build-nuget=false
SET build-version=

IF /I "%1" EQU "release" (
  set build-target=build
  set build-config=Release
  set build-dotnet=true
  set build-nuget=true
  GOTO :args-done
)

IF /I "%1" EQU "clean" (
  set build-target=clean
  GOTO :args-done
)

IF /I "%1" EQU "test" (
  set build-target=test
  GOTO :args-done
)

:args-start
IF /I "%1" EQU "" GOTO args-done

IF /I "%1" EQU "--dotnet" SET build-dotnet=true&&GOTO args-loop
IF /I "%1" EQU "--skiptest" SET build-test=false&&GOTO args-loop
IF /I "%1" EQU "--nuget" SET build-nuget=true&&GOTO args-loop
IF /I "%1" EQU "--config" GOTO :args-config
IF /I "%1" EQU "--platform" GOTO :args-platform
IF /I "%1" EQU "--verbosity" GOTO args-verbosity
GOTO :args-error

:args-config
  SHIFT
  SET build-config=%1
  GOTO args-loop
:args-platform
  SHIFT
  SET build-platform=%1
  GOTO args-loop
:args-verbosity
  SHIFT
  SET build-verbosity=%1
  GOTO args-loop

:args-loop
SHIFT
GOTO :args-start

:args-done

IF /I "%build-config%" EQU "" GOTO :args-error
IF /I "%build-platform%" EQU "" GOTO :args-error
IF /I "%build-verbosity%" EQU "" GOTO :args-error

ECHO Build configuration: %build-config%
ECHO Build platform: %build-platform%
ECHO Build dotnet: %build-dotnet%
ECHO Run tests: %build-test%
ECHO Build NuGet package: %build-nuget%
ECHO.

IF /I "%build-target%" == "clean" GOTO :build-clean

IF /I "%build-dotnet%" EQU "false" GOTO :build-target
CALL :file-exists dotnet exe
  IF "%dotnetPath%" == "" (
  ECHO .Net Core SDK is not installed. If you unzipped the package, make sure the location is in PATH.
  GOTO :exit
)

:build-target
IF /I "%build-target%" == "build" GOTO :build-sln
IF /I "%build-target%" == "test" GOTO :build-done

:args-error
CALL :handle-error 1
GOTO :exit

:build-clean
"%MSBuildPath%" amqp.sln /t:Clean /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
SET return-code=%ERRORLEVEL%
GOTO :exit

:build-sln
FOR /F "tokens=1-3* delims=() " %%A in (.\src\Properties\Version.cs) do (
  IF "%%B" == "AssemblyInformationalVersion" SET build-version=%%C
)
IF "%build-version%" == "" (
  ECHO Cannot find version from Version.cs.
  SET return-code=2
  GOTO :exit
)

echo Build version %build-version%
"%MSBuildPath%" amqp.sln /t:Rebuild /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  GOTO :exit
)
REM build other versions of the lite NETMF project
FOR /L %%I IN (2,1,3) DO (
  "%MSBuildPath%" .\src\Amqp.Micro.NetMF.csproj /t:Rebuild /p:Configuration=%build-config%;Platform="%build-platform: =%";FrameworkVersionMajor=4;FrameworkVersionMinor=%%I /verbosity:%build-verbosity%
  IF %ERRORLEVEL% NEQ 0 (
    SET return-code=%ERRORLEVEL%
    GOTO :exit
  )
)

IF /I "%build-dotnet%" EQU "false" GOTO :build-done
CALL "%dotnetPath%" restore dotnet
IF %ERRORLEVEL% NEQ 0 (
  ECHO dotnet restore failed with error %ERRORLEVEL%
  SET return-code=%ERRORLEVEL%
  GOTO :exit
)
CALL "%dotnetPath%" build dotnet/Amqp dotnet/Amqp.Listener dotnet/Test.Amqp --configuration %build-config%
IF %ERRORLEVEL% NEQ 0 (
  ECHO dotnet build failed with error %ERRORLEVEL%
  SET return-code=%ERRORLEVEL%
  GOTO :exit
)

:build-done

IF /I "%build-test%" EQU "false" GOTO :nuget-package

CALL :file-exists MSTest exe
IF "%MSTestPath%" == "" (
  ECHO MSTest.exe does not exist or is not under PATH. Will not run tests.
  GOTO :exit
)

TASKLIST /NH /FI "IMAGENAME eq TestAmqpBroker.exe" | FINDSTR TestAmqpBroker.exe 1>nul 2>nul
IF %ERRORLEVEL% EQU 0 (
  ECHO TestAmqpBroker is already running.
  GOTO :run-test
)

SET TestBrokerPath=.\bin\%build-config%\TestAmqpBroker\TestAmqpBroker.exe
ECHO Starting the test AMQP broker
ECHO %TestBrokerPath% amqp://localhost:5672 amqps://localhost:5671 ws://localhost:18080 /creds:guest:guest /cert:localhost
START CMD.exe /C %TestBrokerPath% amqp://localhost:5672 amqps://localhost:5671 ws://localhost:18080 /creds:guest:guest /cert:localhost
rem Delay to allow broker to start up
PING -n 1 -w 2000 1.1.1.1 >nul 2>&1

:run-test
ECHO.
ECHO Running NET tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net\Test.Amqp.Net.dll
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  IF /I "%is-elevated%" == "false" ECHO WebSocket tests may be failing because the broker was started without Administrator permission
  GOTO :exit
)

ECHO.
ECHO Running NET35 tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net35\Test.Amqp.Net35.dll
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  IF /I "%is-elevated%" == "false" ECHO WebSocket tests may be failing because the broker was started without Administrator permission
  GOTO :exit
)

ECHO.
ECHO Running DOTNET (.Net Core 1.0) tests...
IF /I "%build-dotnet%" EQU "false" GOTO done-test
"%dotnetPath%" run --configuration %build-config% --project dotnet\Test.Amqp -- no-broker
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO .Net Core Test failed!
  GOTO :exit
)

:done-test
TASKKILL /F /IM TestAmqpBroker.exe

:nuget-package
IF /I "%build-nuget%" EQU "false" GOTO :exit

IF /I "%build-config%" NEQ "Release" (
  ECHO Not building release. Skipping NuGet package.
  GOTO :exit
)

rem Build NuGet package
ECHO.
CALL :file-exists NuGet exe
IF "%NuGetPath%" == "" (
  ECHO NuGet.exe does not exist or is not under PATH.
  ECHO If you want to build NuGet package, install NuGet.CommandLine
  ECHO package, or download NuGet.exe and place it under .\Build\tools
  ECHO directory.
) ELSE (
  IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"
  ECHO Building NuGet package with version %build-version%
  "%NuGetPath%" pack Amqp.Net.nuspec -Version %build-version% -OutputDirectory ".\Build\Packages"
  "%NuGetPath%" pack Amqp.Micro.nuspec -Version %build-version% -OutputDirectory ".\Build\Packages"
)

GOTO :exit

:exit
ENDLOCAL
EXIT /b !return-code!

:usage
ECHO build.cmd [clean^|release] [options]
ECHO   clean: clean intermediate files
ECHO   release: a shortcut for "--config Release --nuget --dotnet"
ECHO options:
ECHO  --config ^<value^>      [Debug] build configuration (e.g. Debug, Release)
ECHO  --platform ^<value^>    [Any CPU] build platform (e.g. Win32, x64, ...)
ECHO  --dotnet              [true] build dotnet
ECHO  --verbosity ^<value^>   [minimal] build verbosity (q[uiet], m[inimal], n[ormal], d[etailed] and diag[nostic])
ECHO  --skiptest            [false] skip test
ECHO  --nuget               [false] create NuGet packet (for Release only)
GOTO :eof 

:handle-error
CALL :usage
SET return-code=%1
GOTO :eof

:file-exists
IF EXIST ".\Build\tools\%1.%2" (
  SET %1Path=.\Build\tools\%1.%2
) ELSE (
  FOR %%f IN (%1.%2) DO IF EXIST "%%~$PATH:f" SET %1Path=%%~$PATH:f
)
GOTO :eof