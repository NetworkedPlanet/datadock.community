FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app
# Install Git command-line for DD repository operations
RUN apt-get update && apt-get install -y git-core
ENV GIT_PATH=/usr/bin/git

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY *.sln ./
COPY DataDock.Worker/DataDock.Worker.csproj DataDock.Worker/
RUN dotnet restore
COPY . .
WORKDIR /src/DataDock.Worker
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "DataDock.Worker.dll"]
