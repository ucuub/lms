/**
 * Keycloak Setup Script
 * Membuat realm, client, roles, dan test users untuk demo LMS.
 * Idempotent — aman dijalankan berulang kali.
 *
 * Env vars:
 *   KEYCLOAK_URL      (default: http://localhost:8080)
 *   LMS_FRONTEND_URL  (default: http://localhost:5173)
 */

const http = require('http');
const qs   = require('querystring');

const BASE         = (process.env.KEYCLOAK_URL || 'http://localhost:8080').replace(/\/$/, '');
const FRONTEND_URL = process.env.LMS_FRONTEND_URL || 'http://localhost:5173';

// ── HTTP helper ────────────────────────────────────────────────────────────────

function req(method, path, body, token, type = 'json') {
  return new Promise((resolve, reject) => {
    const isForm    = type === 'form';
    const payload   = !body ? '' : isForm ? qs.stringify(body) : JSON.stringify(body);
    const url       = new URL(BASE + path);
    const headers   = { 'Content-Length': Buffer.byteLength(payload) };

    if (payload)  headers['Content-Type'] = isForm
      ? 'application/x-www-form-urlencoded'
      : 'application/json';
    if (token)    headers['Authorization'] = 'Bearer ' + token;

    const options = {
      hostname: url.hostname,
      port:     url.port || 8080,
      path:     url.pathname + url.search,
      method,
      headers,
    };

    const r = http.request(options, res => {
      let data = '';
      res.on('data', c => (data += c));
      res.on('end', () => {
        const parsed = (() => { try { return JSON.parse(data); } catch { return data; } })();
        resolve({ status: res.statusCode, data: parsed });
      });
    });
    r.on('error', reject);
    if (payload) r.write(payload);
    r.end();
  });
}

const get  = (p, t)    => req('GET',    p, null, t);
const post = (p, b, t, type) => req('POST', p, b, t, type);
const put  = (p, b, t) => req('PUT',    p, b, t);

// ── Main ───────────────────────────────────────────────────────────────────────

