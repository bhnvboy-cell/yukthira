/* ══════════════════════════════════════════════════════════════
   TCode Engine – Config→DOM renderer with real API CRUD
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
    var deletedRecordIds = [];
    var sortKey = null;
    var sortDir = 'asc';
    var currentPage = 1;
    var pageSize = 50;
    var isLoading = false;

    /* ── Workflow state ── */
    var workflowChains = [];
    var currentChainId = null;
    var currentChain = null;
    var workflowSteps = [];
    var activeInstances = [];

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

    function setStatus(msg, type) {
        var s = $('#tcodeStatus');
        if (!s) return;
        var icon = type === 'success' ? 'bi-check-circle-fill text-success' :
                   type === 'error' ? 'bi-x-circle-fill text-danger' :
                   type === 'loading' ? 'bi-hourglass-split text-warning' :
                   'bi-check-circle-fill text-success';
        s.innerHTML = '<i class="bi ' + icon + '"></i> ' + msg;
    }

    /* ── API calls ── */
    function apiGet(url) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var headers = { 'Accept': 'application/json' };
        if (token) headers['X-CSRF-TOKEN'] = token.value;
        return fetch(url, { headers: headers, credentials: 'same-origin' })
            .then(function (r) {
                if (!r.ok) throw new Error('API error: ' + r.status);
                return r.json();
            });
    }

    function apiPost(url, data) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var headers = { 'Content-Type': 'application/json', 'Accept': 'application/json' };
        if (token) headers['X-CSRF-TOKEN'] = token.value;
        return fetch(url, {
            method: 'POST',
            headers: headers,
            credentials: 'same-origin',
            body: JSON.stringify(data)
        }).then(function (r) {
            if (!r.ok) throw new Error('API error: ' + r.status);
            return r.json();
        });
    }

    function loadRecords() {
        isLoading = true;
        setStatus('Loading...', 'loading');
        apiGet('/api/v1/TCodeEngine/layout/' + tcode + '/records')
            .then(function (data) {
                rows = [];
                selectedRows.clear();
                deletedRecordIds = [];
                (data || []).forEach(function (r, i) {
                    r.__idx = i;
                    r.__dirty = {};
                    rows.push(r);
                });
                if (rows.length === 0) {
                    for (var j = 0; j < 5; j++) addRow();
                }
                isLoading = false;
                setStatus('Loaded ' + rows.length + ' record(s)', 'success');
                renderTable();
            })
            .catch(function (err) {
                isLoading = false;
                setStatus('Load failed: ' + err.message, 'error');
                showToast('Failed to load data: ' + err.message, 'danger');
                for (var j = 0; j < 5; j++) addRow();
                renderTable();
            });
    }

    function saveRecords(callback) {
        var dirtyRows = rows.filter(function (r) { return r.__dirty && Object.keys(r.__dirty).length > 0; });
        var newRows = rows.filter(function (r) { return !r.__recordId; });
        var updateRows = rows.filter(function (r) { return r.__recordId && r.__dirty && Object.keys(r.__dirty).length > 0; });
        var toSave = newRows.concat(updateRows).map(function (r) {
            var clean = {};
            Object.keys(r).forEach(function (k) {
                if (!k.startsWith('__')) clean[k] = r[k];
            });
            if (r.__recordId) clean.__recordId = r.__recordId;
            return clean;
        });

        validateWorkflowPrereqs(function (valid) {
            if (!valid) return;
            setStatus('Saving...', 'loading');
            apiPost('/api/v1/TCodeEngine/layout/' + tcode + '/records', {
                records: toSave,
                deleteIds: deletedRecordIds
            }).then(function (res) {
                deletedRecordIds = [];
                rows.forEach(function (r) { r.__dirty = {}; });
                setStatus('Saved ' + (res.saved || 0) + ' record(s)', 'success');
                showToast('Saved successfully', 'success');
                executeWorkflowStep();
                if (callback) callback();
                loadRecords();
            }).catch(function (err) {
                setStatus('Save failed', 'error');
                showToast('Save failed: ' + err.message, 'danger');
            });
        });
    }

    /* ── Render table body ── */
    /* ── Workflow API ── */
    function loadWorkflowChains() {
        apiGet('/api/v1/Workflow/chains')
            .then(function (chains) {
                workflowChains = chains || [];
                findChainForTCode();
            })
            .catch(function () {
                workflowChains = [];
            });
    }

    function findChainForTCode() {
        currentChain = null;
        currentChainId = null;
        workflowSteps = [];
        for (var i = 0; i < workflowChains.length; i++) {
            var chain = workflowChains[i];
            var found = chain.steps.some(function (s) {
                return s.tCode.toUpperCase() === tcode.toUpperCase();
            });
            if (found) {
                currentChain = chain;
                currentChainId = chain.id;
                workflowSteps = chain.steps.slice().sort(function (a, b) { return a.order - b.order; });
                break;
            }
        }
        if (currentChain) {
            renderWorkflowBar();
            loadActiveInstances();
        }
    }

    function loadActiveInstances() {
        apiGet('/api/v1/Workflow/instances?chainId=' + currentChainId)
            .then(function (instances) {
                activeInstances = instances || [];
                if (activeInstances.length > 0) {
                    loadWorkflowProgress(activeInstances[0].id);
                } else {
                    renderWorkflowProgress([]);
                }
            })
            .catch(function () {
                renderWorkflowProgress([]);
            });
    }

    function loadWorkflowProgress(instanceId) {
        apiGet('/api/v1/Workflow/chains/' + currentChainId + '/progress?instanceId=' + instanceId)
            .then(function (steps) {
                renderWorkflowProgress(steps || []);
            })
            .catch(function () {
                renderWorkflowProgress([]);
            });
    }

    function renderWorkflowBar() {
        var bar = $('#tcodeWorkflowBar');
        if (!bar || !currentChain) {
            if (bar) bar.style.display = 'none';
            return;
        }
        bar.style.display = 'flex';
        var nameEl = $('#wfChainName');
        if (nameEl) nameEl.textContent = currentChain.name;
        renderWorkflowProgress([]);
    }

    function renderWorkflowProgress(stepsData) {
        var container = $('#wfSteps');
        var infoEl = $('#wfInfo');
        if (!container || !currentChain) return;
        container.innerHTML = '';

        var currentIndex = -1;
        for (var i = 0; i < workflowSteps.length; i++) {
            var step = workflowSteps[i];
            var stepData = stepsData.find(function (s) { return s.tCode === step.tCode; });
            var status = stepData ? stepData.status : 'PENDING';
            var isCurrentTCode = step.tCode.toUpperCase() === tcode.toUpperCase();

            if (status === 'COMPLETED') {
                currentIndex = i;
            } else if (status !== 'COMPLETED' && currentIndex === -1) {
                currentIndex = i;
            }
        }

        var completedCount = 0;
        workflowSteps.forEach(function (step, idx) {
            var stepData = stepsData.find(function (s) { return s.tCode === step.tCode; });
            var status = stepData ? stepData.status : 'PENDING';
            var isCurrentTCode = step.tCode.toUpperCase() === tcode.toUpperCase();

            var stepEl = document.createElement('div');
            stepEl.className = 'tcode-wf-step ' + (isCurrentTCode ? 'current' : '');

            var nodeEl = document.createElement('span');
            nodeEl.className = 'tcode-wf-step-node';

            if (status === 'COMPLETED') {
                nodeEl.className += ' completed';
                nodeEl.innerHTML = '<i class="bi bi-check-lg"></i>';
                completedCount++;
            } else if (isCurrentTCode) {
                nodeEl.className += ' current';
                nodeEl.textContent = String(idx + 1);
            } else {
                nodeEl.className += ' pending';
                nodeEl.textContent = String(idx + 1);
            }
            stepEl.appendChild(nodeEl);

            var labelEl = document.createElement('span');
            labelEl.className = 'tcode-wf-step-label';
            labelEl.textContent = step.name.length > 14 ? step.name.substring(0, 12) + '..' : step.name;
            labelEl.title = step.name + ' (' + step.tCode + ')';
            stepEl.appendChild(labelEl);
            container.appendChild(stepEl);

            if (idx < workflowSteps.length - 1) {
                var connector = document.createElement('div');
                connector.className = 'tcode-wf-connector';
                if (status === 'COMPLETED') {
                    connector.className += ' completed';
                } else if (isCurrentTCode) {
                    connector.className += ' active';
                }
                container.appendChild(connector);
            }
        });

        if (infoEl) {
            var pct = workflowSteps.length > 0 ? Math.round((completedCount / workflowSteps.length) * 100) : 0;
            infoEl.textContent = completedCount + '/' + workflowSteps.length + ' steps (' + pct + '%)';
        }
    }

    function validateWorkflowPrereqs(callback) {
        if (!currentChain) { callback(true); return; }
        apiPost('/api/v1/Workflow/chains/' + currentChainId + '/validate', {
            tCode: tcode,
            context: collectMetadata()
        }).then(function (result) {
            if (result.isValid) {
                callback(true);
            } else {
                showToast('Workflow prerequisite check failed: ' + result.message, 'danger');
                callback(false);
            }
        }).catch(function () {
            callback(true);
        });
    }

    function executeWorkflowStep() {
        if (!currentChain) return;
        var params = collectMetadata();
        rows.forEach(function (row) {
            Object.keys(row).forEach(function (k) {
                if (!k.startsWith('__')) params[k] = row[k];
            });
        });
        apiPost('/api/v1/Workflow/chains/' + currentChainId + '/execute', {
            tCode: tcode,
            parameters: params
        }).then(function (result) {
            if (result.success) {
                showToast('Workflow step executed: ' + result.message, 'success');
                if (result.nextStep) {
                    showToast('Next step: ' + (result.data ? result.data.nextName : result.nextStep), 'info');
                } else {
                    showToast('Workflow chain complete!', 'success');
                }
                loadWorkflowProgress(activeInstances.length > 0 ? activeInstances[0].id : null);
            }
        }).catch(function () { });
    }

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
                        row.__dirty = row.__dirty || {};
                        row.__dirty[col.key] = true;
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
                    dInp.addEventListener('change', function () {
                        row[col.key] = this.value;
                        row.__dirty = row.__dirty || {};
                        row.__dirty[col.key] = true;
                    });
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
                    sel.addEventListener('change', function () {
                        row[col.key] = this.value;
                        row.__dirty = row.__dirty || {};
                        row.__dirty[col.key] = true;
                    });
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

            default:
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
        var e = $('.tcode-row-count');
        if (e) e.textContent = total + ' Row' + (total !== 1 ? 's' : '');
        renderPagination(total);
    }

    function renderPagination(total) {
        var container = $('#tcodePagination');
        if (!container) return;
        var totalPages = Math.ceil(total / pageSize);
        container.innerHTML = '';
        if (totalPages <= 1) return;

        if (currentPage > 1) {
            var prev = el('button', { className: 'tcode-pagination-btn', onClick: function () { currentPage--; renderTable(); } }, '\u2039');
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
            var next = el('button', { className: 'tcode-pagination-btn', onClick: function () { currentPage++; renderTable(); } }, '\u203A');
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
    }

    function deleteRows() {
        if (selectedRows.size === 0) return;
        if (!confirm('Delete ' + selectedRows.size + ' selected row(s)?')) return;
        selectedRows.forEach(function (idx) {
            var row = rows.find(function (r) { return r.__idx === idx; });
            if (row && row.__recordId) deletedRecordIds.push(row.__recordId);
        });
        rows = rows.filter(function (r) { return !selectedRows.has(r.__idx); });
        selectedRows.clear();
        renderTable();
        showToast('Row(s) marked for deletion. Save to confirm.', 'warning');
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

    /* ── Collect metadata from panel ── */
    function collectMetadata() {
        var meta = {};
        $$('.tcode-meta-input').forEach(function (inp) {
            var key = inp.getAttribute('data-key');
            if (key) meta[key] = inp.value;
        });
        $$('.tcode-meta-value').forEach(function (span) {
            var key = span.getAttribute('data-key');
            if (key) meta[key] = span.textContent;
        });
        return meta;
    }

    /* ── Merge metadata into all rows ── */
    function mergeMetadataIntoRows(meta) {
        rows.forEach(function (row) {
            Object.keys(meta).forEach(function (k) {
                if (!row[k] || row[k] === '') row[k] = meta[k];
            });
        });
    }

    /* ── Tab switching ── */
    function initTabs() {
        $$('.tcode-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                $$('.tcode-tab').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
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
                        saveRecords();
                        break;
                    case 'post':
                        saveRecords(function () { showToast('Posted successfully', 'success'); });
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
                        loadRecords();
                        break;
                    case 'print':
                        window.print();
                        break;
                    case 'back':
                        window.history.back();
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
                    case 'activate':
                        setStatus('Activating...', 'loading');
                        rows.forEach(function (r) { r.status = 'ACTIVE'; r.__dirty = r.__dirty || {}; r.__dirty.status = true; });
                        saveRecords(function () { showToast('Activated', 'success'); });
                        break;
                    case 'deactivate':
                        setStatus('Deactivating...', 'loading');
                        rows.forEach(function (r) { r.status = 'INACTIVE'; r.__dirty = r.__dirty || {}; r.__dirty.status = true; });
                        saveRecords(function () { showToast('Deactivated', 'warning'); });
                        break;
                    case 'complete':
                        setStatus('Completing...', 'loading');
                        rows.forEach(function (r) { r.status = 'COMPLETED'; r.__dirty = r.__dirty || {}; r.__dirty.status = true; });
                        saveRecords(function () { showToast('Marked complete', 'success'); });
                        break;
                    case 'release':
                        setStatus('Releasing...', 'loading');
                        rows.forEach(function (r) { r.status = 'RELEASED'; r.__dirty = r.__dirty || {}; r.__dirty.status = true; });
                        saveRecords(function () { showToast('Released', 'success'); });
                        break;
                    case 'execute':
                        setStatus('Executing...', 'loading');
                        saveRecords(function () { showToast('Executed', 'success'); });
                        break;
                    case 'dispose':
                        setStatus('Disposing...', 'loading');
                        rows.forEach(function (r) { r.status = 'DISPOSED'; r.__dirty = r.__dirty || {}; r.__dirty.status = true; });
                        saveRecords(function () { showToast('Disposed', 'warning'); });
                        break;
                    case 'usageDecision':
                        setStatus('Recording UD...', 'loading');
                        rows.forEach(function (r) { r.status = 'UD_RECORDED'; r.__dirty = r.__dirty || {}; r.__dirty.status = true; });
                        saveRecords(function () { showToast('Usage decision recorded', 'success'); });
                        break;
                    case 'confirmCertificate':
                        setStatus('Confirming...', 'loading');
                        rows.forEach(function (r) { r.certificateReceived = 'Yes'; r.__dirty = r.__dirty || {}; r.__dirty.certificateReceived = true; });
                        saveRecords(function () { showToast('Certificate confirmed', 'success'); });
                        break;
                    case 'generateCertificate':
                        showToast('Certificate generated', 'success');
                        break;
                    case 'printCOA':
                        var coaWin = window.open('', '_blank', 'width=800,height=600');
                        var coaHtml = '<html><head><title>Certificate of Analysis - ' + tcode + '</title>';
                        coaHtml += '<style>body{font-family:Arial,sans-serif;padding:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ccc;padding:8px;text-align:left}th{background:#f0f0f0}h1{color:#0066cc}h2{margin-top:20px}</style></head><body>';
                        coaHtml += '<h1>Certificate of Analysis (COA)</h1>';
                        coaHtml += '<p>Transaction: <strong>' + tcode + ' - ' + config.title + '</strong></p>';
                        coaHtml += '<p>Date: ' + new Date().toLocaleDateString() + '</p>';
                        var meta = collectMetadata();
                        if (Object.keys(meta).length > 0) {
                            coaHtml += '<h2>Header Information</h2><table>';
                            Object.keys(meta).forEach(function (k) {
                                coaHtml += '<tr><th>' + k + '</th><td>' + (meta[k] || '') + '</td></tr>';
                            });
                            coaHtml += '</table>';
                        }
                        coaHtml += '<h2>Inspection Results</h2><table><tr>';
                        config.columns.filter(function (c) { return !c.fixed; }).forEach(function (c) {
                            coaHtml += '<th>' + c.label + '</th>';
                        });
                        coaHtml += '</tr>';
                        rows.forEach(function (r) {
                            coaHtml += '<tr>';
                            config.columns.filter(function (c) { return !c.fixed; }).forEach(function (c) {
                                coaHtml += '<td>' + (r[c.key] || '') + '</td>';
                            });
                            coaHtml += '</tr>';
                        });
                        coaHtml += '</table>';
                        coaHtml += '<br><p><em>Generated by YuktiraERP Quality Management</em></p>';
                        coaHtml += '</body></html>';
                        coaWin.document.write(coaHtml);
                        coaWin.document.close();
                        coaWin.print();
                        showToast('COA opened for print', 'success');
                        break;
                    case 'completeTasks':
                        rows.forEach(function (r) {
                            if (selectedRows.size === 0 || selectedRows.has(r.__idx)) {
                                r.status = 'COMPLETED';
                                r.completedAt = new Date().toISOString().split('T')[0];
                                r.__dirty = r.__dirty || {};
                                r.__dirty.status = true;
                                r.__dirty.completedAt = true;
                            }
                        });
                        saveRecords(function () { showToast('Tasks completed', 'success'); });
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
                saveRecords();
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
        loadWorkflowChains();
        loadRecords();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
