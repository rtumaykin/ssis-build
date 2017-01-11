@echo off

pushd %~dp0

call ensureNuget.cmd

%LocalAppData%\NuGet\NuGet.exe install FAKE -OutputDirectory src\packages -ExcludeVersion

%LocalAppData%\NuGet\NuGet.exe install xunit.runner.console -OutputDirectory src\packages\FAKE -ExcludeVersion

rem cls

set encoding=utf-8

copy %LocalAppData%\NuGet\NuGet.exe src\packages\FAKE\tools\

src\packages\FAKE\tools\FAKE.exe build.fsx %*

popd
