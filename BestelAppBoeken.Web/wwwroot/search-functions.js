// BestelAppBoeken - Search Functions
// Zoekfunctionaliteit voor Klanten en Boeken

// ============================================
// KLANTEN ZOEKEN
// ============================================

async function searchKlanten() {
    const searchInput = document.getElementById('klanten-search-input');
    const query = searchInput.value.trim();

    if (!query) {
        showError('Voer een zoekterm in');
        return;
    }

    try {
        // Toon loading state
        const loadingEl = document.getElementById('klanten-loading');
        const tabelEl = document.getElementById('klanten-tabel');

        if (loadingEl) loadingEl.style.display = 'block';
        if (tabelEl) tabelEl.style.display = 'none';

        // TODO: Uncomment wanneer API endpoint beschikbaar is
        // const results = await apiCall(`/klanten/search?query=${encodeURIComponent(query)}`);

        // Mock search: filter lokale klanten data
        const results = klanten.filter(klant => {
            const searchLower = query.toLowerCase();
            return klant.naam.toLowerCase().includes(searchLower) ||
                klant.email.toLowerCase().includes(searchLower) ||
                klant.telefoon.toLowerCase().includes(searchLower) ||
                (klant.adres && klant.adres.toLowerCase().includes(searchLower));
        });

        // Update display met zoekresultaten
        displayKlanten(results);

        // Toon zoekresultaten bericht
        const resultsDiv = document.getElementById('klanten-search-results');
        if (resultsDiv) {
            if (results.length === 0) {
                resultsDiv.innerHTML = `
                    <i class="fas fa-search"></i> 
                    Geen resultaten gevonden voor "<strong>${escapeHtml(query)}</strong>"
                `;
                resultsDiv.style.color = 'var(--danger)';
            } else {
                resultsDiv.innerHTML = `
                    <i class="fas fa-check-circle"></i> 
                    ${results.length} ${results.length === 1 ? 'klant' : 'klanten'} gevonden voor "<strong>${escapeHtml(query)}</strong>"
                `;
                resultsDiv.style.color = 'var(--success)';
            }
            resultsDiv.style.display = 'block';
        }

        // Verberg loading, toon tabel
        if (loadingEl) loadingEl.style.display = 'none';
        if (tabelEl) tabelEl.style.display = 'table';

    } catch (error) {
        console.error('Fout bij zoeken klanten:', error);
        showError('Er ging iets mis bij het zoeken');

        // Herstel UI state
        const loadingEl = document.getElementById('klanten-loading');
        const tabelEl = document.getElementById('klanten-tabel');
        if (loadingEl) loadingEl.style.display = 'none';
        if (tabelEl) tabelEl.style.display = 'table';
    }
}

function clearKlantenSearch() {
    const searchInput = document.getElementById('klanten-search-input');
    const resultsDiv = document.getElementById('klanten-search-results');

    if (searchInput) searchInput.value = '';
    if (resultsDiv) {
        resultsDiv.innerHTML = '';
        resultsDiv.style.display = 'none';
    }

    // Herlaad alle klanten
    loadKlanten();
}

// ============================================
// BOEKEN ZOEKEN
// ============================================

async function searchBoeken() {
    const searchInput = document.getElementById('boeken-search-input');
    const query = searchInput.value.trim();

    if (!query) {
        showError('Voer een zoekterm in');
        return;
    }

    try {
        // Toon loading state
        const loadingEl = document.getElementById('boeken-loading');
        const tabelEl = document.getElementById('boeken-tabel');

        if (loadingEl) loadingEl.style.display = 'block';
        if (tabelEl) tabelEl.style.display = 'none';

        // TODO: Uncomment wanneer API endpoint beschikbaar is
        // const results = await apiCall(`/boeken/search?query=${encodeURIComponent(query)}`);

        // Mock search: filter lokale boeken data
        const results = boeken.filter(boek => {
            const searchLower = query.toLowerCase();
            return boek.titel.toLowerCase().includes(searchLower) ||
                boek.auteur.toLowerCase().includes(searchLower) ||
                boek.isbn.toLowerCase().includes(searchLower);
        });

        // Update display met zoekresultaten
        displayBoeken(results);
        updateBoekDropdown();

        // Toon zoekresultaten bericht
        const resultsDiv = document.getElementById('boeken-search-results');
        if (resultsDiv) {
            if (results.length === 0) {
                resultsDiv.innerHTML = `
                    <i class="fas fa-search"></i> 
                    Geen resultaten gevonden voor "<strong>${escapeHtml(query)}</strong>"
                `;
                resultsDiv.style.color = 'var(--danger)';
            } else {
                resultsDiv.innerHTML = `
                    <i class="fas fa-check-circle"></i> 
                    ${results.length} ${results.length === 1 ? 'boek' : 'boeken'} gevonden voor "<strong>${escapeHtml(query)}</strong>"
                `;
                resultsDiv.style.color = 'var(--success)';
            }
            resultsDiv.style.display = 'block';
        }

        // Verberg loading, toon tabel
        if (loadingEl) loadingEl.style.display = 'none';
        if (tabelEl) tabelEl.style.display = 'table';

    } catch (error) {
        console.error('Fout bij zoeken boeken:', error);
        showError('Er ging iets mis bij het zoeken');

        // Herstel UI state
        const loadingEl = document.getElementById('boeken-loading');
        const tabelEl = document.getElementById('boeken-tabel');
        if (loadingEl) loadingEl.style.display = 'none';
        if (tabelEl) tabelEl.style.display = 'table';
    }
}

