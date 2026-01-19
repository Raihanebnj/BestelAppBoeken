// Customers.js - Customer Management Page Logic

const API_BASE = '/api';
let klanten = [];
let allKlanten = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    loadCustomers();
    setupForm();
});

// Setup form
function setupForm() {
    const form = document.getElementById('customer-form');
    if (form) {
        form.addEventListener('submit', handleFormSubmit);
    }
}

// Load all customers
async function loadCustomers() {
    try {
        const response = await fetch(`${API_BASE}/klanten`);
        if (!response.ok) throw new Error('Kon klanten niet laden');
        
        klanten = await response.json();
        allKlanten = [...klanten];
        displayCustomers();
        
        document.getElementById('customers-loading').style.display = 'none';
        document.getElementById('customers-table-container').style.display = 'block';
    } catch (error) {
        console.error('Error loading customers:', error);
        document.getElementById('customers-loading').innerHTML = 
            '<div class="message error"><i class="fas fa-exclamation-circle"></i> Kon klanten niet laden</div>';
    }
}

// Display customers in table
function displayCustomers() {
    const tbody = document.getElementById('customers-body');
    
    if (klanten.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 40px;">
                    <div class="empty-state">
                        <i class="fas fa-user-slash"></i>
                        <p style="font-size: 18px; margin-top: 15px;">Geen klanten gevonden</p>
                    </div>
                </td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = klanten.map(klant => `
        <tr>
            <td>${klant.id}</td>
            <td><strong>${escapeHtml(klant.naam)}</strong></td>
            <td>${escapeHtml(klant.email)}</td>
            <td>${escapeHtml(klant.telefoon)}</td>
            <td>${escapeHtml(klant.adres || '-')}</td>
            <td>
                <button class="btn btn-icon btn-warning" onclick="editCustomer(${klant.id})" title="Bewerken">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn btn-icon btn-danger" onclick="deleteCustomer(${klant.id})" title="Verwijderen">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

// Search customers
function searchCustomers() {
    const query = document.getElementById('search-input').value.toLowerCase().trim();
    const resultsDiv = document.getElementById('search-results');
    
    if (!query) {
        klanten = [...allKlanten];
        resultsDiv.style.display = 'none';
    } else {
        klanten = allKlanten.filter(k => 
            k.naam.toLowerCase().includes(query) ||
            k.email.toLowerCase().includes(query) ||
            k.telefoon.toLowerCase().includes(query) ||
            (k.adres && k.adres.toLowerCase().includes(query))
        );
        
        resultsDiv.style.display = 'block';
        resultsDiv.innerHTML = `<i class="fas fa-search"></i> ${klanten.length} klant(en) gevonden`;
    }
    
    displayCustomers();
}

// Clear search
function clearSearch() {
    document.getElementById('search-input').value = '';
    document.getElementById('search-results').style.display = 'none';
    klanten = [...allKlanten];
    displayCustomers();
}

// Open modal for new customer
function openNewCustomerModal() {
    document.getElementById('modal-title').innerHTML = '<i class="fas fa-user-plus"></i> Nieuwe Klant';
    document.getElementById('customer-form').reset();
    document.getElementById('customer-id').value = '';
    document.getElementById('customer-modal').style.display = 'block';
}

// Edit customer
function editCustomer(id) {
    const klant = allKlanten.find(k => k.id === id);
    if (!klant) return;
    
    document.getElementById('modal-title').innerHTML = '<i class="fas fa-user-edit"></i> Klant Bewerken';
    document.getElementById('customer-id').value = klant.id;
    document.getElementById('customer-name').value = klant.naam;
    document.getElementById('customer-email').value = klant.email;
    document.getElementById('customer-phone').value = klant.telefoon;
    document.getElementById('customer-address').value = klant.adres || '';
    document.getElementById('customer-modal').style.display = 'block';
}

// Delete customer
async function deleteCustomer(id) {
    if (!confirm('Weet u zeker dat u deze klant wilt verwijderen?')) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/klanten/${id}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Kon klant niet verwijderen' }));
            throw new Error(error.error || 'Kon klant niet verwijderen');
        }
        
        showSuccess('Klant succesvol verwijderd');
        loadCustomers();
    } catch (error) {
        console.error('Error deleting customer:', error);
        showError(error.message);
    }
}

// Handle form submit
async function handleFormSubmit(e) {
    e.preventDefault();
    
    const id = document.getElementById('customer-id').value;
    const customerData = {
        naam: document.getElementById('customer-name').value.trim(),
        email: document.getElementById('customer-email').value.trim(),
        telefoon: document.getElementById('customer-phone').value.trim(),
        adres: document.getElementById('customer-address').value.trim()
    };
    
    try {
        let response;
        if (id) {
            // Update existing customer
            response = await fetch(`${API_BASE}/klanten/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(customerData)
            });
        } else {
            // Create new customer
            response = await fetch(`${API_BASE}/klanten`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(customerData)
            });
        }
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Er is een fout opgetreden' }));
            throw new Error(error.error || 'Kon klant niet opslaan');
        }
        
        showSuccess(id ? 'Klant succesvol bijgewerkt' : 'Klant succesvol toegevoegd');
        closeModal();
        loadCustomers();
    } catch (error) {
        console.error('Error saving customer:', error);
        showError(error.message);
    }
}

// Close modal
function closeModal() {
    document.getElementById('customer-modal').style.display = 'none';
    document.getElementById('customer-form').reset();
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
    window.scrollTo({ top: 0, behavior: 'smooth' });
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
    window.scrollTo({ top: 0, behavior: 'smooth' });
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
    const modal = document.getElementById('customer-modal');
    if (event.target === modal) {
        closeModal();
    }
}
