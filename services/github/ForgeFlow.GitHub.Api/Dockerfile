FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["services/github/ForgeFlow.GitHub.Api/ForgeFlow.GitHub.Api.csproj", "services/github/ForgeFlow.GitHub.Api/"]
COPY ["services/github/ForgeFlow.GitHub.Application/ForgeFlow.GitHub.Application.csproj", "services/github/ForgeFlow.GitHub.Application/"]
COPY ["services/github/ForgeFlow.GitHub.Infrastructure/ForgeFlow.GitHub.Infrastructure.csproj", "services/github/ForgeFlow.GitHub.Infrastructure/"]
COPY ["services/github/ForgeFlow.GitHub.Domain/ForgeFlow.GitHub.Domain.csproj", "services/github/ForgeFlow.GitHub.Domain/"]
COPY ["contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj", "contracts/ForgeFlow.Contracts/"]
RUN dotnet restore "services/github/ForgeFlow.GitHub.Api/ForgeFlow.GitHub.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/services/github/ForgeFlow.GitHub.Api"
RUN dotnet build "ForgeFlow.GitHub.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ForgeFlow.GitHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ForgeFlow.GitHub.Api.dll"]
