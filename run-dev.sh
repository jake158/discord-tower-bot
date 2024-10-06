#!/bin/bash

nohup dotnet run --project ~/Documents/tower-server/AntivirusServer > antivirus.log 2>&1 &
TOWERPIPE_PID=$!

cleanup() {
    echo "Shutting down towerpipe..."
    kill $TOWERPIPE_PID

    echo "Stopping containers..."
    docker compose stop
    cd ..
}
trap cleanup SIGINT

export SQL_SERVER_PASSWORD=$(dotnet user-secrets list -p Tower/ | grep "ConnectionStrings:SqlServerPassword" | awk -F' = ' '{print $2}')

docker compose up -d

cd Tower/

dotnet ef database update
dotnet run --environment Development
wait
