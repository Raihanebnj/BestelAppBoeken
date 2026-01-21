// Orders.js - Orders Management Page Logic

const API_BASE = '/api';
let orders = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('?? Orders.html page loaded');
    
    // Check if we came from a successful order placement
    const hasNewOrder = checkForNewOrder();
    
    // First try to show cached orders quickly to reduce perceived load time
    try {
        const cached = localStorage.getItem('cached_orders');
        if (cached) {
            orders = JSON.parse(cached);
            console.log('?? Loaded orders from cache:', orders.length);
            displayOrders();
            updateSummary();
            document.getElementById('orders-loading').style.display = 'none';
            document.getElementById('orders-table-container').style.display = 'block';
        }

// Download invoice for a specific order (from orders page)
function downloadInvoiceFromOrders(orderId) {
    if (!orderId) return;
    const id = orderId;
    // Show a quick message
    if (typeof showInfo === 'function') showInfo('Factuur wordt gegenereerd...');

    fetch(`/api/backup/export/order/${id}/invoice`, { headers: { 'X-Api-Key': 'BOOKSTORE-API-2026-SECRET-KEY-XYZ789' } })
        .then(resp => {
            if (!resp.ok) throw new Error('Kon factuur niet genereren');
            return resp.blob();
        })
        .then(blob => {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `factuur_${id}_${Date.now()}.pdf`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(url);
            if (typeof showSuccess === 'function') showSuccess('Factuur gedownload');
        })
        .catch(err => {
            console.error('Error downloading invoice:', err);
            if (typeof showError === 'function') showError('Kon factuur niet genereren');
        });
}
    } catch (e) {
        console.warn('No cached orders available or parse error', e);
    }

    // Always load fresh orders in background
    loadOrders();
    
    // If new order was detected, schedule a refresh after load
    if (hasNewOrder) {
        console.log('?? New order detected, will refresh after 2 seconds...');
        setTimeout(() => {
            console.log('?? Refreshing orders to ensure new order is visible...');
            loadOrders();
        }, 2000);
    }
});

// ? Check for new order notification from cart.html
function checkForNewOrder() {
    const urlParams = new URLSearchParams(window.location.search);
    const newOrder = urlParams.get('newOrder');
    const orderId = urlParams.get('orderId');
    
    if (newOrder === 'true') {
        console.log('? New order detected from URL params!', { orderId });
        
        // Show success notification
        setTimeout(() => {
            if (typeof showSuccess === 'function') {
                showSuccess(`? Bestelling succesvol geplaatst!${orderId ? ` Order ID: ${orderId}` : ''}`);
            } else {
                console.log('Success notification:', `Order ${orderId} placed successfully`);
            }
            
            // Clean URL (remove query params)
            window.history.replaceState({}, document.title, window.location.pathname);
        }, 500);
        
        return true;
    }
    
    return false;
}

// Load all orders
async function loadOrders() {
    try {
        console.log('?? Loading orders from API...');
        const response = await fetch(`${API_BASE}/orders`, {
            headers: {
                'X-Api-Key': 'BOOKSTORE-API-2026-SECRET-KEY-XYZ789'  // ? API KEY TOEGEVOEGD!
            }
        });
        if (!response.ok) throw new Error('Kon bestellingen niet laden');
        
        orders = await response.json();
        console.log(`? Loaded ${orders.length} orders`);
        
        displayOrders();
        updateStatistics();

        // Notify other pages (admin) that orders were updated
        try {
            if ('BroadcastChannel' in window) {
                const bc = new BroadcastChannel('orders_channel');
                bc.postMessage({ type: 'orders-updated', timestamp: Date.now(), count: orders.length });
                bc.close();
            }
        } catch (e) {
            console.warn('BroadcastChannel not available:', e);
        }

        // Fallback via localStorage to trigger storage event in other tabs
        try {
            localStorage.setItem('orders-updated', Date.now().toString());
        } catch (e) {
            console.warn('localStorage orders-updated failed:', e);
        }
        
        document.getElementById('orders-loading').style.display = 'none';
        document.getElementById('orders-table-container').style.display = 'block';
    } catch (error) {
        console.error('? Error loading orders:', error);
        document.getElementById('orders-loading').innerHTML = 
            '<div style="color: var(--danger); text-align: center;"><i class="fas fa-exclamation-circle"></i> Kon bestellingen niet laden</div>';
        
        if (typeof showError === 'function') {
            showError('Kon bestellingen niet laden: ' + error.message);
        }
    }
}

// ? Refresh orders (can be called from UI button)
async function refreshOrders() {
    console.log('?? Refreshing orders...');
    document.getElementById('orders-loading').style.display = 'block';
    document.getElementById('orders-table-container').style.display = 'none';
    
    await loadOrders();
    
    if (typeof showInfo === 'function') {
        showInfo('?? Orders bijgewerkt!');
    }
}

