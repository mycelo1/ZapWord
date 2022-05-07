FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY . .
WORKDIR /app/Server
RUN dotnet restore
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime-env
ENV ASPNETCORE_ENVIRONMENT Production
WORKDIR /app
COPY --from=build-env /app/Server/bin/Release/net6.0/publish/ .
CMD dotnet ZapWord.Server.dll --urls http://0.0.0.0:$PORT