async function main() {
  console.log(`\nKeycloak: ${BASE}\nFrontend: ${FRONTEND_URL}\n`);

  // ── Admin token ─────────────────────────────────────────────────────────────

  const tokenRes = await post(
    '/realms/master/protocol/openid-connect/token',
    { client_id: 'admin-cli', username: 'admin', password: 'admin', grant_type: 'password' },
    null, 'form'
  );
  if (!tokenRes.data?.access_token) {
    throw new Error('Gagal mendapatkan admin token: ' + JSON.stringify(tokenRes.data));
  }
  const T = tokenRes.data.access_token;
  console.log('✓ Admin token');

  // ── Realm: lms ──────────────────────────────────────────────────────────────

  const realmCheck = await get('/admin/realms/lms', T);
  if (realmCheck.status === 404) {
    await post('/admin/realms', {
      realm:       'lms',
      enabled:     true,
      displayName: 'LMS',
      sslRequired: 'none',
      registrationAllowed: false,
      loginTheme:  'keycloak',
      accessTokenLifespan: 300,       // 5 menit
      ssoSessionIdleTimeout: 1800,    // 30 menit
    }, T);
    console.log('✓ Realm lms dibuat');
  } else {
    console.log('✓ Realm lms sudah ada');
  }

  // ── Client: lms-app (public) ─────────────────────────────────────────────────

  const clientsRes = await get('/admin/realms/lms/clients?clientId=lms-app', T);
  let clientUuid;

  if (clientsRes.data?.length > 0) {
    clientUuid = clientsRes.data[0].id;
    // Update redirect URIs (bisa berubah tiap Codespaces session)
    await put(`/admin/realms/lms/clients/${clientUuid}`, {
      ...clientsRes.data[0],
      redirectUris: [FRONTEND_URL + '/*', 'http://localhost:5173/*', '*'],
      webOrigins:   [FRONTEND_URL, 'http://localhost:5173', '+'],
    }, T);
    console.log('✓ Client lms-app diupdate');
  } else {
    await post('/admin/realms/lms/clients', {
      clientId:    'lms-app',
      name:        'LMS Application',
      description: 'Vue.js LMS frontend',
      publicClient:               true,    // tidak butuh client_secret di frontend
      standardFlowEnabled:        true,    // authorization code flow
      directAccessGrantsEnabled:  true,    // ROPC (untuk testing via Swagger)
      implicitFlowEnabled:        false,
      serviceAccountsEnabled:     false,
      redirectUris: [FRONTEND_URL + '/*', 'http://localhost:5173/*', '*'],
      webOrigins:   [FRONTEND_URL, 'http://localhost:5173', '+'],
      attributes: {
        'pkce.code.challenge.method': 'S256',
        'post.logout.redirect.uris':  FRONTEND_URL + '/*',
      },
    }, T);
    // Ambil UUID yang baru dibuat
    const newClient = await get('/admin/realms/lms/clients?clientId=lms-app', T);
    clientUuid = newClient.data[0].id;
    console.log('✓ Client lms-app dibuat (UUID: ' + clientUuid + ')');
  }

  // ── Audience mapper — wajib agar token punya aud: ["lms-app"] ──────────────
  // Tanpa ini, backend ValidateAudience = true akan selalu gagal (401).
  // Mapper ini menambahkan "lms-app" ke claim aud di access token.

  const mappersRes = await get(`/admin/realms/lms/clients/${clientUuid}/protocol-mappers/models`, T);
  const hasAudMapper = mappersRes.data?.some(m => m.name === 'lms-app-audience');

  if (!hasAudMapper) {
    await post(`/admin/realms/lms/clients/${clientUuid}/protocol-mappers/models`, {
      name:           'lms-app-audience',
      protocol:       'openid-connect',
      protocolMapper: 'oidc-audience-mapper',
      config: {
        'included.client.audience': 'lms-app',
        'id.token.claim':           'false',
        'access.token.claim':       'true',
      },
    }, T);
    console.log('✓ Audience mapper dibuat (aud: lms-app)');
  } else {
    console.log('  - Audience mapper sudah ada');
  }

  // ── Client roles: admin, teacher, student ────────────────────────────────────

  const roleNames = ['admin', 'teacher', 'student'];
  for (const name of roleNames) {
    const r = await post(`/admin/realms/lms/clients/${clientUuid}/roles`, { name }, T);
    console.log(r.status === 409 ? `  - Role ${name} sudah ada` : `✓ Role ${name} dibuat`);
  }

  // Ambil role objects (butuh id untuk assignment)
  const rolesRes = await get(`/admin/realms/lms/clients/${clientUuid}/roles`, T);
  const roleMap  = Object.fromEntries(rolesRes.data.map(r => [r.name, r]));

  // ── Test users ────────────────────────────────────────────────────────────────

  const users = [
    { username: 'student1', email: 'student1@lms.local', firstName: 'Budi',   lastName: 'Santoso', role: 'student' },
    { username: 'teacher1', email: 'teacher1@lms.local', firstName: 'Dewi',   lastName: 'Lestari', role: 'teacher' },
    { username: 'admin1',   email: 'admin1@lms.local',   firstName: 'Ahmad',  lastName: 'Fauzi',   role: 'admin'   },
  ];

  for (const u of users) {
    // Cek existing
    const existRes = await get(
      `/admin/realms/lms/users?username=${encodeURIComponent(u.username)}&exact=true`, T
    );

    let userId;
    if (existRes.data?.length > 0) {
      userId = existRes.data[0].id;
      console.log(`  - User ${u.username} sudah ada`);
    } else {
      await post('/admin/realms/lms/users', {
        username:      u.username,
        email:         u.email,
        firstName:     u.firstName,
        lastName:      u.lastName,
        enabled:       true,
        emailVerified: true,
        credentials: [{ type: 'password', value: 'Password1!', temporary: false }],
      }, T);
      const created = await get(
        `/admin/realms/lms/users?username=${encodeURIComponent(u.username)}&exact=true`, T
      );
      userId = created.data[0].id;
      console.log(`✓ User ${u.username} (${u.firstName} ${u.lastName}) dibuat`);
    }

    // Assign client role
    const role = roleMap[u.role];
    if (role) {
      await post(
        `/admin/realms/lms/users/${userId}/role-mappings/clients/${clientUuid}`,
        [{ id: role.id, name: role.name }],
        T
      );
      console.log(`  → Assigned role: ${u.role}`);
    }
  }

  console.log('\n✅ Keycloak siap!\n');
}

main().catch(err => {
  console.error('\n❌ Setup gagal:', err.message || err);
  process.exit(1);
});
