Write-Output "Installing .NET MicroFramework 4.3 ..."
$msiPath = "$($env:USERPROFILE)\MicroFrameworkSDK43.MSI"
(New-Object Net.WebClient).DownloadFile('http://download-codeplex.sec.s-msft.com/Download/Release?ProjectName=netmf&DownloadId=1423116&FileTime=130667921437670000&Build=21046', $msiPath)
& msiexec.exe /i $msiPath /quiet /log $env:USERPROFILE\netmf43.log | Write-Output
Write-Output "NETMF43 Installed"

Write-Output "Installing .NET MicroFramework 4.4 ..."
$msiPath = "$($env:USERPROFILE)\MicroFrameworkSDK44.MSI"
(New-Object Net.WebClient).DownloadFile('https://github.com/NETMF/netmf-interpreter/releases/download/v4.4-RTW-20-Oct-2015/MicroFrameworkSDK.MSI', $msiPath)
& msiexec.exe /i $msiPath /quiet /log $env:USERPROFILE\netmf44.log | Write-Output
Write-Output "NETMF44 Installed"

Write-Output "Installing Application Builder for CF 2013 ..."
$zipPath = "$($env:USERPROFILE)\VSAppBuilderCF2013.zip"
$unzipFolder = "$($env:USERPROFILE)\VSAppBuilderCF2013"
(New-Object Net.WebClient).DownloadFile('https://download.microsoft.com/download/B/C/4/BC4FA89D-4F7B-4022-A4C1-2B3B6E08D8BE/AppBuilderSetup_VS2013_v50608.zip', $zipPath)
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $unzipFolder)
& $unzipFolder\VSEmbedded_AppBuilder.exe /Quiet /NoRestart /Full /L $env:USERPROFILE\netcf.txt | Write-Output
Write-Output "NETCF Installed"

Write-Output "Installing dotnet cli ..."
$msiPath = "$($env:USERPROFILE)\DotNetCoreSDK101.exe"
(New-Object Net.WebClient).DownloadFile('https://go.microsoft.com/fwlink/?LinkID=827524', $msiPath)
& $msiPath /install /quiet /log $env:USERPROFILE\dotnetcore101.log | Write-Output
Write-Output "dotnet core 1.0.1 Installed"

Write-Output "Copying NuGet.exe ..."
New-Item c:\projects\amqpnetlite\build\tools -type directory
copy c:\tools\NuGet\NuGet.exe c:\projects\amqpnetlite\build\tools\NuGet.exe

Write-Output "Expanding PATH ..."
$env:Path = "$($env:PATH);$($env:ProgramFiles)\dotnet;$($env:VS120COMNTOOLS)..\IDE"
[Environment]::SetEnvironmentVariable("PATH", $env:Path, "User")

Write-Output "Invoking build.cmd script ..."
& c:\projects\amqpnetlite\build.cmd | Write-Output

& c:\projects\amqpnetlite\build.cmd release | Write-Output