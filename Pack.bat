@echo off
echo BUILD Cadmus Chgc Packages
del .\Cadmus.Chgc.Parts\bin\Debug\*.*nupkg
del .\Cadmus.Chgc.Services\bin\Debug\*.*nupkg
del .\Cadmus.Chgc.Export\bin\Debug\*.*nupkg
del .\Cadmus.Chgc.Import\bin\Debug\*.*nupkg

cd .\Cadmus.Chgc.Parts
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Services
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Export
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Import
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

pause
