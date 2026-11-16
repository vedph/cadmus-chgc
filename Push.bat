@echo off
echo PUSH PACKAGES TO NUGET
prompt
set nu=C:\Exe\nuget.exe
set src=-Source https://api.nuget.org/v3/index.json

%nu% push .\Cadmus.Chgc.Parts\bin\Release\*.nupkg %src% -SkipDuplicate
%nu% push .\Cadmus.Chgc.Services\bin\Release\*.nupkg %src% -SkipDuplicate
%nu% push .\Cadmus.Chgc.Export\bin\Release\*.nupkg %src% -SkipDuplicate
%nu% push .\Cadmus.Chgc.Import\bin\Release\*.nupkg %src% -SkipDuplicate
echo COMPLETED
pause
echo on
