// Admin.js - Admin Panel Logic

const API_BASE = '/api';
let boeken = [];
let allBoeken = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    loadBooks();
    setupForm();
    updateStatistics();
});

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
        updateStatistics();
        
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
        const voorraadClass = boek.voorraadAantal < 15 ? 'voorraad-laag' : 'voorraad-ok';
        const voorraadIcon = boek.voorraadAantal < 15 ? 
            '<i class="fas fa-exclamation-triangle"></i>' : 
            '<i class="fas fa-check-circle"></i>';
        
        return `
            <tr>
                <td><strong>${boek.id}</strong></td>
                <td><strong>${escapeHtml(boek.titel)}</strong></td>
                <td>${escapeHtml(boek.auteur)}</td>
                <td><strong>EUR ${boek.prijs.toFixed(2)}</strong></td>
                <td class="${voorraadClass}">
                    ${voorraadIcon} ${boek.voorraadAantal}
                    ${boek.voorraadAantal < 15 ? '<br><small>(Laag!)</small>' : ''}
                </td>
                <td><small>${escapeHtml(boek.isbn)}</small></td>
                <td>
                    <button class="btn btn-warning" onclick="editBook(${boek.id})" style="padding: 8px 16px; margin-bottom: 5px;">
                        <i class="fas fa-edit"></i> Bewerk
                    </button>
                    <button class="btn btn-danger" onclick="deleteBook(${boek.id})" style="padding: 8px 16px;">
                        <i class="fas fa-trash"></i> Verwijder
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

// Update statistics
function updateStatistics() {
    if (boeken.length === 0) return;
    
    const totalBooks = boeken.length;
    const totalStock = boeken.reduce((sum, b) => sum + b.voorraadAantal, 0);
    const lowStockCount = boeken.filter(b => b.voorraadAantal < 15).length;
    const totalValue = boeken.reduce((sum, b) => sum + (b.prijs * b.voorraadAantal), 0);
    
    document.getElementById('total-books').textContent = totalBooks;
    document.getElementById('total-stock').textContent = totalStock;
    document.getElementById('low-stock-count').textContent = lowStockCount;
    document.getElementById('total-value').textContent = `€${totalValue.toFixed(2)}`;
}

// Search books
function searchBooks() {
    const query = document.getElementById('search-input').value.toLowerCase().trim();
    
    if (!query) {
        boeken = [...allBoeken];
    } else {
        boeken = allBoeken.filter(b => 
            b.titel.toLowerCase().includes(query) ||
            b.auteur.toLowerCase().includes(query) ||
            b.isbn.toLowerCase().includes(query)
        );
    }
    
    displayBooks();
    showSuccess(`${boeken.length} boek(en) gevonden`);
}

// Clear search
function clearSearch() {
    document.getElementById('search-input').value = '';
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

// Database functions
async function createBackup() {
    try {
        const response = await fetch(`${API_BASE}/backup/create`, {
            method: 'POST'
        });
        
        if (!response.ok) throw new Error('Backup maken mislukt');
        
        const result = await response.json();
        showSuccess('? Backup succesvol gemaakt!');
    } catch (error) {
        console.error('Error creating backup:', error);
        showError('Kon backup niet maken');
    }
}

async function exportData() {
    try {
        showSuccess('Export wordt voorbereid...');
        
        // Export JSON
        const response = await fetch(`${API_BASE}/backup/export-json`, {
            method: 'POST'
        });
        
        if (!response.ok) throw new Error('Export mislukt');
        
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `bookstore_export_${new Date().toISOString().split('T')[0]}.json`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        showSuccess('? Data succesvol geëxporteerd!');
    } catch (error) {
        console.error('Error exporting data:', error);
        showError('Kon data niet exporteren');
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

// ============================================
// DATABASE BACKUP & EXPORT FUNCTIES
// ============================================

async function createBackup() {
    try {
        showSuccess('Backup wordt aangemaakt...');
        const response = await fetch('/api/backup/create', { method: 'POST' });
        const data = await response.json();
        if (data.success) {
            showSuccess('Backup succesvol aangemaakt: ' + data.fileName);
            loadBackups();
        }
    } catch (error) {
        showError('Fout bij aanmaken backup: ' + error.message);
    }
}

async function loadBackups() {
    try {
        const response = await fetch('/api/backup/list');
        const data = await response.json();
        const container = document.getElementById('backup-list-container');
        
        if (!data.backups || data.backups.length === 0) {
            container.innerHTML = '<div style="text-align: center; padding: 40px; color: #999;"><i class="fas fa-database" style="font-size: 48px; opacity: 0.3; margin-bottom: 15px;"></i><p>Nog geen backups beschikbaar</p></div>';
            return;
        }
        
        container.innerHTML = '<p style="margin-bottom: 15px;"><strong>' + data.count + '</strong> backup(s) gevonden</p>';
    } catch (error) {
        const container = document.getElementById('backup-list-container');
        container.innerHTML = '<div class="message error">Fout bij laden backups</div>';
    }
}

async function exportOrdersJson() {
    try {
        showSuccess('JSON export gestart...');
        window.open('/api/backup/export/orders/json', '_blank');
        setTimeout(() => showSuccess('JSON export compleet!'), 500);
    } catch (error) {
        showError('Fout bij JSON export');
    }
}

async function exportOrdersTxt() {
    try {
        showSuccess('TXT export gestart...');
        window.open('/api/backup/export/orders/txt', '_blank');
        setTimeout(() => showSuccess('TXT export compleet!'), 500);
    } catch (error) {
        showError('Fout bij TXT export');
    }
}

async function exportOrdersPdf() {
    try {
        showSuccess('PDF export gestart...');
        window.open('/api/backup/export/orders/pdf', '_blank');
        setTimeout(() => showSuccess('PDF export compleet!'), 500);
    } catch (error) {
        showError('Fout bij PDF export');
    }
}

// Load backups on page load
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('backup-list-container')) {
        loadBackups();
    }
});

// Close modal when clicking outside
window.onclick = function(event) {
    const modal = document.getElementById('book-modal');
    if (event.target === modal) {
        closeModal();
    }
}
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
