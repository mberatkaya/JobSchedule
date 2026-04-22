# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY JobScheduler.sln .
COPY JobScheduler/JobScheduler.csproj JobScheduler/
COPY JobScheduler.Tests/JobScheduler.Tests.csproj JobScheduler.Tests/

RUN dotnet restore

COPY . .

RUN dotnet build JobScheduler.sln -c Release --no-restore
RUN dotnet test JobScheduler.sln -c Release --no-build --verbosity normal

RUN dotnet publish JobScheduler/JobScheduler.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "JobScheduler.dll"]
