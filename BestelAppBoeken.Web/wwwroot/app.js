// BestelAppBoeken - Main Application JavaScript
// Deze JS werkt ALLEEN met wwwroot/index.html (het nieuwe dashboard)
// en NIET met de bestaande MVC Views

const API_BASE = '/api';
let winkelmandje = [];
let klanten = [];
let boeken = [];
let orders = [];

// Check of we op de juiste pagina zijn (index.html dashboard)
const isDashboardPage = () => {
    return document.getElementById('klanten-body') !== null;
};

// Initialisatie bij laden van de pagina
document.addEventListener('DOMContentLoaded', () => {
    // Alleen uitvoeren als we op het dashboard zijn
    if (isDashboardPage()) {
        console.log('?? Dashboard gedetecteerd - app.js wordt geïnitialiseerd');
        loadKlanten();
        loadBoeken();
        loadOrders();
        loadBackups(); // ? Load backups on startup
        setupForms();
        setupTableScrollIndicators();
    } else {
        console.log('?? Geen dashboard pagina - app.js wordt niet geladen');
    }
});

// Setup scroll indicators voor tables
function setupTableScrollIndicators() {
    const tableContainers = document.querySelectorAll('.table-container');
    
    tableContainers.forEach(container => {
        // Hide scroll arrow when user scrolls
        container.addEventListener('scroll', function() {
            if (this.scrollLeft > 20) {
                this.classList.add('scrolled');
            } else {
                this.classList.remove('scrolled');
            }
        });
        
        // Check if scrollable on load
        const isScrollable = container.scrollWidth > container.clientWidth;
        if (!isScrollable) {
            // Hide scroll indicator if content fits
            container.classList.add('no-scroll');
        }
    });
}

// Setup event listeners voor formulieren
function setupForms() {
    // Veilig event listeners toevoegen met null checks
    const klantForm = document.getElementById('klant-form');
    const klantModalForm = document.getElementById('klant-modal-form');
    const boekModalForm = document.getElementById('boek-modal-form');
    
    if (klantForm) {
        klantForm.addEventListener('submit', submitKlantForm);
    }
    if (klantModalForm) {
        klantModalForm.addEventListener('submit', submitKlantModalForm);
    }
    if (boekModalForm) {
        boekModalForm.addEventListener('submit', submitBoekModalForm);
    }
}

// ============================================
// API Communicatie Functies
// ============================================

async function apiCall(endpoint, method = 'GET', body = null) {
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json',
        }
    };
    
    if (body) {
        options.body = JSON.stringify(body);
    }
    
    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Er is een fout opgetreden' }));
            throw new Error(error.error || error.message || 'Er is een fout opgetreden');
        }
        
        return await response.json();
    } catch (error) {
        showError(error.message);
        throw error;
    }
}

// ============================================
// KLANTEN - Klantbeheer Functies
// ============================================

async function loadKlanten() {
    try {
        // Echte API call naar backend
        klanten = await apiCall('/klanten');
        
        displayKlanten();
        updateKlantDropdown();
        document.getElementById('klanten-loading').style.display = 'none';
        document.getElementById('klanten-tabel').style.display = 'table';
        updateHeaderStats();
    } catch (error) {
        console.error('Fout bij laden klanten:', error);
        document.getElementById('klanten-loading').innerHTML = 
            '<div class="error">Kon klanten niet laden. Probeer later opnieuw.</div>';
    }
}

