/* ═══════════════════════════════════════════════════════════════
   Universal Module Layout — Sheet View Interactions
   ═══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initAllModules();
    });

    function initAllModules() {
        document.querySelectorAll('[data-module-root]').forEach(function (root) {
            var code = root.getAttribute('data-module-root');
            initTabs(root);
            initSelectAll(root);
            initSearch(root);
            initPagination(root);
        });
    }

    /* ── Tab Switching ── */
    function initTabs(root) {
        root.querySelectorAll('.module-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                var key = this.getAttribute('data-tab');
                root.querySelectorAll('.module-tab').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
                root.querySelectorAll('.sheet-tab-panel').forEach(function (p) {
                    p.style.display = p.getAttribute('data-tab-panel') === key ? '' : 'none';
                });
                root.setAttribute('data-active-tab', key);
                toggleActionButton(root, key);
                resetPagination(root);
            });
        });
        /* Initialize button state for default active tab */
        var initial = root.getAttribute('data-active-tab');
        if (initial) toggleActionButton(root, initial);
    }

    /* ── Dynamic Action Button ── */
    function toggleActionButton(root, tabKey) {
        var matched = false;
        root.querySelectorAll('.module-action-btn').forEach(function (btn) {
            var actionTab = btn.getAttribute('data-action-tab');
            if (actionTab) {
                var show = actionTab === tabKey;
                btn.style.display = show ? 'inline-flex' : 'none';
                if (show) matched = true;
            }
        });
        /* If no tab-specific button matches, show fallback (global PrimaryAction) */
        var fallback = root.querySelector('.module-action-fallback');
        if (fallback) {
            fallback.style.display = matched ? 'none' : 'inline-flex';
        }
    }

    /* ── Select All / Row Selection ── */
    function initSelectAll(root) {
        root.addEventListener('change', function (e) {
            if (e.target.classList.contains('sheet-select-all')) {
                var tab = root.getAttribute('data-active-tab');
                var checkboxes = root.querySelectorAll('.sheet-tab-panel[data-tab-panel="' + tab + '"] .sheet-row-cb');
                checkboxes.forEach(function (cb) { cb.checked = e.target.checked; });
                updateSelection(root, tab);
            }
            if (e.target.classList.contains('sheet-row-cb')) {
                var tab = root.getAttribute('data-active-tab');
                updateSelection(root, tab);
                var all = root.querySelectorAll('.sheet-tab-panel[data-tab-panel="' + tab + '"] .sheet-row-cb');
                var checked = root.querySelectorAll('.sheet-tab-panel[data-tab-panel="' + tab + '"] .sheet-row-cb:checked');
                var selectAll = root.querySelector('.sheet-select-all');
                if (selectAll) selectAll.checked = all.length > 0 && all.length === checked.length;
            }
        });
    }

    function updateSelection(root, tab) {
        var panel = root.querySelector('.sheet-tab-panel[data-tab-panel="' + tab + '"]');
        if (!panel) return;
        var total = panel.querySelectorAll('.sheet-row-cb').length;
        var checked = panel.querySelectorAll('.sheet-row-cb:checked').length;
        var countEl = root.querySelector('.sheet-selected-count');
        if (countEl) {
            countEl.textContent = checked > 0 ? checked + ' of ' + total + ' selected' : '';
        }
        panel.querySelectorAll('tr[data-row]').forEach(function (tr) {
            var cb = tr.querySelector('.sheet-row-cb');
            tr.classList.toggle('selected', cb && cb.checked);
        });
    }

    /* ── Client-Side Search ── */
    function initSearch(root) {
        root.querySelectorAll('.sheet-search-input').forEach(function (input) {
            input.addEventListener('input', function () {
                var tab = root.getAttribute('data-active-tab');
                var panel = root.querySelector('.sheet-tab-panel[data-tab-panel="' + tab + '"]');
                if (!panel) return;
                var q = this.value.toLowerCase();
                panel.querySelectorAll('tr[data-row]').forEach(function (tr) {
                    var text = tr.textContent.toLowerCase();
                    tr.style.display = text.indexOf(q) === -1 ? 'none' : '';
                });
                updatePaginationInfo(root, tab);
            });
        });
    }

    /* ── Pagination ── */
    function initPagination(root) {
        root.querySelectorAll('.page-size-select').forEach(function (sel) {
            sel.addEventListener('change', function () {
                var tab = root.getAttribute('data-active-tab');
                var panel = root.querySelector('.sheet-tab-panel[data-tab-panel="' + tab + '"]');
                if (!panel) return;
                panel.setAttribute('data-page-size', this.value);
                panel.setAttribute('data-page', '1');
                renderPage(root, tab);
            });
        });
        root.querySelectorAll('.page-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var tab = root.getAttribute('data-active-tab');
                var panel = root.querySelector('.sheet-tab-panel[data-tab-panel="' + tab + '"]');
                if (!panel) return;
                var dir = this.getAttribute('data-dir');
                var page = parseInt(panel.getAttribute('data-page') || '1');
                if (dir === 'prev') page = Math.max(1, page - 1);
                else if (dir === 'next') page++;
                else page = parseInt(this.getAttribute('data-page') || '1');
                panel.setAttribute('data-page', page.toString());
                renderPage(root, tab);
            });
        });
    }

    function renderPage(root, tab) {
        var panel = root.querySelector('.sheet-tab-panel[data-tab-panel="' + tab + '"]');
        if (!panel) return;
        var size = parseInt(panel.getAttribute('data-page-size') || '25');
        var page = parseInt(panel.getAttribute('data-page') || '1');
        var rows = panel.querySelectorAll('tr[data-row]');
        var visibleRows = Array.from(rows).filter(function (r) { return r.style.display !== 'none'; });
        var total = visibleRows.length;
        var totalPages = Math.max(1, Math.ceil(total / size));
        if (page > totalPages) page = totalPages;
        panel.setAttribute('data-page', page.toString());

        var start = (page - 1) * size;
        var end = start + size;
        visibleRows.forEach(function (tr, i) {
            tr.style.display = (i >= start && i < end) ? '' : 'none';
        });

        updatePaginationInfo(root, tab, page, totalPages, total, size);
        renderPageButtons(root, panel, tab, page, totalPages);
    }

    function updatePaginationInfo(root, tab, page, totalPages, total, size) {
        var panel = root.querySelector('.sheet-tab-panel[data-tab-panel="' + tab + '"]');
        if (!panel) return;
        if (!page) {
            var rows = Array.from(panel.querySelectorAll('tr[data-row]')).filter(function (r) { return r.style.display !== 'none'; });
            total = rows.length;
            size = parseInt(panel.getAttribute('data-page-size') || '25');
            page = parseInt(panel.getAttribute('data-page') || '1');
            totalPages = Math.max(1, Math.ceil(total / size));
        }
        var start = total === 0 ? 0 : (page - 1) * size + 1;
        var end = Math.min(page * size, total);
        var info = panel.closest('[data-module-root]').querySelector('.sheet-pagination-info');
        if (info) {
            info.textContent = 'Showing ' + start + '-' + end + ' of ' + total + ' rows';
        }
    }

    function renderPageButtons(root, panel, tab, currentPage, totalPages) {
        var container = root.querySelector('.sheet-pagination-controls');
        if (!container) return;
        var html = '<button class="page-btn" data-dir="prev"' + (currentPage <= 1 ? ' disabled' : '') + '><i class="bi bi-chevron-left"></i></button>';
        var maxVisible = 5;
        var startP = Math.max(1, currentPage - Math.floor(maxVisible / 2));
        var endP = Math.min(totalPages, startP + maxVisible - 1);
        if (endP - startP < maxVisible - 1) startP = Math.max(1, endP - maxVisible + 1);
        if (startP > 1) html += '<button class="page-btn" data-page="1">1</button>';
        if (startP > 2) html += '<button disabled>...</button>';
        for (var i = startP; i <= endP; i++) {
            html += '<button class="page-btn' + (i === currentPage ? ' active' : '') + '" data-page="' + i + '">' + i + '</button>';
        }
        if (endP < totalPages - 1) html += '<button disabled>...</button>';
        if (endP < totalPages) html += '<button class="page-btn" data-page="' + totalPages + '">' + totalPages + '</button>';
        html += '<button class="page-btn" data-dir="next"' + (currentPage >= totalPages ? ' disabled' : '') + '><i class="bi bi-chevron-right"></i></button>';
        container.innerHTML = html;
        initPagination(root);
    }

    function resetPagination(root) {
        root.querySelectorAll('.sheet-tab-panel').forEach(function (p) {
            p.setAttribute('data-page', '1');
        });
    }

    /* ── Status Badge Helper (for inline use) ── */
    window.moduleLayout = {
        statusClass: function (status) {
            if (!status) return 'info';
            var s = status.toLowerCase().replace(/[\s_-]/g, '');
            if (['completed', 'approved', 'released', 'active', 'matched', 'cleared', 'posted', 'delivered', 'billed'].indexOf(s) !== -1) return 'success';
            if (['pending', 'inprogress', 'inprogress', 'partial', 'open', 'sent', 'partial'].indexOf(s) !== -1) return 'warning';
            if (['rejected', 'cancelled', 'blocked', 'expired', 'oos', 'failed', 'overdue'].indexOf(s) !== -1) return 'danger';
            if (['draft', 'created', 'new'].indexOf(s) !== -1) return 'info';
            if (['qualityhold', 'qiheld', 'inquality'].indexOf(s) !== -1) return 'purple';
            return 'info';
        }
    };
})();
