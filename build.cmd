@ECHO OFF
SETLOCAL EnableExtensions EnableDelayedExpansion

ECHO Build Amqp.Net Lite
ECHO.

SET BUILD_SCRIPT=1
SET return-code=0

SET build-sln=amqp.sln
SET build-target=build
SET build-config=Debug
SET build-platform=Any CPU
SET build-verbosity=minimal
SET build-test=true
SET build-nuget=false
SET build-version=

IF /I "%1" EQU "release" (
  set build-target=build
  set build-config=Release
  set build-nuget=true
  SHIFT
)

IF /I "%1" EQU "clean" (
  set build-target=clean
  SHIFT
)

IF /I "%1" EQU "test" (
  set build-target=test
  SHIFT
)

IF /I "%1" EQU "pack" (
  SET build-target=pack
  set build-config=Release
  set build-test=false
  set build-nuget=true
  SHIFT
)

:args-start
IF /I "%1" EQU "" GOTO args-done

IF /I "%1" EQU "--solution" GOTO args-solution
IF /I "%1" EQU "--skiptest" SET build-test=false&&GOTO args-loop
IF /I "%1" EQU "--nuget" SET build-nuget=true&&GOTO args-loop
IF /I "%1" EQU "--config" GOTO args-config
IF /I "%1" EQU "--platform" GOTO args-platform
IF /I "%1" EQU "--verbosity" GOTO args-verbosity
SET return-code=1
GOTO :args-error

:args-solution
  SHIFT
  SET build-sln=%~1
  GOTO args-loop
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

:args-error
CALL :handle-error 1
GOTO :exit

:args-done

IF /I "%build-sln%" EQU "amqp-nanoFramework.sln" SET build-test=false
IF /I "%build-sln%" EQU "amqp-vs2015.sln" SET build-test=false

FOR /F "tokens=1-3* delims=() " %%A in (.\src\Properties\Version.cs) do (
  IF "%%B" == "AssemblyInformationalVersion" SET build-version=%%C
)
IF "%build-version%" == "" (
  ECHO Cannot find version from Version.cs.
  SET return-code=2
  GOTO :exit
)

ECHO Build solution: %build-sln%
ECHO Build target: %build-target%
ECHO Build version: %build-version%
ECHO Build configuration: %build-config%
ECHO Build platform: %build-platform%
ECHO Build run tests: %build-test%
ECHO Build NuGet package: %build-nuget%
ECHO.

IF /I "%build-config%" EQU "" GOTO :args-error
IF /I "%build-platform%" EQU "" GOTO :args-error
IF /I "%build-verbosity%" EQU "" GOTO :args-error

CALL :findfile NuGet exe
ECHO NuGet: "%NuGetPath%"

CALL :findfile MSBuild exe
ECHO MSBuild: "%MSBuildPath%"

CALL :findfile dotnet exe
ECHO dotnet: "%dotnetPath%"

CALL :findfile vstest.console exe
ECHO vstest.console: %vstest.consolePath%

IF /I "%build-target%" == "test" GOTO :build-done
IF /I "%build-target%" == "pack" GOTO :build-done

:build-start
TASKKILL /F /IM TestAmqpBroker.exe >nul 2>&1

IF /I "%build-target%" == "clean" GOTO :build-target
IF /I "%build-target%" == "build" GOTO :build-target
ECHO Unknown build target "%build-target%"
GOTO :args-error

:build-target
SET return-code=0
FOR %%G in (%build-sln%) DO (
  CALL :run-build %build-target% %%G
  IF ERRORLEVEL 1 (
    SET return-code=1
    GOTO :exit
  )
)

:build-done

IF /I "%build-target%" == "clean" GOTO :exit

IF /I "%build-test%" EQU "false" GOTO :nuget-package

TASKLIST /NH /FI "IMAGENAME eq TestAmqpBroker.exe" | FINDSTR TestAmqpBroker.exe 1>nul 2>nul
IF NOT ERRORLEVEL 1 (
  ECHO TestAmqpBroker is already running.
  GOTO :run-test
)

SET TestBrokerPath=.\bin\%build-config%\TestAmqpBroker\TestAmqpBroker.exe
ECHO Starting the test AMQP broker
ECHO %TestBrokerPath% amqp://localhost:5672 amqps://localhost:5671 ws://localhost:18080 /creds:guest:guest /cert:localhost
START CMD.exe /C %TestBrokerPath% amqp://localhost:5672 amqps://localhost:5671 ws://localhost:18080 /creds:guest:guest /cert:localhost
rem Delay to allow broker to start up
timeout 3

:run-test
IF "%vstest.consolePath%" == "" (
  ECHO vstest.console.exe does not exist or is not under PATH. Will not run tests.
  SET return-code=1
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

ECHO.
ECHO Running NET tests...
"%vstest.consolePath%" .\bin\%build-config%\Test.Amqp.NetFX\Test.Amqp.Net.dll
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  IF /I "%is-elevated%" == "false" ECHO WebSocket tests may be failing because the broker was started without Administrator permission
  GOTO :exit
)

