// Customers.js - Customer Management Page Logic

const API_BASE = '/api';
let klanten = [];
let allKlanten = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    checkAdminAndInit();
    setupForm();
});

// Check admin login and initialize UI accordingly
function checkAdminAndInit() {
    const isAdmin = localStorage.getItem('adminLoggedIn') === 'true';
    console.log('checkAdminAndInit(): adminLoggedIn=', localStorage.getItem('adminLoggedIn'));

    // Add admin link or login link next to header
    const adminContainer = document.getElementById('admin-link-container');
    if (adminContainer) {
        if (isAdmin) {
            adminContainer.innerHTML = `
                <a href="admin.html" class="btn btn-info" style="text-decoration:none;"><i class="fas fa-user-shield"></i> Admin Paneel</a>
                <button class="btn btn-danger" onclick="logoutAdmin()"><i class="fas fa-sign-out-alt"></i> Uitloggen</button>
            `;
            // If previously a style was added to hide action buttons, remove it for admins
            const prevStyle = document.getElementById('hide-actions-style');
            if (prevStyle) prevStyle.remove();
            // Ensure the new button is visible for admins
            const newBtn = document.querySelector('.card-header .btn-success');
            if (newBtn) newBtn.style.display = '';
            // Ensure the customers card is visible for admins
            const card = document.getElementById('customers-card');
            if (card) card.style.display = '';
        } else {
            const returnUrl = encodeURIComponent(window.location.pathname.replace(/^\//, ''));
            adminContainer.innerHTML = `
                <a href="login.html?returnUrl=${returnUrl}" class="btn btn-primary" style="text-decoration:none;"><i class="fas fa-sign-in-alt"></i> Inloggen als Admin</a>
            `;
        }
    }

    // If not admin, hide management controls and show message
    if (!isAdmin) {
        // Hide add-new button
        const newBtn = document.querySelector('.card-header .btn-success');
        if (newBtn) newBtn.style.display = 'none';

        // Hide actions column content via CSS
        const actionsStyle = document.createElement('style');
        actionsStyle.id = 'hide-actions-style';
        actionsStyle.innerHTML = `
            .card .btn-icon, .card .btn-warning, .card .btn-danger { display: none !important; }
        `;
        document.head.appendChild(actionsStyle);

        // Show prominent message that admin rights are required
        const msgContainer = document.getElementById('message-container');
        if (msgContainer) {
            msgContainer.style.display = 'block';
            const returnUrl = encodeURIComponent(window.location.pathname.replace(/^\//, ''));
            msgContainer.innerHTML = `
                <div class="admin-warning">
                    <strong>Je moet ingelogd zijn als admin om de klantencatalogus te beheren.</strong><br>
                    <a href="login.html?returnUrl=${returnUrl}" style="color: #856404; font-weight:700; text-decoration:underline;">Log in als admin</a> om volledige toegang te krijgen.
                </div>
            `;
            console.log('Displayed admin warning in customers page');
        } else {
            console.warn('message-container not found on customers page');
        }

        // Still load the list (read-only) so visitors can browse
        loadCustomers();
        return;
    }

    // If admin, load customers with full controls
    loadCustomers();
}

function logoutAdmin() {
    localStorage.removeItem('adminLoggedIn');
    localStorage.removeItem('adminUser');
    localStorage.removeItem('loginTime');
    window.location.reload();
}

// Helper: Kontrolleer of gebruiker admin is, zo niet toon waarschuwing en stop actie
function ensureAdminOrShowWarning() {
    const isAdmin = localStorage.getItem('adminLoggedIn') === 'true';
    if (isAdmin) return true;

    const msgContainer = document.getElementById('message-container');
    if (msgContainer) {
        const returnUrl = encodeURIComponent(window.location.pathname.replace(/^\//, ''));
        msgContainer.innerHTML = `\
            <div class="admin-warning">\
                <strong>Je moet ingelogd zijn als admin om deze actie uit te voeren.</strong><br/>\
                <a href="login.html?returnUrl=${returnUrl}" style="color: #856404; font-weight:700; text-decoration:underline;">Log in als admin</a> om door te gaan.\
            </div>`;
        window.scrollTo({ top: 0, behavior: 'smooth' });
    } else {
        alert('Je moet ingelogd zijn als admin om deze actie uit te voeren.');
    }

    return false;
}

// Setup form
function setupForm() {
    const form = document.getElementById('customer-form');
    if (form) {
        form.addEventListener('submit', handleFormSubmit);
    }
}

// Validate customer form fields on client-side
function validateCustomerForm() {
    clearFieldErrors();
    const name = document.getElementById('customer-name').value.trim();
    const email = document.getElementById('customer-email').value.trim();
    const phone = document.getElementById('customer-phone').value.trim();
    const address = document.getElementById('customer-address').value.trim();

    const errors = [];

    if (!name) {
        errors.push('Naam is verplicht');
        showFieldError('customer-name', 'Naam is verplicht');
    } else if (name.length > 100) {
        errors.push('Naam is te lang (max 100 tekens)');
        showFieldError('customer-name', 'Te lang (max 100)');
    }

    if (!email) {
        errors.push('E-mail is verplicht');
        showFieldError('customer-email', 'E-mail is verplicht');
    } else if (!/^\S+@\S+\.\S+$/.test(email)) {
        errors.push('Ongeldig e-mail formaat');
        showFieldError('customer-email', 'Ongeldig e-mail');
    }

    if (phone && phone.length > 20) {
        errors.push('Telefoonnummer is te lang');
        showFieldError('customer-phone', 'Te lang');
    }

    if (address && address.length > 300) {
        errors.push('Adres is te lang');
        showFieldError('customer-address', 'Te lang');
    }

    return { valid: errors.length === 0, errors };
}

function showFieldError(fieldId, message) {
    try {
        const field = document.getElementById(fieldId);
        if (!field) return;
        field.classList.add('input-error');
        let hint = field.parentElement.querySelector('.field-error');
        if (!hint) {
            hint = document.createElement('div');
            hint.className = 'field-error';
            hint.style.color = '#b02a37';
            hint.style.fontSize = '12px';
            hint.style.marginTop = '6px';
            field.parentElement.appendChild(hint);
        }
        hint.textContent = message;
    } catch (e) {
        console.warn('showFieldError failed', e);
    }
}

function clearFieldErrors() {
    try {
        document.querySelectorAll('.input-error').forEach(el => el.classList.remove('input-error'));
        document.querySelectorAll('.field-error').forEach(el => el.remove());
    } catch (e) {
        // ignore
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
    const isAdmin = localStorage.getItem('adminLoggedIn') === 'true';

    // Ensure header and new-button visibility reflect admin state
    try {
        const actionsHeader = document.querySelector('.card thead th:last-child');
        if (actionsHeader) actionsHeader.style.display = isAdmin ? '' : 'none';
        const newBtn = document.getElementById('new-customer-btn');
        if (newBtn) newBtn.style.display = isAdmin ? '' : 'none';
    } catch (e) {
        console.warn('Error toggling header/new button visibility', e);
    }

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
    tbody.innerHTML = klanten.map(klant => {
        const actionsHtml = isAdmin ? `
            <button class="btn btn-icon btn-warning" onclick="editCustomer(${klant.id})" title="Bewerken">
                <i class="fas fa-edit"></i>
            </button>
            <button class="btn btn-icon btn-danger" onclick="deleteCustomer(${klant.id})" title="Verwijderen">
                <i class="fas fa-trash"></i>
            </button>
        ` : '';

        return `
        <tr>
            <td>${klant.id}</td>
            <td><strong>${escapeHtml(klant.naam)}</strong></td>
            <td>${escapeHtml(klant.email)}</td>
            <td>${escapeHtml(klant.telefoon)}</td>
            <td>${escapeHtml(klant.adres || '-')}</td>
            <td>${actionsHtml}</td>
        </tr>
        `;
    }).join('');
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
    if (!ensureAdminOrShowWarning()) return;

    document.getElementById('modal-title').innerHTML = '<i class="fas fa-user-plus"></i> Nieuwe Klant';
    document.getElementById('customer-form').reset();
    document.getElementById('customer-id').value = '';
    document.getElementById('customer-modal').style.display = 'block';
}

// Edit customer
function editCustomer(id) {
    if (!ensureAdminOrShowWarning()) return;

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
    if (!ensureAdminOrShowWarning()) return;

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
    if (!ensureAdminOrShowWarning()) return;
    
    const id = document.getElementById('customer-id').value;

    // Client-side validation
    const validation = validateCustomerForm();
    if (!validation.valid) {
        showError(validation.errors.join(' • '));
        return;
    }

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
            // Try to parse JSON error, fallback to text
            let errorMessage = 'Kon klant niet opslaan';
            try {
                const errJson = await response.json();
                errorMessage = errJson.error || errJson.message || errorMessage;
            } catch (e) {
                try {
                    const text = await response.text();
                    if (text) errorMessage = text;
                } catch { }
            }
            throw new Error(errorMessage);
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
