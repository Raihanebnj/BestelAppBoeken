// Cart.js - Shopping Cart Page Logic

const API_BASE = '/api';
let winkelmandje = [];
let klanten = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    loadCartFromStorage();
    loadKlanten();
    displayCart();
});

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
}

// Load klanten for dropdown
async function loadKlanten() {
    try {
        console.log('🔄 [CART] Laden van klanten...');
        console.log('API URL:', `${API_BASE}/klanten`);
        
        const response = await fetch(`${API_BASE}/klanten`);
        console.log('Response status:', response.status);
        console.log('Response OK:', response.ok);
        
        if (!response.ok) throw new Error('Kon klanten niet laden');
        
        klanten = await response.json();
        
        // EXTRA DEBUG: Log de RAW response
        console.log('📦 RAW API Response:', JSON.stringify(klanten, null, 2));
        console.log('✓ [CART] Klanten geladen:', klanten.length, 'klanten');
        
        if (klanten.length > 0) {
            console.log('📋 Eerste klant details:');
            console.log('  - ID:', klanten[0].id, '(type:', typeof klanten[0].id, ')');
            console.log('  - Naam:', klanten[0].naam);
            console.log('  - Email:', klanten[0].email);
            console.log('📋 Alle klant IDs:', klanten.map(k => `${k.id} (${typeof k.id})`));
        }
        
        updateKlantDropdown();
    } catch (error) {
        console.error('❌ [CART] Error loading klanten:', error);
        showError('Kon klanten niet laden: ' + error.message);
    }
}

function updateKlantDropdown() {
    const select = document.getElementById('order-klant');
    console.log('📝 Updating klant dropdown, select element:', select ? 'gevonden' : 'NIET GEVONDEN');
    console.log('Aantal klanten om toe te voegen:', klanten.length);
    
    select.innerHTML = '<option value="">Kies een klant om een bestelling te plaatsen</option>' +
        klanten.map(k => {
            console.log(`Adding klant option: ID=${k.id}, Naam=${k.naam}`);
            return `<option value="${k.id}">${escapeHtml(k.naam)} - ${escapeHtml(k.email)}</option>`;
        }).join('');
    
    console.log('✓ Dropdown updated met', klanten.length, 'opties');
}

// Display cart items
function displayCart() {
    const container = document.getElementById('cart-items-container');
    const clearBtn = document.getElementById('clear-cart-btn');
    const orderForm = document.getElementById('order-form-card');

    if (winkelmandje.length === 0) {
        container.innerHTML = `
            <div class="empty-cart">
                <i class="fas fa-shopping-cart"></i>
                <p style="font-size: 18px; margin-bottom: 20px;">Uw winkelmandje is leeg</p>
                <a href="Index.html" class="btn btn-primary">
                    <i class="fas fa-book"></i> Boeken Bekijken
                </a>
            </div>
        `;
        clearBtn.style.display = 'none';
        orderForm.style.display = 'none';
        return;
    }

    clearBtn.style.display = 'inline-flex';
    orderForm.style.display = 'block';

    container.innerHTML = winkelmandje.map((item, index) => `
        <div class="cart-item">
            <div class="cart-item-info">
                <div class="cart-item-title">${escapeHtml(item.titel)}</div>
                <div class="cart-item-details">
                    <i class="fas fa-pen"></i> ${escapeHtml(item.auteur)} | 
                    <i class="fas fa-euro-sign"></i> ${item.prijs.toFixed(2)} per stuk
                </div>
            </div>
            <div class="cart-item-actions">
                <div class="quantity-control">
                    <button class="quantity-btn" onclick="updateQuantity(${index}, -1)" title="Verminder">
                        <i class="fas fa-minus"></i>
                    </button>
                    <span class="quantity-value">${item.aantal}</span>
                    <button class="quantity-btn" onclick="updateQuantity(${index}, 1)" title="Verhoog">
                        <i class="fas fa-plus"></i>
                    </button>
                </div>
                <div class="cart-item-price">
                    EUR ${(item.prijs * item.aantal).toFixed(2)}
                </div>
                <button class="btn btn-danger" onclick="removeFromCart(${index})" style="padding: 8px 12px;">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `).join('');

    updateSummary();
}

// Update quantity
function updateQuantity(index, change) {
    if (index >= 0 && index < winkelmandje.length) {
        winkelmandje[index].aantal += change;
        
        if (winkelmandje[index].aantal <= 0) {
            winkelmandje.splice(index, 1);
        }
        
        saveCartToStorage();
        displayCart();
    }
}

// Remove item from cart
function removeFromCart(index) {
    if (confirm('Weet u zeker dat u dit item wilt verwijderen?')) {
        winkelmandje.splice(index, 1);
        saveCartToStorage();
        displayCart();
        showSuccess('Item verwijderd uit winkelmandje');
    }
}

// Clear entire cart
function clearCart() {
    if (confirm('Weet u zeker dat u het hele winkelmandje wilt legen?')) {
        winkelmandje = [];
        saveCartToStorage();
        displayCart();
        showSuccess('Winkelmandje geleegd');
    }
}

