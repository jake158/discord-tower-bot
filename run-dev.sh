#!/bin/bash

secrets=$(dotnet user-secrets list -p Tower/)
export SQL_SERVER_PORT=$(echo "$secrets" | grep "SqlServerPort" | awk -F' = ' '{print $2}')
export SQL_SERVER_PASSWORD=$(echo "$secrets" | grep "SqlServerPassword" | awk -F' = ' '{print $2}')
export DISCORD_TOKEN=$(echo "$secrets" | grep "DiscordToken" | awk -F' = ' '{print $2}')

# dotnet user-secrets set -p Tower/ "GoogleServiceAccountJson" "$(cat ./tower-web-risk.json | jq -c .)"
export GOOGLE_SERVICE_ACCOUNT_JSON=$(echo "$secrets" | grep "GoogleServiceAccountJson" | awk -F' = ' '{print $2}')

python3 mock_server.py &
ANTIVIRUS_SERVER_PID=$!

cleanup() {
    echo "Shutting down antivirus server..."
    kill $ANTIVIRUS_SERVER_PID 2>/dev/null

    echo "Stopping containers..."
    docker compose -f docker-compose.dev.yml stop 
}
trap cleanup SIGINT SIGTERM EXIT

docker compose -f docker-compose.dev.yml down
docker compose -f docker-compose.dev.yml up -d sqlserver
docker compose -f docker-compose.dev.yml up bot --build &
DOCKER_BOT_PID=$!

wait $DOCKER_BOT_PID
