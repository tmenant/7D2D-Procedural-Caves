@echo off

cd %~dp0

dotnet build --no-incremental

if ERRORLEVEL 1 exit /b 1

cls

.\bin\Debug\net8.0\cave-viewer.exe %*

