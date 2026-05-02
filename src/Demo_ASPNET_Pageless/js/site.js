// MySqlExpress Pageless Demo — tiny JS view helpers.
// No framework. No build step. No fetch abstraction — every handler writes
// its fetch call in full, so a reader sees the complete request lifecycle
// at every call-site. The sibling project (Demo_ASPNET_Pageless_DelegateDb)
// shows the wrapped-helper version of the same code.

(function () {
    'use strict';

    // ------------- Sidebar toggle (mobile) -------------

    window.toggleSidebar = function () {
        var sb = document.querySelector('.sidebar');
        var ov = document.querySelector('.sidebar-overlay');
        if (!sb || !ov) return;
        sb.classList.toggle('open');
        ov.classList.toggle('show');
    };

    // ------------- HTML escaping -------------

    window.escapeHtml = function (s) {
        if (s === null || s === undefined) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    };

    // ------------- Flash messages (inline, in-page) -------------

    window.flash = function (kind, message, elementId) {
        var el = document.getElementById(elementId || 'flashArea');
        if (!el) return;
        var icon = kind === 'success' ? 'check-circle'
                 : kind === 'error'   ? 'exclamation-triangle'
                 : 'info-circle';
        el.innerHTML =
            '<div class="flash flash-' + kind + '">' +
            '<i class="fas fa-' + icon + '"></i> ' +
            window.escapeHtml(message) +
            '</div>';
    };

    window.clearFlash = function (elementId) {
        var el = document.getElementById(elementId || 'flashArea');
        if (el) el.innerHTML = '';
    };

})();
