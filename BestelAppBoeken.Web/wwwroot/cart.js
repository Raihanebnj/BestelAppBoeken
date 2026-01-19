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
            console.log('✅ Cart loaded from storage:', winkelmandje.length, 'items');
        } catch (e) {
            console.error('❌ Error loading cart:', e);
            winkelmandje = [];
        }
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
        console.log('🔄 Loading klanten from API...');
        
        const response = await fetch(`${API_BASE}/klanten`);
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        klanten = await response.json();
        
        console.log('✅ Klanten geladen:', klanten.length, 'klanten');
        
        // Debug: log eerste klant details
        if (klanten.length > 0) {
            console.log('📋 Eerste klant:', {
                id: klanten[0].id,
                type: typeof klanten[0].id,
                naam: klanten[0].naam,
                email: klanten[0].email
            });
        }
        
        updateKlantDropdown();
    } catch (error) {
        console.error('❌ Error loading klanten:', error);
        showError('Kon klanten niet laden: ' + error.message);
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
        console.warn('⚠️ Geen klanten om toe te voegen aan dropdown');
        return;
    }
    
    select.innerHTML = '<option value="">Kies een klant om een bestelling te plaatsen</option>' +
        klanten.map(k => {
            // Zorg ervoor dat ID altijd een number is
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
            showSuccess('Item verwijderd uit winkelmandje');
        } else {
            showSuccess(`Aantal bijgewerkt: ${winkelmandje[index].aantal}x`);
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
    const subtotal = winkelmandje.reduce((sum, item) => sum + ((item.prijs || 0) * item.aantal), 0);
    const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);

    const subtotalElement = document.getElementById('subtotal');
    const itemCountElement = document.getElementById('item-count');
    const totalAmountElement = document.getElementById('total-amount');

    if (subtotalElement) subtotalElement.textContent = `EUR ${subtotal.toFixed(2)}`;
    if (itemCountElement) itemCountElement.textContent = itemCount;
    if (totalAmountElement) totalAmountElement.textContent = `EUR ${subtotal.toFixed(2)}`;
}

