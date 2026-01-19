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
    return document.getElementById('bestellingen-body') !== null;
};

// Load cart from localStorage
function loadCartFromStorage() {
    const stored = localStorage.getItem('winkelmandje');
    if (stored) {
        try {
            winkelmandje = JSON.parse(stored);
        } catch (e) {
            console.error('Error loading cart:', e);
            winkelmandje = [];
        }
    }
}

// Save cart to localStorage
function saveCartToStorage() {
    localStorage.setItem('winkelmandje', JSON.stringify(winkelmandje));
    updateCartCounter();
}

// Update cart counter in navigation
function updateCartCounter() {
    const cartCount = document.getElementById('cart-count');
    const cartItemCount = document.getElementById('cart-item-count');
    const total = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);
    
    if (cartCount) {
        if (total > 0) {
            cartCount.textContent = total;
            cartCount.style.display = 'flex';
        } else {
            cartCount.style.display = 'none';
        }
    }
    
    if (cartItemCount) {
        cartItemCount.textContent = `${total} item${total !== 1 ? 's' : ''}`;
    }
    
    updateCartSummary();
}

// Update cart summary on main page
function updateCartSummary() {
    const quickView = document.getElementById('winkelmandje-quick-view');
    if (!quickView) return;
    
    if (winkelmandje.length === 0) {
        quickView.innerHTML = `
            <p style="color: #999; text-align: center; padding: 20px;">
                <i class="fas fa-shopping-cart" style="font-size: 24px; opacity: 0.5; display: block; margin-bottom: 10px;"></i>
                Geen items in winkelmandje
            </p>
        `;
        return;
    }
    
    const total = winkelmandje.reduce((sum, item) => sum + (item.prijs * item.aantal), 0);
    const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);
    
    quickView.innerHTML = `
        <div style="background: var(--light); padding: 15px; border-radius: 8px; margin-bottom: 10px;">
            <div style="display: flex; justify-content: space-between; margin-bottom: 10px;">
                <strong><i class="fas fa-shopping-bag"></i> Aantal items:</strong>
                <span>${itemCount}</span>
            </div>
            <div style="display: flex; justify-content: space-between; font-size: 18px; color: var(--success); font-weight: 700;">
                <strong><i class="fas fa-euro-sign"></i> Totaal:</strong>
                <span>EUR ${total.toFixed(2)}</span>
            </div>
        </div>
        <div style="font-size: 12px; color: var(--gray);">
            ${winkelmandje.slice(0, 3).map(item => 
                `• ${escapeHtml(item.titel)} (${item.aantal}x)`
            ).join('<br>')}
            ${winkelmandje.length > 3 ? `<br>... en ${winkelmandje.length - 3} meer` : ''}
        </div>
    `;
}

// Initialisatie bij laden van de pagina
document.addEventListener('DOMContentLoaded', () => {
    // Alleen uitvoeren als we op het dashboard zijn
    if (isDashboardPage()) {
        console.log('📚 Dashboard gedetecteerd - app.js wordt geïnitialiseerd');
        loadCartFromStorage();
        updateCartCounter();
        loadKlanten();
        loadBoeken();
        loadBoekenMenu(); // Load boeken menu voor snelle bestelling
        loadOrders();
        loadBackups(); // 🔄 Load backups on startup
        setupForms();
        setupTableScrollIndicators();
    } else {
        console.log('❌ Geen dashboard pagina - app.js wordt niet geladen');
    }
});

