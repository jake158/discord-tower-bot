FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Tower/*.csproj ./
RUN dotnet restore

COPY Tower/ .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY ./wait-for-it.sh .
RUN chmod +x wait-for-it.sh

COPY --from=publish /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet Tower.dll --environment $1"]
CMD ["Production"]