// 🎯 FIX: Place order - Verbeterde klant zoek logica
async function plaatsOrder() {
    console.log('═══════════════════════════════════════');
    console.log('🎬 PLAATS ORDER GESTART');
    console.log('═══════════════════════════════════════');
    
    const klantIdRaw = document.getElementById('order-klant').value;
    console.log('📝 Dropdown waarde (raw):', klantIdRaw, 'Type:', typeof klantIdRaw);

    // Validatie: Is er een klant geselecteerd?
    if (!klantIdRaw || klantIdRaw === '') {
        console.error('❌ Geen klant geselecteerd');
        showError('⚠️ Kies een klant om een bestelling te plaatsen');
        return;
    }

    // Validatie: Is winkelmandje leeg?
    if (winkelmandje.length === 0) {
        console.error('❌ Winkelmandje is leeg');
        showError('⚠️ Winkelmandje is leeg');
        return;
    }

    // Parse klant ID naar integer
    const klantId = parseInt(klantIdRaw, 10);
    console.log('🔢 Klant ID (parsed):', klantId, 'Type:', typeof klantId);

    // Validatie: Is het een geldig getal?
    if (isNaN(klantId)) {
        console.error('❌ Ongeldig klant ID:', klantIdRaw);
        showError('⚠️ Ongeldige klant geselecteerd');
        return;
    }

    // Debug: Toon alle beschikbare klanten
    console.log('📋 Beschikbare klanten:');
    klanten.forEach((k, idx) => {
        const kId = parseInt(k.id, 10);
        console.log(`  [${idx}] ID=${kId} (type=${typeof kId}), Naam="${k.naam}", Email="${k.email}"`);
    });

    // 🎯 VERBETERD: Zoek klant met PARSED ID vergelijking
    const klant = klanten.find(k => parseInt(k.id, 10) === klantId);
    
    if (!klant) {
        console.error('💥 KLANT NIET GEVONDEN!');
        console.error('Gezocht naar ID:', klantId, '(type:', typeof klantId, ')');
        console.error('Aantal beschikbare klanten:', klanten.length);
        
        showError(`❌ Klant niet gevonden! (ID: ${klantId}). Probeer een andere klant te selecteren of herlaad de pagina.`);
        return;
    }
    
    console.log('✅ Klant gevonden!');
    console.log('📋 Klant details:', {
        id: klant.id,
        naam: klant.naam,
        email: klant.email,
        adres: klant.adres
    });

    // Valideer winkelmandje items
    console.log('🛒 Winkelmandje validatie...');
    const invalidItems = winkelmandje.filter(item => !item.boekId || !item.aantal || item.aantal <= 0);
    if (invalidItems.length > 0) {
        console.error('❌ Ongeldige items in winkelmandje:', invalidItems);
        showError('❌ Sommige items in het winkelmandje zijn ongeldig');
        return;
    }

    // ✅ FIX: Correcte API request format (CreateOrderRequest)
    // Backend verwacht: { KlantId: int, Items: [ { BoekId: int, Aantal: int } ] }
    const orderData = {
        KlantId: klantId,  // ✅ Correct! Integer
        Items: winkelmandje.map(item => ({
            BoekId: parseInt(item.boekId, 10),  // ✅ Correct! Integer
            Aantal: parseInt(item.aantal, 10)   // ✅ Correct! Integer
        }))
    };
    
    console.log('📦 Order Data (naar API):');
    console.log(JSON.stringify(orderData, null, 2));
    console.log('  - KlantId:', orderData.KlantId, '(type:', typeof orderData.KlantId, ')');
    console.log('  - Items count:', orderData.Items.length);
    orderData.Items.forEach((item, idx) => {
        console.log(`    [${idx}] BoekId=${item.BoekId}, Aantal=${item.Aantal}`);
    });

    try {
        console.log('📤 Versturen naar API...');
        
        // 🎯 SHOW LOADING MODAL (Direct feedback)
        const loadingModal = showLoadingModal();
        
        const response = await fetch(`${API_BASE}/orders`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(orderData)
        });

        console.log('📥 Response status:', response.status, response.statusText);

        if (!response.ok) {
            // Remove loading modal
            if (loadingModal && loadingModal.parentNode) {
                loadingModal.remove();
            }
            
            const errorText = await response.text();
            console.error('❌ API Error Response:', errorText);
            
            let errorMessage = 'Kon bestelling niet plaatsen';
            try {
                const errorJson = JSON.parse(errorText);
                errorMessage = errorJson.error || errorJson.message || errorMessage;
            } catch (e) {
                errorMessage = errorText || errorMessage;
            }
            
            throw new Error(errorMessage);
        }

        const result = await response.json();
        
        console.log('✅ Order succesvol geplaatst!');
        console.log('📋 Result:', result);
        
        // Get order details VOOR cart clearing
        const orderId = result.id || result.orderId || 'N/A';
        const totalAmount = winkelmandje.reduce((sum, item) => {
            return sum + ((item.prijs || 0) * item.aantal);
        }, 0);
        const itemCount = winkelmandje.reduce((sum, item) => sum + item.aantal, 0);
        
        // ✅ STEP 1: CLEAR CART DATA
        console.log('🗑️ Step 1: Clearing cart data...');
        winkelmandje = [];
        saveCartToStorage();
        console.log('✅ Cart data cleared, winkelmandje.length =', winkelmandje.length);
        
        // ✅ STEP 2: UPDATE UI TO SHOW EMPTY CART
        console.log('🎨 Step 2: Updating UI to show empty cart...');
        displayCart();
        console.log('✅ displayCart() called - UI should show empty state');
        
        // ✅ STEP 3: Remove loading modal
        if (loadingModal && loadingModal.parentNode) {
            loadingModal.remove();
            console.log('✅ Loading modal removed');
        }
        
        // ✅ STEP 4: Show success toast
        showSuccess(`✅ Bestelling succesvol geplaatst! Order ID: ${orderId}`);
        
        // ✅ STEP 5: Wait 300ms for DOM update to render, then show success modal
        console.log('⏳ Waiting 300ms for DOM update...');
        setTimeout(() => {
            console.log('🎯 Showing order placed modal...');
            showOrderPlacedModal(orderId, totalAmount, itemCount);
            
            // Optioneel: Send confirmation email
            if (klant.email) {
                console.log('📧 Bevestigingsmail zou worden verzonden naar:', klant.email);
                // await sendConfirmationEmail(klant.email, orderData);
            }
            
            console.log('✅ Cart geleegd, UI updated, modal getoond');
            console.log('═══════════════════════════════════════');
        }, 300);

    } catch (error) {
        console.error('💥 Error placing order:', error);
        showError(`❌ ${error.message}`);
    }
    
    console.log('═══════════════════════════════════════');
}

