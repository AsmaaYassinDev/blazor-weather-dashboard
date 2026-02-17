# ==========================================
# STAGE 1: Base Runtime (Lightweight)
# ==========================================
# This is the final environment your app will run in. No SDKs, just the runtime.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ==========================================
# STAGE 2: Build Environment (Heavy)
# ==========================================
# This stage brings in the .NET 10 SDK and installs Node.js to compile Tailwind.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Install Node.js (Required to build Tailwind CSS v4)
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && \
    apt-get install -y nodejs

# 2. Copy the project file and restore C# dependencies
COPY ["WeatherApp.csproj", "./"]
RUN dotnet restore "./WeatherApp.csproj"

# 3. Copy the rest of your application code
COPY . .

# 4. Install npm packages and build Tailwind CSS
RUN npm install
RUN npm run css:build

# 5. Build the .NET project
RUN dotnet build "WeatherApp.csproj" -c Release -o /app/build

# ==========================================
# STAGE 3: Publish
# ==========================================
# This optimizes the C# code for production.
FROM build AS publish
RUN dotnet publish "WeatherApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# STAGE 4: Final Image
# ==========================================
# This copies the compiled files from STAGE 3 into the lightweight STAGE 1 environment.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherApp.dll"]