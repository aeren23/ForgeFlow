# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["contracts/ForgeFlow.Contracts/ForgeFlow.Contracts.csproj", "contracts/ForgeFlow.Contracts/"]
COPY ["services/work/ForgeFlow.Work.Api/ForgeFlow.Work.Api.csproj", "services/work/ForgeFlow.Work.Api/"]

# Restore
RUN dotnet restore "services/work/ForgeFlow.Work.Api/ForgeFlow.Work.Api.csproj"

# Copy the rest of the source
COPY . .

# Publish
WORKDIR "/src/services/work/ForgeFlow.Work.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ForgeFlow.Work.Api.dll"]