function clearBoekenSearch() {
    const searchInput = document.getElementById('boeken-search-input');
    const resultsDiv = document.getElementById('boeken-search-results');

    if (searchInput) searchInput.value = '';
    if (resultsDiv) {
        resultsDiv.innerHTML = '';
        resultsDiv.style.display = 'none';
    }

    // Herlaad alle boeken
    loadBoeken();
}


// ============================================
// ENTER KEY SUPPORT
// ============================================

// Setup event listeners wanneer DOM geladen is
document.addEventListener('DOMContentLoaded', () => {
    // Klanten zoeken met Enter toets
    const klantenSearchInput = document.getElementById('klanten-search-input');
    if (klantenSearchInput) {
        klantenSearchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchKlanten();
            }
        });
    }

    // Boeken zoeken met Enter toets
    const boekenSearchInput = document.getElementById('boeken-search-input');
    if (boekenSearchInput) {
        boekenSearchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchBoeken();
            }
        });
    }
});

// ============================================
// GEAVANCEERDE ZOEKFUNCTIES (Voor toekomstig gebruik)
// ============================================

// Filter boeken op prijsrange
function filterBoekenByPriceRange(minPrice, maxPrice) {
    const results = boeken.filter(boek => {
        return boek.prijs >= minPrice && boek.prijs <= maxPrice;
    });
    displayBoeken(results);

    const resultsDiv = document.getElementById('boeken-search-results');
    if (resultsDiv) {
        resultsDiv.innerHTML = `
            <i class="fas fa-filter"></i> 
            ${results.length} boeken gevonden tussen EUR ${minPrice.toFixed(2)} en EUR ${maxPrice.toFixed(2)}
        `;
        resultsDiv.style.display = 'block';
        resultsDiv.style.color = 'var(--info)';
    }

    return results;
}

// Filter boeken op voorraadstatus
function filterBoekenByStock(lowStockOnly = false) {
    const results = lowStockOnly
        ? boeken.filter(boek => boek.voorraadAantal < 15)
        : boeken.filter(boek => boek.voorraadAantal >= 15);

    displayBoeken(results);

    const resultsDiv = document.getElementById('boeken-search-results');
    if (resultsDiv) {
        resultsDiv.innerHTML = `
            <i class="fas fa-boxes"></i> 
            ${results.length} boeken met ${lowStockOnly ? 'lage' : 'normale'} voorraad
        `;
        resultsDiv.style.display = 'block';
        resultsDiv.style.color = lowStockOnly ? 'var(--warning)' : 'var(--success)';
    }

    return results;
}

// Sorteer boeken
function sortBoeken(sortBy = 'titel', ascending = true) {
    const sorted = [...boeken].sort((a, b) => {
        let compareA = a[sortBy];
        let compareB = b[sortBy];

        // Voor strings, converteer naar lowercase
        if (typeof compareA === 'string') {
            compareA = compareA.toLowerCase();
            compareB = compareB.toLowerCase();
        }

        if (ascending) {
            return compareA > compareB ? 1 : -1;
        } else {
            return compareA < compareB ? 1 : -1;
        }
    });

    displayBoeken(sorted);
    return sorted;
}

// Sorteer klanten
function sortKlanten(sortBy = 'naam', ascending = true) {
    const sorted = [...klanten].sort((a, b) => {
        let compareA = a[sortBy];
        let compareB = b[sortBy];

        // Voor strings, converteer naar lowercase
        if (typeof compareA === 'string') {
            compareA = compareA.toLowerCase();
            compareB = compareB.toLowerCase();
        }

        if (ascending) {
            return compareA > compareB ? 1 : -1;
        } else {
            return compareA < compareB ? 1 : -1;
        }
    });

    displayKlanten(sorted);
    return sorted;
}

// Zoek boek op ISBN (exacte match)
function searchBoekByISBN(isbn) {
    const boek = boeken.find(b => b.isbn === isbn.trim());
    if (boek) {
        displayBoeken([boek]);
        showSuccess(`Boek gevonden: ${boek.titel}`);
        return boek;
    } else {
        displayBoeken([]);
        showError(`Geen boek gevonden met ISBN: ${isbn}`);
        return null;
    }
}

// Zoek klant op email (exacte match)
function searchKlantByEmail(email) {
    const klant = klanten.find(k => k.email.toLowerCase() === email.trim().toLowerCase());
    if (klant) {
        displayKlanten([klant]);
        showSuccess(`Klant gevonden: ${klant.naam}`);
        return klant;
    } else {
        displayKlanten([]);
        showError(`Geen klant gevonden met email: ${email}`);
        return null;
    }
}

// Export geavanceerde functies voor gebruik in console/debugging
if (typeof window !== 'undefined') {
    window.searchFunctions = {
        filterBoekenByPriceRange,
        filterBoekenByStock,
        sortBoeken,
        sortKlanten,
        searchBoekByISBN,
        searchKlantByEmail
    };
}

// Console log
console.log('%c?? Zoekfuncties geladen', 'color: #4299e1; font-size: 12px; font-weight: bold;');
console.log('%c?? Gebruik window.searchFunctions voor geavanceerde zoekopties', 'color: #718096; font-size: 11px;');