// Setup scroll indicators voor tables
function setupTableScrollIndicators() {
    const tableContainers = document.querySelectorAll('.table-container');

    tableContainers.forEach(container => {
        // Hide scroll arrow when user scrolls
        container.addEventListener('scroll', function () {
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
    console.log('🔄 [INDEX] Laden van klanten...');
    klanten = await apiCall('/klanten');

    console.log('✓ [INDEX] Klanten geladen:', klanten.length, 'klanten');
    if (klanten.length > 0) {
        console.log('Eerste klant:', klanten[0]);
    }

    displayKlanten();
    updateKlantDropdown();
    updateQuickStats();
        
        // Check if elements exist before updating
        const klantenLoading = document.getElementById('klanten-loading');
        const klantenTabel = document.getElementById('klanten-tabel');
        
        if (klantenLoading) {
            klantenLoading.style.display = 'none';
        }
        if (klantenTabel) {
            klantenTabel.style.display = 'table';
        }
        
        updateHeaderStats();
    } catch (error) {
        console.error('Fout bij laden klanten:', error);
        const klantenLoading = document.getElementById('klanten-loading');
        if (klantenLoading) {
            klantenLoading.innerHTML = '<div class="error">Kon klanten niet laden. Probeer later opnieuw.</div>';
        }
    }
}

function displayKlanten(klantenLijst = klanten) {
    const tbody = document.getElementById('klanten-body');
    
    // Only display if element exists (not on index page)
    if (!tbody) {
        console.log('Klanten tabel niet gevonden - wordt overgeslagen (normaal op index page)');
        return;
    }

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
    
    if (!select) {
        console.log('Klant dropdown niet gevonden - wordt overgeslagen');
        return;
    }
    
    console.log(`Updating klant dropdown met ${klanten.length} klanten`);
    
    select.innerHTML = '<option value="">Kies een klant om een bestelling te plaatsen</option>' +
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

// Open Quick Klant Modal (from winkelmandje)
function openQuickKlantModal() {
    document.getElementById('klant-modal-title').innerHTML = '<i class="fas fa-user-plus"></i> Nieuwe Klant Aanmaken';
    document.getElementById('klant-modal-form').reset();
    document.getElementById('modal-klant-id').value = '';
    document.getElementById('klant-modal').style.display = 'block';
    
    // Show helpful message
    showSuccess('💡 Vul de klantgegevens in. Na opslaan wordt de klant automatisch geselecteerd!');
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
        let newKlantId = null;
        
        if (id) {
            await apiCall(`/klanten/${id}`, 'PUT', klantData);
            showSuccess('✓ Klant succesvol bijgewerkt');
            newKlantId = parseInt(id);
        } else {
            const result = await apiCall('/klanten', 'POST', klantData);
            showSuccess(`✓ Klant "${klantData.naam}" succesvol toegevoegd!`);
            newKlantId = result.id;
        }

        closeKlantModal();
        await loadKlanten(); // Reload klanten list
        
        // Auto-select the new/updated customer in dropdown
        if (newKlantId) {
            const orderKlantSelect = document.getElementById('order-klant');
            if (orderKlantSelect) {
                orderKlantSelect.value = newKlantId;
                
                // Visual feedback
                orderKlantSelect.style.borderColor = 'var(--success)';
                orderKlantSelect.style.boxShadow = '0 0 0 3px rgba(72, 187, 120, 0.2)';
                
                setTimeout(() => {
                    orderKlantSelect.style.borderColor = '';
                    orderKlantSelect.style.boxShadow = '';
                }, 2000);
                
                // Show confirmation
                showSuccess(`🎉 Klant "${klantData.naam}" is nu geselecteerd! Voeg boeken toe aan je winkelmandje.`);
            }
        }
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
        loadBoekenMenu(); // Load boeken menu cards
        updateQuickStats(); // Update quick stats cards
        
        // Check if elements exist before updating
        const boekenLoading = document.getElementById('boeken-loading');
        const boekenTabel = document.getElementById('boeken-tabel');
        
        if (boekenLoading) {
            boekenLoading.style.display = 'none';
        }
        if (boekenTabel) {
            boekenTabel.style.display = 'table';
        }
        
        updateHeaderStats();
    } catch (error) {
        console.error('Fout bij laden boeken:', error);
        const boekenLoading = document.getElementById('boeken-loading');
        if (boekenLoading) {
            boekenLoading.innerHTML = '<div class="error">Kon boeken niet laden. Probeer later opnieuw.</div>';
        }
    }
}

function displayBoeken(boekenLijst = boeken) {
    const tbody = document.getElementById('boeken-body');
    
    // Only display if element exists (not on index page)
    if (!tbody) {
        console.log('Boeken tabel niet gevonden - wordt overgeslagen (normaal op index page)');
        return;
    }

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
    
    if (!select) {
        console.log('Boek dropdown niet gevonden - wordt overgeslagen');
        return;
    }
    
    console.log(`Updating boek dropdown met ${boeken.length} boeken`);
    
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

// Load Boeken Menu (Simple grid view for quick ordering)
async function loadBoekenMenu() {
    try {
        const boekenData = await apiCall('/books');
        
        const boekenMenu = boekenData.map(boek => ({
            id: boek.id,
            titel: boek.title,
            auteur: boek.author,
            prijs: boek.price,
            voorraadAantal: boek.voorraadAantal,
            isbn: boek.isbn
        }));
        
        displayBoekenMenu(boekenMenu);
        
        const boekenLoading = document.getElementById('boeken-loading');
        if (boekenLoading) {
            boekenLoading.style.display = 'none';
        }
    } catch (error) {
        console.error('Fout bij laden boeken menu:', error);
        const boekenLoading = document.getElementById('boeken-loading');
        if (boekenLoading) {
            boekenLoading.innerHTML = '<div class="error"><i class="fas fa-exclamation-circle"></i> Kon boeken niet laden</div>';
        }
    }
}

function displayBoekenMenu(boekenLijst) {
    const container = document.getElementById('boeken-menu-grid');
    const dropdown = document.getElementById('boeken-dropdown');
    
    if (!container || !dropdown) {
        console.log('Boeken menu containers niet gevonden');
        return;
    }
    
    if (boekenLijst.length === 0) {
        container.innerHTML = '<p style="text-align: center; color: var(--gray); padding: 40px;">Geen boeken beschikbaar</p>';
        dropdown.innerHTML = '<option value="">Geen boeken beschikbaar</option>';
        return;
    }
    
    // Populate Grid View (Compact Cards)
    container.innerHTML = boekenLijst.map(boek => {
        const stockClass = boek.voorraadAantal < 15 ? 'low' : '';
        const stockIcon = boek.voorraadAantal < 15 ? 
            '<i class="fas fa-exclamation-triangle"></i>' : 
            '<i class="fas fa-check-circle"></i>';
        
        return `
            <div class="boek-card-compact" onclick="selectBoekFromCard(${boek.id})" style="background: white; border: 2px solid var(--border); border-radius: 10px; padding: 15px; cursor: pointer; transition: all 0.3s;">
                <div style="font-weight: 700; font-size: 14px; color: var(--dark); margin-bottom: 5px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${escapeHtml(boek.titel)}</div>
                <div style="font-size: 12px; color: var(--gray); margin-bottom: 10px;">${escapeHtml(boek.auteur)}</div>
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                    <div style="font-size: 20px; font-weight: 700; color: var(--success);">€${boek.prijs.toFixed(2)}</div>
                    <div style="font-size: 11px; color: ${boek.voorraadAantal < 15 ? 'var(--danger)' : 'var(--success)'};">${stockIcon} ${boek.voorraadAantal}</div>
                </div>
                <button class="btn btn-primary" onclick="event.stopPropagation(); quickAddToCart(${boek.id})" style="width: 100%; padding: 8px; font-size: 13px;">
                    <i class="fas fa-cart-plus"></i> Toevoegen
                </button>
            </div>
        `;
    }).join('');
    
    // Populate Dropdown
    dropdown.innerHTML = '<option value="">-- Kies een boek --</option>' + 
        boekenLijst.map(boek => {
            const voorraadText = boek.voorraadAantal < 15 ? 
                `⚠️ Laag (${boek.voorraadAantal})` : 
                `✓ ${boek.voorraadAantal}`;
            
            return `<option value="${boek.id}" data-book='${JSON.stringify(boek)}'>
                ${escapeHtml(boek.titel)} - ${escapeHtml(boek.auteur)} | €${boek.prijs.toFixed(2)} | ${voorraadText}
            </option>`;
        }).join('');
    
    // Setup dropdown change handler
    dropdown.removeEventListener('change', handleDropdownChangeSimple);
    dropdown.addEventListener('change', handleDropdownChangeSimple);
}

function selectBoekForOrder(boekId) {
    // Deprecated - gebruik quickAddToCart of selectBoekFromCard
    quickAddToCart(boekId);
}

function quickAddToCart(boekId) {
    const boek = boeken.find(b => b.id === boekId);
    if (!boek) return;
    
    // Check if already in cart
    const bestaandItem = winkelmandje.find(item => item.id === boekId);
    if (bestaandItem) {
        if (bestaandItem.aantal + 1 > boek.voorraadAantal) {
            showError(`Maximum voorraad bereikt (${boek.voorraadAantal})`);
            return;
        }
        bestaandItem.aantal += 1;
    } else {
        winkelmandje.push({
            id: boekId,
            titel: boek.titel,
            auteur: boek.auteur,
            prijs: boek.prijs,
            aantal: 1
        });
    }
    
    saveCartToStorage();
    showSuccess(`✓ ${boek.titel} toegevoegd aan winkelmandje`);
}

// Toggle tussen Grid en Dropdown view
function toggleBoekenView() {
    const gridView = document.getElementById('boeken-menu-grid');
    const dropdownView = document.getElementById('boeken-dropdown-view');
    const toggleBtn = document.getElementById('toggle-view-btn');
    
    if (!gridView || !dropdownView) return;
    
    if (gridView.style.display === 'none') {
        // Switch to Grid View
        gridView.style.display = 'grid';
        dropdownView.style.display = 'none';
        toggleBtn.innerHTML = '<i class="fas fa-list"></i> Wissel Weergave';
    } else {
        // Switch to Dropdown View
        gridView.style.display = 'none';
        dropdownView.style.display = 'block';
        toggleBtn.innerHTML = '<i class="fas fa-th"></i> Wissel Weergave';
    }
}

// Simple dropdown handler for combined view
function handleDropdownChangeSimple(event) {
    const selectedOption = event.target.options[event.target.selectedIndex];
    const previewContainer = document.getElementById('selected-book-preview');
    
    if (!previewContainer) return;
    
    if (!selectedOption.value) {
        previewContainer.style.display = 'none';
        return;
    }
    
    try {
        const boek = JSON.parse(selectedOption.getAttribute('data-book'));
        displaySimpleBookPreview(boek);
    } catch (error) {
        console.error('Error parsing book data:', error);
    }
}

// Display simple book preview
function displaySimpleBookPreview(boek) {
    const previewContainer = document.getElementById('selected-book-preview');
    if (!previewContainer) return;
    
    const stockClass = boek.voorraadAantal < 15 ? 'color: var(--danger);' : 'color: var(--success);';
    const stockIcon = boek.voorraadAantal < 15 ? 
        '<i class="fas fa-exclamation-triangle"></i>' : 
        '<i class="fas fa-check-circle"></i>';
    
    previewContainer.innerHTML = `
        <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 15px;">
            <div style="flex: 1; min-width: 200px;">
                <div style="font-size: 18px; font-weight: 700; color: var(--dark); margin-bottom: 5px;">${escapeHtml(boek.titel)}</div>
                <div style="font-size: 14px; color: var(--gray); margin-bottom: 10px;">${escapeHtml(boek.auteur)}</div>
                <div style="font-size: 13px; ${stockClass} font-weight: 600;">
                    ${stockIcon} Voorraad: ${boek.voorraadAantal}
                </div>
            </div>
            <div style="text-align: right;">
                <div style="font-size: 32px; font-weight: 700; color: var(--success); margin-bottom: 10px;">
                    €${boek.prijs.toFixed(2)}
                </div>
                <button class="btn btn-success" onclick="quickAddToCart(${boek.id})" style="padding: 10px 20px;">
                    <i class="fas fa-cart-plus"></i> Direct Toevoegen
                </button>
            </div>
        </div>
    `;
    
    previewContainer.style.display = 'block';
}

// Select book from card click
function selectBoekFromCard(boekId) {
    // Update aantal input
    const aantalInput = document.getElementById('order-aantal');
    if (aantalInput) {
        aantalInput.value = 1;
    }
    
    // Scroll to cart summary
    const cartSummary = document.getElementById('winkelmandje-summary');
    if (cartSummary) {
        cartSummary.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
    
    // Add to cart immediately
    quickAddToCart(boekId);
}

// Handle dropdown selection (OLD - keep for compatibility)
function handleDropdownChange(event) {
    handleDropdownChangeSimple(event);
}

// Display selected book details (OLD - keep for compatibility)
function displaySelectedBookDetails(boek) {
    displaySimpleBookPreview(boek);
}

// ===================================================
// VIEW SWITCHING - Bestellen vs Overzicht
// ===================================================

// View switching functions
function switchToBestellenView() {
    const bestellenView = document.getElementById('bestellen-view');
    const overzichtView = document.getElementById('overzicht-view');
    const btnBestellen = document.getElementById('btn-bestellen-view');
    const btnOverzicht = document.getElementById('btn-overzicht-view');
    
    if (!bestellenView || !overzichtView) return;
    
    bestellenView.style.display = 'block';
    overzichtView.style.display = 'none';
    
    if (btnBestellen) {
        btnBestellen.style.opacity = '1';
        btnBestellen.style.transform = 'scale(1.05)';
    }
    if (btnOverzicht) {
        btnOverzicht.style.opacity = '0.7';
        btnOverzicht.style.transform = 'scale(1)';
    }
}

function switchToOverzichtView() {
    const bestellenView = document.getElementById('bestellen-view');
    const overzichtView = document.getElementById('overzicht-view');
    const btnBestellen = document.getElementById('btn-bestellen-view');
    const btnOverzicht = document.getElementById('btn-overzicht-view');
    
    if (!bestellenView || !overzichtView) return;
    
    bestellenView.style.display = 'none';
    overzichtView.style.display = 'block';
    
    if (btnOverzicht) {
        btnOverzicht.style.opacity = '1';
        btnOverzicht.style.transform = 'scale(1.05)';
    }
    if (btnBestellen) {
        btnBestellen.style.opacity = '0.7';
        btnBestellen.style.transform = 'scale(1)';
    }
}

function toggleDetailView() {
    const simpleView = document.getElementById('boeken-simple-view');
    const detailView = document.getElementById('boeken-detail-view');
    const toggleBtn = document.getElementById('toggle-detail-btn');
    
    if (!simpleView || !detailView) return;
    
    if (simpleView.style.display === 'none') {
        simpleView.style.display = 'block';
        detailView.style.display = 'none';
        if (toggleBtn) toggleBtn.innerHTML = '<i class="fas fa-th-large"></i> Details';
    } else {
        simpleView.style.display = 'none';
        detailView.style.display = 'block';
        if (toggleBtn) toggleBtn.innerHTML = '<i class="fas fa-list"></i> Lijst';
    }
}

// ===================================================
// BOEKEN DISPLAY FUNCTIONS
// ===================================================

let boekenOverzichtData = [];

// Update loadBoekenMenu to populate both views
async function loadBoekenMenu() {
    try {
        const boekenData = await apiCall('/books');
        
        const boekenMenu = boekenData.map(boek => ({
            id: boek.id,
            titel: boek.title,
            auteur: boek.author,
            prijs: boek.price,
            voorraadAantal: boek.voorraadAantal,
            isbn: boek.isbn
        }));
        
        boekenOverzichtData = boekenMenu;
        
        // Populate Bestellen view
        displayBoekenQuickGrid(boekenMenu);
        
        // Populate Overzicht view
        displayBoekenSimpleView(boekenMenu);
        displayBoekenDetailView(boekenMenu);
        
        // Hide loading indicators
        const quickLoading = document.getElementById('boeken-quick-loading');
        const overzichtLoading = document.getElementById('boeken-overzicht-loading');
        
        if (quickLoading) quickLoading.style.display = 'none';
        if (overzichtLoading) overzichtLoading.style.display = 'none';
        
        // Setup search
        setupBoekenSearch();
    } catch (error) {
        console.error('Fout bij laden boeken menu:', error);
    }
}

// Quick Grid for Bestellen View (compact, fast add)
function displayBoekenQuickGrid(boekenLijst) {
    const container = document.getElementById('boeken-quick-grid');
    if (!container) return;
    
    if (boekenLijst.length === 0) {
        container.innerHTML = '<p style="text-align: center; color: var(--gray); padding: 20px; grid-column: 1/-1;">Geen boeken beschikbaar</p>';
        return;
    }
    
    container.innerHTML = boekenLijst.map(boek => `
        <div onclick="quickAddToCart(${boek.id})" style="background: white; border: 2px solid ${boek.voorraadAantal < 15 ? 'var(--danger)' : 'var(--border)'}; border-radius: 10px; padding: 12px; cursor: pointer; transition: all 0.2s; text-align: center;" onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'">
            <div style="font-weight: 700; font-size: 13px; margin-bottom: 5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title="${escapeHtml(boek.titel)}">${escapeHtml(boek.titel)}</div>
            <div style="font-size: 11px; color: var(--gray); margin-bottom: 8px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${escapeHtml(boek.auteur)}</div>
            <div style="font-size: 18px; font-weight: 700; color: var(--success); margin-bottom: 8px;">€${boek.prijs.toFixed(2)}</div>
            <div style="font-size: 10px; color: ${boek.voorraadAantal < 15 ? 'var(--danger)' : 'var(--success)'}; font-weight: 600;">
                ${boek.voorraadAantal < 15 ? '⚠️' : '✓'} ${boek.voorraadAantal}
            </div>
        </div>
    `).join('');
}

// Simple List View for Overzicht
function displayBoekenSimpleView(boekenLijst) {
    const container = document.getElementById('boeken-list-container');
    if (!container) return;
    
    if (boekenLijst.length === 0) {
        container.innerHTML = '<p style="text-align: center; color: var(--gray); padding: 40px;">Geen boeken gevonden</p>';
        return;
    }
    
    container.innerHTML = boekenLijst.map(boek => {
        const stockColor = boek.voorraadAantal < 15 ? 'var(--danger)' : 'var(--success)';
        const stockIcon = boek.voorraadAantal < 15 ? 'fa-exclamation-triangle' : 'fa-check-circle';
        
        return `
            <div style="background: white; border: 2px solid var(--border); border-radius: 10px; padding: 15px; display: flex; justify-content: space-between; align-items: center; gap: 15px; transition: all 0.3s;" onmouseover="this.style.borderColor='var(--primary)'" onmouseout="this.style.borderColor='var(--border)'">
                <div style="flex: 1; min-width: 0;">
                    <div style="font-weight: 700; font-size: 16px; color: var(--dark); margin-bottom: 3px;">${escapeHtml(boek.titel)}</div>
                    <div style="font-size: 13px; color: var(--gray);">${escapeHtml(boek.auteur)}</div>
                </div>
                <div style="text-align: center; min-width: 80px;">
                    <div style="font-size: 20px; font-weight: 700; color: var(--success);">€${boek.prijs.toFixed(2)}</div>
                    <div style="font-size: 11px; color: ${stockColor};"><i class="fas ${stockIcon}"></i> ${boek.voorraadAantal}</div>
                </div>
                <button class="btn btn-primary" onclick="quickAddToCart(${boek.id})" style="padding: 10px 20px; white-space: nowrap;">
                    <i class="fas fa-cart-plus"></i> Toevoegen
                </button>
            </div>
        `;
    }).join('');
}

// Detailed Card View for Overzicht
function displayBoekenDetailView(boekenLijst) {
    const container = document.getElementById('boeken-detail-container');
    if (!container) return;
    
    if (boekenLijst.length === 0) {
        container.innerHTML = '<p style="text-align: center; color: var(--gray); padding: 40px; grid-column: 1/-1;">Geen boeken gevonden</p>';
        return;
    }
    
    container.innerHTML = boekenLijst.map(boek => {
        const stockColor = boek.voorraadAantal < 15 ? 'var(--danger)' : 'var(--success)';
        const stockIcon = boek.voorraadAantal < 15 ? 'fa-exclamation-triangle' : 'fa-check-circle';
        
        return `
            <div style="background: white; border: 2px solid var(--border); border-radius: 12px; padding: 20px; transition: all 0.3s;" onmouseover="this.style.transform='translateY(-5px)'; this.style.boxShadow='var(--shadow-lg)'" onmouseout="this.style.transform=''; this.style.boxShadow=''">
                <div style="margin-bottom: 15px;">
                    <div style="font-weight: 700; font-size: 18px; color: var(--dark); margin-bottom: 5px;">${escapeHtml(boek.titel)}</div>
                    <div style="font-size: 14px; color: var(--gray); font-style: italic;">door ${escapeHtml(boek.auteur)}</div>
                </div>
                <div style="margin: 15px 0; padding: 15px; background: var(--light); border-radius: 8px;">
                    <div style="display: flex; justify-content: space-between; margin-bottom: 10px;">
                        <span style="color: var(--gray); font-size: 13px;"><i class="fas fa-barcode"></i> ISBN</span>
                        <code style="font-size: 12px;">${escapeHtml(boek.isbn)}</code>
                    </div>
                    <div style="display: flex; justify-content: space-between; margin-bottom: 10px;">
                        <span style="color: var(--gray); font-size: 13px;"><i class="fas fa-boxes"></i> Voorraad</span>
                        <span style="font-size: 13px; color: ${stockColor}; font-weight: 600;"><i class="fas ${stockIcon}"></i> ${boek.voorraadAantal} stuks</span>
                    </div>
                    <div style="display: flex; justify-content: space-between;">
                        <span style="color: var(--gray); font-size: 13px;"><i class="fas fa-euro-sign"></i> Prijs</span>
                        <span style="font-size: 20px; font-weight: 700; color: var(--success);">€${boek.prijs.toFixed(2)}</span>
                    </div>
                </div>
                <button class="btn btn-primary" onclick="quickAddToCart(${boek.id})" style="width: 100%; padding: 12px; font-size: 15px;">
                    <i class="fas fa-cart-plus"></i> Toevoegen aan Winkelmandje
                </button>
            </div>
        `;
    }).join('');
}

// Setup search functionality
function setupBoekenSearch() {
    const searchInput = document.getElementById('search-boeken-input');
    if (!searchInput) return;
    
    searchInput.addEventListener('input', (e) => {
        const query = e.target.value.toLowerCase().trim();
        const filtered = boekenOverzichtData.filter(b => 
            b.titel.toLowerCase().includes(query) ||
            b.auteur.toLowerCase().includes(query)
        );
        displayBoekenSimpleView(filtered);
        displayBoekenDetailView(filtered);
    });
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
        .filter(item => item.id === boekId)
        .reduce((sum, item) => sum + item.aantal, 0);

    if (huidigAantalInMandje + aantal > boek.voorraadAantal) {
        showError(`Onvoldoende voorraad. Beschikbaar: ${boek.voorraadAantal - huidigAantalInMandje}`);
        return;
    }

    // Toevoegen aan winkelmandje
    const bestaandItem = winkelmandje.find(item => item.id === boekId);
    if (bestaandItem) {
        bestaandItem.aantal += aantal;
    } else {
        winkelmandje.push({
            id: boekId,
            titel: boek.titel,
            auteur: boek.auteur,
            prijs: boek.prijs,
            aantal: aantal
        });
    }

    saveCartToStorage();
    document.getElementById('order-aantal').value = 1;
    showSuccess(`<i class="fas fa-check-circle"></i> ${boek.titel} (${aantal}x) toegevoegd aan winkelmandje`);
}

// displayWinkelmandje function removed - cart now handled on cart.html page

// verwijderenUitWinkelmandje function removed - cart now handled on cart.html page

// plaatsOrder function removed - order placement now handled on cart.html page

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

// Debug logging
console.log('🔍 [INDEX] Zoeken naar klant...');
console.log('Geselecteerde klantId:', klantId, 'Type:', typeof klantId);
console.log('Aantal klanten geladen:', klanten.length);
console.log('Klanten IDs:', klanten.map(k => ({ id: k.id, type: typeof k.id, naam: k.naam })));

// Convert both to numbers for comparison
const klant = klanten.find(k => parseInt(k.id) === parseInt(klantId));
    
if (!klant) {
    console.error('❌ [INDEX] Klant niet gevonden!');
    console.error('Gezocht naar ID:', klantId);
    console.error('Beschikbare klanten:', klanten);
    showError('Klant niet gevonden. Probeer de pagina te herladen.');
    return;
}
    
console.log('✓ [INDEX] Klant gevonden:', klant);

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

// Update Quick Stats Cards (on index page)
function updateQuickStats() {
    // Only run if on index page
    if (!isDashboardPage()) return;
    
    // Update orders stat
    const ordersStat = document.getElementById('stat-orders');
    if (ordersStat && bestellingen) {
        ordersStat.textContent = bestellingen.length;
    }
    
    // Update klanten stat  
    const klantenStat = document.getElementById('stat-klanten');
    if (klantenStat && klanten) {
        klantenStat.textContent = klanten.length;
    }
    
    // Update boeken stat
    const boekenStat = document.getElementById('stat-boeken');
    if (boekenStat && boeken) {
        boekenStat.textContent = boeken.length;
    }
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

async function exportOrdersPdf() {
    try {
        showSuccess('?? Bestellingen worden geëxporteerd naar PDF...');

        // Direct download trigger
        const exportUrl = '/api/backup/export/orders/pdf';
        window.open(exportUrl, '_blank');

        setTimeout(() => {
            showSuccess('? PDF export gestart! Controleer je downloads folder.');
        }, 500);
    } catch (error) {
        showError('Fout bij exporteren naar PDF: ' + error.message);
    }
}

// Close modals wanneer buiten geklikt wordt
window.onclick = function (event) {
    if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
    }
}

// Close modals met ESC toets
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        document.querySelectorAll('.modal').forEach(modal => {
            modal.style.display = 'none';
        });
    }
});

// Console log voor development
console.log('%c?? BestelAppBoeken Dashboard geladen', 'color: #667eea; font-size: 16px; font-weight: bold;');
console.log('%c?? Integraties: RabbitMQ + Salesforce + SAP R/3', 'color: #48bb78; font-size: 12px;');