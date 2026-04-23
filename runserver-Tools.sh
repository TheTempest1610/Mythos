#!/usr/bin/env bash
dotnet run --project Content.Server --configuration Tools -- --config-file "$(dirname "$0")/config/server_config.toml"
read -p "Press enter to continue"
