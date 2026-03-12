# ── Build Stage ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY src/LmsApp/LmsApp.csproj ./src/LmsApp/
RUN dotnet restore ./src/LmsApp/LmsApp.csproj

COPY src/LmsApp/ ./src/LmsApp/
RUN dotnet publish ./src/LmsApp/LmsApp.csproj -c Release -o /publish

# ── Runtime Stage ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /publish .

ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8000

ENTRYPOINT ["dotnet", "LmsApp.dll"]
