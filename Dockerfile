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
COPY --from=backend-build /publish ./

# Directories for persistent data
RUN mkdir -p /app/data /app/keys

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Sextant.dll"]
