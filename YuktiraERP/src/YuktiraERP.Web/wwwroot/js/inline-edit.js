var InlineEdit = (function () {
    'use strict';

    function getCsrfToken() {
        var meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function toggleEditRow(btn) {
        var row = btn.closest('tr');
        if (!row) return;
        var isEditing = row.classList.contains('editing');

        if (isEditing) {
            cancelInlineEdit(row);
        } else {
            startEditRow(row);
        }
    }

    function startEditRow(row) {
        var cells = row.querySelectorAll('td[data-field]');
        cells.forEach(function (cell) {
            var field = cell.getAttribute('data-field');
            var currentVal = cell.getAttribute('data-value') || cell.textContent.trim();
            var entityType = cell.getAttribute('data-type') || 'text';
            var input = document.createElement('input');
            input.type = entityType === 'number' ? 'number' : 'text';
            input.value = currentVal;
            input.className = 'form-control form-control-sm';
            input.style.minWidth = '80px';
            if (entityType === 'number') input.step = 'any';
            cell.setAttribute('data-original', cell.innerHTML);
            cell.innerHTML = '';
            cell.appendChild(input);
        });

        row.classList.add('editing');
        row.style.background = 'rgba(37,99,235,0.04)';

        var actionsCell = row.querySelector('td.actions-cell');
        if (actionsCell) {
            actionsCell.setAttribute('data-original', actionsCell.innerHTML);
            actionsCell.innerHTML =
                '<button type="button" class="btn btn-sm btn-success me-1" onclick="InlineEdit.saveRow(this)" title="Save"><i class="bi bi-check-lg"></i></button>' +
                '<button type="button" class="btn btn-sm btn-secondary" onclick="InlineEdit.cancelRow(this)" title="Cancel"><i class="bi bi-x-lg"></i></button>';
        }

        var firstInput = row.querySelector('td[data-field] input');
        if (firstInput) firstInput.focus();
    }

    function cancelInlineEdit(row) {
        var cells = row.querySelectorAll('td[data-field]');
        cells.forEach(function (cell) {
            var orig = cell.getAttribute('data-original');
            if (orig !== null) cell.innerHTML = orig;
        });

        var actionsCell = row.querySelector('td.actions-cell');
        if (actionsCell) {
            var orig = actionsCell.getAttribute('data-original');
            if (orig !== null) actionsCell.innerHTML = orig;
        }

        row.classList.remove('editing');
        row.style.background = '';
    }

    function cancelRow(btn) {
        var row = btn.closest('tr');
        if (row) cancelInlineEdit(row);
    }

    function saveRow(btn) {
        var row = btn.closest('tr');
        if (!row) return;

        var entityId = row.getAttribute('data-id');
        var entityType = row.getAttribute('data-entity');
        var module = row.getAttribute('data-module');
        var payload = {};

        row.querySelectorAll('td[data-field]').forEach(function (cell) {
            var field = cell.getAttribute('data-field');
            var input = cell.querySelector('input');
            if (input) {
                var val = input.value;
                if (input.type === 'number') val = parseFloat(val) || 0;
                payload[field] = val;
            }
        });

        var saveBtn = btn;
        saveBtn.disabled = true;
        saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

        var url = '/api/v1/' + encodeURIComponent(module) + '/' + encodeURIComponent(entityType) + '/inline-update';
        fetch(url, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify({ id: entityId, fields: payload })
        })
        .then(function (resp) {
            if (!resp.ok) throw new Error('Save failed');
            return resp.json();
        })
        .then(function (data) {
            row.querySelectorAll('td[data-field]').forEach(function (cell) {
                var field = cell.getAttribute('data-field');
                if (payload.hasOwnProperty(field)) {
                    var displayVal = payload[field];
                    cell.setAttribute('data-value', displayVal);
                    cell.textContent = typeof displayVal === 'number' ? displayVal.toLocaleString() : displayVal;
                }
            });

            var actionsCell = row.querySelector('td.actions-cell');
            if (actionsCell) {
                var orig = actionsCell.getAttribute('data-original');
                if (orig !== null) actionsCell.innerHTML = orig;
            }

            row.classList.remove('editing');
            row.style.background = '';
            row.style.transition = 'background 0.5s';
            row.style.background = 'rgba(5,150,105,0.08)';
            setTimeout(function () { row.style.background = ''; }, 1500);
        })
        .catch(function (err) {
            row.classList.remove('editing');
            row.style.background = 'rgba(220,38,38,0.06)';
            setTimeout(function () { row.style.background = ''; }, 2000);
            saveBtn.disabled = false;
            saveBtn.innerHTML = '<i class="bi bi-check-lg"></i>';
        });
    }

    function initTable(tableEl) {
        if (!tableEl) return;
        tableEl.querySelectorAll('tbody tr[data-id]').forEach(function (row) {
            row.querySelectorAll('td[data-field]').forEach(function (cell) {
                cell.setAttribute('data-value', cell.textContent.trim());
            });
        });
    }

    return {
        toggleEditRow: toggleEditRow,
        saveRow: saveRow,
        cancelRow: cancelRow,
        initTable: initTable
    };
})();
