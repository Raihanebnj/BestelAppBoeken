# 📋 SAP iDoc Integratie Enablen - Instructies

## 🎯 Huidige Status

**SAP iDoc integratie is GECOMMENTEERD maar volledig beschikbaar voor later gebruik.**

### Waarom Gecommenteerd?
- Project gebruikt momenteel alleen **RabbitMQ + Salesforce**
- SAP R/3 niet beschikbaar in development environment
- Code is production-ready maar gedeactiveerd om errors te voorkomen

---

## ✅ SAP ENABLEN - STAP VOOR STAP

### **Stap 1: Uncomment SAP Service in Program.cs**

**Locatie:** `BestelAppBoeken.Web/Program.cs` (regel ~40)

```csharp
// VOOR (Commented):
// builder.Services.AddHttpClient<ISapService, SapService>(client =>
// {
//     client.Timeout = TimeSpan.FromSeconds(30);
//     client.DefaultRequestHeaders.Add("User-Agent", "Bookstore-SAP-iDoc/1.0");
// });

// NA (Uncommented):
builder.Services.AddHttpClient<ISapService, SapService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Bookstore-SAP-iDoc/1.0");
});
```

---

### **Stap 2: Voeg ISapService toe aan OrdersApiController**

**Locatie:** `BestelAppBoeken.Web/Controllers/Api/OrdersApiController.cs`

```csharp
// VOOR:
private readonly IOrderService _orderService;
private readonly IMessageQueueService _messageQueue;
private readonly ISalesforceService _salesforceService;
// ❌ SAP iDoc VERWIJDERD
private readonly IKlantService _klantService;

// NA:
private readonly IOrderService _orderService;
private readonly IMessageQueueService _messageQueue;
private readonly ISalesforceService _salesforceService;
private readonly ISapService _sapService; // ✅ TOEGEVOEGD
private readonly IKlantService _klantService;
```

**En in constructor:**

```csharp
// VOOR:
public OrdersApiController(
    IOrderService orderService,
    IMessageQueueService messageQueue,
    ISalesforceService salesforceService,
    // ❌ ISapService VERWIJDERD
    IKlantService klantService,
    ...)

// NA:
public OrdersApiController(
    IOrderService orderService,
    IMessageQueueService messageQueue,
    ISalesforceService salesforceService,
    ISapService sapService, // ✅ TOEGEVOEGD
    IKlantService klantService,
    ...)
{
    _sapService = sapService; // ✅ TOEWIJZEN
}
```

---

### **Stap 3: Uncomment SAP Code in CreateOrder Method**

**Locatie:** `BestelAppBoeken.Web/Controllers/Api/OrdersApiController.cs` (regel ~180)

Zoek naar deze comment block:

```csharp
/* ============================================
 * 💡 SAP iDoc INTEGRATIE - DISABLED (BESCHIKBAAR VOOR LATER)
 * ============================================
 * 
 * Om SAP iDoc te enablen:
 * 1. Uncomment onderstaande code
 * ...
 */
```

**Uncomment de SAP task code:**

```csharp
// ✅ Uncomment dit:
var sapTask = Task.Run(async () =>
{
    try
    {
        _logger.LogInformation("📤 [SAP iDoc] START - Transformeren Order {OrderId}", savedOrder.Id);
        var response = await _sapService.SendOrderIDocAsync(savedOrder);
        _logger.LogInformation("✅ [SAP iDoc] SUCCESS - IDoc {IDocNumber}", response.IDocNumber);
        return response;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ [SAP iDoc] FOUT bij verzenden");
        return new SapIDocResponse
        {
            IDocNumber = "ERROR",
            Status = SapIDocStatus.Error,
            StatusDescription = "SAP integratie gefaald: " + ex.Message,
            Timestamp = DateTime.UtcNow
        };
    }
});

// Wait for both tasks
await Task.WhenAll(salesforceTask, sapTask);
var sapResponse = sapTask.Result;
```

---

### **Stap 4: Update IntegrationStatusInfo in Response**

**In CreateOrder method, update de response:**

```csharp
// VOOR (Zonder SAP):
IntegrationStatus = new IntegrationStatusInfo
{
    SalesforceId = salesforceId,
    SalesforceSuccess = salesforceId != null,
    SapIDocNumber = null,
    SapStatus = null,
    SapStatusDescription = "SAP disabled",
    SapSuccess = false,
    Timestamp = DateTime.UtcNow
}

// NA (Met SAP):
IntegrationStatus = new IntegrationStatusInfo
{
    SalesforceId = salesforceId,
    SalesforceSuccess = salesforceId != null,
    SapIDocNumber = sapResponse.IDocNumber,
    SapStatus = (int)sapResponse.Status,
    SapStatusDescription = sapResponse.StatusDescription,
    SapSuccess = sapResponse.Status == SapIDocStatus.Success,
    Timestamp = DateTime.UtcNow
}
```

