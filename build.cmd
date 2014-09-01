
MSBuild.exe amqp.sln /p:Configuration=Release

IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"

.\Tools\NuGet\NuGet.exe pack Amqp.Net.nuspec -OutputDirectory ".\Build\Packages"
