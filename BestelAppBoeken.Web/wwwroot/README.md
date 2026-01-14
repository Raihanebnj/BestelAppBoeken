# BestelAppBoeken Dashboard

## ?? Overzicht

Dit project gebruikt een **modern static HTML dashboard** als hoofdinterface.

### **Modern Dashboard** (Hoofd Interface) ?
- **Route**: `/` (root URL - automatisch geladen)
- **Bestanden**:
  - `wwwroot/index.html`
  - `wwwroot/app.js`
  - `wwwroot/search-functions.js`
- **Functionaliteit**: 
  - ? Volledig klantenbeheer (CRUD)
  - ? Boekenbeheer met voorraadcontrole
  - ? Order management
  - ? Winkelmandje functionaliteit
  - ? Zoekfuncties voor klanten en boeken
  - ? Real-time statistieken
  - ? Responsive design (smartphone-ready)

### **MVC Views** (Optioneel - Voor referentie)
De originele MVC views zijn nog beschikbaar voor referentie:
- **Routes**: 
  - `/Home/Index` - Boeken catalogus
  - `/Home/Order` - Bestelformulier
  - `/Home/OrderConfirmation` - Bevestigingspagina
- **Bestanden**: 
  - `Views/Home/Index.cshtml`
  - `Views/Home/Order.cshtml`
  - `Views/Home/OrderConfirmation.cshtml`
- **Backend**: HomeController.cs

## ?? Gebruik

### Toegang tot het Dashboard

1. **Start de applicatie**:
   ```bash
   cd BestelAppBoeken.Web
   dotnet run
   ```

2. **Open in browser**:
   ```
   https://localhost:5001/
   ```
   
   Het dashboard (`index.html`) wordt **automatisch** geladen! ??

## ?? Bestandsstructuur

```
BestelAppBoeken.Web/
??? wwwroot/
?   ??? index.html            # ? HOOFD DASHBOARD (wordt automatisch geladen)
?   ??? app.js                # Dashboard JavaScript functionaliteit
?   ??? search-functions.js   # Zoekfuncties voor dashboard
?   ??? README.md             # Deze file
??? Views/
?   ??? Home/                 # Originele MVC views (optioneel)
?       ??? Index.cshtml      
?       ??? Order.cshtml      
?       ??? OrderConfirmation.cshtml
??? Controllers/
?   ??? HomeController.cs     # MVC Controller (optioneel)
??? Program.cs                # ? Geconfigureerd voor static index.html
```

## ?? Technische Details

### Dashboard Features

? **Klantenbeheer**
- Klanten toevoegen, bewerken, verwijderen
- Zoeken op naam, email, telefoon

? **Boekenbeheer**
- Boeken toevoegen, bewerken, verwijderen
- Voorraadcontrole en waarschuwingen
- Zoeken op titel, auteur, ISBN

? **Bestellingen**
- Winkelmandje functionaliteit
- Orders plaatsen
- Order geschiedenis bekijken
- Integratie met RabbitMQ, Salesforce, SAP R/3

? **UI/UX**
- Modern gradient design
- Responsive (smartphone-ready)
- Font Awesome icons
- Real-time statistieken

### Conflictpreventie

De `app.js` bevat een **page detection** mechanisme:

```javascript
const isDashboardPage = () => {
    return document.getElementById('klanten-body') !== null;
};
```

Dit zorgt ervoor dat de dashboard JavaScript **alleen** wordt uitgevoerd op `dashboard.html` en niet interfereert met de MVC Views.

## ?? Toekomstige Uitbreidingen

### API Integratie

Om het dashboard te verbinden met echte backend API's:

1. **Maak API Controllers aan**:
   ```csharp
   // Controllers/Api/KlantenController.cs
   [ApiController]
   [Route("api/[controller]")]
   public class KlantenController : ControllerBase
   {
       // GET: api/klanten
       [HttpGet]
       public IActionResult GetAll() { ... }
       
       // POST: api/klanten
       [HttpPost]
       public IActionResult Create([FromBody] Klant klant) { ... }
       
       // PUT: api/klanten/{id}
       [HttpPut("{id}")]
       public IActionResult Update(int id, [FromBody] Klant klant) { ... }
       
       // DELETE: api/klanten/{id}
       [HttpDelete("{id}")]
       public IActionResult Delete(int id) { ... }
   }
   ```

2. **Activeer API calls in app.js**:
   
   Uncomment de API calls en verwijder mock data:
   ```javascript
   // Verander dit:
   // klanten = [mock data];
   
   // Naar dit:
   klanten = await apiCall('/klanten');
   ```

3. **Database integratie**:
   ```csharp
   // Program.cs
   builder.Services.AddDbContext<BoekenDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

## ?? Aanpassingen

### Kleuren aanpassen

In `dashboard.html`, pas de CSS variabelen aan:

```css
:root {
    --primary: #667eea;      /* Hoofdkleur */
    --secondary: #764ba2;     /* Secundaire kleur */
    --success: #48bb78;       /* Success kleur */
    --danger: #f56565;        /* Danger kleur */
    --warning: #ed8936;       /* Warning kleur */
}
```

### Extra functies toevoegen

Voeg functies toe in `app.js` of `search-functions.js`:

```javascript
// Voorbeeld: Filter boeken op prijsrange
function filterBoekenByPrice(min, max) {
    const filtered = boeken.filter(b => b.prijs >= min && b.prijs <= max);
    displayBoeken(filtered);
}
```

## ?? Opmerkingen

- De dashboard interface is **volledig standalone** en vereist geen wijzigingen aan bestaande MVC code
- Beide interfaces kunnen naast elkaar draaien zonder conflicten
- Mock data wordt gebruikt totdat API endpoints worden geïmplementeerd
- Alle integraties (RabbitMQ, Salesforce, SAP) zijn voorbereid in de code structuur

## ?? Support

Voor vragen of problemen:
- Email: support@bookstore@ehb.be
- Locatie: Nijverheidskaai 170, 1070 Brussel

---

**Powered by RabbitMQ + Salesforce + SAP R/3**
