# ── Stage 1: Build Backend ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app
COPY src/LmsApp/LmsApp.csproj ./src/LmsApp/
RUN dotnet restore ./src/LmsApp/LmsApp.csproj
COPY src/LmsApp/ ./src/LmsApp/
RUN dotnet publish ./src/LmsApp/LmsApp.csproj -c Release -o /publish

# ── Stage 2: Build Frontend ───────────────────────────────────────────────────
FROM node:20-alpine AS frontend-build
WORKDIR /frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=backend-build /publish .
COPY --from=frontend-build /frontend/dist ./wwwroot/spa

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "LmsApp.dll"]
