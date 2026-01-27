# BestelAppBoeken

Kort overzicht
- Project: bestel- en klantbeheer voor een boekhandel.
- Tech stack: .NET 10, ASP.NET Core (Razor Pages + Web API), EF Core, Worker Service.
- Doel: klanten-, boeken- en bestelbeheer, integraties (Salesforce, SAP), berichtenverwerking (RabbitMQ), JSON-export/import & backups.
- Repository: https://github.com/Raihanebnj/BestelAppBoeken

Doel en scope
- Webfrontend voor klanten en beheerders (Razor Pages / `wwwroot`).
- REST API's om klanten, bestellingen en boeken te beheren.
- Background workers voor polling en externe synchronisatie (Salesforce) en asynchrone verwerking via RabbitMQ.
- Export/import van data en eenvoudige backups in JSON-formaat.

Team
- Raihane Benjilali
- Kim Thill
- Kiran Chaud-Ry
- Lina Benhaj
- Ludger De Sousa Lima Cardoso
  

Belangrijke bestanden en services (selectie)
- Web / UI: `BestelAppBoeken.Web/` (`wwwroot/index.html`, `wwwroot/app.js`)
- API controllers: `BestelAppBoeken.Web\Controllers\Api\*` (`OrdersApiController.cs`, `KlantenApiController.cs`, `BooksApiController.cs`)
- Background worker: `BestelAppBoeken.Receiver/`
- Data & seeding: `BestelAppBoeken.Infrastructure\Data\BookstoreDbContext.cs`, `DbSeeder.cs`
- Services: `BestelAppBoeken.Infrastructure\Services\*` (`OrderService`, `KlantService`, `BookService`, `RabbitMqService`, `SalesforceService`, `SapService`)
- Domein & interfaces: `BestelAppBoeken.Core\*` (`Klant`, `Order`, `Book`, `IOrderService`, `IKlantService`, `IBookService`)

Mappenstructuur (hoog niveau)
- `BestelAppBoeken.Web/` — webfrontend & API
- `BestelAppBoeken.Receiver/` — worker background service
- `BestelAppBoeken.Infrastructure/` — implementaties, EF Core, seeder
- `BestelAppBoeken.Core/` — domeinmodellen en interfaces
- `Tools/` — scripts (bv. `PublishTestMessage`)
- `Downloads/` — voorbeeld JSON exports/imports (in jouw omgeving)

Validatie
- Meerdere validatielagen:
  - Client-side (JS in `wwwroot`) voor UX-validatie.
  - Server-side modelvalidatie in controllers en DTOs.
  - Business-regelvalidatie in services (`Infrastructure`) voor voorraadcontrole, prijsregels en klantstatus.
- Bestelling-specifieke checks: productbeschikbaarheid, klantstatus, betalings- en adresvalidatie vóór bevestiging.

Foutenafhandeling (klanten & bestellingen)
- Consistente HTTP-statuscodes:
  - `200`/`201` bij succes
  - `400` bij validatiefouten (veld-specifieke berichten)
  - `404` bij niet-gevonden resources
  - `409` bij conflicts (bv. voorraadrace)
  - `500` bij interne fouten
- Centrale exception-middleware en logging; `GdprCompliantLogger` (maskering PII).
- Asynchrone verwerking: retry-policy en DLQ-ondersteuning voor RabbitMQ.
- Transacties: gebruik EF Core transacties bij order + voorraad updates; rollback bij falen.

Backups en JSON — waarvoor en hoe in dit project
- Locaties in jouw workspace:
  - Voorbeeld/seed JSON: `Downloads/boekhandel_database_1769465925173.json`
  - Export orders: `Downloads/orders_export_20260115_190819.json`
  - Seeder: `BestelAppBoeken.Infrastructure\Data\DbSeeder.cs` leest voorbeeld JSON voor dev seeding.
  - (Optioneel) API endpoints voor export/import: verwacht in `BestelAppBoeken.Web\Controllers\Api\BackupApiController.cs` (indien aanwezig / geconfigureerd).
- Doelen en gebruik:
  - Seeding: snel vullen van dev/test databases met consistente voorbeelddata (`DbSeeder` leest JSON).
  - Export/import: verplaatsen van order- en klantdata tussen omgevingen of voor handmatig herstel (`orders_export_*.json`).
  - Handmatige backups: portable snapshots voor herstel of forensische analyse.
  - Migratie/opschoning: JSON gebruiken als tussenformaat bij migraties of bulk-correcties.
- Best practices:
  - Geen onversleutelde persoonsgegevens in publieke backups: anonimiseer of versleutel (gebruik secret store / opslag met toegangscontrole).
  - Bewaar backups op beveiligde opslag en beperk toegang via environment-configuratie (`appsettings*.json`).
  - Documenteer backup/restore-procedures en test herstelperiodiek.
- Voor welk doel niet gebruiken:
  - JSON-backups zijn geschikt voor kleine/medium datasets en dev/test. Voor productie en grote datasets, gebruik database-native backups en versiebeheer van schema/migraties.

Technische implementatie (hoog niveau)
- Platform: .NET 10, ASP.NET Core (Razor Pages / Web API), EF Core (`BookstoreDbContext`).
- Messaging & integratie: RabbitMQ (`RabbitMqService`), Salesforce (`SalesforceService` + polling), SAP (`SapService`).
- Architectuur:
  - DI: interfaces in `Core`, implementaties in `Infrastructure`.
  - Controllers → Services → DbContext (scheiding van verantwoordelijkheden).
  - Background worker (WorkerService) voor polling/async taken.
  - Retry/Resilience: Polly of eigen retry voor externe calls.
- Security:
  - Credentials via environment/secrets, niet in repo.
  - Log-masking voor PII (gebruik `GdprCompliantLogger`).

Runnen (snelstart)
1. Configureer `appsettings.json` / `appsettings.Development.json` (DB, RabbitMQ, credentials).
2. Migrations & seeding: EF Core migrations + `DbSeeder` (optioneel JSON seeding).
3. Start webapp: `dotnet run --project BestelAppBoeken.Web`
4. Start worker: `dotnet run --project BestelAppBoeken.Receiver`

Contributie & tests
- Feature-branch → PR met teststappen en beschrijving.
- Voeg unit- en integratietests toe voor services, validatie en error flows.
- Voeg tests voor messaging en DLQ handling toe.

Referenties & documenten in repo
- `DATABASE.md`, `SEED-DATA-GUIDE.md`, `DLQ-IMPLEMENTATION.md`, `SAP-IDOC-INTEGRATION.md`, `BESTELLING-PLAATSEN-FIXED.md`

Opmerking
- Wil je dat ik dit bestand direct probeer aan te maken in de repository? Zeg “maak aan” en ik probeer het nogmaals.
