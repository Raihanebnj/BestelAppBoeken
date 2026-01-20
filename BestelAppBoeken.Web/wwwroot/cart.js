// Cart.js - FIXED & UPDATED VERSION
// ✅ API Keys toegevoegd
// ✅ Emoji's correct
// ✅ Alle functionaliteit werkend

const API_BASE = '/api';
const API_KEY = 'BOOKSTORE-API-2026-SECRET-KEY-XYZ789';

let winkelmandje = [];
let klanten = [];

// ============================================
// INITIALIZATION
// ============================================
document.addEventListener('DOMContentLoaded', () => {
    console.log('🚀 Cart.js geladen');
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
            console.log('✅ Cart loaded:', winkelmandje.length, 'items');
        } catch (e) {
            console.error('❌ Error loading cart:', e);
            winkelmandje = [];
        }
    }
}

// Save cart to localStorage
function saveCartToStorage() {
    localStorage.setItem('winkelmandje', JSON.stringify(winkelmandje));
    console.log('💾 Cart saved:', winkelmandje.length, 'items');
}

// ============================================
// KLANTEN LADEN
// ============================================
async function loadKlanten() {
    try {
        console.log('🔄 Loading klanten...');
        
        const response = await fetch(`${API_BASE}/klanten`, {
            headers: {
                'X-Api-Key': API_KEY
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        
        klanten = await response.json();
        console.log('✅ Klanten geladen:', klanten.length);
        
        updateKlantDropdown();
    } catch (error) {
        console.error('❌ Error loading klanten:', error);
        showError('Kon klanten niet laden: ' + error.message);
    }
}

function updateKlantDropdown() {
    const select = document.getElementById('order-klant');
    
    if (!select) {
        console.error('❌ Dropdown niet gevonden!');
        return;
    }
    
    if (klanten.length === 0) {
        select.innerHTML = '<option value="">Geen klanten beschikbaar</option>';
        return;
    }
    
    select.innerHTML = '<option value="">Kies een klant</option>' +
        klanten.map(k => {
            const klantId = parseInt(k.id, 10);
            return `<option value="${klantId}">${escapeHtml(k.naam)} - ${escapeHtml(k.email)}</option>`;
        }).join('');
    
    console.log('✅ Dropdown updated:', klanten.length, 'klanten');
}

// ============================================
// DISPLAY CART
// ============================================
function displayCart() {
    const container = document.getElementById('cart-items-container');
    const clearBtn = document.getElementById('clear-cart-btn');
    const orderForm = document.getElementById('order-form-card');

    if (!container) {
        console.error('❌ Cart container not found!');
        return;
    }

    // Empty cart
    if (winkelmandje.length === 0) {
        container.innerHTML = `
            <div class="empty-cart">
                <i class="fas fa-shopping-cart"></i>
                <p style="font-size: 18px; margin-bottom: 20px;">Winkelmandje is leeg</p>
                <a href="Index.html" class="btn btn-primary">
                    <i class="fas fa-book"></i> Boeken Bekijken
                </a>
            </div>
        `;
        if (clearBtn) clearBtn.style.display = 'none';
        if (orderForm) orderForm.style.display = 'none';
        return;
    }

    // Show cart items
    if (clearBtn) clearBtn.style.display = 'inline-flex';
    if (orderForm) orderForm.style.display = 'block';

    container.innerHTML = winkelmandje.map((item, index) => `
        <div class="cart-item">
            <div class="cart-item-info">
                <div class="cart-item-title">${escapeHtml(item.titel)}</div>
                <div class="cart-item-details">
                    ${item.auteur ? `<i class="fas fa-pen"></i> ${escapeHtml(item.auteur)} | ` : ''}
                    <i class="fas fa-euro-sign"></i> €${(item.prijs || 0).toFixed(2)} per stuk
                </div>
            </div>
            <div class="cart-item-actions">
                <div class="quantity-control">
                    <button class="quantity-btn" onclick="updateQuantity(${index}, -1)" style="background: #ffb3ba; color: #8b0000;">
                        <i class="fas fa-minus"></i>
                    </button>
                    <span class="quantity-value">${item.aantal}</span>
                    <button class="quantity-btn" onclick="updateQuantity(${index}, 1)" style="background: #baffc9; color: #006400;">
                        <i class="fas fa-plus"></i>
                    </button>
                </div>
                <div class="cart-item-price">
                    €${((item.prijs || 0) * item.aantal).toFixed(2)}
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
            showSuccess('Item verwijderd');
        } else {
            showSuccess(`Aantal: ${winkelmandje[index].aantal}x`);
        }
        
        saveCartToStorage();
        displayCart();
    }
}

// Remove item
function removeFromCart(index) {
    if (confirm('Item verwijderen?')) {
        winkelmandje.splice(index, 1);
        saveCartToStorage();
        displayCart();
        showSuccess('Item verwijderd');
    }
}

// Clear cart
function clearCart() {
    if (confirm('Hele winkelmandje legen?')) {
        winkelmandje = [];
        saveCartToStorage();
        displayCart();
        showSuccess('Winkelmandje geleegd');
    }
}

// Update summary
function updateSummary() {
    const subtotal = winkelmandje.reduce((sum, item) => sum + ((item.prijs || 0) * item.aantal), 0);
    const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);

    const subtotalElement = document.getElementById('subtotal');
    const itemCountElement = document.getElementById('item-count');
    const totalAmountElement = document.getElementById('total-amount');

    if (subtotalElement) subtotalElement.textContent = `€${subtotal.toFixed(2)}`;
    if (itemCountElement) itemCountElement.textContent = itemCount;
    if (totalAmountElement) totalAmountElement.textContent = `€${subtotal.toFixed(2)}`;
}

// ============================================
// 🎯 PLAATS ORDER - MAIN FUNCTION
// ============================================
async function plaatsOrder() {
    console.log('═══════════════════════════════════════');
    console.log('🎬 PLAATS ORDER START');
    console.log('═══════════════════════════════════════');
    
    // Get selected klant
    const klantIdRaw = document.getElementById('order-klant').value;
    console.log('📝 Selected klant ID:', klantIdRaw);

    // Validation
    if (!klantIdRaw || klantIdRaw === '') {
        showError('⚠️ Kies een klant');
        return;
    }

    if (winkelmandje.length === 0) {
        showError('⚠️ Winkelmandje is leeg');
        return;
    }

    const klantId = parseInt(klantIdRaw, 10);
    
    if (isNaN(klantId)) {
        showError('⚠️ Ongeldige klant');
        return;
    }

    // Find klant
    const klant = klanten.find(k => parseInt(k.id, 10) === klantId);
    
    if (!klant) {
        showError(`❌ Klant niet gevonden (ID: ${klantId})`);
        return;
    }
    
    console.log('✅ Klant:', klant.naam, '-', klant.email);

    // Prepare order data
    const orderData = {
        KlantId: klantId,
        Items: winkelmandje.map(item => ({
            BoekId: parseInt(item.boekId, 10),
            Aantal: parseInt(item.aantal, 10)
        }))
    };
    
    console.log('📦 Order data:', JSON.stringify(orderData, null, 2));

    try {
        // Show loading
        const loadingModal = showLoadingModal();
        
        // Send to API
        console.log('📤 Sending to API...');
        const response = await fetch(`${API_BASE}/orders`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Api-Key': API_KEY
            },
            body: JSON.stringify(orderData)
        });

        console.log('📥 Response:', response.status, response.statusText);

        if (!response.ok) {
            if (loadingModal) loadingModal.remove();
            
            const errorText = await response.text();
            console.error('❌ API Error:', errorText);
            
            let errorMessage = 'Bestelling mislukt';
            try {
                const errorJson = JSON.parse(errorText);
                errorMessage = errorJson.error || errorJson.message || errorMessage;
            } catch (e) {
                errorMessage = errorText || errorMessage;
            }
            
            throw new Error(errorMessage);
        }

        const result = await response.json();
        console.log('✅ Order succesvol!', result);
        
        // Get order details
        const orderId = result.id || result.orderId || 'N/A';
        const totalAmount = winkelmandje.reduce((sum, item) => 
            sum + ((item.prijs || 0) * item.aantal), 0);
        const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);
        
        // Clear cart
        console.log('🗑️ Clearing cart...');
        winkelmandje = [];
        saveCartToStorage();
        displayCart();
        
        // Remove loading
        if (loadingModal) loadingModal.remove();
        
        // Show success
        showSuccess(`✅ Bestelling geplaatst! Order ID: ${orderId}`);
        
        // Show modal after short delay
        setTimeout(() => {
            showOrderPlacedModal(orderId, totalAmount, itemCount);
        }, 300);

        console.log('═══════════════════════════════════════');

    } catch (error) {
        console.error('💥 Error:', error);
        showError(`❌ ${error.message}`);
    }
}

// ============================================
// LOADING MODAL
// ============================================
function showLoadingModal() {
    const modal = document.createElement('div');
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.85);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        animation: fadeIn 0.3s;
    `;
    
    modal.innerHTML = `
        <div style="
            background: white;
            padding: 50px 60px;
            border-radius: 24px;
            max-width: 500px;
            width: 90%;
            box-shadow: 0 25px 70px rgba(0,0,0,0.5);
            text-align: center;
        ">
            <div style="
                width: 100px;
                height: 100px;
                border: 8px solid #e2e8f0;
                border-top-color: #667eea;
                border-radius: 50%;
                margin: 0 auto 30px;
                animation: spin 1s linear infinite;
            "></div>
            
            <h2 style="color: #667eea; margin-bottom: 15px; font-size: 28px; font-weight: 800;">
                Bestelling Plaatsen... 🚀
            </h2>
            
            <p style="color: #718096; font-size: 16px; line-height: 1.6;">
                Even geduld terwijl we uw bestelling verwerken.
            </p>
            
            <div style="
                margin-top: 25px;
                padding: 15px;
                background: linear-gradient(135deg, #bee3f8, #90cdf4);
                border-radius: 10px;
            ">
                <p style="color: #2c5282; font-size: 14px; margin: 0; font-weight: 600;">
                    <i class="fas fa-sync-alt fa-spin"></i> Synchroniseren...
                </p>
            </div>
        </div>
        
        <style>
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
            @keyframes fadeIn {
                from { opacity: 0; }
                to { opacity: 1; }
            }
        </style>
    `;
    
    document.body.appendChild(modal);
    console.log('⏳ Loading modal shown');
    return modal;
}

// ============================================
// SUCCESS MODAL
// ============================================
function showOrderPlacedModal(orderId, totalAmount, itemCount) {
    const modal = document.createElement('div');
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.8);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        animation: fadeIn 0.3s;
    `;
    
    modal.innerHTML = `
        <div style="
            background: white;
            padding: 50px;
            border-radius: 24px;
            max-width: 600px;
            width: 90%;
            box-shadow: 0 25px 70px rgba(0,0,0,0.4);
            text-align: center;
            position: relative;
        ">
            <div style="
                width: 120px;
                height: 120px;
                background: linear-gradient(135deg, #c6f6d5, #48bb78);
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                margin: 0 auto 30px;
                box-shadow: 0 10px 30px rgba(72, 187, 120, 0.4);
            ">
                <i class="fas fa-check-circle" style="font-size: 60px; color: white;"></i>
            </div>
            
            <h2 style="color: #48bb78; margin-bottom: 15px; font-size: 32px; font-weight: 800;">
                Bestelling Geplaatst! 🎉
            </h2>
            
            <p style="color: #718096; font-size: 16px; margin-bottom: 30px; line-height: 1.6;">
                Uw bestelling is succesvol verwerkt en wordt nu gesynchroniseerd.
            </p>
            
            <div style="
                background: linear-gradient(135deg, #f0fff4, #c6f6d5);
                padding: 25px;
                border-radius: 16px;
                margin-bottom: 30px;
                border: 2px solid #48bb78;
            ">
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; text-align: left;">
                    <div>
                        <div style="color: #718096; font-size: 14px; margin-bottom: 5px;">Order ID</div>
                        <div style="color: #2d3748; font-size: 20px; font-weight: 700;">
                            ORD-${String(orderId).padStart(6, '0')}
                        </div>
                    </div>
                    <div>
                        <div style="color: #718096; font-size: 14px; margin-bottom: 5px;">Totaal</div>
                        <div style="color: #48bb78; font-size: 20px; font-weight: 700;">
                            €${totalAmount.toFixed(2)}
                        </div>
                    </div>
                </div>
                <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #9ae6b4;">
                    <div style="color: #718096; font-size: 14px; margin-bottom: 5px;">Items</div>
                    <div style="color: #2d3748; font-size: 18px; font-weight: 600;">
                        <i class="fas fa-shopping-bag"></i> ${itemCount} ${itemCount === 1 ? 'item' : 'items'}
                    </div>
                </div>
            </div>
            
            <div style="
                background: linear-gradient(135deg, #bee3f8, #90cdf4);
                padding: 15px 20px;
                border-radius: 12px;
                margin-bottom: 30px;
            ">
                <p style="color: #2c5282; font-size: 14px; margin: 0;">
                    <i class="fas fa-sync-alt fa-spin"></i>
                    <strong>Synchronisatie:</strong> RabbitMQ → Salesforce → SAP
                </p>
            </div>
            
            <div id="auto-redirect-timer" style="
                background: linear-gradient(135deg, #feebc8, #fbd38d);
                padding: 12px 20px;
                border-radius: 10px;
                margin-bottom: 25px;
            ">
                <i class="fas fa-clock"></i>
                <span style="color: #7c2d12; font-weight: 600;">
                    Redirect over <span id="countdown">5</span> seconden...
                </span>
            </div>
            
            <div style="display: flex; gap: 15px; justify-content: center; flex-wrap: wrap;">
                <button onclick="window.location.href='orders.html?newOrder=true&orderId=${orderId}'" 
                        class="btn btn-success" 
                        style="padding: 16px 32px; font-size: 18px; flex: 1; min-width: 200px;">
                    <i class="fas fa-clipboard-list"></i> Bekijk Bestellingen
                </button>
                <button onclick="window.location.href='Index.html'" 
                        class="btn btn-primary" 
                        style="padding: 16px 32px; font-size: 18px; flex: 1; min-width: 200px;">
                    <i class="fas fa-home"></i> Dashboard
                </button>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Auto redirect countdown
    let countdown = 5;
    const countdownElement = modal.querySelector('#countdown');
    
    const interval = setInterval(() => {
        countdown--;
        if (countdownElement) {
            countdownElement.textContent = countdown;
        }
        
        if (countdown <= 0) {
            clearInterval(interval);
            window.location.href = `orders.html?newOrder=true&orderId=${orderId}`;
        }
    }, 1000);
    
    console.log('🎉 Success modal shown - auto redirect in 5s');
}

// ============================================
// MESSAGES
// ============================================
function showSuccess(message) {
    console.log('✅', message);
    if (typeof window.showMessage === 'function') {
        window.showMessage(message, 'success');
    } else {
        alert(message);
    }
}

function showError(message) {
    console.error('❌', message);
    if (typeof window.showMessage === 'function') {
        window.showMessage(message, 'error');
    } else {
        alert(message);
    }
}

function showInfo(message) {
    console.log('ℹ️', message);
    if (typeof window.showMessage === 'function') {
        window.showMessage(message, 'info');
    } else {
        alert(message);
    }
}

// ============================================
// UTILITY
// ============================================
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
