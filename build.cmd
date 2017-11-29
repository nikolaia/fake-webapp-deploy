@echo off
cls
if not exist "tools\FAKE\tools\Fake.exe" "tools\nuget.exe" "install" "FAKE" "-OutputDirectory" "tools" "-ExcludeVersion" "-Version" "4.63.2"
if not exist "tools\Nunit.ConsoleRunner\tools\nunit3-console.exe" "tools\nuget.exe" "install" "NUnit.ConsoleRunner" "-OutputDirectory" "tools" "-ExcludeVersion" "-Version" "3.7.0"
if not exist "tools\Nuget.Core\lib\net40-Client\NuGet.Core.dll" "tools\nuget.exe" "install" "Nuget.Core" "-OutputDirectory" "tools" "-ExcludeVersion" "-Version" "2.14.0"

"tools\FAKE\tools\Fake.exe" build.fsx %* --nocache