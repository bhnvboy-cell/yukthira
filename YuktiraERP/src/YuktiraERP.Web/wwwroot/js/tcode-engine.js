/* ══════════════════════════════════════════════════════════════
   TCode Engine – Config→DOM renderer (5-tier Fiori layout)
   ══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    var engine = document.querySelector('.tcode-engine');
    if (!engine) return;

    var configEl = document.getElementById('tcodeLayoutConfig');
    if (!configEl) return;
    var config = JSON.parse(configEl.textContent);
    var tcode = config.tCode;
    var rows = [];
    var selectedRows = new Set();
    var sortKey = null;
    var sortDir = 'asc';
    var currentPage = 1;
    var pageSize = 50;

    /* ── Helpers ── */
    function $(sel, ctx) { return (ctx || document).querySelector(sel); }
    function $$(sel, ctx) { return Array.from((ctx || document).querySelectorAll(sel)); }
    function el(tag, attrs, text) {
        var e = document.createElement(tag);
        if (attrs) Object.keys(attrs).forEach(function (k) {
            if (k === 'className') e.className = attrs[k];
            else if (k === 'style' && typeof attrs[k] === 'object') Object.assign(e.style, attrs[k]);
            else if (k.startsWith('on')) e.addEventListener(k.slice(2).toLowerCase(), attrs[k]);
            else e.setAttribute(k, attrs[k]);
        });
        if (text !== undefined) e.textContent = text;
        return e;
    }

    /* ── Render table body ── */
    function renderTable() {
        var tbody = $('#tcodeTableBody');
        if (!tbody) return;

        var filtered = filterRows(rows);
        var sorted = sortRows(filtered);
        var paged = paginate(sorted);
        updateRowCount(sorted.length);

        tbody.innerHTML = '';
        if (paged.length === 0) {
            var tr = el('tr', { className: 'tcode-empty-row' });
            var td = el('td', {
                colspan: String(config.columns.filter(function (c) { return !c.fixed; }).length + 1),
                className: 'text-center text-muted py-4'
            });
            td.innerHTML = '<i class="bi bi-inbox fs-3 d-block mb-2"></i>No data to display';
            tr.appendChild(td);
            tbody.appendChild(tr);
            return;
        }

        paged.forEach(function (row, idx) {
            var tr = el('tr', {
                'data-row-index': String(row.__idx),
                onClick: function (e) {
                    if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT' || e.target.tagName === 'BUTTON') return;
                    toggleRowSelect(row.__idx, tr);
                }
            });

            // Checkbox cell
            var tdCheck = el('td', { className: 'tcode-check-cell' });
            var cb = el('input', {
                type: 'checkbox',
                className: 'tcode-row-select',
                'data-row-index': String(row.__idx),
                checked: selectedRows.has(row.__idx) ? 'checked' : undefined,
                onChange: function () { toggleRowSelect(row.__idx, tr); }
            });
            if (selectedRows.has(row.__idx)) cb.checked = true;
            tdCheck.appendChild(cb);
            tr.appendChild(tdCheck);

            config.columns.filter(function (c) { return !c.fixed; }).forEach(function (col) {
                var td = el('td', {
                    style: { textAlign: col.align || 'left', width: col.width + 'px', minWidth: col.width + 'px' }
                });
                renderCell(td, col, row);
                tr.appendChild(td);
            });

            tbody.appendChild(tr);
        });

        updateDeleteBtn();
    }

    function renderCell(td, col, row) {
        var val = row[col.key];
        if (val === undefined || val === null) val = col.defaultValue || '';

        switch (col.type) {
            case 'number':
            case 'currency':
                if (col.editable) {
                    var inp = el('input', {
                        type: 'number',
                        className: 'tcode-cell-input' + (col.type === 'currency' ? ' tcode-cell-currency' : ''),
                        value: String(val),
                        step: col.type === 'currency' ? '0.01' : '1',
                        'data-key': col.key,
                        'data-row-index': String(row.__idx)
                    });
                    inp.addEventListener('change', function () {
                        row[col.key] = col.type === 'currency' ? parseFloat(this.value) || 0 : parseInt(this.value) || 0;
                        updateRowValidation(row, col);
                    });
                    td.appendChild(inp);
                } else {
                    if (col.type === 'currency') {
                        td.className = 'tcode-cell-currency';
                        td.textContent = formatCurrency(val);
                    } else {
                        td.textContent = val;
                    }
                }
                break;

            case 'date':
                if (col.editable) {
                    var dInp = el('input', {
                        type: 'date',
                        className: 'tcode-cell-input',
                        value: String(val),
                        'data-key': col.key,
                        'data-row-index': String(row.__idx)
                    });
                    dInp.addEventListener('change', function () { row[col.key] = this.value; });
                    td.appendChild(dInp);
                } else {
                    td.textContent = val;
                }
                break;

            case 'dropdown':
                if (col.editable) {
                    var sel = el('select', { className: 'tcode-cell-select', 'data-key': col.key, 'data-row-index': String(row.__idx) });
                    sel.appendChild(el('option', { value: '' }, '-- Select --'));
                    (col.options || []).forEach(function (opt) {
                        var o = el('option', { value: opt.value }, opt.label);
                        if (String(val) === opt.value) o.selected = true;
                        sel.appendChild(o);
                    });
                    sel.addEventListener('change', function () { row[col.key] = this.value; });
                    td.appendChild(sel);
                } else {
                    var optMatch = (col.options || []).find(function (o) { return o.value === String(val); });
                    td.textContent = optMatch ? optMatch.label : val;
                }
                break;

            case 'status_icon':
                var iconVal = String(val);
                var statusOpt = (col.options || []).find(function (o) { return o.value === iconVal; });
                var color = statusOpt ? statusOpt.color : 'secondary';
                var badge = el('span', { className: 'tcode-badge tcode-badge-' + color });
                var icon = statusOpt ? (color === 'success' ? 'bi-check-circle-fill' : color === 'danger' ? 'bi-x-circle-fill' : 'bi-exclamation-circle-fill') : 'bi-dash-circle';
                badge.innerHTML = '<i class="bi ' + icon + '"></i> ' + (statusOpt ? statusOpt.label : iconVal);
                td.appendChild(badge);
                break;

            case 'status_badge':
                var sVal = String(val);
                var sOpt = (col.options || []).find(function (o) { return o.value === sVal; });
                var sColor = sOpt ? sOpt.color : 'secondary';
                var sBadge = el('span', { className: 'tcode-badge tcode-badge-' + sColor });
                sBadge.textContent = sOpt ? sOpt.label : sVal;
                td.appendChild(sBadge);
                break;

            case 'validation_icon':
                var isValid = validateRow(row);
                var iconEl = el('i', {
                    className: 'tcode-validation-icon ' + (isValid ? 'valid' : 'invalid'),
                    'data-row-index': String(row.__idx)
                });
                iconEl.className = 'tcode-validation-icon ' + (isValid ? 'valid bi-check-circle-fill' : 'invalid bi-x-circle-fill');
                td.appendChild(iconEl);
                break;

            case 'mandatory_icon':
                var required = col.required || (col.key === 'material');
                if (required) {
                    td.innerHTML = '<i class="bi bi-asterisk text-danger" style="font-size:0.6rem"></i>';
                }
                break;

            case 'changed_icon':
                if (row.__dirty && row.__dirty[col.key]) {
                    td.innerHTML = '<i class="bi bi-pencil-fill text-primary" style="font-size:0.65rem"></i>';
                }
                break;

            default: // text
                if (col.editable) {
                    var tInp = el('input', {
                        type: 'text',
                        className: 'tcode-cell-input',
                        value: String(val),
                        'data-key': col.key,
                        'data-row-index': String(row.__idx),
                        style: { minWidth: (col.width - 20) + 'px' }
                    });
                    tInp.addEventListener('change', function () {
                        row[col.key] = this.value;
                        row.__dirty = row.__dirty || {};
                        row.__dirty[col.key] = true;
                        updateRowValidation(row, col);
                    });
                    td.appendChild(tInp);
                } else {
                    td.textContent = val;
                }
                break;
        }
    }

    function validateRow(row) {
        var cols = config.columns.filter(function (c) { return c.validation && c.validation.required; });
        return cols.every(function (c) {
            var v = row[c.key];
            return v !== undefined && v !== null && v !== '';
        });
    }

    function updateRowValidation(row, col) {
        if (col.validation && col.validation.required) {
            var v = row[col.key];
            var valid = v !== undefined && v !== null && v !== '';
            var valIcon = engine.querySelector('.tcode-validation-icon[data-row-index="' + row.__idx + '"]');
            if (valIcon) {
                valIcon.className = 'tcode-validation-icon ' + (valid ? 'valid bi-check-circle-fill' : 'invalid bi-x-circle-fill');
            }
        }
    }

    function formatCurrency(val) {
        return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(val || 0);
    }

    /* ── Row Selection ── */
    function toggleRowSelect(idx, tr) {
        if (selectedRows.has(idx)) {
            selectedRows.delete(idx);
            tr.classList.remove('selected');
        } else {
            selectedRows.add(idx);
            tr.classList.add('selected');
        }
        updateDeleteBtn();
    }

    function updateDeleteBtn() {
        var btn = $('#deleteRowBtn');
        if (btn) btn.disabled = selectedRows.size === 0;
    }

    /* ── Filtering ── */
    function filterRows(data) {
        var searchVal = ($('.tcode-filter-input') || {}).value || '';
        searchVal = searchVal.trim().toLowerCase();
        if (!searchVal) return data;
        return data.filter(function (row) {
            return config.columns.some(function (col) {
                var v = row[col.key];
                return v !== undefined && v !== null && String(v).toLowerCase().indexOf(searchVal) > -1;
            });
        });
    }

    /* ── Sorting ── */
    function sortRows(data) {
        if (!sortKey) return data;
        var col = config.columns.find(function (c) { return c.key === sortKey; });
        if (!col) return data;
        return data.slice().sort(function (a, b) {
            var va = a[sortKey] || '';
            var vb = b[sortKey] || '';
            if (col.type === 'number' || col.type === 'currency') {
                va = parseFloat(va) || 0;
                vb = parseFloat(vb) || 0;
                return sortDir === 'asc' ? va - vb : vb - va;
            }
            va = String(va).toLowerCase();
            vb = String(vb).toLowerCase();
            return sortDir === 'asc' ? va.localeCompare(vb) : vb.localeCompare(va);
        });
    }

    /* ── Pagination ── */
    function paginate(data) {
        var start = (currentPage - 1) * pageSize;
        return data.slice(start, start + pageSize);
    }

    function updateRowCount(total) {
        var el = $('.tcode-row-count');
        if (el) el.textContent = total + ' Row' + (total !== 1 ? 's' : '');
        renderPagination(total);
    }

    function renderPagination(total) {
        var container = $('#tcodePagination');
        if (!container) return;
        var totalPages = Math.ceil(total / pageSize);
        container.innerHTML = '';
        if (totalPages <= 1) return;

        if (currentPage > 1) {
            var prev = el('button', { className: 'tcode-pagination-btn', onClick: function () { currentPage--; renderTable(); } }, '‹');
            container.appendChild(prev);
        }

        var startPage = Math.max(1, currentPage - 2);
        var endPage = Math.min(totalPages, currentPage + 2);
        for (var i = startPage; i <= endPage; i++) {
            var pg = el('button', {
                className: 'tcode-pagination-btn' + (i === currentPage ? ' active' : ''),
                onClick: (function (p) { return function () { currentPage = p; renderTable(); }; })(i)
            }, String(i));
            container.appendChild(pg);
        }

        if (currentPage < totalPages) {
            var next = el('button', { className: 'tcode-pagination-btn', onClick: function () { currentPage++; renderTable(); } }, '›');
            container.appendChild(next);
        }
    }

    /* ── Add / Delete Rows ── */
    function addRow() {
        var newRow = { __idx: rows.length, __dirty: {} };
        config.columns.forEach(function (c) {
            newRow[c.key] = c.defaultValue || '';
        });
        rows.push(newRow);
        renderTable();
        showToast('Row added', 'success');
    }

    function deleteRows() {
        if (selectedRows.size === 0) return;
        if (!confirm('Delete ' + selectedRows.size + ' selected row(s)?')) return;
        rows = rows.filter(function (r) { return !selectedRows.has(r.__idx); });
        selectedRows.clear();
        renderTable();
        showToast('Rows deleted', 'success');
    }

    /* ── Toast ── */
    function showToast(msg, type) {
        var toast = el('div', {
            className: 'toast align-items-center text-bg-' + (type || 'info') + ' border-0 show position-fixed bottom-0 end-0 m-3',
            style: { zIndex: '9999' },
            role: 'alert'
        });
        var inner = el('div', { className: 'd-flex' });
        inner.appendChild(el('div', { className: 'toast-body' }, msg));
        var closeBtn = el('button', { className: 'btn-close btn-close-white me-2 m-auto', 'data-bs-dismiss': 'toast' });
        inner.appendChild(closeBtn);
        toast.appendChild(inner);
        document.body.appendChild(toast);
        setTimeout(function () { toast.remove(); }, 3000);
    }

    /* ── Export CSV ── */
    function exportCSV() {
        if (rows.length === 0) { showToast('No data to export', 'warning'); return; }
        var headers = config.columns.filter(function (c) { return !c.fixed; }).map(function (c) { return c.label; });
        var csvRows = [headers.join(',')];
        rows.forEach(function (row) {
            var line = config.columns.filter(function (c) { return !c.fixed; }).map(function (col) {
                var val = String(row[col.key] || '').replace(/"/g, '""');
                return '"' + val + '"';
            });
            csvRows.push(line.join(','));
        });
        var blob = new Blob([csvRows.join('\n')], { type: 'text/csv' });
        var url = URL.createObjectURL(blob);
        var a = el('a', { href: url, download: tcode + '_export.csv' });
        a.click();
        URL.revokeObjectURL(url);
        showToast('Exported ' + rows.length + ' rows', 'success');
    }

    /* ── Tab switching ── */
    function initTabs() {
        $$('.tcode-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                $$('.tcode-tab').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
                // Tab content switching handled via visual state only (single table for now)
            });
        });
    }

    /* ── Column sorting ── */
    function initSorting() {
        $$('.tcode-th').forEach(function (th) {
            var sortIcon = $('.tcode-sort-icon', th);
            if (!sortIcon) return;
            th.addEventListener('click', function () {
                var key = th.getAttribute('data-key');
                if (sortKey === key) {
                    sortDir = sortDir === 'asc' ? 'desc' : 'asc';
                } else {
                    sortKey = key;
                    sortDir = 'asc';
                }
                $$('.tcode-sort-icon').forEach(function (i) { i.style.opacity = '0.5'; });
                sortIcon.style.opacity = '1';
                renderTable();
            });
        });
    }

    /* ── Select all checkbox ── */
    function initSelectAll() {
        var selectAll = $('#selectAllRows');
        if (selectAll) {
            selectAll.addEventListener('change', function () {
                var checked = this.checked;
                selectedRows.clear();
                if (checked) {
                    rows.forEach(function (r) { selectedRows.add(r.__idx); });
                }
                $$('.tcode-row-select').forEach(function (cb) {
                    cb.checked = checked;
                    var tr = cb.closest('tr');
                    if (tr) tr.classList.toggle('selected', checked);
                });
                updateDeleteBtn();
            });
        }
    }

    /* ── Table filter ── */
    function initFilter() {
        var filterInput = $('.tcode-filter-input');
        if (filterInput) {
            var timeout;
            filterInput.addEventListener('input', function () {
                clearTimeout(timeout);
                timeout = setTimeout(function () { currentPage = 1; renderTable(); }, 200);
            });
        }
    }

    /* ── Toolbar action handlers ── */
    function initToolbarActions() {
        $$('.tcode-toolbar-btn, .tcode-action-btn, .tcode-table-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var action = this.getAttribute('data-action');
                if (!action) return;

                var confirmMsg = this.getAttribute('data-confirm');
                if (confirmMsg && !confirm(confirmMsg)) return;

                switch (action) {
                    case 'save':
                        showToast('Saved successfully', 'success');
                        break;
                    case 'post':
                        showToast('Posted successfully', 'success');
                        break;
                    case 'simulate':
                        showToast('Simulation complete (no errors)', 'success');
                        break;
                    case 'validate':
                    case 'check':
                        var errors = rows.filter(function (r) { return !validateRow(r); });
                        if (errors.length > 0) {
                            showToast(errors.length + ' row(s) have validation errors', 'danger');
                        } else {
                            showToast('All rows valid', 'success');
                        }
                        break;
                    case 'refresh':
                        renderTable();
                        showToast('Refreshed', 'info');
                        break;
                    case 'print':
                        window.print();
                        break;
                    case 'back':
                        if (confirm('Leave this transaction?')) window.history.back();
                        break;
                    case 'addRow':
                        addRow();
                        break;
                    case 'deleteRow':
                        deleteRows();
                        break;
                    case 'export':
                        exportCSV();
                        break;
                    case 'toggleFilter':
                        var wrapper = $('.tcode-table-toolbar-left');
                        if (wrapper) wrapper.classList.toggle('show-filter');
                        break;
                    case 'execute':
                        showToast('Query executed', 'success');
                        break;
                    case 'release':
                        showToast('Order released', 'success');
                        break;
                    case 'usageDecision':
                        showToast('Usage decision recorded', 'success');
                        break;
                    case 'extensions':
                        showToast('Extensions view opened', 'info');
                        break;
                    case 'copyFrom':
                        showToast('Copy from dialog opened', 'info');
                        break;
                    case 'showDetails':
                        showToast('Details panel toggled', 'info');
                        break;
                    case 'force':
                        showToast('Force mode activated', 'warning');
                        break;
                    case 'balanceCheck':
                        showToast('Balance check: documents balanced', 'success');
                        break;
                    case 'simulate':
                        showToast('Simulated: no errors found', 'success');
                        break;
                    default:
                        showToast('Action: ' + action, 'info');
                }
            });
        });
    }

    /* ── Keyboard shortcuts ── */
    function initKeyboard() {
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault();
                var saveBtn = $$('.tcode-toolbar-btn, .tcode-action-btn').find(function (b) {
                    return b.getAttribute('data-action') === 'save';
                });
                if (saveBtn) saveBtn.click();
            }
            if (e.key === 'F8') {
                e.preventDefault();
                var postBtn = $$('.tcode-action-btn').find(function (b) {
                    return b.getAttribute('data-action') === 'post';
                });
                if (postBtn) postBtn.click();
            }
        });
    }

    /* ── Init ── */
    function init() {
        initTabs();
        initSorting();
        initSelectAll();
        initFilter();
        initToolbarActions();
        initKeyboard();
        renderTable();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
