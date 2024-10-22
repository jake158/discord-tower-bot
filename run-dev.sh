#!/bin/bash

secrets=$(dotnet user-secrets list -p Tower/)
export SQL_SERVER_PORT=$(echo "$secrets" | grep "SqlServerPort" | awk -F' = ' '{print $2}')
export SQL_SERVER_PASSWORD=$(echo "$secrets" | grep "SqlServerPassword" | awk -F' = ' '{print $2}')
export DISCORD_TOKEN=$(echo "$secrets" | grep "DiscordToken" | awk -F' = ' '{print $2}')


nohup dotnet run --project ~/Documents/tower-server/AntivirusServer > antivirus.log 2>&1 &
TOWERPIPE_PID=$!

cleanup() {
    echo "Shutting down towerpipe..."
    kill $TOWERPIPE_PID 2>/dev/null

    echo "Stopping containers..."
    docker compose -f docker-compose.dev.yml stop 
}
trap cleanup SIGINT SIGTERM EXIT

docker compose -f docker-compose.dev.yml down
docker compose -f docker-compose.dev.yml up -d sqlserver
docker compose -f docker-compose.dev.yml up bot --build &
DOCKER_BOT_PID=$!

wait $DOCKER_BOT_PID
