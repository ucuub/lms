import urllib.request, urllib.parse, json

base = "http://localhost:8080"
client_id = "ee287ea8-7037-4e30-8dd0-9945a7452d94"

# Get admin token
data = urllib.parse.urlencode({
    "client_id": "admin-cli",
    "username": "admin",
    "password": "admin",
    "grant_type": "password"
}).encode()
req = urllib.request.urlopen(f"{base}/realms/master/protocol/openid-connect/token", data)
token = json.loads(req.read())["access_token"]
print("Token OK")

# Update client redirect URIs
payload = json.dumps({"redirectUris": ["*"], "webOrigins": ["*"]}).encode()
req2 = urllib.request.Request(f"{base}/admin/realms/lms/clients/{client_id}", data=payload, method="PUT")
req2.add_header("Authorization", "Bearer " + token)
req2.add_header("Content-Type", "application/json")
urllib.request.urlopen(req2)
print("Client updated")

# Create test user
user_payload = json.dumps({
    "username": "admin",
    "email": "admin@lms.com",
    "enabled": True,
    "credentials": [{"type": "password", "value": "admin123", "temporary": False}]
}).encode()
req3 = urllib.request.Request(f"{base}/admin/realms/lms/users", data=user_payload, method="POST")
req3.add_header("Authorization", "Bearer " + token)
req3.add_header("Content-Type", "application/json")
urllib.request.urlopen(req3)
print("User created: admin / admin123")
