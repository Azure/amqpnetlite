
MSBuild.exe amqp.sln /p:Configuration=Release

IF NOT EXIST ".\Packages" MKDIR ".\Packages"

.\Tools\NuGet\NuGet.exe pack Amqp.Net.nuspec -OutputDirectory ".\Packages"
pause