// Cart.js - SIMPLIFIED WORKING VERSION
// No complex modals, just basic functionality

const API_BASE = '/api';
let winkelmandje = [];
let klanten = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('🚀 Cart pagina geladen');
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
            console.log('✅ Cart loaded from storage:', winkelmandje.length, 'items');
        } catch (e) {
            console.error('❌ Error loading cart:', e);
            winkelmandje = [];
        }
    } else {
        console.log('ℹ️ No cart data in localStorage');
    }
}

// Save cart to localStorage
function saveCartToStorage() {
    localStorage.setItem('winkelmandje', JSON.stringify(winkelmandje));
    console.log('💾 Cart saved to storage:', winkelmandje.length, 'items');
}

// Load klanten for dropdown
async function loadKlanten() {
    try {
        console.log('📡 Loading klanten from API...');
        const response = await fetch(`${API_BASE}/klanten`);
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        klanten = await response.json();
        console.log('✅ Klanten geladen:', klanten.length, 'klanten');
        
        updateKlantDropdown();
    } catch (error) {
        console.error('❌ Error loading klanten:', error);
        alert('Kon klanten niet laden: ' + error.message);
    }
}

function updateKlantDropdown() {
    const select = document.getElementById('order-klant');
    
    if (!select) {
        console.error('❌ Dropdown element niet gevonden!');
        return;
    }
    
    if (klanten.length === 0) {
        select.innerHTML = '<option value="">Geen klanten beschikbaar</option>';
        return;
    }
    
    select.innerHTML = '<option value="">Kies een klant om een bestelling te plaatsen</option>' +
        klanten.map(k => {
            const klantId = parseInt(k.id, 10);
            return `<option value="${klantId}">${escapeHtml(k.naam)} - ${escapeHtml(k.email)}</option>`;
        }).join('');
    
    console.log('✅ Dropdown updated met', klanten.length, 'klanten');
}

