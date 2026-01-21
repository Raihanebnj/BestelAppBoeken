// Books.js - Book Management Page Logic

const API_BASE = '/api';
let boeken = [];
let allBoeken = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    checkAdminAndInit();
    setupForm();
});

// Check admin login and initialize UI accordingly
function checkAdminAndInit() {
    const isAdmin = localStorage.getItem('adminLoggedIn') === 'true';

    // Add admin link or login link next to header
    const adminContainer = document.getElementById('admin-link-container');
    if (adminContainer) {
        if (isAdmin) {
            adminContainer.innerHTML = `
                <a href="admin.html" class="btn btn-info" style="text-decoration:none;"><i class="fas fa-user-shield"></i> Admin Paneel</a>
                <button class="btn btn-danger" onclick="logoutAdmin()"><i class="fas fa-sign-out-alt"></i> Uitloggen</button>
            `;
        } else {
            // Add returnUrl to login link
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

        // Hide actions column content via CSS or remove buttons after load
        const actionsStyle = document.createElement('style');
        actionsStyle.id = 'hide-actions-style';
        actionsStyle.innerHTML = `
            /* Hide action buttons for non-admins */
            .card .btn-icon, .card .btn-warning, .card .btn-danger { display: none !important; }
        `;
        document.head.appendChild(actionsStyle);

        // Show prominent message that admin rights are required
        const msgContainer = document.getElementById('message-container');
        if (msgContainer) {
            msgContainer.innerHTML = `
                <div class="admin-warning">
                    <strong>Je moet ingelogd zijn als admin om de boekencatalogus te beheren.</strong><br>
                    Log in als admin om volledige toegang te krijgen.
                </div>
            `;
        }

        // Still load the list (read-only) so visitors can browse
        loadBooks();
        return;
    }

    // If admin, load books with full controls
    loadBooks();
}

// Logout admin helper
function logoutAdmin() {
    localStorage.removeItem('adminLoggedIn');
    localStorage.removeItem('adminUser');
    localStorage.removeItem('loginTime');
    // Refresh page to update UI
    window.location.reload();
}

// Setup form
function setupForm() {
    const form = document.getElementById('book-form');
    if (form) {
        form.addEventListener('submit', handleFormSubmit);
    }
}

// Load all books
async function loadBooks() {
    try {
        const response = await fetch(`${API_BASE}/books`);
        if (!response.ok) throw new Error('Kon boeken niet laden');
        
        const boekenData = await response.json();
        
        // Transform data
        boeken = boekenData.map(boek => ({
            id: boek.id,
            titel: boek.title,
            auteur: boek.author,
            prijs: boek.price,
            voorraadAantal: boek.voorraadAantal,
            isbn: boek.isbn
        }));
        
        allBoeken = [...boeken];
        displayBooks();
        
        document.getElementById('books-loading').style.display = 'none';
        document.getElementById('books-table-container').style.display = 'block';
    } catch (error) {
        console.error('Error loading books:', error);
        document.getElementById('books-loading').innerHTML = 
            '<div class="message error"><i class="fas fa-exclamation-circle"></i> Kon boeken niet laden</div>';
    }
}

// Display books in table
function displayBooks() {
    const tbody = document.getElementById('books-body');
    
    if (boeken.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" style="text-align: center; padding: 40px;">
                    <div style="color: var(--gray);">
                        <i class="fas fa-book" style="font-size: 48px; opacity: 0.3; display: block; margin-bottom: 15px;"></i>
                        <p style="font-size: 18px;">Geen boeken gevonden</p>
                    </div>
                </td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = boeken.map(boek => {
        const voorraadClass = boek.voorraadAantal < 15 ? 'voorraad-laag' : '';
        const voorraadWarning = boek.voorraadAantal < 15 ? '<span class="voorraad-info">(Laag!)</span>' : '';
        
        return `
            <tr>
                <td>${boek.id}</td>
                <td><strong>${escapeHtml(boek.titel)}</strong></td>
                <td>${escapeHtml(boek.auteur)}</td>
                <td><strong>EUR ${boek.prijs.toFixed(2)}</strong></td>
                <td class="${voorraadClass}">
                    ${boek.voorraadAantal} ${voorraadWarning}
                </td>
                <td><small>${escapeHtml(boek.isbn)}</small></td>
                <td>
                    <div style="display:flex; gap:8px; align-items:center;">
                        <button class="btn btn-icon" style="background:#ffe9e6; color:#c53030;" onclick="adjustStock(${boek.id}, -1)" title="-">
                            <i class="fas fa-minus"></i>
                        </button>
                        <button class="btn btn-icon" style="background:#e6ffef; color:#047857;" onclick="adjustStock(${boek.id}, 1)" title="+">
                            <i class="fas fa-plus"></i>
                        </button>
                        <button class="btn btn-icon btn-warning" onclick="editBook(${boek.id})" title="Bewerken">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-icon btn-danger" onclick="deleteBook(${boek.id})" title="Verwijderen">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

// Adjust stock quickly (+/-) and persist via API
async function adjustStock(id, delta) {
    try {
        const boekIndex = allBoeken.findIndex(b => b.id === id);
        if (boekIndex === -1) return showError('Boek niet gevonden');

        const boek = { ...allBoeken[boekIndex] };
        const newStock = Math.max(0, (boek.voorraadAantal || 0) + delta);

        // Optimistic UI update
        allBoeken[boekIndex].voorraadAantal = newStock;
        boeken = boeken.map(b => b.id === id ? { ...b, voorraadAantal: newStock } : b);
        displayBooks();

        // Prepare payload matching API Book model
        const payload = {
            id: boek.id,
            title: boek.titel,
            author: boek.auteur,
            price: parseFloat(boek.prijs),
            voorraadAantal: newStock,
            isbn: boek.isbn
        };

        const resp = await fetch(`${API_BASE}/books/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!resp.ok) {
            // Revert optimistic update
            const error = await resp.json().catch(() => ({ error: 'Kon voorraad niet bijwerken' }));
            // revert
            allBoeken[boekIndex].voorraadAantal = boek.voorraadAantal;
            boeken = boeken.map(b => b.id === id ? { ...b, voorraadAantal: boek.voorraadAantal } : b);
            displayBooks();
            throw new Error(error.error || 'Kon voorraad niet bijwerken');
        }

        showSuccess('Voorraad succesvol bijgewerkt');
    } catch (err) {
        console.error('Error updating stock:', err);
        showError(err.message || 'Fout bij voorraad update');
    }
}

// Search books
function searchBooks() {
    const query = document.getElementById('search-input').value.toLowerCase().trim();
    const resultsDiv = document.getElementById('search-results');
    
    if (!query) {
        boeken = [...allBoeken];
        resultsDiv.style.display = 'none';
    } else {
        boeken = allBoeken.filter(b => 
            b.titel.toLowerCase().includes(query) ||
            b.auteur.toLowerCase().includes(query) ||
            b.isbn.toLowerCase().includes(query)
        );
        
        resultsDiv.style.display = 'block';
        resultsDiv.innerHTML = `<i class="fas fa-search"></i> ${boeken.length} boek(en) gevonden`;
    }
    
    displayBooks();
}

// Clear search
function clearSearch() {
    document.getElementById('search-input').value = '';
    document.getElementById('search-results').style.display = 'none';
    boeken = [...allBoeken];
    displayBooks();
}

// Open modal for new book
function openNewBookModal() {
    document.getElementById('modal-title').innerHTML = '<i class="fas fa-book-medical"></i> Nieuw Boek';
    document.getElementById('book-form').reset();
    document.getElementById('book-id').value = '';
    document.getElementById('book-modal').style.display = 'block';
}

// Edit book
function editBook(id) {
    const boek = allBoeken.find(b => b.id === id);
    if (!boek) return;
    
    document.getElementById('modal-title').innerHTML = '<i class="fas fa-book"></i> Boek Bewerken';
    document.getElementById('book-id').value = boek.id;
    document.getElementById('book-title').value = boek.titel;
    document.getElementById('book-author').value = boek.auteur;
    document.getElementById('book-price').value = boek.prijs;
    document.getElementById('book-stock').value = boek.voorraadAantal;
    document.getElementById('book-isbn').value = boek.isbn;
    document.getElementById('book-modal').style.display = 'block';
}

// Delete book
async function deleteBook(id) {
    if (!confirm('Weet u zeker dat u dit boek wilt verwijderen?')) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/books/${id}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Kon boek niet verwijderen' }));
            throw new Error(error.error || 'Kon boek niet verwijderen');
        }
        
        showSuccess('Boek succesvol verwijderd');
        loadBooks();
    } catch (error) {
        console.error('Error deleting book:', error);
        showError(error.message);
    }
}