function displayKlanten(klantenLijst = klanten) {
    const tbody = document.getElementById('klanten-body');
    
    if (klantenLijst.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; padding: 40px; color: #999;">Geen klanten gevonden</td></tr>';
        return;
    }
    
    tbody.innerHTML = klantenLijst.map(klant => `
        <tr>
            <td>${escapeHtml(klant.naam)}</td>
            <td>${escapeHtml(klant.email)}</td>
            <td>${escapeHtml(klant.telefoon)}</td>
            <td>
                <button class="btn btn-icon btn-bewerken" onclick="bewerkenKlant(${klant.id})" title="Bewerken">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn btn-icon btn-verwijderen" onclick="verwijderenKlant(${klant.id})" title="Verwijderen">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

function updateKlantDropdown() {
    const select = document.getElementById('order-klant');
    select.innerHTML = '<option value="">Selecteer een klant</option>' +
        klanten.map(k => `<option value="${k.id}">${escapeHtml(k.naam)} - ${escapeHtml(k.email)}</option>`).join('');
}

async function submitKlantForm(e) {
    e.preventDefault();
    
    const id = document.getElementById('klant-id').value;
    const klantData = {
        naam: document.getElementById('klant-naam').value.trim(),
        email: document.getElementById('klant-email').value.trim(),
        telefoon: document.getElementById('klant-telefoon').value.trim(),
        adres: document.getElementById('klant-adres').value.trim()
    };
    
    try {
        if (id) {
            // Update bestaande klant
            await apiCall(`/klanten/${id}`, 'PUT', klantData);
            showSuccess('Klant succesvol bijgewerkt');
        } else {
            // Nieuwe klant toevoegen
            await apiCall('/klanten', 'POST', klantData);
            showSuccess('Klant succesvol toegevoegd');
        }
        
        resetKlantForm();
        loadKlanten();
    } catch (error) {
        console.error('Fout bij opslaan klant:', error);
    }
}

function bewerkenKlant(id) {
    const klant = klanten.find(k => k.id === id);
    if (klant) {
        document.getElementById('modal-klant-id').value = klant.id;
        document.getElementById('modal-klant-naam').value = klant.naam;
        document.getElementById('modal-klant-email').value = klant.email;
        document.getElementById('modal-klant-telefoon').value = klant.telefoon;
        document.getElementById('modal-klant-adres').value = klant.adres;
        document.getElementById('klant-modal-title').innerHTML = '<i class="fas fa-user-edit"></i> Klant Bewerken';
        document.getElementById('klant-modal').style.display = 'block';
    }
}

async function verwijderenKlant(id) {
    if (!confirm('Weet u zeker dat u deze klant wilt verwijderen?')) {
        return;
    }
    
    try {
        await apiCall(`/klanten/${id}`, 'DELETE');
        showSuccess('Klant succesvol verwijderd');
        loadKlanten();
    } catch (error) {
        console.error('Fout bij verwijderen klant:', error);
    }
}

function resetKlantForm() {
    document.getElementById('klant-form').reset();
    document.getElementById('klant-id').value = '';
}

function openNieuweKlantModal() {
    document.getElementById('klant-modal-title').innerHTML = '<i class="fas fa-user-plus"></i> Nieuwe Klant';
    document.getElementById('klant-modal-form').reset();
    document.getElementById('modal-klant-id').value = '';
    document.getElementById('klant-modal').style.display = 'block';
}

function closeKlantModal() {
    document.getElementById('klant-modal').style.display = 'none';
}

async function submitKlantModalForm(e) {
    e.preventDefault();
    
    const id = document.getElementById('modal-klant-id').value;
    const klantData = {
        naam: document.getElementById('modal-klant-naam').value.trim(),
        email: document.getElementById('modal-klant-email').value.trim(),
        telefoon: document.getElementById('modal-klant-telefoon').value.trim(),
        adres: document.getElementById('modal-klant-adres').value.trim()
    };
    
    try {
        if (id) {
            await apiCall(`/klanten/${id}`, 'PUT', klantData);
            showSuccess('Klant succesvol bijgewerkt');
        } else {
            await apiCall('/klanten', 'POST', klantData);
            showSuccess('Klant succesvol toegevoegd');
        }
        
        closeKlantModal();
        loadKlanten();
    } catch (error) {
        console.error('Fout bij opslaan klant:', error);
    }
}

// ============================================
// BOEKEN - Boekbeheer Functies
// ============================================

async function loadBoeken() {
    try {
        // Echte API call naar backend
        const boekenData = await apiCall('/books');
        
        // Transform data voor display (API gebruikt Title/Author/Price, JS gebruikt titel/auteur/prijs)
        boeken = boekenData.map(boek => ({
            id: boek.id,
            titel: boek.title,
            auteur: boek.author,
            prijs: boek.price,
            voorraadAantal: boek.voorraadAantal,
            isbn: boek.isbn
        }));
        
        displayBoeken();
        updateBoekDropdown();
        document.getElementById('boeken-loading').style.display = 'none';
        document.getElementById('boeken-tabel').style.display = 'table';
        updateHeaderStats();
    } catch (error) {
        console.error('Fout bij laden boeken:', error);
        document.getElementById('boeken-loading').innerHTML = 
            '<div class="error">Kon boeken niet laden. Probeer later opnieuw.</div>';
    }
}

function displayBoeken(boekenLijst = boeken) {
    const tbody = document.getElementById('boeken-body');
    
    if (boekenLijst.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 40px; color: #999;">Geen boeken gevonden</td></tr>';
        return;
    }
    
    tbody.innerHTML = boekenLijst.map(boek => {
        const voorraadClass = boek.voorraadAantal < 15 ? 'voorraad-laag' : '';
        const voorraadWarning = boek.voorraadAantal < 15 ? '<span class="voorraad-info">(Laag!)</span>' : '';
        
        return `
        <tr>
            <td><strong>${escapeHtml(boek.titel)}</strong></td>
            <td>${escapeHtml(boek.auteur)}</td>
            <td><strong>EUR ${boek.prijs.toFixed(2)}</strong></td>
            <td class="${voorraadClass}">
                ${boek.voorraadAantal} ${voorraadWarning}
            </td>
            <td><small>${escapeHtml(boek.isbn)}</small></td>
            <td>
                <button class="btn btn-icon btn-bewerken" onclick="bewerkenBoek(${boek.id})" title="Bewerken">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn btn-icon btn-verwijderen" onclick="verwijderenBoek(${boek.id})" title="Verwijderen">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `}).join('');
}

function updateBoekDropdown() {
    const select = document.getElementById('order-boek');
    select.innerHTML = '<option value="">Selecteer een boek</option>' +
        boeken.map(b => `
            <option value="${b.id}">
                ${escapeHtml(b.titel)} - EUR ${b.prijs.toFixed(2)} (Voorraad: ${b.voorraadAantal})
            </option>
        `).join('');
}

function bewerkenBoek(id) {
    const boek = boeken.find(b => b.id === id);
    if (boek) {
        document.getElementById('modal-boek-id').value = boek.id;
        document.getElementById('modal-boek-titel').value = boek.titel;
        document.getElementById('modal-boek-auteur').value = boek.auteur;
        document.getElementById('modal-boek-prijs').value = boek.prijs;
        document.getElementById('modal-boek-voorraad').value = boek.voorraadAantal;
        document.getElementById('modal-boek-isbn').value = boek.isbn;
        document.getElementById('boek-modal-title').innerHTML = '<i class="fas fa-book"></i> Boek Bewerken';
        document.getElementById('boek-modal').style.display = 'block';
    }
}

async function verwijderenBoek(id) {
    if (!confirm('Weet u zeker dat u dit boek wilt verwijderen?')) {
        return;
    }
    
    try {
        await apiCall(`/books/${id}`, 'DELETE');
        showSuccess('Boek succesvol verwijderd');
        loadBoeken();
    } catch (error) {
        console.error('Fout bij verwijderen boek:', error);
    }
}

function openNieuwBoekModal() {
    document.getElementById('boek-modal-title').innerHTML = '<i class="fas fa-book-medical"></i> Nieuw Boek';
    document.getElementById('boek-modal-form').reset();
    document.getElementById('modal-boek-id').value = '';
    document.getElementById('boek-modal').style.display = 'block';
}

function closeBoekModal() {
    document.getElementById('boek-modal').style.display = 'none';
}

async function submitBoekModalForm(e) {
    e.preventDefault();
    
    const id = document.getElementById('modal-boek-id').value;
    const boekData = {
        title: document.getElementById('modal-boek-titel').value.trim(),
        author: document.getElementById('modal-boek-auteur').value.trim(),
        price: parseFloat(document.getElementById('modal-boek-prijs').value),
        voorraadAantal: parseInt(document.getElementById('modal-boek-voorraad').value),
        isbn: document.getElementById('modal-boek-isbn').value.trim()
    };
    
    // Validatie
    if (boekData.price <= 0) {
        showError('Prijs moet groter zijn dan 0');
        return;
    }
    
    if (boekData.voorraadAantal < 0) {
        showError('Voorraad kan niet negatief zijn');
        return;
    }
    
    try {
        if (id) {
            await apiCall(`/books/${id}`, 'PUT', boekData);
            showSuccess('Boek succesvol bijgewerkt');
        } else {
            await apiCall('/books', 'POST', boekData);
            showSuccess('Boek succesvol toegevoegd');
        }
        
        closeBoekModal();
        loadBoeken();
    } catch (error) {
        console.error('Fout bij opslaan boek:', error);
    }
}

// ============================================
// WINKELMANDJE - Functies
// ============================================

function toevoegenAanWinkelmandje() {
    const boekId = parseInt(document.getElementById('order-boek').value);
    const aantal = parseInt(document.getElementById('order-aantal').value);
    
    if (!boekId || isNaN(boekId)) {
        showError('Selecteer een boek');
        return;
    }
    
    if (!aantal || aantal < 1) {
        showError('Voer een geldig aantal in (minimaal 1)');
        return;
    }
    
    const boek = boeken.find(b => b.id === boekId);
    if (!boek) {
        showError('Boek niet gevonden');
        return;
    }
    
    // Check voorraad
    const huidigAantalInMandje = winkelmandje
        .filter(item => item.boekId === boekId)
        .reduce((sum, item) => sum + item.aantal, 0);
    
    if (huidigAantalInMandje + aantal > boek.voorraadAantal) {
        showError(`Onvoldoende voorraad. Beschikbaar: ${boek.voorraadAantal - huidigAantalInMandje}`);
        return;
    }
    
    // Toevoegen aan winkelmandje
    const bestaandItem = winkelmandje.find(item => item.boekId === boekId);
    if (bestaandItem) {
        bestaandItem.aantal += aantal;
    } else {
        winkelmandje.push({
            boekId: boekId,
            titel: boek.titel,
            auteur: boek.auteur,
            prijs: boek.prijs,
            aantal: aantal
        });
    }
    
    displayWinkelmandje();
    document.getElementById('order-aantal').value = 1;
    showSuccess(`<i class="fas fa-check-circle"></i> ${boek.titel} (${aantal}x) toegevoegd aan winkelmandje`);
}

function displayWinkelmandje() {
    const container = document.getElementById('winkelmandje-items');
    
    if (winkelmandje.length === 0) {
        container.innerHTML = `
            <p style="color: #999; text-align: center; padding: 20px;">
                <i class="fas fa-shopping-cart" style="font-size: 24px; opacity: 0.5; display: block; margin-bottom: 10px;"></i>
                Geen items in winkelmandje
            </p>
        `;
        document.getElementById('totaal-bedrag').innerHTML = '<i class="fas fa-euro-sign"></i> Totaal: EUR 0.00';
        return;
    }
    
    container.innerHTML = winkelmandje.map((item, index) => `
        <div class="cart-item">
            <div>
                <strong>${escapeHtml(item.titel)}</strong><br>
                <small style="color: #666;">${escapeHtml(item.auteur)}</small><br>
                <small>EUR ${item.prijs.toFixed(2)} × ${item.aantal} = <strong>EUR ${(item.prijs * item.aantal).toFixed(2)}</strong></small>
            </div>
            <button class="btn btn-icon btn-verwijderen" onclick="verwijderenUitWinkelmandje(${index})" title="Verwijderen">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `).join('');
    
    const totaal = winkelmandje.reduce((sum, item) => sum + (item.prijs * item.aantal), 0);
    const aantalItems = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);
    
    document.getElementById('totaal-bedrag').innerHTML = `
        <i class="fas fa-euro-sign"></i> Totaal: EUR ${totaal.toFixed(2)} 
        <small style="font-size: 14px; font-weight: normal;">(${aantalItems} item${aantalItems !== 1 ? 's' : ''})</small>
    `;
}