// Update summary
function updateSummary() {
    const subtotal = winkelmandje.reduce((sum, item) => sum + (item.prijs * item.aantal), 0);
    const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);

    document.getElementById('subtotal').textContent = `EUR ${subtotal.toFixed(2)}`;
    document.getElementById('item-count').textContent = itemCount;
    document.getElementById('total-amount').textContent = `EUR ${subtotal.toFixed(2)}`;
}

// Place order
async function plaatsOrder() {
    console.log('🎬 [CART] plaatsOrder() gestart');
    
    const klantId = document.getElementById('order-klant').value;
    console.log('📝 Dropdown value:', klantId, '(type:', typeof klantId, ')');

    if (!klantId) {
        showError('Kies een klant om een bestelling te plaatsen');
        return;
    }

    if (winkelmandje.length === 0) {
        showError('Winkelmandje is leeg');
        return;
    }

    // SUPER DEBUG logging
    console.log('🔍 [CART] Zoeken naar klant...');
    console.log('Geselecteerde klantId:', klantId, 'Type:', typeof klantId);
    console.log('Aantal klanten in array:', klanten.length);
    console.log('📋 Alle klanten in array:');
    klanten.forEach((k, idx) => {
        console.log(`  [${idx}] ID: ${k.id} (${typeof k.id}), Naam: ${k.naam}, Email: ${k.email}`);
    });

    // TRY MULTIPLE WAYS TO FIND THE CUSTOMER
    console.log('🔎 Methode 1: parseInt vergelijking...');
    let klant = klanten.find(k => parseInt(k.id) === parseInt(klantId));
    
    if (!klant) {
        console.log('❌ Methode 1 faalt. Probeer Methode 2: strict equality...');
        klant = klanten.find(k => k.id == klantId);
    }
    
    if (!klant) {
        console.log('❌ Methode 2 faalt. Probeer Methode 3: string vergelijking...');
        klant = klanten.find(k => String(k.id) === String(klantId));
    }
    
    if (!klant) {
        console.error('💥 [CART] ALLE METHODES GEFAALD! Klant niet gevonden!');
        console.error('Gezocht naar ID:', klantId, '(type:', typeof klantId, ')');
        console.error('Beschikbare klanten:', JSON.stringify(klanten, null, 2));
        
        showError(`Klant niet gevonden! Debug info: Gezocht ID=${klantId} (type=${typeof klantId}), Aantal klanten=${klanten.length}. Check de browser console voor details.`);
        return;
    }
    
    console.log('✅ [CART] Klant gevonden!');
    console.log('📋 Klant details:', JSON.stringify(klant, null, 2));

    // ✅ FIX: Gebruik correcte API request format (CreateOrderRequest)
    // Backend verwacht: { KlantId: int, Items: [ { BoekId: int, Aantal: int } ] }
    const orderData = {
        KlantId: parseInt(klant.id, 10),  // ✅ Correct! Parse naar INT
        Items: winkelmandje.map(item => ({
            BoekId: parseInt(item.boekId, 10),  // ✅ Correct! Parse naar INT
            Aantal: parseInt(item.aantal, 10)   // ✅ Correct! Parse naar INT
        }))
    };
    
    console.log('📦 Order Data (naar API):', JSON.stringify(orderData, null, 2));

    try {
        const response = await fetch(`${API_BASE}/orders`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(orderData)
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Er is een fout opgetreden' }));
            throw new Error(error.error || 'Kon bestelling niet plaatsen');
        }

        const result = await response.json();
        
        // Clear cart
        winkelmandje = [];
        saveCartToStorage();
        
        // Send confirmation email
        await sendConfirmationEmail(klant.email, orderData);
        
        // Show success and redirect
        showSuccess('Bestelling succesvol geplaatst! Bevestigingsmail wordt verzonden naar ' + klant.email);
        
        setTimeout(() => {
            window.location.href = 'Index.html';
        }, 2000);

    } catch (error) {
        console.error('Error placing order:', error);
        showError(error.message);
    }
}

// Send confirmation email
async function sendConfirmationEmail(email, orderData) {
    try {
        // This would integrate with your email service
        // For now, we'll just log it
        console.log('Sending confirmation email to:', email);
        console.log('Order data:', orderData);
        
        // In a real implementation, you would call an email API here
        // await fetch(`${API_BASE}/orders/send-confirmation`, {
        //     method: 'POST',
        //     headers: { 'Content-Type': 'application/json' },
        //     body: JSON.stringify({ email, orderData })
        // });
        
    } catch (error) {
        console.error('Error sending confirmation email:', error);
        // Don't throw error - email failure shouldn't stop the order
    }
}

// Show messages
function showSuccess(message) {
    const container = document.getElementById('message-container');
    container.innerHTML = `
        <div class="message success">
            <i class="fas fa-check-circle"></i>
            <span>${escapeHtml(message)}</span>
        </div>
    `;
    setTimeout(() => container.innerHTML = '', 5000);
}

function showError(message) {
    const container = document.getElementById('message-container');
    container.innerHTML = `
        <div class="message error">
            <i class="fas fa-exclamation-circle"></i>
            <span>${escapeHtml(message)}</span>
        </div>
    `;
    setTimeout(() => container.innerHTML = '', 5000);
}

// Escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
