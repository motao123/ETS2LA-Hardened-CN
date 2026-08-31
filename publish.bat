rem Neutral restore matches the committed lock files (CI runs locked mode).
dotnet restore ETS2LA.sln --force-evaluate || exit /b 1
dotnet build ETS2LA.sln -c Release --no-incremental --no-restore || exit /b 1
rem RID-specific restore for the self-contained publish (updates the lock
rem locally; CI never commits it).
dotnet restore ETS2LA\ETS2LA.csproj -r win-x64 --force-evaluate || exit /b 1
dotnet publish ETS2LA\ETS2LA.csproj -c Release -r win-x64 --self-contained -o .\publish --no-restore || exit /b 1
xcopy /E /I /Y .\Assets .\publish\Assets || exit /b 1
