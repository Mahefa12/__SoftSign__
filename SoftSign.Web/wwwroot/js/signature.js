/**
 * SoftSign Signature Drawing Module
 * Handles canvas-based signature drawing within signature zones
 */

(function () {
    'use strict';

    // Store for canvas contexts and drawing state
    const signatureCanvases = {};
    let activeZoneId = null;
    let isDrawing = false;
    let lastX = 0;
    let lastY = 0;

    /**
     * Initialize signature zones on page load
     */
    function initSignatureZones() {
        const zones = document.querySelectorAll('.signature-zone[data-zone-id]');
        
        zones.forEach(zone => {
            const zoneId = zone.dataset.zoneId;
            const canvas = document.getElementById(`canvas-${zoneId}`);
            
            if (canvas) {
                // Initialize canvas context
                const ctx = canvas.getContext('2d');
                signatureCanvases[zoneId] = {
                    canvas: canvas,
                    ctx: ctx,
                    isDirty: false
                };

                // Set up drawing events
                setupDrawingEvents(canvas, zoneId);

                // Set up zone click event
                zone.addEventListener('click', (e) => {
                    // Don't activate if clicking on toolbar buttons
                    if (e.target.closest('.signature-canvas-toolbar')) {
                        return;
                    }
                    activateZone(zoneId);
                });
            }
        });

        // Deactivate zone when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.signature-zone') && activeZoneId) {
                deactivateZone(activeZoneId);
            }
        });
    }

    /**
     * Set up mouse and touch drawing events for a canvas
     */
    function setupDrawingEvents(canvas, zoneId) {
        const ctx = canvas.getContext('2d');
        
        // Configure line style
        ctx.strokeStyle = '#1a1a1a';
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        // Mouse events
        canvas.addEventListener('mousedown', (e) => startDrawing(e, zoneId));
        canvas.addEventListener('mousemove', (e) => draw(e, zoneId));
        canvas.addEventListener('mouseup', () => stopDrawing(zoneId));
        canvas.addEventListener('mouseout', () => stopDrawing(zoneId));

        // Touch events
        canvas.addEventListener('touchstart', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            const mouseEvent = new MouseEvent('mousedown', {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            startDrawing(mouseEvent, zoneId);
        });

        canvas.addEventListener('touchmove', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            const mouseEvent = new MouseEvent('mousemove', {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            draw(mouseEvent, zoneId);
        });

        canvas.addEventListener('touchend', (e) => {
            e.preventDefault();
            stopDrawing(zoneId);
        });
    }

    /**
     * Get canvas coordinates from mouse/touch event
     */
    function getCanvasCoordinates(canvas, event) {
        const rect = canvas.getBoundingClientRect();
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;
        
        return {
            x: (event.clientX - rect.left) * scaleX,
            y: (event.clientY - rect.top) * scaleY
        };
    }

    /**
     * Start drawing
     */
    function startDrawing(event, zoneId) {
        const canvasData = signatureCanvases[zoneId];
        if (!canvasData) return;

        isDrawing = true;
        const coords = getCanvasCoordinates(canvasData.canvas, event);
        lastX = coords.x;
        lastY = coords.y;
        canvasData.isDirty = true;
    }

    /**
     * Draw on canvas
     */
    function draw(event, zoneId) {
        if (!isDrawing) return;

        const canvasData = signatureCanvases[zoneId];
        if (!canvasData) return;

        const coords = getCanvasCoordinates(canvasData.canvas, event);
        const ctx = canvasData.ctx;

        ctx.beginPath();
        ctx.moveTo(lastX, lastY);
        ctx.lineTo(coords.x, coords.y);
        ctx.stroke();

        lastX = coords.x;
        lastY = coords.y;
    }

    /**
     * Stop drawing
     */
    function stopDrawing(zoneId) {
        isDrawing = false;
    }

    /**
     * Activate a signature zone for drawing
     */
    function activateZone(zoneId) {
        // Deactivate previous zone
        if (activeZoneId && activeZoneId !== zoneId) {
            deactivateZone(activeZoneId);
        }

        const zone = document.getElementById(`zone-${zoneId}`);
        if (!zone || zone.classList.contains('signed')) return;

        // Add active class
        zone.classList.remove('pending');
        zone.classList.add('active');
        activeZoneId = zoneId;

        // Enable canvas for drawing by ensuring pointer events
        const canvasContainer = zone.querySelector('.signature-canvas-container');
        if (canvasContainer) {
            canvasContainer.style.display = 'block';
            canvasContainer.style.pointerEvents = 'auto';
        }

        // Ensure canvas has pointer events
        const canvas = document.getElementById(`canvas-${zoneId}`);
        if (canvas) {
            canvas.style.pointerEvents = 'auto';
            canvas.focus();
        }
    }

    /**
     * Deactivate a signature zone
     */
    function deactivateZone(zoneId) {
        const zone = document.getElementById(`zone-${zoneId}`);
        if (!zone) return;

        zone.classList.remove('active');
        zone.classList.add('pending');
        
        if (activeZoneId === zoneId) {
            activeZoneId = null;
        }
    }

    /**
     * Clear the signature canvas
     */
    window.clearSignature = function(zoneId) {
        const canvasData = signatureCanvases[zoneId];
        if (!canvasData) return;

        const ctx = canvasData.ctx;
        const canvas = canvasData.canvas;
        
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        canvasData.isDirty = false;
    };

    /**
     * Save the signature to the backend
     */
    window.saveSignature = async function(zoneId) {
        const canvasData = signatureCanvases[zoneId];
        if (!canvasData || !canvasData.isDirty) return;

        const canvas = canvasData.canvas;
        
        // Convert canvas to base64 PNG
        const signatureDataUrl = canvas.toDataURL('image/png');
        
        // Get document ID from the page
        const documentId = getDocumentIdFromPage();
        
        console.log('=== SAVE SIGNATURE DEBUG ===');
        console.log('zoneId:', zoneId);
        console.log('documentId:', documentId);
        console.log('signatureData length:', signatureDataUrl.length);
        console.log('Request token:', getRequestToken() ? 'present' : 'missing');
        
        if (!documentId) {
            showMessage('Erreur: ID du document non trouvé', 'error');
            return;
        }

        try {
            // Submit signature to backend using existing endpoint
            // Note: Only include documentId and zoneId in FormData, not in query string
            // to avoid duplicate parameter issues
            const formData = new FormData();
            formData.append('documentId', documentId);
            formData.append('zoneId', zoneId);
            formData.append('signatureData', signatureDataUrl);

            const response = await fetch('/Signature/Submit', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getRequestToken(),
                    'Accept': 'application/json'
                },
                body: formData
            });

            console.log('Response status:', response.status);
            console.log('Response ok:', response.ok);

            if (response.ok) {
                let result;
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.indexOf('application/json') !== -1) {
                    result = await response.json();
                    console.log('JSON response:', result);
                }
                
                // Update zone to signed state
                updateZoneToSigned(zoneId, signatureDataUrl);
                showMessage('Signature enregistrée avec succès!', 'success');
                
                // Deactivate the zone
                deactivateZone(zoneId);
                
                // Reload page after short delay to refresh the document
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                let errorMessage = 'Erreur lors de l\'enregistrement de la signature';
                try {
                    const contentType = response.headers.get('content-type');
                    if (contentType && contentType.indexOf('application/json') !== -1) {
                        const errorResult = await response.json();
                        console.log('Error JSON:', errorResult);
                        errorMessage = errorResult.message || errorMessage;
                    } else {
                        const errorText = await response.text();
                        console.error('Signature save error:', errorText);
                    }
                } catch (e) {
                    console.error('Error parsing response:', e);
                }
                showMessage(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Error saving signature:', error);
            showMessage('Erreur de connexion lors de l\'enregistrement', 'error');
        }
    };

    /**
     * Update zone UI to signed state
     */
    function updateZoneToSigned(zoneId, signatureImage) {
        const zone = document.getElementById(`zone-${zoneId}`);
        if (!zone) return;

        // Update classes
        zone.classList.remove('pending', 'active');
        zone.classList.add('signed');

        // Replace canvas container with signature image
        const canvasContainer = zone.querySelector('.signature-canvas-container');
        if (canvasContainer) {
            canvasContainer.style.display = 'none';
        }

        // Check if signature image already exists
        let img = zone.querySelector('.signature-image');
        if (!img) {
            img = document.createElement('img');
            img.className = 'signature-image';
            zone.appendChild(img);
        }
        img.src = signatureImage;
        img.alt = 'Signature';

        // Add signed badge if not exists
        let badge = zone.querySelector('.signature-zone-badge');
        if (!badge) {
            badge = document.createElement('div');
            badge.className = 'signature-zone-badge';
            badge.innerHTML = '<i class="bi bi-check-circle"></i> Signé';
            zone.appendChild(badge);
        }

        // Update the label
        const label = zone.querySelector('.signature-zone-label');
        if (label) {
            label.textContent = 'Signé';
        }
    }

    /**
     * Get document ID from page
     */
    function getDocumentIdFromPage() {
        // First try to get from data attribute
        const detailsElement = document.getElementById('docPreview');
        if (detailsElement && detailsElement.dataset.documentId) {
            return detailsElement.dataset.documentId;
        }

        // Try to get from URL
        const pathParts = window.location.pathname.split('/');
        for (let i = 0; i < pathParts.length; i++) {
            if (pathParts[i] === 'Details' && pathParts[i - 1]) {
                // Try to parse as GUID
                const id = pathParts[i - 1];
                if (id.match(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)) {
                    return id;
                }
            }
        }

        return null;
    }

    /**
     * Get request verification token
     */
    function getRequestToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    /**
     * Show message to user
     */
    function showMessage(message, type) {
        // Create toast notification
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
                <span>${message}</span>
            </div>
        `;
        
        // Add styles if not already added
        let style = document.getElementById('signature-toast-styles');
        if (!style) {
            style = document.createElement('style');
            style.id = 'signature-toast-styles';
            style.textContent = `
                .toast-notification {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    z-index: 10000;
                    padding: 12px 20px;
                    border-radius: 6px;
                    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                    animation: slideIn 0.3s ease;
                    font-family: var(--font-family, -apple-system, sans-serif);
                }
                .toast-success {
                    background: #22c55e;
                    color: white;
                }
                .toast-error {
                    background: #ef4444;
                    color: white;
                }
                .toast-content {
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }
                @keyframes slideIn {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(toast);

        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.style.animation = 'slideIn 0.3s ease reverse';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    /**
     * Submit all signatures at once (alternative method)
     */
    window.submitAllSignatures = async function() {
        const unsignedZones = document.querySelectorAll('.signature-zone.pending, .signature-zone.active');
        
        if (unsignedZones.length === 0) {
            showMessage('Aucune signature à soumettre', 'error');
            return;
        }

        for (const zone of unsignedZones) {
            const zoneId = zone.dataset.zoneId;
            const canvasData = signatureCanvases[zoneId];
            
            if (canvasData && canvasData.isDirty) {
                await window.saveSignature(zoneId);
            }
        }
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSignatureZones);
    } else {
        initSignatureZones();
    }

})();