ECHO.
ECHO Running NET40 tests...
"%vstest.consolePath%" .\bin\%build-config%\Test.Amqp.NetFX40\Test.Amqp.Net40.dll
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

ECHO.
ECHO Running NET35 tests...
"%vstest.consolePath%" .\bin\%build-config%\Test.Amqp.NetFX35\Test.Amqp.Net35.dll
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

IF NOT "%build-sln:amqp.sln=%" == "%build-sln%" GOTO :run-dotnet-test
GOTO :done-test

:run-dotnet-test
ECHO Running DOTNET (netcoreapp 2.1) tests...
"%dotnetPath%" test -c %build-config% --no-build test\Test.Amqp.NetCoreApp\Test.Amqp.NetCoreApp.csproj -- no-broker
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO dotnet Test failed!
  GOTO :exit
)

:done-test
TASKKILL /F /IM TestAmqpBroker.exe

:nuget-package
IF /I "%build-nuget%" EQU "false" GOTO :exit

IF "%NuGetPath%" == "" (
  ECHO NuGet.exe does not exist or is not under PATH.
  SET return-code=1
  GOTO :exit
)

IF /I "%build-config%" NEQ "Release" (
  ECHO Not building release. Skipping NuGet package.
  GOTO :exit
)

IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"
ECHO Building NuGet package with version %build-version%
IF NOT "%build-sln:amqp.sln=%" == "%build-sln%" GOTO :package-main
GOTO :package-netmf
:package-main
"%NuGetPath%" pack .\nuspec\AMQPNetLite.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
IF ERRORLEVEL 1 (
  SET return-code=1
  GOTO :exit
)
:package-dotnet
FOR %%G IN (AMQPNetLite.Core AMQPNetLite.Serialization AMQPNetLite.WebSockets) DO (
  "%NuGetPath%" pack .\nuspec\%%G.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages" -Symbols -SymbolPackageFormat snupkg
  IF ERRORLEVEL 1 (
    SET return-code=1
    GOTO :exit
  )
)
:package-netmf
IF NOT "%build-sln:amqp-vs2015.sln=%" == "%build-sln%" (
  FOR %%G IN (AMQPNetLite.NetMF AMQPNetMicro) DO (
    "%NuGetPath%" pack .\nuspec\%%G.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
    IF ERRORLEVEL 1 (
      SET return-code=1
      GOTO :exit
    )
  )
)
IF NOT "%build-sln:amqp-nanoFramework.sln=%" == "%build-sln%" (
  FOR %%G IN (AMQPNetLite.nanoFramework AMQPNetMicro.nanoFramework) DO (
    "%NuGetPath%" pack .\nuspec\%%G.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
    IF ERRORLEVEL 1 (
      SET return-code=1
      GOTO :exit
    )
  )
)

GOTO :exit

:exit
EXIT /b %return-code%

:usage
  ECHO build.cmd [clean^|release^|test^|pack] [options]
  ECHO   clean: clean intermediate files
  ECHO   release: a shortcut for "--config Release --nuget"
  ECHO   test: run tests only from existing build
  ECHO   pack: create NuGet packages only from Release build
  ECHO options:
  ECHO  --solution ^<value^>    [amqp.sln]   solution to build
  ECHO  --config ^<value^>      [Debug]   build configuration (e.g. Debug, Release)
  ECHO  --platform ^<value^>    [Any CPU] build platform (e.g. Win32, x64, ...)
  ECHO  --verbosity ^<value^>   [minimal] build verbosity (q[uiet], m[inimal], n[ormal], d[etailed] and diag[nostic])
  ECHO  --skiptest            [false]   skip test
  ECHO  --nuget               [false]   create NuGet packet (for Release only)
  GOTO :eof 

:handle-error
  CALL :usage
  SET return-code=%1
  GOTO :eof

:run-build
  ECHO Build solution %2
  "%NuGetPath%" restore %2
  IF ERRORLEVEL 1 EXIT /b 1
  IF /I "%2" EQU "amqp.sln" (
    "%MSBuildPath%" %2 /t:%1 /nologo /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
    IF ERRORLEVEL 1 EXIT /b 1
  )
  IF /I "%2" EQU "amqp-nanoFramework.sln" (
    "%MSBuildPath%" %2 /t:%1 /nologo /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity% /p:NoWarn=1591
    IF ERRORLEVEL 1 EXIT /b 1
  )
  IF /I "%2" EQU "amqp-vs2015.sln" (
    call build2015.cmd %1 %build-config% "%build-platform%" %build-verbosity%
    IF ERRORLEVEL 1 EXIT /b 1
  )

  EXIT /b 0

:findfile
  IF EXIST ".\Build\tools\%1.%2" (
    SET %1Path=.\Build\tools\%1.%2
  ) ELSE (
    FOR %%f IN (%1.%2) DO IF EXIST "%%~$PATH:f" SET %1Path=%%~$PATH:f
  )
  GOTO :eof
