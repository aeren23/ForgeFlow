FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy contracts first
COPY contracts/ForgeFlow.Contracts/*.csproj contracts/ForgeFlow.Contracts/
RUN dotnet restore contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj

# Copy notification service project
COPY services/notification/ForgeFlow.Notification.Service/*.csproj services/notification/ForgeFlow.Notification.Service/
RUN dotnet restore services/notification/ForgeFlow.Notification.Service/ForgeFlow.Notification.Service.csproj

# Copy all source files
COPY contracts/ contracts/
COPY services/notification/ services/notification/

# Build
WORKDIR /src/services/notification/ForgeFlow.Notification.Service
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ForgeFlow.Notification.Service.dll"]
