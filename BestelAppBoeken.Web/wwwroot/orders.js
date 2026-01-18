// Orders.js - Orders Management Page Logic

const API_BASE = '/api';
let orders = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    loadOrders();
});

// Load all orders
async function loadOrders() {
    try {
        const response = await fetch(`${API_BASE}/orders`);
        if (!response.ok) throw new Error('Kon bestellingen niet laden');
        
        orders = await response.json();
        displayOrders();
        updateStatistics();
        
        document.getElementById('orders-loading').style.display = 'none';
        document.getElementById('orders-table-container').style.display = 'block';
    } catch (error) {
        console.error('Error loading orders:', error);
        document.getElementById('orders-loading').innerHTML = 
            '<div style="color: var(--danger); text-align: center;"><i class="fas fa-exclamation-circle"></i> Kon bestellingen niet laden</div>';
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
                <td>
                    <button class="btn btn-info" onclick="viewOrderDetails(${order.id})" style="padding: 8px 16px;">
                        <i class="fas fa-eye"></i> Details
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

// Update statistics
function updateStatistics() {
    const totalOrders = orders.length;
    const totalRevenue = orders.reduce((sum, order) => sum + order.totalAmount, 0);
    const pendingOrders = orders.filter(o => o.status === 'Pending').length;
    const completedOrders = orders.filter(o => o.status === 'Verwerkt').length;
    
    document.getElementById('total-orders').textContent = totalOrders;
    document.getElementById('total-revenue').textContent = `€${totalRevenue.toFixed(2)}`;
    document.getElementById('pending-orders').textContent = pendingOrders;
    document.getElementById('completed-orders').textContent = completedOrders;
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
