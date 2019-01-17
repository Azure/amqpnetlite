@ECHO OFF
SETLOCAL EnableExtensions EnableDelayedExpansion

ECHO Build Amqp.Net Lite
ECHO.

SET return-code=0

SET build-sln=amqp.sln
SET build-dotnet2=false
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

IF /I "%1" EQU "package" (
  SET build-target=package
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
  SET build-sln=%1
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
IF /I "%build-sln%" EQU "amqp-netmf.sln" SET build-test=false
IF /I "%build-sln%" EQU "amqp-dotnet.sln" SET build-dotnet2=true

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
ECHO Build dotnet2: %build-dotnet2%
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

CALL :findfile MSTest exe
ECHO MSTest: %MSTestPath%

IF /I "%build-dotnet2%" EQU "false" (
  IF "%NuGetPath%" == "" (
    ECHO NuGet.exe does not exist or is not under PATH.
    CALL :handle-error 2
    GOTO :exit
  )
  IF "%MSBuildPath%" == "" (
    ECHO MSBuild.exe does not exist or is not under PATH.
    ECHO This can be resolved by building from a VS developer command prompt.
    CALL :handle-error 2
    GOTO :exit
  )
) ELSE (
  IF "%dotnetPath%" == "" (
    ECHO .Net Core SDK is not installed. If you unzipped the package, make sure the location is in PATH.
    EXIT /b 1
  )
)

IF /I "%build-target%" == "test" GOTO :build-done
IF /I "%build-target%" == "package" GOTO :build-done

:build-start
TASKKILL /F /IM TestAmqpBroker.exe >nul 2>&1

IF /I "%build-target%" == "clean" GOTO :build-clean
IF /I "%build-target%" == "build" GOTO :build-target
ECHO Unknown build target "%build-target%"
GOTO :args-error

:build-clean
SET return-code=0
CALL :run-build Clean
IF ERRORLEVEL 1 SET return-code=1
GOTO :exit

:build-target
CALL :run-build build
IF ERRORLEVEL 1 (
  SET return-code=1
  GOTO :exit
)

:build-done

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
PING -n 1 -w 2000 1.1.1.1 >nul 2>&1

:run-test
IF /I "%build-dotnet2%" EQU "true" GOTO :run-dotnet2-test

IF "%MSTestPath%" == "" (
  ECHO MSTest.exe does not exist or is not under PATH. Will not run tests.
  GOTO :exit
)

ECHO.
ECHO Running NET tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net\Test.Amqp.Net.dll
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  IF /I "%is-elevated%" == "false" ECHO WebSocket tests may be failing because the broker was started without Administrator permission
  GOTO :exit
)

ECHO.
ECHO Running NET40 tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net40\Test.Amqp.Net40.dll
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

ECHO.
ECHO Running NET35 tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net35\Test.Amqp.Net35.dll
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

ECHO.
ECHO Running DOTNET (.Net Core 1.0) tests...
"%dotnetPath%" bin\Test.Amqp\bin\%build-config%\netcoreapp1.0\Test.Amqp.dll -- no-broker
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO .Net Core Test failed!
  GOTO :exit
)

GOTO :done-test

:run-dotnet2-test
ECHO Running DOTNET (.Net Core 2.0) tests...
"%dotnetPath%" test -c %build-config% --no-build test\Test.Amqp\Test.Amqp.csproj -- no-broker
IF ERRORLEVEL 1 (
  SET return-code=1
  ECHO .Net Core 2. 0 Test failed!
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

rem Build NuGet package
ECHO.
IF "%NuGetPath%" == "" (
  ECHO NuGet.exe does not exist or is not under PATH.
  ECHO If you want to build NuGet package, install NuGet.CommandLine
  ECHO package, or download NuGet.exe and place it under .\Build\tools
  ECHO directory.
) ELSE (
  IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"
  ECHO Building NuGet package with version %build-version%
  IF /I "%build-sln%" NEQ "amqp-nanoFramework.sln" (
    FOR %%G IN (AMQPNetLite AMQPNetLite.NetMF AMQPNetMicro AMQPNetLite.Core AMQPNetLite.Serialization AMQPNetLite.WebSockets) DO (
      "%NuGetPath%" pack .\nuspec\%%G.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
      IF ERRORLEVEL 1 (
        SET return-code=1
        GOTO :exit
      )
    )
  ) ELSE (  
    FOR %%G IN (AMQPNetLite.nanoFramework AMQPNetMicro.nanoFramework) DO (
      "%NuGetPath%" pack .\nuspec\%%G.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
      IF ERRORLEVEL 1 (
        SET return-code=1
        GOTO :exit
      )
    )
  )
)

GOTO :exit

:exit
EXIT /b %return-code%

:usage
  ECHO build.cmd [clean^|release^|test^|package] [options]
  ECHO   clean: clean intermediate files
  ECHO   release: a shortcut for "--config Release --nuget"
  ECHO   test: run tests only from existing build
  ECHO   package: create NuGet packages only from Release build
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
  ECHO Build solution %build-sln%
  IF /I "%build-sln%" EQU "amqp.sln" set build-micro=true
  IF /I "%build-sln%" EQU "amqp-netmf.sln" set build-micro=true
  IF /I "%build-dotnet2%" EQU "false" (
    "%NuGetPath%" restore %build-sln%
    IF ERRORLEVEL 1 EXIT /b 1
    "%MSBuildPath%" %build-sln% /t:%1 /nologo /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
    IF ERRORLEVEL 1 EXIT /b 1
	IF /I "%build-micro%" EQU "true" (
      ECHO Build other versions of the micro NETMF projects
      FOR /L %%I IN (2,1,3) DO (
        "%MSBuildPath%" .\netmf\Amqp.Micro.NetMF.csproj /t:%1 /nologo /p:Configuration=%build-config%;Platform="%build-platform: =%";FrameworkVersionMajor=4;FrameworkVersionMinor=%%I /verbosity:%build-verbosity%
        IF ERRORLEVEL 1 EXIT /b 1
      )
    )
  ) ELSE (
    "%dotnetPath%" %1 -c %build-config% -v %build-verbosity% %build-sln%
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
