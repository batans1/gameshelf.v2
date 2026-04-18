// Theme management for GameShelf
(function() {
    'use strict';

    const THEME_STORAGE_KEY = 'gameshelf-theme';
    const THEME_ATTRIBUTE = 'data-theme';

    /**
     * Get the current theme from localStorage or system preference
     */
    function getTheme() {
        const stored = localStorage.getItem(THEME_STORAGE_KEY);
        if (stored) {
            return stored;
        }
        
        // Check system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        
        return 'light';
    }

    /**
     * Set the theme on the document
     */
    function setTheme(theme) {
        if (theme === 'dark') {
            document.documentElement.setAttribute(THEME_ATTRIBUTE, 'dark');
        } else {
            document.documentElement.removeAttribute(THEME_ATTRIBUTE);
        }
        localStorage.setItem(THEME_STORAGE_KEY, theme);
        updateThemeToggleIcon(theme);
    }

    /**
     * Toggle between light and dark theme
     */
    function toggleTheme() {
        const currentTheme = getTheme();
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        setTheme(newTheme);
    }

    /**
     * Update the theme toggle button icon
     */
    function updateThemeToggleIcon(theme) {
        const toggleBtn = document.getElementById('theme-toggle');
        if (!toggleBtn) return;

        const icon = toggleBtn.querySelector('span');
        if (icon) {
            if (theme === 'dark') {
                toggleBtn.setAttribute('aria-label', 'Switch to light mode');
                toggleBtn.setAttribute('title', 'Switch to light mode');
                icon.textContent = '☀️';
            } else {
                toggleBtn.setAttribute('aria-label', 'Switch to dark mode');
                toggleBtn.setAttribute('title', 'Switch to dark mode');
                icon.textContent = '🌙';
            }
        }
    }

    /**
     * Initialize theme on page load
     */
    function initTheme() {
        const theme = getTheme();
        setTheme(theme);

        // Listen for system theme changes
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                // Only auto-switch if user hasn't manually set a preference
                if (!localStorage.getItem(THEME_STORAGE_KEY)) {
                    setTheme(e.matches ? 'dark' : 'light');
                }
            });
        }

        // Attach click handler to theme toggle button
        const toggleBtn = document.getElementById('theme-toggle');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', toggleTheme);
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheme);
    } else {
        initTheme();
    }

    // Expose toggleTheme for external use
    window.GameShelfTheme = {
        toggle: toggleTheme,
        set: setTheme,
        get: getTheme
    };
})();
