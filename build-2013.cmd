@ECHO OFF
SETLOCAL EnableExtensions EnableDelayedExpansion

ECHO Build Amqp.Net Lite
ECHO.

SET return-code=0

CALL :findfile MSBuild exe
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
SET return-code=1
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

:args-error
CALL :handle-error 1
GOTO :exit

:args-done

ECHO Build target: %build-target%
ECHO Build configuration: %build-config%
ECHO Build platform: %build-platform%
ECHO Build dotnet: %build-dotnet%
ECHO Build run tests: %build-test%
ECHO Build NuGet package: %build-nuget%
ECHO.

IF /I "%build-config%" EQU "" GOTO :args-error
IF /I "%build-platform%" EQU "" GOTO :args-error
IF /I "%build-verbosity%" EQU "" GOTO :args-error

IF /I "%build-dotnet%" EQU "false" GOTO :build-start
CALL :findfile dotnet exe
IF "%dotnetPath%" == "" (
  ECHO .Net Core SDK is not installed. If you unzipped the package, make sure the location is in PATH.
  GOTO :exit
)

:build-start
IF /I "%build-target%" == "clean" GOTO :build-clean
IF /I "%build-target%" == "build" GOTO :build-target
IF /I "%build-target%" == "test" GOTO :build-done
GOTO :args-error

TASKKILL /F /IM TestAmqpBroker.exe >nul 2>&1

:build-clean
CALL :run-build Clean
SET return-code=%ERRORLEVEL%
GOTO :exit

:build-target
FOR /F "tokens=1-3* delims=() " %%A in (.\src\Properties\Version.cs) do (
  IF "%%B" == "AssemblyInformationalVersion" SET build-version=%%C
)
IF "%build-version%" == "" (
  ECHO Cannot find version from Version.cs.
  SET return-code=2
  GOTO :exit
)

echo Build version %build-version%
CALL :findfile NuGet exe
IF "%NuGetPath%" == "" (
  ECHO NuGet.exe does not exist or is not under PATH.
) ELSE (
  "%NuGetPath%" restore amqp-vs2013.sln
)

CALL :run-build Rebuild
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  GOTO :exit
)

:build-done

IF /I "%build-test%" EQU "false" GOTO :nuget-package

CALL :findfile MSTest exe
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
ECHO Running NET40 tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net40\Test.Amqp.Net40.dll
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

ECHO.
ECHO Running NET35 tests...
"%MSTestPath%" /testcontainer:.\bin\%build-config%\Test.Amqp.Net35\Test.Amqp.Net35.dll
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO Test failed!
  TASKKILL /F /IM TestAmqpBroker.exe
  GOTO :exit
)

ECHO.
ECHO Running DOTNET (.Net Core 1.0) tests...
IF /I "%build-dotnet%" EQU "false" GOTO done-test
pushd dotnet\Test.Amqp
"%dotnetPath%" run --configuration %build-config% -- no-broker
IF %ERRORLEVEL% NEQ 0 (
  SET return-code=%ERRORLEVEL%
  ECHO .Net Core Test failed!
  GOTO :exit
)
popd

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
IF "%NuGetPath%" == "" (
  ECHO NuGet.exe does not exist or is not under PATH.
  ECHO If you want to build NuGet package, install NuGet.CommandLine
  ECHO package, or download NuGet.exe and place it under .\Build\tools
  ECHO directory.
) ELSE (
  IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"
  ECHO Building NuGet package with version %build-version%
  "%NuGetPath%" pack .\nuspec\AMQPNetLite.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
  IF %ERRORLEVEL% NEQ 0 (
    SET return-code=%ERRORLEVEL%
    GOTO :exit
  )
  "%NuGetPath%" pack .\nuspec\AMQPNetMicro.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
  IF %ERRORLEVEL% NEQ 0 (
    SET return-code=%ERRORLEVEL%
    GOTO :exit
  )
  "%NuGetPath%" pack .\nuspec\AMQPNetLite.Core.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
  IF %ERRORLEVEL% NEQ 0 (
    SET return-code=%ERRORLEVEL%
    GOTO :exit
  )
  "%NuGetPath%" pack .\nuspec\AMQPNetLite.Serialization.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
  IF %ERRORLEVEL% NEQ 0 (
    SET return-code=%ERRORLEVEL%
    GOTO :exit
  )
  "%NuGetPath%" pack .\nuspec\AMQPNetLite.WebSockets.nuspec -Version %build-version% -BasePath .\ -OutputDirectory ".\Build\Packages"
  IF %ERRORLEVEL% NEQ 0 (
    SET return-code=%ERRORLEVEL%
    GOTO :exit
  )
)

GOTO :exit

:exit
EXIT /b %return-code%

:usage
  ECHO build.cmd [clean^|release^|test] [options]
  ECHO   clean: clean intermediate files
  ECHO   release: a shortcut for "--config Release --nuget --dotnet"
  ECHO   test: run tests only from existing build
  ECHO options:
  ECHO  --config ^<value^>      [Debug]   build configuration (e.g. Debug, Release)
  ECHO  --platform ^<value^>    [Any CPU] build platform (e.g. Win32, x64, ...)
  ECHO  --dotnet              [true]    build dotnet
  ECHO  --verbosity ^<value^>   [minimal] build verbosity (q[uiet], m[inimal], n[ormal], d[etailed] and diag[nostic])
  ECHO  --skiptest            [false]   skip test
  ECHO  --nuget               [false]   create NuGet packet (for Release only)
  GOTO :eof 

:handle-error
  CALL :usage
  SET return-code=%1
  GOTO :eof

:run-build
  ECHO Build solution amqp-vs2013.sln
  "%MSBuildPath%" amqp-vs2013.sln /t:%1 /nologo /p:Configuration=%build-config%;Platform="%build-platform%" /verbosity:%build-verbosity%
  IF %ERRORLEVEL% NEQ 0 EXIT /b %ERRORLEVEL%

  ECHO Build other versions of the micro NETMF projects
  FOR /L %%I IN (2,1,3) DO (
    "%MSBuildPath%" .\src\Amqp.Micro.NetMF.csproj /t:%1 /nologo /p:Configuration=%build-config%;Platform="%build-platform: =%";FrameworkVersionMajor=4;FrameworkVersionMinor=%%I /verbosity:%build-verbosity%
    IF !ERRORLEVEL! NEQ 0 EXIT /b !ERRORLEVEL!
  )

  IF /I "%build-dotnet%" EQU "false" EXIT /b 0
  IF /I "%1" EQU "Clean" EXIT /b 0
  ECHO Build dotnet projects
  pushd dotnet
  "%dotnetPath%" restore
  IF !ERRORLEVEL! NEQ 0 EXIT /b !ERRORLEVEL!
  "%dotnetPath%" build Amqp Amqp.Serialization Amqp.WebSockets.Client HelloAmqp Test.Amqp
  IF !ERRORLEVEL! NEQ 0 EXIT /b !ERRORLEVEL!
  popd

  EXIT /b 0

:findfile
  IF EXIST ".\Build\tools\%1.%2" (
    SET %1Path=.\Build\tools\%1.%2
  ) ELSE (
    FOR %%f IN (%1.%2) DO IF EXIST "%%~$PATH:f" SET %1Path=%%~$PATH:f
  )
  GOTO :eof