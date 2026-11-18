@echo off
echo PRESS ANY KEY TO INSTALL Cadmus Libraries TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.
c:\exe\nuget add .\Cadmus.Chgc.Parts\bin\Release\Cadmus.Chgc.Parts.5.0.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Services\bin\Release\Cadmus.Chgc.Services.5.0.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Export\bin\Release\Cadmus.Chgc.Export.5.0.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Import\bin\Release\Cadmus.Chgc.Import.5.0.0.nupkg -source C:\Projects\_NuGet
pause
