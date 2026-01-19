# ?? Nieuwe Frontend Structuur - Overzicht

## ?? Doel
De frontend is nu opgesplitst in aparte, gespecialiseerde pagina's voor betere gebruikservaring en minder scrolling.

---

## ?? Pagina Structuur

### 1. **Index.html** (Dashboard - Hoofdpagina)
**URL**: `/Index.html`

**Bevat:**
- ? **Navigatiemenu** ? Links naar alle andere pagina's
- ? **Recente Bestellingen** ? Overzicht van laatste orders
- ? **Bestel een boek** ? Quick order formulier
  - Klant selectie dropdown
  - Boek selectie dropdown
  - Aantal invoer
  - Toevoegen aan winkelmandje
  - Winkelmandje samenvatting
- ? **Database Backup & Export** ? Backup functies

**Verwijderd van Index:**
- ? Klantenbeheer (? nu op customers.html)
- ? Boekenlijst (? nu op books.html)
- ? Winkelmandje details (? nu op cart.html)

---

### 2. **orders.html** (Alle Bestellingen)
**URL**: `/orders.html`

**Bevat:**
- ?? **Statistieken Dashboard**
  - Totaal aantal orders
  - Totale omzet
  - Pending orders
  - Voltooide orders
- ?? **Complete bestellingenlijst**
  - Order nummer
  - Klantinformatie
  - Datum & tijd
  - Aantal items
  - Totaalbedrag
  - Status badge
- ??? **Order Details Modal**
  - Volledige order informatie
  - Klantgegevens
  - Itemlijst met prijzen
  - Totaal overzicht

**JavaScript**: `orders.js`

---

### 3. **books.html** (Boekenbeheer)
**URL**: `/books.html`

**Bevat:**
- ?? **Complete boekenlijst**
  - ID, Titel, Auteur
  - Prijs, Voorraad, ISBN
  - Voorraad waarschuwing (rood bij < 15)
- ?? **Zoekfunctionaliteit**
  - Zoeken op titel, auteur of ISBN
  - Real-time resultaten
- ? **Boek toevoegen/bewerken**
  - Modal dialog
  - Formulier validatie
  - CRUD operaties

**JavaScript**: `books.js`

---

### 4. **customers.html** (Klantenbeheer)
**URL**: `/customers.html`

**Bevat:**
- ?? **Complete klantenlijst**
  - ID, Naam, Email, Telefoon, Adres
- ?? **Zoekfunctionaliteit**
  - Zoeken op naam, email, telefoon of adres
- ? **Klant toevoegen/bewerken**
  - Modal dialog
  - CRUD operaties

**JavaScript**: `customers.js`

---

### 5. **cart.html** (Winkelmandje & Checkout)
**URL**: `/cart.html`

**Bevat:**
- ?? **Winkelmandje items**
  - Item naam, auteur, prijs
  - Aantal aanpassen (+/- knoppen)
  - Verwijderen optie
- ?? **Bestelling samenvatting**
  - Subtotaal
  - Aantal items
  - Totaalbedrag
- ?? **Checkout proces**
  - Klant selectie
  - Order plaatsen
  - Automatische emailbevestiging

**JavaScript**: `cart.js`

---

## ?? Navigatie Structuur

```
???????????????????????????????????????????????
?           Index.html (Dashboard)             ?
?  - Navigatiemenu                            ?
?  - Recente bestellingen                     ?
?  - Bestel een boek                          ?
?  - Backup & Export                          ?
???????????????????????????????????????????????
                  ?
        ???????????????????????????????
        ?         ?         ?         ?
    ????????? ???????? ????????? ????????
    ?Orders ? ?Books ? ?Custo- ? ?Cart  ?
    ?.html  ? ?.html ? ?mers   ? ?.html ?
    ?       ? ?      ? ?.html  ? ?      ?
    ????????? ???????? ????????? ????????
```

---

## ?? Navigatiemenu (op alle pagina's)

```html
???????????????????????????????????????????????
?  [?? Alle Bestellingen] [?? Boekenbeheer]   ?
?  [?? Klantenbeheer] [?? Winkelmandje (2)]  ?
???????????????????????????????????????????????
```

**Knoppen:**
1. **Alle Bestellingen** ? `orders.html`
2. **Boekenbeheer** ? `books.html`
3. **Klantenbeheer** ? `customers.html`
4. **Winkelmandje** ? `cart.html` (met counter badge)

---

## ?? Data Flow

### Bestelling Plaatsen
```
Index.html
   ? (Selecteer klant + boek)
   ? (Toevoegen aan winkelmandje)
   ? (localStorage)
cart.html
   ? (Klant bevestigen)
   ? (POST /api/orders)
   ?
Backend
   ?? Database opslaan
   ?? RabbitMQ publiceren
   ?? Salesforce sync
   ?? SAP integratie
   ?? ?? Email bevestiging
```

### Cart Synchronisatie
```
localStorage 'winkelmandje'
   ?
   ?? Index.html (quick view)
   ?? cart.html (volledige view)
   ?? Navigatie (counter badge)
```

