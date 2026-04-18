// MySqlExpress Pageless Demo — tiny JS helpers.
// No framework. No build step. Just the bits every page reuses.

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

    // ------------- Fetch wrappers -------------

    window.apiGet = function (url, onSuccess, onError) {
        fetch(url, { method: 'GET', credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (d) {
                if (onSuccess) onSuccess(d);
            })
            .catch(function (e) {
                console.error('GET failed', url, e);
                if (onError) onError(e);
            });
    };

    window.apiPostForm = function (url, formData, onSuccess, onError) {
        var body;
        if (formData instanceof FormData) {
            // Convert to URLSearchParams for application/x-www-form-urlencoded —
            // plays nicer with HttpRequest.Form server-side.
            body = new URLSearchParams();
            formData.forEach(function (v, k) { body.append(k, v); });
        } else if (formData instanceof URLSearchParams) {
            body = formData;
        } else {
            body = new URLSearchParams();
            for (var key in formData) {
                if (Object.prototype.hasOwnProperty.call(formData, key)) {
                    body.append(key, formData[key]);
                }
            }
        }

        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8' },
            credentials: 'same-origin',
            body: body.toString()
        })
            .then(function (r) { return r.json(); })
            .then(function (d) {
                if (onSuccess) onSuccess(d);
            })
            .catch(function (e) {
                console.error('POST failed', url, e);
                if (onError) onError(e);
            });
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

    // ------------- Confirm delete (typical list-row action) -------------

    window.confirmDelete = function (url, reloadOnSuccess) {
        if (!confirm('Are you sure? This cannot be undone.')) return;
        window.apiPostForm(url, {}, function (d) {
            if (d && d.success) {
                if (reloadOnSuccess !== false) window.location.reload();
            } else {
                alert((d && d.message) || 'Delete failed');
            }
        });
    };

})();