// Send confirmation email (placeholder)
async function sendConfirmationEmail(email, orderData) {
    try {
        console.log('📧 Sending confirmation email to:', email);
        console.log('Order data:', orderData);
        
        // In a real implementation, you would call an email API here
        // await fetch(`${API_BASE}/orders/send-confirmation`, {
        //     method: 'POST',
        //     headers: { 'Content-Type': 'application/json' },
        //     body: JSON.stringify({ email, orderData })
        // });
        
    } catch (error) {
        console.error('❌ Error sending confirmation email:', error);
        // Don't throw error - email failure shouldn't stop the order
    }
}

// Show messages - Use toast notifications if available
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

// Utility function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ============================================
// 🎯 SHOW LOADING MODAL (Tijdens API call)
// ============================================
function showLoadingModal() {
    const modal = document.createElement('div');
    modal.className = 'loading-modal-overlay';
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
            
            <h2 style="color: var(--primary); margin-bottom: 15px; font-size: 28px; font-weight: 800;">
                Bestelling Plaatsen... 🚀
            </h2>
            
            <p style="color: var(--gray); font-size: 16px; line-height: 1.6;">
                Even geduld terwijl we uw bestelling verwerken en synchroniseren met onze systemen.
            </p>
            
            <div style="
                margin-top: 25px;
                padding: 15px;
                background: linear-gradient(135deg, #bee3f8, #90cdf4);
                border-radius: 10px;
                border-left: 4px solid #4299e1;
            ">
                <p style="color: #2c5282; font-size: 14px; margin: 0; font-weight: 600;">
                    <i class="fas fa-sync-alt fa-spin"></i> Verbinden met RabbitMQ...
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
// 🎯 SHOW LOADING MODAL (Tijdens API call)
// ============================================
function showLoadingModal() {
    const modal = document.createElement('div');
    modal.className = 'loading-modal-overlay';
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
            
            <h2 style="color: var(--primary); margin-bottom: 15px; font-size: 28px; font-weight: 800;">
                Bestelling Plaatsen... 🚀
            </h2>
            
            <p style="color: var(--gray); font-size: 16px; line-height: 1.6;">
                Even geduld terwijl we uw bestelling verwerken en synchroniseren met onze systemen.
            </p>
            
            <div style="
                margin-top: 25px;
                padding: 15px;
                background: linear-gradient(135deg, #bee3f8, #90cdf4);
                border-radius: 10px;
                border-left: 4px solid #4299e1;
            ">
                <p style="color: #2c5282; font-size: 14px; margin: 0; font-weight: 600;">
                    <i class="fas fa-sync-alt fa-spin"></i> Verbinden met RabbitMQ...
                </p>
            </div>
        </div>
        
        <style>
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        </style>
    `;
    
    document.body.appendChild(modal);
    console.log('⏳ Loading modal shown');
    return modal;
}

// ============================================
// 🎯 SHOW ORDER PLACED MODAL (Popup na bestelling) - WITH AUTO REDIRECT
// ============================================
function showOrderPlacedModal(orderId, totalAmount, itemCount) {
    // Create modal overlay
    const modal = document.createElement('div');
    modal.className = 'order-placed-modal-overlay';
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
    
    // Create modal content
    modal.innerHTML = `
        <div style="
            background: white;
            padding: 50px;
            border-radius: 24px;
            max-width: 600px;
            width: 90%;
            box-shadow: 0 25px 70px rgba(0,0,0,0.4);
            animation: slideUp 0.4s;
            text-align: center;
            position: relative;
        ">
            <!-- Confetti Animation -->
            <div style="
                position: absolute;
                top: -30px;
                left: 50%;
                transform: translateX(-50%);
                font-size: 80px;
            ">🎉</div>
            
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
            
            <h2 style="color: var(--success); margin-bottom: 15px; font-size: 32px; font-weight: 800;">
                Bestelling Geplaatst! ✅
            </h2>
            
            <p style="color: var(--gray); font-size: 16px; margin-bottom: 30px; line-height: 1.6;">
                Uw bestelling is succesvol verwerkt en wordt nu gesynchroniseerd met onze systemen.
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
                        <div style="color: var(--gray); font-size: 14px; margin-bottom: 5px;">Order ID</div>
                        <div style="color: var(--dark); font-size: 20px; font-weight: 700;">ORD-${String(orderId).padStart(6, '0')}</div>
                    </div>
                    <div>
                        <div style="color: var(--gray); font-size: 14px; margin-bottom: 5px;">Totaal Bedrag</div>
                        <div style="color: var(--success); font-size: 20px; font-weight: 700;">EUR ${totalAmount.toFixed(2)}</div>
                    </div>
                </div>
                <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #9ae6b4;">
                    <div style="color: var(--gray); font-size: 14px; margin-bottom: 5px;">Aantal Items</div>
                    <div style="color: var(--dark); font-size: 18px; font-weight: 600;">
                        <i class="fas fa-shopping-bag"></i> ${itemCount} ${itemCount === 1 ? 'item' : 'items'}
                    </div>
                </div>
            </div>
            
            <div style="
                background: linear-gradient(135deg, #bee3f8, #90cdf4);
                padding: 15px 20px;
                border-radius: 12px;
                margin-bottom: 30px;
                border-left: 4px solid #4299e1;
            ">
                <p style="color: #2c5282; font-size: 14px; margin: 0; display: flex; align-items: center; justify-content: center; gap: 10px;">
                    <i class="fas fa-sync-alt fa-spin"></i>
                    <strong>Synchronisatie gestart:</strong> RabbitMQ → Salesforce → SAP R/3
                </p>
            </div>
            
            <div id="auto-redirect-timer" style="
                background: linear-gradient(135deg, #feebc8, #fbd38d);
                padding: 12px 20px;
                border-radius: 10px;
                margin-bottom: 25px;
                border-left: 4px solid #ed8936;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 10px;
            ">
                <i class="fas fa-clock"></i>
                <span style="color: #7c2d12; font-weight: 600;">
                    Automatische redirect naar bestellingen overzicht over <span id="countdown">5</span> seconden...
                </span>
            </div>
            
            <div style="display: flex; gap: 15px; justify-content: center; flex-wrap: wrap;">
                <button onclick="window.location.href='orders.html?newOrder=true&orderId=${orderId}'" class="btn btn-success" style="padding: 16px 32px; font-size: 18px; flex: 1; min-width: 200px; box-shadow: 0 4px 15px rgba(72, 187, 120, 0.3);">
                    <i class="fas fa-clipboard-list"></i> Bekijk Bestellingen Nu
                </button>
                <button onclick="window.location.href='Index.html'" class="btn btn-primary" style="padding: 16px 32px; font-size: 18px; flex: 1; min-width: 200px;">
                    <i class="fas fa-home"></i> Terug naar Dashboard
                </button>
            </div>
            
            <p style="color: var(--gray); font-size: 12px; margin-top: 20px; opacity: 0.7;">
                Een bevestigingsmail wordt verzonden naar het opgegeven e-mailadres.
            </p>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // ✅ AUTO REDIRECT COUNTDOWN
    let countdown = 5;
    const countdownElement = modal.querySelector('#countdown');
    
    const countdownInterval = setInterval(() => {
        countdown--;
        if (countdownElement) {
            countdownElement.textContent = countdown;
        }
        
        if (countdown <= 0) {
            clearInterval(countdownInterval);
            console.log('⏰ Auto-redirect to orders.html...');
            window.location.href = `orders.html?newOrder=true&orderId=${orderId}`;
        }
    }, 1000);
    
    // Store interval ID on modal so we can clear it if user clicks button
    modal.dataset.countdownInterval = countdownInterval;
    
    console.log('✅ Order placed modal shown with auto-redirect in 5 seconds');
}
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
    
    // Create modal content
    modal.innerHTML = `
        <div style="
            background: white;
            padding: 50px;
            border-radius: 24px;
            max-width: 600px;
            width: 90%;
            box-shadow: 0 25px 70px rgba(0,0,0,0.4);
            animation: slideUp 0.4s;
            text-align: center;
            position: relative;
        ">
            <!-- Confetti Animation -->
            <div style="
                position: absolute;
                top: -30px;
                left: 50%;
                transform: translateX(-50%);
                font-size: 80px;
            ">🎉</div>
            
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
            
            <h2 style="color: var(--success); margin-bottom: 15px; font-size: 32px; font-weight: 800;">
                Bestelling Geplaatst! ✅
            </h2>
            
            <p style="color: var(--gray); font-size: 16px; margin-bottom: 30px; line-height: 1.6;">
                Uw bestelling is succesvol verwerkt en wordt nu gesynchroniseerd met onze systemen.
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
                        <div style="color: var(--gray); font-size: 14px; margin-bottom: 5px;">Order ID</div>
                        <div style="color: var(--dark); font-size: 20px; font-weight: 700;">ORD-${String(orderId).padStart(6, '0')}</div>
                    </div>
                    <div>
                        <div style="color: var(--gray); font-size: 14px; margin-bottom: 5px;">Totaal Bedrag</div>
                        <div style="color: var(--success); font-size: 20px; font-weight: 700;">EUR ${totalAmount.toFixed(2)}</div>
                    </div>
                </div>
                <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #9ae6b4;">
                    <div style="color: var(--gray); font-size: 14px; margin-bottom: 5px;">Aantal Items</div>
                    <div style="color: var(--dark); font-size: 18px; font-weight: 600;">
                        <i class="fas fa-shopping-bag"></i> ${itemCount} ${itemCount === 1 ? 'item' : 'items'}
                    </div>
                </div>
            </div>
            
            <div style="
                background: linear-gradient(135deg, #bee3f8, #90cdf4);
                padding: 15px 20px;
                border-radius: 12px;
                margin-bottom: 30px;
                border-left: 4px solid #4299e1;
            ">
                <p style="color: #2c5282; font-size: 14px; margin: 0; display: flex; align-items: center; justify-content: center; gap: 10px;">
                    <i class="fas fa-sync-alt fa-spin"></i>
                    <strong>Synchronisatie gestart:</strong> RabbitMQ → Salesforce → SAP R/3
                </p>
            </div>
            
            <div style="display: flex; gap: 15px; justify-content: center; flex-wrap: wrap;">
                <button onclick="window.location.href='orders.html?newOrder=true&orderId=${orderId}'" class="btn btn-success" style="padding: 16px 32px; font-size: 18px; flex: 1; min-width: 200px; box-shadow: 0 4px 15px rgba(72, 187, 120, 0.3);">
                    <i class="fas fa-clipboard-list"></i> Bekijk Bestellingen
                </button>
                <button onclick="window.location.href='Index.html'" class="btn btn-primary" style="padding: 16px 32px; font-size: 18px; flex: 1; min-width: 200px;">
                    <i class="fas fa-home"></i> Terug naar Dashboard
                </button>
            </div>
            
            <p style="color: var(--gray); font-size: 12px; margin-top: 20px; opacity: 0.7;">
                Een bevestigingsmail wordt verzonden naar het opgegeven e-mailadres.
            </p>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Prevent closing by clicking overlay (force user to choose option)
    modal.addEventListener('click', (e) => {
        // Do nothing - force user to click a button
    });
    
    console.log('✅ Order placed modal shown');
}
