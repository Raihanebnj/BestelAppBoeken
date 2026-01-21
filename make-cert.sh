#!/bin/bash
echo "🔐 Creating new self-signed certificate..."
openssl req -x509 -newkey rsa:4096 -sha256 -days 365 -nodes \
  -keyout temp.key -out temp.crt \
  -subj "/CN=localhost" -addext "subjectAltName=DNS:localhost,DNS:*.localhost,IP:127.0.0.1" 2>/dev/null

openssl pkcs12 -export -out aspnetapp.pfx \
  -inkey temp.key -in temp.crt \
  -passout pass: 2>/dev/null  # LEEG wachtwoord!

rm temp.key temp.crt 2>/dev/null
echo "✅ New certificate created with empty password"
