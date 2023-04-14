@echo off
echo BUILD Cadmus Chgc Packages
del .\Cadmus.Chgc.Parts\bin\Debug\*.snupkg
del .\Cadmus.Chgc.Services\bin\Debug\*.snupkg

cd .\Cadmus.Chgc.Parts
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Cadmus.Chgc.Services
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

pause
