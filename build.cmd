
@ECHO OFF
SETLOCAL

CALL :ExeExists MSBuild
IF "%MSBuildPath%" == "" (
  ECHO MSBuild.exe does not exist or is not under PATH.
  ECHO This can be resolved by building from a VS developer command prompt.
  GOTO :exit
)

"%MSBuildPath%" amqp.sln /p:Configuration=Release

rem Build NuGet package
ECHO.
CALL :ExeExists NuGet
IF "%NuGetPath%" == "" (
  ECHO NuGet.exe does not exist or is not under PATH.
  ECHO If you want to build NuGet package, install NuGet.CommandLine
  ECHO package, or download NuGet.exe and place it under .\Build\tools
  ECHO directory.
) ELSE (
  IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"
  "%NuGetPath%" pack Amqp.Net.nuspec -OutputDirectory ".\Build\Packages"
)

:exit
ENDLOCAL
GOTO :eof

:ExeExists
IF EXIST ".\Build\tools\%1.exe" (
  SET %1Path=.\Build\tools\%1.exe
) ELSE (
  FOR %%f IN (%1.exe) DO IF EXIST "%%~$PATH:f" SET %1Path=%%~$PATH:f
)
GOTO :eof