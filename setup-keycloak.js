const h = require('http');
const q = require('querystring');

const CLIENT_UUID = 'ee287ea8-7037-4e30-8dd0-9945a7452d94';

function post(path, data, headers) {
  return new Promise((resolve, reject) => {
    const body = typeof data === 'string' ? data : JSON.stringify(data);
    const req = h.request({
      host: 'localhost', port: 8080, path, method: 'POST',
      headers: { 'Content-Length': Buffer.byteLength(body), ...headers }
    }, res => {
      let b = '';
      res.on('data', c => b += c);
      res.on('end', () => resolve(b));
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function put(path, data, token) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify(data);
    const req = h.request({
      host: 'localhost', port: 8080, path, method: 'PUT',
      headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) }
    }, res => {
      let b = '';
      res.on('data', c => b += c);
      res.on('end', () => resolve(res.statusCode));
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function postJson(path, data, token) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify(data);
    const req = h.request({
      host: 'localhost', port: 8080, path, method: 'POST',
      headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) }
    }, res => {
      let b = '';
      res.on('data', c => b += c);
      res.on('end', () => resolve(res.statusCode));
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

async function main() {
  // Get token
  const tokenResp = await post(
    '/realms/master/protocol/openid-connect/token',
    q.stringify({ client_id: 'admin-cli', username: 'admin', password: 'admin', grant_type: 'password' }),
    { 'Content-Type': 'application/x-www-form-urlencoded' }
  );
  const token = JSON.parse(tokenResp).access_token;
  console.log('Token OK');

  // Update client
  const status = await put(`/admin/realms/lms/clients/${CLIENT_UUID}`, { redirectUris: ['*'], webOrigins: ['*'] }, token);
  console.log('Client updated, status:', status);

  // Create user
  const userStatus = await postJson('/admin/realms/lms/users', {
    username: 'admin', email: 'admin@lms.com', enabled: true,
    credentials: [{ type: 'password', value: 'admin123', temporary: false }]
  }, token);
  console.log('User created, status:', userStatus);
}

main().catch(console.error);
