/**
 * ============================================
 * TOAST NOTIFICATIONS SYSTEM
 * Moderne popup meldingen voor alle pagina's
 * Versie: 1.0.0
 * ============================================
 */

function showMessage(text, type = 'success') {
    const container = document.getElementById('message-container');
    if (!container) {
        console.error('❌ Message container not found!');
        console.log('Fallback message:', text, type);
        return;
    }

    // Bepaal icoon en klasse
    const iconMap = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'info': 'fa-info-circle',
        'warning': 'fa-exclamation-triangle'
    };

    const icon = iconMap[type] || 'fa-info-circle';
    const toastClass = type === 'warning' ? 'toast-warning' : `toast-${type}`;

    // Convert newlines to <br> for multi-line messages
    const formattedText = text.replace(/\n/g, '<br>');

    // Maak toast element
    const toast = document.createElement('div');
    toast.className = toastClass;
    toast.innerHTML = `
        <i class="fas ${icon}"></i>
        <span style="white-space: pre-wrap;">${formattedText}</span>
        <button class="toast-close" onclick="closeToast(this)" title="Sluiten">
            <i class="fas fa-times"></i>
        </button>
        <div class="toast-progress"></div>
    `;

    // Voeg toe aan container
    container.appendChild(toast);

    // Auto remove na 8 seconden voor error messages (langer zodat gebruiker kan lezen)
    const duration = type === 'error' ? 8000 : 5000;
    const autoRemoveTimer = setTimeout(() => {
        if (toast && toast.parentNode) {
            removeToast(toast);
        }
    }, duration);

    // Store timer op toast voor cleanup
    toast.dataset.timer = autoRemoveTimer;

    return toast;
}

function closeToast(button) {
    const toast = button.closest('.toast-success, .toast-error, .toast-info, .toast-warning');
    if (toast) {
        // Clear auto-remove timer
        if (toast.dataset.timer) {
            clearTimeout(parseInt(toast.dataset.timer));
        }
        removeToast(toast);
    }
}

function removeToast(toast) {
    if (!toast || !toast.parentNode) return;
    
    toast.classList.add('toast-fadeout');
    setTimeout(() => {
        if (toast.parentNode) {
            toast.remove();
        }
    }, 300);
}

// ============================================
// SHORTHAND FUNCTIONS
// ============================================

/**
 * Show success message
 * @param {string} text - Message text
 */
function showSuccess(text) {
    return showMessage(text, 'success');
}

/**
 * Show error message
 * @param {string} text - Message text
 */
function showError(text) {
    return showMessage(text, 'error');
}

/**
 * Show info message
 * @param {string} text - Message text
 */
function showInfo(text) {
    return showMessage(text, 'info');
}

/**
 * Show warning message
 * @param {string} text - Message text
 */
function showWarning(text) {
    return showMessage(text, 'warning');
}

// Log successful load
console.log('✅ Toast Notifications System loaded (v1.0.0)');
