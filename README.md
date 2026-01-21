
# IT Business Case – Salesforce Integratie met RabbitMQ

## 📋 Projectoverzicht

Dit project implementeert een end-to-end integratieoplossing waarbij een mobiele verkoopapplicatie via RabbitMQ berichten uitwisselt met Salesforce. Het stelt verkopers in staat om snel orders in te geven bij klanten, waarna deze gegevens asynchroon worden doorgegeven aan Salesforce.

## 👥 Team 

In samenwerking met : 
- Kimberley Thill
-  Raïhane Benjilali
-  Ludger De Sousa Lima Cardoso
-  Lina Benhaj
- Kiran Chaud-ry.

## 🏗️ Architectuur

De applicatie volgt een microservices-architectuur met asynchrone communicatie via RabbitMQ als message broker.
[Frontend Applicatie] → [Backend API] → [RabbitMQ] → [Salesforce]

## 📦 Functionaliteiten

Core Features
✅ Orderplaatsing in Salesforce via RabbitMQ

✅ Klantenbeheer (toevoegen, aanpassen, verwijderen)

✅ Veilige communicatie via API-key/token authenticatie

✅ Asynchrone berichtverwerking met RabbitMQ

✅ Foutafhandeling en retry mechanismen

## Technische Stack

Frontend: React/Vue.js (AI-generated)
Backend: Node.js/Python (AI-generated)
Message Broker: RabbitMQ
CRM: Salesforce REST API
CI/CD: GitHub Actions (voor testing)
Version Control: Git

##🚀 Installatie & Configuratie
### Vereisten
Node.js/C# 
RabbitMQ server
Salesforce Developer Account

## Git stappen
1. Clone repository
2. bash
3. git clone https://github.com/Raihanebnj/BestelAppBoeken.git
4. cd BestelAppBoeken
5. RabbitMQ Setup

## Installeer RabbitMQ op de virtuele server

1. Configureer exchanges en queues
2. Stel credentials in via environment variables
3. Salesforce Configuratie
4. Maak een Connected App in Salesforce
5. Genereer OAuth credentials
6. Configureer de REST API endpoints

## Applicatie Setup

# Install dependencies
npm install

### Configure environment
cp .env.example .env
### Edit .env with your credentials

### Start application
npm start
🔧 CI/CD Pipeline

## Het project gebruikt GitHub Actions voor:

1. Automatische build en testing
2. Code quality checks
3. Deployment automatisering
4. Zie .github/workflows/ voor pipeline configuraties.

## 📁 Projectstructuur
text
├── frontend/          # React/Vue.js applicatie
├── backend/           # Node.js/Python backend
├── rabbitmq/          # RabbitMQ configuratie
├── salesforce/        # Salesforce integratielogica
├── tests/             # Integratie- en unittesten
├── .github/workflows/ # CI/CD pipelines
└── docs/              # Documentatie

## 🧪 Testing

### Run unit tests
npm test

### Run integration tests
npm run test:integration

### Test RabbitMQ connection
npm run test:rabbitmq

👥 Teamrollen

Teamlid 1	Project Lead	Coördinatie, architectuur
Teamlid 2	Backend Developer	RabbitMQ integratie
Teamlid 3	Frontend Developer	UI/UX ontwikkeling
Teamlid 4	Salesforce Expert	CRM integratie
Teamlid 5	DevOps Engineer	CI/CD & deployment

## 🔒 Security & GDPR Compliance
API Key Authentication Middleware: Controleert elke request op geldige API key - beschermt tegen onbevoegde toegang (GDPR Art. 32).

GDPR Compliant Logger: Logt zonder persoonsgegevens (alleen gehashte IDs) - houdt audit trail bij voor accountability (GDPR Art. 5,25,30).

JSON Schema Validator: Blokkeert PII-velden zoals email, telefoon, BSN - implementeert privacy by design (GDPR Art. 25).

Rate Limiting Middleware: Limiteert naar 100 requests/minuut - voorkomt DDoS en brute force attacks (GDPR Art. 32).

Security Headers Middleware: Voorkomt XSS, clickjacking, MIME-sniffing - beveiligt tegen web-aanvallen.

## GDPR Compliance
Dataminimalisatie: JSON Schema blokkeert PII velden

Privacy by Design: Validatie aan de bron, logging zonder PII

Beveiliging: API keys, rate limiting, encryptie in transit

Accountability: Audit logging van alle verwerkingen

Recht op inzage: Gestructureerde logs voor DPO

### Screenshots

