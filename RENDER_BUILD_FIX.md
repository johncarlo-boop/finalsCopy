# Render Build Error Fix - "dotnet: command not found"

## Problem
Ang error na `bash: line 1: dotnet: command not found` ay nangyayari dahil hindi naka-detect ng Render ang .NET SDK.

## Solutions Applied

### 1. Created `global.json`
Ginawa ang `global.json` file para i-specify ang .NET 8.0 SDK version:
```json
{
  "sdk": {
    "version": "8.0.0",
    "rollForward": "latestMajor"
  }
}
```

### 2. Updated `render.yaml`
- Changed `runtime: dotnet` to `env: dotnet`
- Added version check sa build command
- Added explicit `dotnet restore` step

## Alternative Solutions (Kung hindi pa rin gumana)

### Option 1: Manual Configuration sa Render Dashboard

Kung hindi gumana ang `render.yaml`, i-configure manually sa Render dashboard:

1. **Environment:**
   - I-set bilang `dotnet` o `Docker`

2. **Build Command:**
   ```
   dotnet restore && dotnet publish -c Release -o ./publish
   ```

3. **Start Command:**
   ```
   cd publish && dotnet PropertyInventory.dll
   ```

4. **Environment Variables:**
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ASPNETCORE_URLS=http://0.0.0.0:$PORT`

### Option 2: Use Docker (Kung available)

Kung may Docker support ang Render plan mo, pwede mong gumamit ng Dockerfile:

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PropertyInventory.csproj", "./"]
RUN dotnet restore "PropertyInventory.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "PropertyInventory.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PropertyInventory.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PropertyInventory.dll"]
```

### Option 3: Check Render Plan

Ang free plan ng Render ay may limitations. I-check:
- Kung supported ang .NET sa free plan
- Kung kailangan ng paid plan para sa .NET 8.0

### Option 4: Use Different Buildpack

Sa Render dashboard, i-try:
1. I-set ang **Environment** bilang `Docker`
2. O i-contact ang Render support para sa .NET 8.0 support

## Verification Steps

1. I-commit at i-push ang `global.json` at updated `render.yaml`:
   ```bash
   git add global.json render.yaml
   git commit -m "Add global.json and fix render.yaml for .NET 8.0"
   git push origin main
   ```

2. I-check ang Render build logs para makita kung:
   - Na-detect na ang .NET SDK
   - Na-restore na ang packages
   - Na-build na ang project

3. Kung may error pa rin, i-check ang build logs at i-share ang complete error message.

## Common Issues

### Issue: "SDK not found"
**Solution:** I-verify na may `global.json` file at tama ang version

### Issue: "Package restore failed"
**Solution:** I-check kung accessible ang NuGet packages, o i-add ang NuGet.config file

### Issue: "Build succeeded but app won't start"
**Solution:** I-check ang start command at PORT environment variable

## Next Steps

1. I-push ang changes sa GitHub
2. I-trigger ang deployment sa Render
3. I-check ang build logs
4. I-test ang deployed application

---

**Note:** Kung may error pa rin, i-share ang complete build logs para mas matulungan kita.

