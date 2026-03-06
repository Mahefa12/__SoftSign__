// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// PDF Viewer fallback handling
document.addEventListener('DOMContentLoaded', function() {
    const pdfViewer = document.querySelector('.pdf-viewer');
    const pdfFallback = document.querySelector('.pdf-fallback');
    
    if (pdfViewer && pdfFallback) {
        // Handle iframe load error (e.g., browser doesn't support inline PDF)
        pdfViewer.addEventListener('error', function() {
            pdfViewer.style.display = 'none';
            pdfFallback.style.display = 'block';
        });
        
        // Also check if the iframe content loaded successfully
        pdfViewer.addEventListener('load', function() {
            try {
                // Try to access the iframe content to check if it loaded
                const iframeDoc = pdfViewer.contentDocument || pdfViewer.contentWindow.document;
                if (iframeDoc.body && iframeDoc.body.innerText && iframeDoc.body.innerText.includes('404')) {
                    pdfViewer.style.display = 'none';
                    pdfFallback.style.display = 'block';
                }
            } catch (e) {
                // Cross-origin access denied - likely loaded successfully
                // Most browsers block cross-origin iframe access
            }
        });
    }
});

// Write your JavaScript code.
