@echo off
dotnet run --project Content.Server --configuration Tools -- --config-file "%~dp0config\server_config.toml"
pause
