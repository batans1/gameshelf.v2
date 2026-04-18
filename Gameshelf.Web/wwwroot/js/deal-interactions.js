// Click tracking for deal cards - Simple: log every click (including middle-click and right-click opens)
(function() {
    'use strict';
    
    // Track right-clicked buttons to log when they're actually opened
    const rightClickedButtons = new WeakMap();
    
    function logClick(btn) {
        // Custom deals use data-custom-deal-id (Guid), live deals use data-deal-id (external id)
        const liveDealId = btn.getAttribute('data-deal-id') || '';
        const customDealId = btn.getAttribute('data-custom-deal-id') || '';
        const dealId = (liveDealId && liveDealId.trim()) ? liveDealId.trim() : ((customDealId && customDealId.trim()) ? customDealId.trim() : '');
        const storeName = (btn.getAttribute('data-store-name') || '').trim();
        const gameTitle = (btn.getAttribute('data-game-title') || '').trim();
        let dealUrl = (btn.getAttribute('data-deal-url') || '').trim();

        if (!dealId || !storeName || !gameTitle) {
            return;
        }
        if (!dealUrl) {
            dealUrl = '#';
        }

        fetch('/api/deal-clicks', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                dealId: dealId,
                storeName: storeName,
                gameTitle: gameTitle,
                dealUrl: dealUrl
            }),
            keepalive: true
        }).catch(function() {});
    }
    
    // Handle regular clicks (left click)
    document.addEventListener('click', function(e) {
        const btn = e.target.closest('.view-deal-btn');
        if (btn) {
            // If this was a right click button, log it now (user selected "Open in new tab" and etc...)
            if (rightClickedButtons.has(btn)) {
                logClick(btn);
                rightClickedButtons.delete(btn);
            } else {
                // Regular left click
                logClick(btn);
            }
        }
    });
    
    // Handle middle mouse wheel click
    document.addEventListener('auxclick', function(e) {
        const btn = e.target.closest('.view-deal-btn');
        if (!btn) return;
        
        // Middle-click (button === 1) - log immediately
        if (e.button === 1) {
            logClick(btn);
        }
        // Right-click (button === 2) - mark it, will log when link is actually opened
        else if (e.button === 2) {
            rightClickedButtons.set(btn, true);
           
            logClick(btn);
        }
    });
    
    // track Ctrl+Click 
    document.addEventListener('click', function(e) {
        if (e.ctrlKey || e.metaKey) {
            const btn = e.target.closest('.view-deal-btn');
            if (btn) {
                logClick(btn);
            }
        }
    });
})();