// Display orders in table
function displayOrders() {
    const tbody = document.getElementById('orders-body');
    
    if (orders.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 40px;">
                    <div style="color: var(--gray);">
                        <i class="fas fa-inbox" style="font-size: 48px; opacity: 0.3; display: block; margin-bottom: 15px;"></i>
                        <p style="font-size: 18px;">Nog geen bestellingen</p>
                    </div>
                </td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = orders.map(order => {
        const statusClass = order.status === 'Verwerkt' ? 'status-verwerkt' : 'status-pending';
        const klantNaam = order.klant ? escapeHtml(order.klant.naam) : 'Onbekend';
        const itemCount = order.items ? order.items.reduce((sum, item) => sum + item.aantal, 0) : 0;
        
        return `
            <tr>
                <td><strong>ORD-${String(order.id).padStart(6, '0')}</strong></td>
                <td>${klantNaam}</td>
                <td>${escapeHtml(order.customerEmail)}</td>
                <td>${formatDate(order.orderDate)}</td>
                <td>${itemCount} item(s)</td>
                <td><strong>EUR ${order.totalAmount.toFixed(2)}</strong></td>
                <td><span class="status-badge ${statusClass}">${order.status}</span></td>
                <td style="display:flex; gap:8px;">
                    <button class="btn btn-info" onclick="viewOrderDetails(${order.id})" style="padding: 8px 12px;">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="btn btn-info" onclick="downloadInvoiceFromOrders(${order.id})" style="padding: 8px 12px;">
                        <i class="fas fa-file-invoice"></i>
                    </button>
                </td>
            </tr>
        `;
    }).join('');

    // Update summary (totals)
    updateSummary();
}

// Update statistics (DISABLED - statistics removed from UI)
function updateStatistics() {
    // Statistics UI elements removed - function kept to prevent errors
    console.log('?? Statistics update skipped (UI removed)');
}

function updateSummary() {
    const summary = document.getElementById('orders-summary');
    if (!summary) return;

    const totalOrders = orders.length;
    const totalAmount = orders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);

    summary.innerHTML = `Totaal bestellingen: <strong>${totalOrders}</strong> — Totaal omzet: <strong>EUR ${totalAmount.toFixed(2)}</strong>`;
}

// View order details
function viewOrderDetails(orderId) {
    const order = orders.find(o => o.id === orderId);
    if (!order) return;
    
    const klantNaam = order.klant ? escapeHtml(order.klant.naam) : 'Onbekend';
    const statusClass = order.status === 'Verwerkt' ? 'status-verwerkt' : 'status-pending';
    
    let itemsHtml = '';
    if (order.items && order.items.length > 0) {
        itemsHtml = order.items.map(item => `
            <div class="order-item">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <strong>${escapeHtml(item.titel)}</strong><br>
                        <small style="color: var(--gray);">Aantal: ${item.aantal} × EUR ${item.prijs.toFixed(2)}</small>
                    </div>
                    <div style="font-weight: 700; color: var(--success);">
                        EUR ${(item.prijs * item.aantal).toFixed(2)}
                    </div>
                </div>
            </div>
        `).join('');
    }
    
    const content = `
        <div class="order-detail">
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px;">
                <div>
                    <strong><i class="fas fa-hashtag"></i> Order Nummer:</strong><br>
                    <span style="font-size: 18px; color: var(--primary);">ORD-${String(order.id).padStart(6, '0')}</span>
                </div>
                <div>
                    <strong><i class="fas fa-calendar"></i> Datum:</strong><br>
                    ${formatDate(order.orderDate)}
                </div>
            </div>
        </div>
        
        <div class="order-detail">
            <strong><i class="fas fa-user"></i> Klant Informatie:</strong><br>
            <div style="margin-top: 10px;">
                <strong>Naam:</strong> ${klantNaam}<br>
                <strong>Email:</strong> ${escapeHtml(order.customerEmail)}
            </div>
        </div>
        
        <div class="order-detail">
            <strong><i class="fas fa-info-circle"></i> Status:</strong><br>
            <span class="status-badge ${statusClass}" style="margin-top: 10px;">${order.status}</span>
        </div>
        
        <div class="order-items">
            <strong style="font-size: 16px; display: block; margin-bottom: 15px;">
                <i class="fas fa-shopping-bag"></i> Bestelde Items:
            </strong>
            ${itemsHtml}
        </div>
        
        <div class="order-detail" style="background: linear-gradient(135deg, #f0fff4, #c6f6d5); border: 2px solid var(--success); margin-top: 20px;">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <strong style="font-size: 18px;"><i class="fas fa-euro-sign"></i> Totaalbedrag:</strong>
                <span style="font-size: 24px; font-weight: 700; color: var(--success);">
                    EUR ${order.totalAmount.toFixed(2)}
                </span>
            </div>
        </div>
    `;
    
    document.getElementById('order-details-content').innerHTML = content;
    document.getElementById('order-modal').style.display = 'block';
}

// Close modal
function closeModal() {
    document.getElementById('order-modal').style.display = 'none';
}

// Format date
function formatDate(dateString) {
    const date = new Date(dateString);
    const options = { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    return date.toLocaleDateString('nl-NL', options);
}

// Escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Close modal when clicking outside
window.onclick = function(event) {
    const modal = document.getElementById('order-modal');
    if (event.target === modal) {
        closeModal();
    }
}
