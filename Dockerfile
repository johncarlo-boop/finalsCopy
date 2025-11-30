# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj file and restore dependencies
COPY PropertyInventory.csproj .
RUN dotnet restore PropertyInventory.csproj

# Copy everything else and build
COPY . .
RUN dotnet build PropertyInventory.csproj -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish PropertyInventory.csproj -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Expose port (Render will set PORT env var)
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "PropertyInventory.dll"]

