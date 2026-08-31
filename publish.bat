dotnet build ETS2LA.sln -c Release --no-incremental || exit /b 1
dotnet publish ETS2LA/ETS2LA.csproj --self-contained -r win-x64 -o .\publish || exit /b 1
xcopy /E /I /Y .\Assets .\publish\Assets || exit /b 1