// Handle form submit
async function handleFormSubmit(e) {
    e.preventDefault();
    
    const id = document.getElementById('book-id').value;
    const bookData = {
        title: document.getElementById('book-title').value.trim(),
        author: document.getElementById('book-author').value.trim(),
        price: parseFloat(document.getElementById('book-price').value),
        voorraadAantal: parseInt(document.getElementById('book-stock').value),
        isbn: document.getElementById('book-isbn').value.trim()
    };
    
    // Validation
    if (bookData.price <= 0) {
        showError('Prijs moet groter zijn dan 0');
        return;
    }
    
    if (bookData.voorraadAantal < 0) {
        showError('Voorraad kan niet negatief zijn');
        return;
    }
    
    try {
        let response;
        if (id) {
            // Update existing book
            response = await fetch(`${API_BASE}/books/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(bookData)
            });
        } else {
            // Create new book
            response = await fetch(`${API_BASE}/books`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(bookData)
            });
        }
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Er is een fout opgetreden' }));
            throw new Error(error.error || 'Kon boek niet opslaan');
        }
        
        showSuccess(id ? 'Boek succesvol bijgewerkt' : 'Boek succesvol toegevoegd');
        closeModal();
        loadBooks();
    } catch (error) {
        console.error('Error saving book:', error);
        showError(error.message);
    }
}

// Close modal
function closeModal() {
    document.getElementById('book-modal').style.display = 'none';
    document.getElementById('book-form').reset();
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
    const modal = document.getElementById('book-modal');
    if (event.target === modal) {
        closeModal();
    }
}
