# Stage 1: Build frontend
FROM node:22-slim AS frontend-build
WORKDIR /app/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build
# Output lands in /app/backend/wwwroot (vite build outDir: '../backend/wwwroot')

# Stage 2: Build backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app/backend
COPY backend/Sextant.csproj ./
RUN dotnet restore
COPY backend/ ./
# Copy built frontend static files
COPY --from=frontend-build /app/backend/wwwroot ./wwwroot
RUN dotnet publish -c Release -o /publish --no-restore

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install vsdbg before copying app files so this layer is cached independently
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl unzip \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
    && apt-get remove -y curl unzip \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/data /app/keys

COPY --from=backend-build /publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Sextant.dll"]
