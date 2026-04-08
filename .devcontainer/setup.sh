#!/bin/bash
# LMS Demo Setup Script
# Dijalankan otomatis oleh Codespaces setelah container start.
# Aman dijalankan berulang kali (idempotent).

set -e
cd "$(dirname "$0")/.."    # root project

echo ""
echo "╔══════════════════════════════════════╗"
echo "║      LMS + Keycloak Setup            ║"
echo "╚══════════════════════════════════════╝"
echo ""

# ── 1. Deteksi environment dan tentukan URL ────────────────────────────────────

if [ -n "$CODESPACE_NAME" ]; then
  DOMAIN="${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}"
  KC_PUBLIC_URL="https://${CODESPACE_NAME}-8080.${DOMAIN}"
  API_PUBLIC_URL="https://${CODESPACE_NAME}-5000.${DOMAIN}"
  FRONTEND_PUBLIC_URL="https://${CODESPACE_NAME}-5173.${DOMAIN}"
  echo "📍 GitHub Codespaces: $FRONTEND_PUBLIC_URL"
else
  KC_PUBLIC_URL="http://localhost:8080"
  API_PUBLIC_URL="http://localhost:5000"
  FRONTEND_PUBLIC_URL="http://localhost:5173"
  echo "📍 Local environment: $FRONTEND_PUBLIC_URL"
fi

KC_AUTHORITY="${KC_PUBLIC_URL}/realms/lms"

# ── 2. Start docker-compose services ──────────────────────────────────────────

echo ""
echo "▶ Starting Postgres + Keycloak..."
docker compose up -d postgres keycloak

# ── 3. Tunggu Postgres ─────────────────────────────────────────────────────────

echo -n "⏳ Waiting for Postgres"
until docker compose exec -T postgres pg_isready -U lmsuser -q 2>/dev/null; do
  echo -n "."
  sleep 2
done
echo " ✅"

# ── 4. Tunggu Keycloak ─────────────────────────────────────────────────────────

echo -n "⏳ Waiting for Keycloak (may take 60s first time)"
until curl -sf "http://localhost:8080/health/ready" > /dev/null 2>&1; do
  echo -n "."
  sleep 3
done
echo " ✅"

# ── 5. Update appsettings.Development.json (backend config) ───────────────────

echo ""
echo "▶ Configuring backend..."
cat > src/LmsApp/appsettings.Development.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lms_dev;Username=lmsuser;Password=lmspassword"
  },
  "Keycloak": {
    "Authority": "${KC_AUTHORITY}",
    "ClientId": "lms-app",
    "AllowedClientId": "lms-app",
    "RequireHttpsMetadata": false
  },
  "Cors": {
    "AllowedOrigins": [
      "${FRONTEND_PUBLIC_URL}",
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  },
  "FileStorage": {
    "BasePath": "wwwroot/uploads"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
EOF
echo "   ✅ appsettings.Development.json updated"

# ── 6. Write frontend .env.local ───────────────────────────────────────────────

echo "▶ Configuring frontend..."
cat > frontend/.env.local << EOF
VITE_API_URL=${API_PUBLIC_URL}/api
VITE_MOCK_AUTH=true
VITE_KEYCLOAK_URL=${KC_PUBLIC_URL}
VITE_KEYCLOAK_REALM=lms
VITE_KEYCLOAK_CLIENT_ID=lms-app
VITE_DWI_MOBILE_URL=http://localhost:8000
EOF
echo "   ✅ frontend/.env.local written"

# ── 7. Jalankan Keycloak setup (realm, client, users) ─────────────────────────

echo "▶ Configuring Keycloak realm + users..."
KEYCLOAK_URL="http://localhost:8080" \
  LMS_FRONTEND_URL="$FRONTEND_PUBLIC_URL" \
  node setup-keycloak.js

# ── 8. Selesai ─────────────────────────────────────────────────────────────────

echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║  ✅  Setup selesai!                                       ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║                                                          ║"
echo "║  Jalankan di terminal terpisah:                          ║"
echo "║                                                          ║"
echo "║  Backend:   cd src/LmsApp && dotnet run                  ║"
echo "║  Frontend:  cd frontend && npm run dev                   ║"
echo "║                                                          ║"
echo "║  ⚡ Mock Auth aktif — pilih user via panel di kanan bawah ║"
echo "║     (student1, teacher1, admin1)                         ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""
