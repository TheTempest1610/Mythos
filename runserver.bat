@echo off
dotnet run --project Content.Server -- --config-file "%~dp0config\server_config.toml"
pause
