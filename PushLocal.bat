@echo off
echo PRESS ANY KEY TO INSTALL Cadmus Libraries TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.
c:\exe\nuget add .\Cadmus.Chgc.Parts\bin\Debug\Cadmus.Chgc.Parts.4.0.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Services\bin\Debug\Cadmus.Chgc.Services.4.0.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Export\bin\Debug\Cadmus.Chgc.Export.4.0.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Import\bin\Debug\Cadmus.Chgc.Import.4.0.1.nupkg -source C:\Projects\_NuGet
pause
