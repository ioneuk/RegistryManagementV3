#!/bin/bash

set -e
run_cmd="dotnet run environment=${ASPNETCORE_ENVIRONMENT} --urls http://*:80"

until dotnet ef database update -c SecurityDbContext; do
>&2 echo "SQL Server is starting up"
sleep 1
done

>&2 echo "SQL Server is up - executing command"
exec $run_cmd