function verwijderenUitWinkelmandje(index) {
    const item = winkelmandje[index];
    if (confirm(`${item.titel} verwijderen uit winkelmandje?`)) {
        winkelmandje.splice(index, 1);
        displayWinkelmandje();
        showSuccess('Item verwijderd uit winkelmandje');
    }
}

// ============================================
// ORDERS - Bestelling Functies
// ============================================

async function loadOrders() {
    try {
        // Echte API call naar backend
        const ordersData = await apiCall('/orders');
        
        // Transform data voor display
        orders = ordersData.map(order => ({
            id: order.id,
            orderNummer: `ORD-${order.id}`,
            klant: order.klant || { naam: 'Onbekend', email: order.customerEmail },
            orderDatum: new Date(order.orderDate),
            totaalBedrag: order.totalAmount,
            aantalItems: order.items.reduce((sum, item) => sum + item.aantal, 0),
            status: order.status,
            items: order.items.map(item => ({
                boekId: item.boekId,
                titel: item.titel,
                aantal: item.aantal,
                prijs: item.prijs,
                subtotaal: item.prijs * item.aantal
            }))
        }));
        
        displayOrders();
        document.getElementById('bestellingen-loading').style.display = 'none';
        document.getElementById('bestellingen-tabel').style.display = 'table';
        updateHeaderStats();
    } catch (error) {
        console.error('Fout bij laden orders:', error);
        document.getElementById('bestellingen-loading').innerHTML = 
            '<div class="error">Kon bestellingen niet laden. Probeer later opnieuw.</div>';
    }
}

