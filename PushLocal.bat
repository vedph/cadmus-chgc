@echo off
echo PRESS ANY KEY TO INSTALL Cadmus Libraries TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.
c:\exe\nuget add .\Cadmus.Chgc.Parts\bin\Debug\Cadmus.Chgc.Parts.1.1.2.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Services\bin\Debug\Cadmus.Chgc.Services.1.1.2.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Chgc.Export\bin\Debug\Cadmus.Chgc.Export.1.1.3.nupkg -source C:\Projects\_NuGet
pause