// Display cart items
function displayCart() {
    const container = document.getElementById('cart-items-container');
    const clearBtn = document.getElementById('clear-cart-btn');
    const orderForm = document.getElementById('order-form-card');

    if (!container) {
        console.error('❌ Cart container niet gevonden!');
        return;
    }

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
        if (clearBtn) clearBtn.style.display = 'none';
        if (orderForm) orderForm.style.display = 'none';
        return;
    }

    if (clearBtn) clearBtn.style.display = 'inline-flex';
    if (orderForm) orderForm.style.display = 'block';

    container.innerHTML = winkelmandje.map((item, index) => `
        <div class="cart-item">
            <div class="cart-item-info">
                <div class="cart-item-title">${escapeHtml(item.titel)}</div>
                <div class="cart-item-details">
                    ${item.auteur ? `<i class="fas fa-pen"></i> ${escapeHtml(item.auteur)} | ` : ''}
                    <i class="fas fa-euro-sign"></i> ${(item.prijs || 0).toFixed(2)} per stuk
                </div>
            </div>
            <div class="cart-item-actions">
                <div class="quantity-control">
                    <button class="quantity-btn" onclick="updateQuantity(${index}, -1)" title="Verminder" style="background: #ffb3ba; color: #8b0000;">
                        <i class="fas fa-minus"></i>
                    </button>
                    <span class="quantity-value">${item.aantal}</span>
                    <button class="quantity-btn" onclick="updateQuantity(${index}, 1)" title="Verhoog" style="background: #baffc9; color: #006400;">
                        <i class="fas fa-plus"></i>
                    </button>
                </div>
                <div class="cart-item-price">
                    EUR ${((item.prijs || 0) * item.aantal).toFixed(2)}
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
    }
}

// Clear entire cart
function clearCart() {
    if (confirm('Weet u zeker dat u het hele winkelmandje wilt legen?')) {
        winkelmandje = [];
        saveCartToStorage();
        displayCart();
    }
}

// Update summary
function updateSummary() {
    const subtotal = winkelmandje.reduce((sum, item) => sum + ((item.prijs || 0) * item.aantal), 0);
    const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);

    const subtotalElement = document.getElementById('subtotal');
    const itemCountElement = document.getElementById('item-count');
    const totalAmountElement = document.getElementById('total-amount');

    if (subtotalElement) subtotalElement.textContent = `EUR ${subtotal.toFixed(2)}`;
    if (itemCountElement) itemCountElement.textContent = itemCount;
    if (totalAmountElement) totalAmountElement.textContent = `EUR ${subtotal.toFixed(2)}`;
}

// Place order - SIMPLIFIED
async function plaatsOrder() {
    console.log('═══════════════════════════════════════');
    console.log('🎬 PLAATS ORDER GESTART');
    console.log('═══════════════════════════════════════');
    
    const klantIdRaw = document.getElementById('order-klant').value;
    console.log('📝 Dropdown waarde:', klantIdRaw);

    // Validatie
    if (!klantIdRaw || klantIdRaw === '') {
        alert('⚠️ Kies een klant om een bestelling te plaatsen');
        return;
    }

    if (winkelmandje.length === 0) {
        alert('⚠️ Winkelmandje is leeg');
        return;
    }

    const klantId = parseInt(klantIdRaw, 10);
    const klant = klanten.find(k => parseInt(k.id, 10) === klantId);
    
    if (!klant) {
        alert('❌ Klant niet gevonden! Probeer een andere klant.');
        return;
    }
    
    console.log('✅ Klant gevonden:', klant.naam);

    // Build order data
    const orderData = {
        KlantId: klantId,
        Items: winkelmandje.map(item => ({
            BoekId: parseInt(item.boekId, 10),
            Aantal: parseInt(item.aantal, 10)
        }))
    };
    
    console.log('📦 Order Data:', JSON.stringify(orderData, null, 2));

    try {
        console.log('📤 Versturen naar API...');
        console.log('📍 API URL:', `${API_BASE}/orders`);
        console.log('📦 Payload:', JSON.stringify(orderData, null, 2));
        
        const response = await fetch(`${API_BASE}/orders`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(orderData)
        });

        console.log('📥 Response status:', response.status);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('❌ API Error:', errorText);
            throw new Error(`API Error: ${response.status} - ${errorText || 'Onbekende fout'}`);
        }

        const result = await response.json();
        console.log('✅ Order succesvol geplaatst!');
        console.log('📋 Result:', result);
        
        const orderId = result.id || result.orderId || 'N/A';
        
        // Clear cart
        console.log('🗑️ Clearing cart...');
        winkelmandje = [];
        saveCartToStorage();
        displayCart();
        
        // Show success
        alert(`✅ Bestelling succesvol geplaatst!\n\nOrder ID: ${orderId}\n\nU wordt doorgestuurd naar het bestellingen overzicht.`);
        
        // Redirect
        setTimeout(() => {
            window.location.href = `orders.html?newOrder=true&orderId=${orderId}`;
        }, 1000);

    } catch (error) {
        console.error('❌ Error placing order:', error);
        
        // ✅ Specifieke error messages
        let errorMessage = 'Fout bij plaatsen bestelling';
        
        if (error.message.includes('Failed to fetch')) {
            errorMessage = `
                <div style="margin-bottom: 10px;">❌ Kan geen verbinding maken met de server</div>
                <div style="font-size: 13px; color: #666;">
                    Mogelijke oorzaken:<br>
                    • Server draait niet (start met: dotnet run)<br>
                    • Verkeerde URL (check: ${API_BASE}/orders)<br>
                    • CORS probleem (check Program.cs)<br>
                    • SSL certificaat probleem (probeer HTTP in plaats van HTTPS)
                </div>
            `;
        } else if (error.message.includes('404')) {
            errorMessage = '❌ API endpoint niet gevonden (/api/orders bestaat niet)';
        } else if (error.message.includes('500')) {
            errorMessage = '❌ Server error - check de console logs van de applicatie';
        } else {
            errorMessage = `❌ ${error.message}`;
        }
        
        alert(errorMessage.replace(/<[^>]*>/g, '')); // Remove HTML tags for alert
        
        // Show in console with full details
        console.error('═══════════════════════════════════════');
        console.error('VOLLEDIGE ERROR DETAILS:');
        console.error('Error type:', error.name);
        console.error('Error message:', error.message);
        console.error('Error stack:', error.stack);
        console.error('═══════════════════════════════════════');
    }
    
    console.log('═══════════════════════════════════════');
}

// Utility function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