function displayOrders() {
    const tbody = document.getElementById('bestellingen-body');
    
    if (orders.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 40px; color: #999;">Nog geen bestellingen</td></tr>';
        return;
    }
    
    tbody.innerHTML = orders.map(order => {
        const klantNaam = order.klant ? escapeHtml(order.klant.naam) : 'Onbekend';
        const statusClass = order.status === 'Verwerkt' ? 'status-verwerkt' : 'status-nieuw';
        
        return `
        <tr>
            <td><strong>${escapeHtml(order.orderNummer)}</strong></td>
            <td>${klantNaam}</td>
            <td>${order.aantalItems} item(s)</td>
            <td>${formatDate(order.orderDatum)}</td>
            <td><strong>EUR ${order.totaalBedrag.toFixed(2)}</strong></td>
            <td>
                <button class="btn btn-primary btn-icon" onclick="toonOrderDetails(${order.id})" title="Bekijk details">
                    <i class="fas fa-eye"></i> Details
                </button>
            </td>
        </tr>
    `}).join('');
}

async function plaatsOrder() {
    const klantId = parseInt(document.getElementById('order-klant').value);
    
    if (!klantId || isNaN(klantId)) {
        showError('Selecteer een klant');
        return;
    }
    
    if (winkelmandje.length === 0) {
        showError('Winkelmandje is leeg');
        return;
    }
    
    const klant = klanten.find(k => k.id === klantId);
    if (!klant) {
        showError('Klant niet gevonden');
        return;
    }
    
    const orderData = {
        klantId: klantId,
        items: winkelmandje.map(item => ({
            boekId: item.boekId,
            aantal: item.aantal
        }))
    };
    
    try {
        // Toon loading state
        const bestelButton = document.querySelector('button[onclick="plaatsOrder()"]');
        const originalText = bestelButton.innerHTML;
        bestelButton.disabled = true;
        bestelButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Bestelling wordt verwerkt...';
        
        // Echte API call naar backend
        const result = await apiCall('/orders', 'POST', orderData);
        
        const totaalBedrag = result.totalAmount;
        const orderNummer = `ORD-${result.id}`;
        
        showSuccess(`
            <div style="text-align: left;">
                <strong style="font-size: 16px;"><i class="fas fa-check-circle"></i> Bestelling succesvol geplaatst!</strong><br><br>
                <strong>Order nummer:</strong> ${orderNummer}<br>
                <strong>Klant:</strong> ${klant.naam}<br>
                <strong>Totaal:</strong> EUR ${totaalBedrag.toFixed(2)}<br>
                <strong>Status:</strong> ${result.status}<br><br>
                <em style="font-size: 12px; color: #666;">
                    <i class="fas fa-info-circle"></i> De bestelling is via RabbitMQ verstuurd naar Salesforce en SAP R/3
                </em>
            </div>
        `);
        
        // Reset winkelmandje
        winkelmandje = [];
        displayWinkelmandje();
        
        // Herlaad data om updates te tonen (ZONDER page refresh!)
        await loadOrders();
        await loadBoeken(); // Voorraad is bijgewerkt
        
        // Reset klant selectie
        document.getElementById('order-klant').value = '';
        
        // Reset button
        bestelButton.disabled = false;
        bestelButton.innerHTML = originalText;
        
        // Scroll naar bestellingen sectie om nieuwe bestelling te zien
        setTimeout(() => {
            const bestellingenSectie = document.getElementById('bestellingen-sectie');
            if (bestellingenSectie) {
                bestellingenSectie.scrollIntoView({ 
                    behavior: 'smooth', 
                    block: 'start' 
                });
                
                // Highlight effect voor de bestellingen sectie
                bestellingenSectie.style.transition = 'box-shadow 0.5s';
                bestellingenSectie.style.boxShadow = '0 0 20px rgba(102, 126, 234, 0.5)';
                setTimeout(() => {
                    bestellingenSectie.style.boxShadow = '';
                }, 2000);
            }
        }, 500);
        
    } catch (error) {
        console.error('Fout bij plaatsen order:', error);
        showError('Er is een fout opgetreden bij het plaatsen van de bestelling');
        
        // Reset button bij error
        const bestelButton = document.querySelector('button[onclick="plaatsOrder()"]');
        bestelButton.disabled = false;
        bestelButton.innerHTML = '<i class="fas fa-check-circle"></i> Bestelling Plaatsen';
    }
}

