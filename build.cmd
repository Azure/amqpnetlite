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
SET build-dnx=false
SET build-test=true
SET build-nuget=false

IF /I "%1" EQU "release" (
  set build-target=build
  set build-config=Release
  set build-dnx=true
  set build-nuget=true
  GOTO :args-done
)

IF /I "%1" EQU "clean" (
  set build-target=clean
  GOTO :args-done
)

:args-start
IF /I "%1" EQU "" GOTO args-done

IF /I "%1" EQU "--dnx" SET build-dnx=true&&GOTO args-loop
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
ECHO Run tests: %build-test%
ECHO Build NuGet package: %build-nuget%
ECHO.

IF /I "%build-target%" == "clean" GOTO :build-clean
IF /I "%build-target%" == "build" GOTO :build-sln

:args-error
CALL :handle-error 1
GOTO :exit

:build-clean
"%MSBuildPath%" amqp.sln /t:Clean /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
SET return-code=%ERRORLEVEL%
GOTO :exit

:build-sln
"%MSBuildPath%" amqp.sln /t:Rebuild /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  GOTO :exit
)

IF /I "%build-dnx%" EQU "false" GOTO :build-done
CALL :file-exists dnu cmd
IF "%dnuPath%" == "" (
  ECHO dnvm is not installed or "dnvm use" is not run to add a runtime.
  GOTO :exit
)
CALL "%dnuPath%" restore dnx\Amqp.DotNet
IF %ERRORLEVEL% NEQ 0 (
  ECHO dnu restore failed with error %ERRORLEVEL%
  SET return-code=%ERRORLEVEL%
  GOTO :exit
)
CALL "%dnuPath%" build dnx\Amqp.DotNet --configuration %build-config% --out bin
IF %ERRORLEVEL% NEQ 0 (
  ECHO dnu build failed with error %ERRORLEVEL%
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
ECHO Starting the test AMQP broker %TestBrokerPath%
START CMD.exe /C %TestBrokerPath% amqp://localhost:5672 amqps://localhost:5671 ws://localhost:18080 /creds:guest:guest /cert:localhost
rem Delay to allow broker to start up
PING -n 1 -w 2000 1.1.1.1 >nul 2>&1
:run-test
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net\Test.Amqp.Net.dll

IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  IF /I "%is-elevated%" == "false" ECHO WebSocket tests may be failing because the broker was started without Administrator permission
  GOTO :exit
)

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
  "%NuGetPath%" pack Amqp.Net.nuspec -OutputDirectory ".\Build\Packages"
)

GOTO :exit

:exit
ENDLOCAL
EXIT /b !return-code!

:usage
ECHO build.cmd [clean^|release] [options]
ECHO   clean: clean intermediate files
ECHO   release: a shortcut for "--config Release --nuget"
ECHO options:
ECHO  --config ^<value^>      [Debug] build configuration (e.g. Debug, Release)
ECHO  --platform ^<value^>    [Any CPU] build platform (e.g. Win32, x64, ...)
ECHO  --dnx                 [false] build dnx
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