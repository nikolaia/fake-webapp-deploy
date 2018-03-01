REM This is currently in preview:
REM dotnet install tool --global dotnet-fake
REM We should use that instead of the Tools.csproj
dotnet restore .\Tools.csproj
dotnet fake run build.fsx --target %*