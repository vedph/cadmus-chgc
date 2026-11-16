@echo off
echo BUILD Cadmus Chgc Packages
del .\Cadmus.Chgc.Parts\bin\Release\*.*nupkg
del .\Cadmus.Chgc.Services\bin\Release\*.*nupkg
del .\Cadmus.Chgc.Export\bin\Release\*.*nupkg
del .\Cadmus.Chgc.Import\bin\Release\*.*nupkg

cd .\Cadmus.Chgc.Parts
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Services
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Export
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Import
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

pause
