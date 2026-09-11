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
            initTabs(root);
            initSelectAll(root);
            initSearch(root);
            initPagination(root);
        });
    }

    function findPanel(root, key) {
        return root.querySelector(
            '.sheet-tab-panel[data-tab-panel="' + key + '"], ' +
            '.module-tab-panel[data-panel="' + key + '"], ' +
            '.module-tab-panel[data-tab-panel="' + key + '"]'
        );
    }

    function findAllPanels(root) {
        return root.querySelectorAll('.sheet-tab-panel, .module-tab-panel');
    }

    function getPanelKey(panel) {
        return panel.getAttribute('data-tab-panel') || panel.getAttribute('data-panel');
    }

    /* ── Tab Switching ── */
    function initTabs(root) {
        root.querySelectorAll('.module-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                var key = this.getAttribute('data-tab');
                root.querySelectorAll('.module-tab').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
                findAllPanels(root).forEach(function (p) {
                    p.style.display = getPanelKey(p) === key ? '' : 'none';
                });
                root.setAttribute('data-active-tab', key);
                toggleActionButton(root, key);
                resetPagination(root);
            });
        });
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
        var fallback = root.querySelector('.module-action-fallback');
        if (fallback) {
            fallback.style.display = matched ? 'none' : 'inline-flex';
        }
    }

    /* ── Select All / Row Selection ── */
    function initSelectAll(root) {
        root.addEventListener('change', function (e) {
            if (e.target.classList.contains('sheet-select-all') || e.target.classList.contains('select-all')) {
                var tab = root.getAttribute('data-active-tab');
                var panel = findPanel(root, tab);
                if (!panel) return;
                var cbs = panel.querySelectorAll('input[type="checkbox"].row-check, input[type="checkbox"].sheet-row-cb, input[type="checkbox"].form-check-input');
                cbs.forEach(function (cb) { if (!cb.classList.contains('select-all') && !cb.classList.contains('sheet-select-all')) cb.checked = e.target.checked; });
                updateSelection(root, tab);
            }
            if (e.target.classList.contains('row-check') || e.target.classList.contains('sheet-row-cb')) {
                var tab = root.getAttribute('data-active-tab');
                var panel = findPanel(root, tab);
                if (!panel) return;
                var allCbs = panel.querySelectorAll('input[type="checkbox"].row-check, input[type="checkbox"].sheet-row-cb, input[type="checkbox"].form-check-input');
                var rowCbs = Array.from(allCbs).filter(function (cb) { return !cb.classList.contains('select-all') && !cb.classList.contains('sheet-select-all'); });
                var checkedCbs = rowCbs.filter(function (cb) { return cb.checked; });
                var selectAll = panel.querySelector('input[type="checkbox"].select-all, input[type="checkbox"].sheet-select-all');
                if (selectAll) selectAll.checked = rowCbs.length > 0 && rowCbs.length === checkedCbs.length;
                updateSelection(root, tab);
            }
        });
    }

    function updateSelection(root, tab) {
        var panel = findPanel(root, tab);
        if (!panel) return;
        var allCbs = panel.querySelectorAll('input[type="checkbox"].row-check, input[type="checkbox"].sheet-row-cb, input[type="checkbox"].form-check-input');
        var rowCbs = Array.from(allCbs).filter(function (cb) { return !cb.classList.contains('select-all') && !cb.classList.contains('sheet-select-all'); });
        var total = rowCbs.length;
        var checked = rowCbs.filter(function (cb) { return cb.checked; }).length;
        var countEl = root.querySelector('.sheet-selected-count');
        if (countEl) {
            countEl.textContent = checked > 0 ? checked + ' of ' + total + ' selected' : '';
        }
    }

    /* ── Client-Side Search ── */
    function initSearch(root) {
        root.querySelectorAll('.sheet-search-input').forEach(function (input) {
            input.addEventListener('input', function () {
                var tab = root.getAttribute('data-active-tab');
                var panel = findPanel(root, tab);
                if (!panel) return;
                var q = this.value.toLowerCase();
                panel.querySelectorAll('tr').forEach(function (tr) {
                    if (tr.querySelector('th')) return;
                    var text = tr.textContent.toLowerCase();
                    tr.style.display = text.indexOf(q) === -1 ? 'none' : '';
                });
            });
        });
    }

    /* ── Pagination ── */
    function initPagination(root) {
        root.querySelectorAll('.page-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var tab = root.getAttribute('data-active-tab');
                var panel = findPanel(root, tab);
                if (!panel) return;
                var dir = this.getAttribute('data-dir');
                var page = parseInt(panel.getAttribute('data-page') || '1');
                if (dir === 'prev') page = Math.max(1, page - 1);
                else if (dir === 'next') page++;
                else page = parseInt(this.getAttribute('data-page') || '1');
                panel.setAttribute('data-page', page.toString());
            });
        });
    }

    function resetPagination(root) {
        findAllPanels(root).forEach(function (p) {
            p.setAttribute('data-page', '1');
        });
    }

    /* ── Status Badge Helper ── */
    window.moduleLayout = {
        statusClass: function (status) {
            if (!status) return 'info';
            var s = status.toLowerCase().replace(/[\s_-]/g, '');
            if (['completed', 'approved', 'released', 'active', 'matched', 'cleared', 'posted', 'delivered', 'billed'].indexOf(s) !== -1) return 'success';
            if (['pending', 'inprogress', 'partial', 'open', 'sent'].indexOf(s) !== -1) return 'warning';
            if (['rejected', 'cancelled', 'blocked', 'expired', 'oos', 'failed', 'overdue'].indexOf(s) !== -1) return 'danger';
            if (['draft', 'created', 'new'].indexOf(s) !== -1) return 'info';
            if (['qualityhold', 'qiheld', 'inquality'].indexOf(s) !== -1) return 'purple';
            return 'info';
        }
    };
})();
