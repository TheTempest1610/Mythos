#!/usr/bin/env bash
dotnet run --project Content.Server -- --config-file "$(dirname "$0")/config/server_config.toml"
read -p "Press enter to continue"