async function toonOrderDetails(orderId) {
    try {
        // TODO: Vervang met echte API call
        // const order = await apiCall(`/orders/${orderId}`);
        
        const order = orders.find(o => o.id === orderId);
        if (!order) {
            showError('Bestelling niet gevonden');
            return;
        }
        
        const statusClass = order.status === 'Verwerkt' ? 'status-verwerkt' : 'status-nieuw';
        
        const content = `
            <div style="line-height: 1.8;">
                <p><strong><i class="fas fa-receipt"></i> Order Nummer:</strong> ${escapeHtml(order.orderNummer)}</p>
                <p><strong><i class="fas fa-calendar"></i> Datum:</strong> ${formatDate(order.orderDatum)}</p>
                <p><strong><i class="fas fa-info-circle"></i> Status:</strong> <span class="status-badge ${statusClass}">${escapeHtml(order.status)}</span></p>
                <p><strong><i class="fas fa-user"></i> Klant:</strong> ${escapeHtml(order.klant.naam)} (${escapeHtml(order.klant.email)})</p>
                <p><strong><i class="fas fa-cloud"></i> Salesforce ID:</strong> ${escapeHtml(order.salesforceId || 'N/A')}</p>
                <p><strong><i class="fas fa-cogs"></i> SAP Status:</strong> ${escapeHtml(order.sapStatus || 'N/A')}</p>
                
                <h3 style="margin-top: 25px; margin-bottom: 15px; color: var(--primary);">
                    <i class="fas fa-shopping-bag"></i> Bestelde Items:
                </h3>
                <table style="width: 100%; margin-top: 10px; border-collapse: collapse;">
                    <thead style="background: linear-gradient(135deg, var(--primary), var(--secondary)); color: white;">
                        <tr>
                            <th style="padding: 12px; text-align: left; border-radius: 8px 0 0 0;">Boek</th>
                            <th style="padding: 12px; text-align: center;">Aantal</th>
                            <th style="padding: 12px; text-align: right;">Prijs</th>
                            <th style="padding: 12px; text-align: right; border-radius: 0 8px 0 0;">Subtotaal</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${order.items.map(item => `
                            <tr style="border-bottom: 1px solid var(--border);">
                                <td style="padding: 12px;"><strong>${escapeHtml(item.titel)}</strong></td>
                                <td style="padding: 12px; text-align: center;">${item.aantal}</td>
                                <td style="padding: 12px; text-align: right;">EUR ${item.prijs.toFixed(2)}</td>
                                <td style="padding: 12px; text-align: right;"><strong>EUR ${item.subtotaal.toFixed(2)}</strong></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
                
                <div class="totaal-bedrag" style="margin-top: 20px;">
                    <i class="fas fa-euro-sign"></i> Totaal: EUR ${order.totaalBedrag.toFixed(2)}
                </div>
                
                <div style="margin-top: 20px; padding: 15px; background: linear-gradient(135deg, #e6f7ff, #bae7ff); border-radius: 10px; font-size: 13px;">
                    <i class="fas fa-info-circle" style="color: #0050b3;"></i> 
                    <strong>Integratie Status:</strong><br>
                    Deze bestelling is automatisch gesynchroniseerd met Salesforce en SAP R/3 via RabbitMQ message queue.
                </div>
            </div>
        `;
        
        document.getElementById('order-details-content').innerHTML = content;
        document.getElementById('order-details-modal').style.display = 'block';
    } catch (error) {
        console.error('Fout bij laden order details:', error);
        showError('Kon order details niet laden');
    }
}

function closeOrderDetailsModal() {
    document.getElementById('order-details-modal').style.display = 'none';
}

// ============================================
// UTILITY FUNCTIES
// ============================================

function showSuccess(message) {
    const container = document.getElementById('message-container');
    container.innerHTML = `<div class="success"><i class="fas fa-check-circle"></i> ${message}</div>`;
    
    // Scroll naar top
    window.scrollTo({ top: 0, behavior: 'smooth' });
    
    setTimeout(() => {
        container.innerHTML = '';
    }, 6000);
}

function showError(message) {
    const container = document.getElementById('message-container');
    container.innerHTML = `<div class="error"><i class="fas fa-exclamation-circle"></i> ${message}</div>`;
    
    // Scroll naar top
    window.scrollTo({ top: 0, behavior: 'smooth' });
    
    setTimeout(() => {
        container.innerHTML = '';
    }, 6000);
}

function escapeHtml(unsafe) {
    if (!unsafe) return '';
    return unsafe
        .toString()
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function formatDate(date) {
    if (!date) return 'N/A';
    
    const d = new Date(date);
    if (isNaN(d.getTime())) return 'Ongeldige datum';
    
    return d.toLocaleString('nl-NL', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function updateHeaderStats() {
    setTimeout(() => {
        const orderCount = orders.length;
        const klantenCount = klanten.length;
        const boekenCount = boeken.length;
        
        document.getElementById('stat-orders').textContent = `${orderCount} Order${orderCount !== 1 ? 's' : ''}`;
        document.getElementById('stat-klanten').textContent = `${klantenCount} Klant${klantenCount !== 1 ? 'en' : ''}`;
        document.getElementById('stat-boeken').textContent = `${boekenCount} Boek${boekenCount !== 1 ? 'en' : ''}`;
    }, 100);
}

// ============================================
// DATABASE BACKUP FUNCTIES
// ============================================

async function createBackup() {
    try {
        showSuccess('? Backup wordt aangemaakt...');
        const response = await apiCall('/backup/create', 'POST');
        
        if (response.success) {
            showSuccess(`? Backup succesvol aangemaakt: ${response.fileName}`);
            loadBackups();
        }
    } catch (error) {
        showError('Fout bij aanmaken backup: ' + error.message);
    }
}

async function loadBackups() {
    try {
        const response = await apiCall('/backup/list');
        const container = document.getElementById('backup-list-container');
        
        if (!response.backups || response.backups.length === 0) {
            container.innerHTML = `
                <div style="text-align: center; padding: 40px; color: var(--gray);">
                    <i class="fas fa-database" style="font-size: 48px; opacity: 0.3; margin-bottom: 15px;"></i>
                    <p style="font-size: 16px;">Nog geen backups beschikbaar</p>
                    <p style="font-size: 14px; margin-top: 10px;">Klik op "Maak Backup" om je eerste backup aan te maken</p>
                </div>
            `;
            return;
        }
        
        container.innerHTML = `
            <div style="margin-bottom: 15px; padding: 12px; background: var(--light); border-radius: 8px; display: flex; align-items: center; justify-content: space-between;">
                <div>
                    <i class="fas fa-check-circle" style="color: var(--success); margin-right: 8px;"></i>
                    <strong>${response.count}</strong> backup${response.count !== 1 ? 's' : ''} gevonden
                </div>
                <div style="font-size: 12px; color: var(--gray);">
                    <i class="fas fa-shield-alt"></i> Alle backups blijven permanent bewaard
                </div>
            </div>
            <div class="table-container">
                <table style="width: 100%;">
                    <thead>
                        <tr>
                            <th style="width: 50%;"><i class="fas fa-file"></i> Bestandsnaam</th>
                            <th style="width: 25%;"><i class="fas fa-clock"></i> Datum/Tijd</th>
                            <th style="width: 25%; text-align: center;"><i class="fas fa-tools"></i> Acties</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${response.backups.map((backup, index) => `
                            <tr style="${index % 2 === 0 ? 'background: var(--light);' : ''}">
                                <td>
                                    <i class="fas fa-database" style="color: var(--primary); margin-right: 8px;"></i>
                                    <code style="font-size: 12px;">${escapeHtml(backup.fileName)}</code>
                                </td>
                                <td>
                                    <i class="fas fa-calendar-alt" style="margin-right: 5px; color: var(--info);"></i>
                                    ${escapeHtml(backup.formattedDate)}
                                </td>
                                <td style="text-align: center;">
                                    <button class="btn btn-icon btn-primary" onclick="restoreBackup('${backup.fileName}')" title="Herstel database">
                                        <i class="fas fa-undo"></i>
                                    </button>
                                    <button class="btn btn-icon btn-success" onclick="downloadBackup('${backup.fileName}')" title="Download backup">
                                        <i class="fas fa-download"></i>
                                    </button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
    } catch (error) {
        const container = document.getElementById('backup-list-container');
        container.innerHTML = `<div class="error"><i class="fas fa-exclamation-triangle"></i> Fout bij laden backups: ${escapeHtml(error.message)}</div>`;
    }
}

async function restoreBackup(fileName) {
    if (!confirm(`?? WAARSCHUWING: Database Herstellen\n\nWeet je zeker dat je de database wilt herstellen naar:\n\n"${fileName}"\n\n? Voordeel: Automatisch wordt eerst een veiligheidsbackup gemaakt van de huidige database!\n\n? Let op: Alle huidige data wordt overschreven met de geselecteerde backup!\n\nDoorgaan?`)) {
        return;
    }
    
    try {
        showSuccess('? Database wordt hersteld... Even geduld...');
        const response = await apiCall('/backup/restore', 'POST', { fileName });
        
        if (response.success) {
            showSuccess(`? Database succesvol hersteld van: ${fileName}\n\n?? Pagina wordt herladen om nieuwe data te tonen...`);
            
            // Reload page after 3 seconds
            setTimeout(() => {
                window.location.reload();
            }, 3000);
        }
    } catch (error) {
        showError('? Fout bij herstellen backup: ' + error.message);
    }
}

async function downloadBackup(fileName) {
    try {
        showSuccess(`?? Backup wordt gedownload: ${fileName}`);
        
        // Open download in new window
        const downloadUrl = `/api/backup/download/${encodeURIComponent(fileName)}`;
        window.open(downloadUrl, '_blank');
        
        setTimeout(() => {
            showSuccess(`? Download gestart: ${fileName}`);
        }, 500);
    } catch (error) {
        showError('Fout bij downloaden backup: ' + error.message);
    }
}

// ============================================
// BESTELLINGEN EXPORT FUNCTIES
// ============================================

async function exportOrdersJson() {
    try {
        showSuccess('?? Bestellingen worden geëxporteerd naar JSON...');
        
        // Direct download trigger
        const exportUrl = '/api/backup/export/orders/json';
        window.open(exportUrl, '_blank');
        
        setTimeout(() => {
            showSuccess('? JSON export gestart! Controleer je downloads folder.');
        }, 500);
    } catch (error) {
        showError('Fout bij exporteren naar JSON: ' + error.message);
    }
}

async function exportOrdersTxt() {
    try {
        showSuccess('?? Bestellingen worden geëxporteerd naar TXT...');
        
        // Direct download trigger
        const exportUrl = '/api/backup/export/orders/txt';
        window.open(exportUrl, '_blank');
        
        setTimeout(() => {
            showSuccess('? TXT export gestart! Controleer je downloads folder.');
        }, 500);
    } catch (error) {
        showError('Fout bij exporteren naar TXT: ' + error.message);
    }
}

// Close modals wanneer buiten geklikt wordt
window.onclick = function(event) {
    if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
    }
}

// Close modals met ESC toets
document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        document.querySelectorAll('.modal').forEach(modal => {
            modal.style.display = 'none';
        });
    }
});

// Console log voor development
console.log('%c?? BestelAppBoeken Dashboard geladen', 'color: #667eea; font-size: 16px; font-weight: bold;');
console.log('%c?? Integraties: RabbitMQ + Salesforce + SAP R/3', 'color: #48bb78; font-size: 12px;');
