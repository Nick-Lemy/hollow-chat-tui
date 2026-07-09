FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/shared.csproj shared/
COPY server/server.csproj server/
RUN dotnet restore server/server.csproj

COPY shared/ shared/
COPY server/ server/
RUN dotnet publish server/server.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 11000

ENTRYPOINT ["dotnet", "server.dll"]
