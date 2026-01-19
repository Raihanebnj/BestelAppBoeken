// 🍞 Universal Toast Notification System
// Dit bestand bevat een herbruikbaar toast notification systeem voor alle pagina's

// Show success message
function showSuccess(message) {
    showToast(message, 'success');
}

// Show error message  
function showError(message) {
    showToast(message, 'error');
}

// Show info message
function showInfo(message) {
    showToast(message, 'info');
}

// Show warning message
function showWarning(message) {
    showToast(message, 'warning');
}

// Main toast function
function showToast(message, type = 'success') {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: 10px;
            max-width: 400px;
            pointer-events: none;
        `;
        document.body.appendChild(toastContainer);
    }

    // Icon and color based on type
    const config = {
        success: {
            icon: 'fa-check-circle',
            gradient: 'linear-gradient(135deg, #48bb78, #38a169)',
            shadow: 'rgba(72, 187, 120, 0.4)'
        },
        error: {
            icon: 'fa-exclamation-circle',
            gradient: 'linear-gradient(135deg, #f56565, #e53e3e)',
            shadow: 'rgba(245, 101, 101, 0.4)'
        },
        info: {
            icon: 'fa-info-circle',
            gradient: 'linear-gradient(135deg, #4299e1, #3182ce)',
            shadow: 'rgba(66, 153, 225, 0.4)'
        },
        warning: {
            icon: 'fa-exclamation-triangle',
            gradient: 'linear-gradient(135deg, #ed8936, #dd6b20)',
            shadow: 'rgba(237, 137, 54, 0.4)'
        }
    };

    const { icon, gradient, shadow } = config[type] || config.success;

    // Create toast element
    const toast = document.createElement('div');
    toast.style.cssText = `
        background: ${gradient};
        color: white;
        padding: 16px 20px;
        border-radius: 12px;
        box-shadow: 0 10px 25px ${shadow};
        display: flex;
        align-items: center;
        gap: 12px;
        min-width: 300px;
        max-width: 400px;
        animation: slideIn 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55), fadeOut 0.3s ease-in 4.7s;
        font-size: 14px;
        font-weight: 600;
        pointer-events: all;
        cursor: default;
        transform-origin: right center;
    `;
    
    toast.innerHTML = `
        <i class="fas ${icon}" style="font-size: 20px; flex-shrink: 0;"></i>
        <span style="flex: 1; word-break: break-word;">${escapeHtml(message)}</span>
        <button class="toast-close-btn" style="
            background: rgba(255,255,255,0.2);
            border: none;
            color: white;
            width: 28px;
            height: 28px;
            border-radius: 50%;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 18px;
            font-weight: bold;
            transition: all 0.2s;
            flex-shrink: 0;
            line-height: 1;
        ">×</button>
    `;
    
    // Close button hover effect
    const closeBtn = toast.querySelector('.toast-close-btn');
    closeBtn.addEventListener('mouseenter', () => {
        closeBtn.style.background = 'rgba(255,255,255,0.3)';
        closeBtn.style.transform = 'scale(1.1)';
    });
    closeBtn.addEventListener('mouseleave', () => {
        closeBtn.style.background = 'rgba(255,255,255,0.2)';
        closeBtn.style.transform = 'scale(1)';
    });
    closeBtn.addEventListener('click', () => {
        toast.style.animation = 'fadeOut 0.2s ease-in';
        setTimeout(() => toast.remove(), 200);
    });
    
    // Add CSS animations if not already added
    if (!document.getElementById('toast-animations')) {
        const style = document.createElement('style');
        style.id = 'toast-animations';
        style.textContent = `
            @keyframes slideIn {
                from {
                    transform: translateX(450px) scale(0.8);
                    opacity: 0;
                }
                to {
                    transform: translateX(0) scale(1);
                    opacity: 1;
                }
            }
            @keyframes fadeOut {
                from {
                    opacity: 1;
                    transform: scale(1);
                }
                to {
                    opacity: 0;
                    transform: scale(0.9);
                }
            }
            
            /* Mobile responsive */
            @media (max-width: 768px) {
                #toast-container {
                    top: 10px;
                    right: 10px;
                    left: 10px;
                    max-width: none !important;
                }
                #toast-container > div {
                    min-width: auto !important;
                    max-width: none !important;
                }
            }
        `;
        document.head.appendChild(style);
    }
    
    toastContainer.appendChild(toast);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.animation = 'fadeOut 0.3s ease-in';
            setTimeout(() => toast.remove(), 300);
        }
    }, 5000);
    
    // Limit number of toasts (max 5)
    const toasts = toastContainer.children;
    if (toasts.length > 5) {
        toasts[0].remove();
    }
}

// Escape HTML for security
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Export for module usage (optional)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { showSuccess, showError, showInfo, showWarning, showToast };
}