---

## ?? Technische Details

### localStorage Schema
```javascript
// winkelmandje array in localStorage
[
  {
    id: 1,              // boekId
    titel: "Book Title",
    auteur: "Author",
    prijs: 19.99,
    aantal: 2
  },
  ...
]
```

### API Endpoints Gebruikt

**Index.html:**
- `GET /api/klanten` - Laad klanten voor dropdown
- `GET /api/books` - Laad boeken voor dropdown
- `GET /api/orders` - Laad recente orders

**orders.html:**
- `GET /api/orders` - Laad alle bestellingen

**books.html:**
- `GET /api/books` - Laad alle boeken
- `POST /api/books` - Nieuw boek toevoegen
- `PUT /api/books/{id}` - Boek bijwerken
- `DELETE /api/books/{id}` - Boek verwijderen

**customers.html:**
- `GET /api/klanten` - Laad alle klanten
- `POST /api/klanten` - Nieuwe klant toevoegen
- `PUT /api/klanten/{id}` - Klant bijwerken
- `DELETE /api/klanten/{id}` - Klant verwijderen

**cart.html:**
- `GET /api/klanten` - Laad klanten voor checkout
- `POST /api/orders` - Plaats bestelling

---

## ? Key Features

### 1. Persistent Shopping Cart
- Cart data blijft bewaard in browser localStorage
- Werkt cross-page (Index ? Cart)
- Real-time counter update in navigatie

### 2. Email Bevestiging
- Automatisch verzonden na succesvolle bestelling
- HTML template met bedrijfsbranding
- Order details en totaalbedrag
- Logging voor tracking

### 3. Responsive Design
- Mobile-first benadering
- Touch-friendly knoppen (min 44x44px)
- Horizontal scroll voor tabellen op mobile
- Hamburger menu friendly layout

### 4. Moderne UI/UX
- Consistente gradient achtergrond
- Card-based design
- Smooth transitions en hover effects
- Font Awesome iconen
- Status badges met kleuren

---

## ?? Responsive Breakpoints

```css
Desktop:   > 1024px  ? Volledige layout
Tablet:    ? 1024px  ? 2-kolom grid
Tablet:    ? 768px   ? Single column
Mobile:    ? 480px   ? Compact design
Mobile:    ? 360px   ? Extra compact
```

---

## ?? Gebruikersinstructies

### Voor Verkopers:

**1. Nieuwe Bestelling Plaatsen:**
1. Ga naar Dashboard (Index.html)
2. Scroll naar "Bestel een boek"
3. Selecteer klant en boek
4. Klik "Toevoegen aan winkelmandje"
5. Klik "Ga naar Winkelmandje & Bestel"
6. Controleer items en klik "Bestelling Plaatsen"
7. ? Bevestigingsmail wordt automatisch verzonden

**2. Boeken Beheren:**
1. Klik "Boekenbeheer" in navigatie
2. Zoek boeken met zoekbalk
3. Klik "Nieuw Boek" om toe te voegen
4. Klik edit/delete icons om aan te passen

**3. Klanten Beheren:**
1. Klik "Klantenbeheer" in navigatie
2. Zoek klanten indien nodig
3. Klik "Nieuwe Klant" om toe te voegen
4. Bewerk of verwijder bestaande klanten

**4. Bestellingen Bekijken:**
1. Klik "Alle Bestellingen" in navigatie
2. Zie statistieken bovenaan
3. Klik "Details" voor volledige order info

---

## ?? Checklist voor Testing

### Desktop:
- [ ] Navigatie werkt naar alle pagina's
- [ ] Cart counter update werkt
- [ ] Bestelling plaatsen werkt
- [ ] Email wordt gelogd
- [ ] CRUD operaties werken op alle pagina's
- [ ] Zoekfuncties werken
- [ ] Modals openen/sluiten correct

### Mobile:
- [ ] Responsive layout op alle pagina's
- [ ] Touch-friendly buttons
- [ ] Tabellen scrollen horizontaal
- [ ] Forms zijn bruikbaar
- [ ] Navigatie is toegankelijk

### Cross-Page:
- [ ] Cart blijft bewaard na page refresh
- [ ] Cart counter sync tussen pagina's
- [ ] localStorage werkt correct

---

## ?? Toekomstige Uitbreidingen

**Mogelijk toe te voegen:**
1. **Search & Filters**
   - Geavanceerde filters per pagina
   - Sort functionaliteit
   - Date range filters voor orders

2. **Dashboard Statistics**
   - Grafieken en charts
   - Real-time analytics
   - Sales trends

3. **User Management**
   - Login/logout functionaliteit
   - Rol-gebaseerde toegang
   - User profiles

4. **Export Functies**
   - CSV export
   - PDF rapporten
   - Excel export

---

## ?? Support

**Voor vragen:**
- Email: support@bookstore@ehb.be
- Locatie: Nijverheidskaai 170, 1070 Brussel

---

**Laatste Update**: Januari 2024
**Versie**: 3.0 (Modular Structure)
**Ontwikkeld door**: Bookstore Development Team