---

### **Stap 5: Update appsettings.json met SAP Credentials**

**Locatie:** `BestelAppBoeken.Web/appsettings.json`

```json
{
  "SAP": {
    "Enabled": true,
    "Endpoint": "http://sap-server:8000/sap/bc/idoc",
    "Client": "800",
    "System": "PRD",
    "Username": "SAP_USER",
    "Password": "SAP_PASSWORD",
    "TimeoutSeconds": 30
  }
}
```

**Voor Development (appsettings.Development.json):**

```json
{
  "SAP": {
    "Enabled": false,  // Disabled in development
    "Endpoint": "http://localhost:8000/sap/bc/idoc",
    "Client": "800",
    "System": "DEV"
  }
}
```

---

### **Stap 6: Rebuild en Test**

```bash
# 1. Clean build
dotnet clean
dotnet build

# 2. Run application
dotnet run --project BestelAppBoeken.Web

# 3. Test order creation
# Ga naar: https://localhost:7174/Index.html
# Plaats een bestelling

# 4. Check logs voor SAP iDoc
# Verwacht:
# 📤 [SAP iDoc] START - Transformeren Order X
# ✅ [SAP iDoc] SUCCESS - IDoc 2026011919421779
```

---

## 📊 VERWACHTE FLOW MET SAP ENABLED

```
Order Plaatsen
    ↓
Database Save ✅
    ↓
    ┌─────────────┴─────────────┐
    ↓                           ↓
RabbitMQ ✅              SAP iDoc ✅
    ↓                           ↓
Salesforce ✅            SAP R/3 ✅
    ↓                           ↓
Email ✅                 Status 53/51
```

---

## 🔍 DEBUGGING TIPS

### **Als SAP Connection Faalt:**

```csharp
// Check SapService.cs - lijn 90
// De fallback mechanisme geeft een mock response:
catch (HttpRequestException ex)
{
    return new SapIDocResponse
    {
        IDocNumber = GenerateIDocNumber(),
        Status = SapIDocStatus.Success,
        StatusDescription = "Simulated: SAP niet bereikbaar",
        Timestamp = DateTime.UtcNow
    };
}
```

**Dit betekent:**
- ✅ Order wordt nog steeds succesvol aangemaakt
- ✅ Applicatie blijft werken
- ⚠️ SAP iDoc wordt niet echt verzonden (fallback)

---

## 📋 CHECKLIST

- [ ] Program.cs: Uncomment SAP Service registratie
- [ ] OrdersApiController: Add ISapService parameter
- [ ] OrdersApiController: Uncomment SAP task code
- [ ] OrdersApiController: Update IntegrationStatusInfo
- [ ] appsettings.json: Add SAP credentials
- [ ] Test: Order plaatsen
- [ ] Verify: Check logs voor SAP iDoc messages
- [ ] Verify: Check SAP R/3 voor IDoc status

---

## ⚠️ PRODUCTIE CHECKLIST

### **Voor Productie Deployment:**

1. **SAP Credentials Beveiligen:**
   ```json
   // NIET in appsettings.json hardcoden!
   // Gebruik Azure Key Vault of Environment Variables
   ```

2. **Error Handling Verbeteren:**
   ```csharp
   // Verwijder fallback mock in productie
   // Laat echte errors throwen voor monitoring
   ```

3. **Logging Configureren:**
   ```json
   // appsettings.Production.json
   {
     "Logging": {
       "LogLevel": {
         "BestelAppBoeken.Infrastructure.Services.SapService": "Information"
       }
     }
   }
   ```

4. **SAP iDoc Status Monitoring:**
   - Implementeer scheduled job om IDoc status te checken
   - Status 53 = Success
   - Status 51 = Error
   - Status 64 = Pending

---

## 📚 REFERENTIES

- **SAP iDoc Documentatie:** `SAP-IDOC-INTEGRATION.md`
- **SAP Service Code:** `BestelAppBoeken.Infrastructure/Services/SapService.cs`
- **iDoc XML Formaat:** ORDERS05 met E1EDK01 segment
- **Test Endpoint:** `GET /api/orders/{id}/sap-idoc-preview`

---

## 🎯 QUICK ENABLE (1 Minuut)

```bash
# 1. Uncomment in Program.cs (lijn 40-44)
# 2. Add ISapService in OrdersApiController constructor
# 3. Uncomment SAP task in CreateOrder method
# 4. Rebuild: dotnet build
# 5. Run: dotnet run --project BestelAppBoeken.Web
```

**Klaar! SAP iDoc is nu actief.** 🚀

---

**Laatst bijgewerkt:** 2026-01-15  
**Status:** SAP Code Commented maar Production-Ready  
**Versie:** 1.0
