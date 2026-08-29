/* ── YuktiraERP Enterprise Form Scripts ── */

/* ═══════════════════════════════════════════════════
   TAB SWITCHING
   ═══════════════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.ent-form-tabs').forEach(function (tabBar) {
        var wrapper = tabBar.closest('.ent-form-wrapper');
        if (!wrapper) return;

        tabBar.querySelectorAll('.ent-form-tab').forEach(function (tab) {
            tab.addEventListener('click', function () {
                var target = this.dataset.tab;
                tabBar.querySelectorAll('.ent-form-tab').forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
                wrapper.querySelectorAll('.ent-form-panel').forEach(function (p) { p.classList.remove('active'); });
                var panel = wrapper.querySelector('.ent-form-panel[data-panel="' + target + '"]');
                if (panel) panel.classList.add('active');
            });
        });
    });

    document.querySelectorAll('.ent-add-line-btn').forEach(function (btn) {
        btn.addEventListener('click', function () { addLineItem(this.dataset.target); });
    });

    recalcAllTotals();
});

/* ═══════════════════════════════════════════════════
   LINE ITEM MANAGEMENT
   ═══════════════════════════════════════════════════ */
function addLineItem(tableId) {
    var table = document.getElementById(tableId);
    if (!table) return;
    var tbody = table.querySelector('tbody');
    if (!tbody) return;
    var idx = tbody.querySelectorAll('tr').length;
    var tmpl = document.getElementById(tableId + '-template');
    if (!tmpl) return;
    var html = tmpl.innerHTML.replace(/\{INDEX\}/g, idx);
    tbody.insertAdjacentHTML('beforeend', html);
    recalcAllTotals();
}

function removeLineItem(btn) {
    var row = btn.closest('tr');
    if (row) {
        row.remove();
        reindexLineItems(row.closest('tbody'));
        recalcAllTotals();
    }
}

function reindexLineItems(tbody) {
    if (!tbody) return;
    var rows = tbody.querySelectorAll('tr');
    rows.forEach(function (row, i) {
        row.querySelectorAll('input, select, textarea').forEach(function (el) {
            if (el.name) el.name = el.name.replace(/\[\d+\]/, '[' + i + ']');
        });
    });
}

/* ═══════════════════════════════════════════════════
   AUTO-CALCULATION
   ═══════════════════════════════════════════════════ */
function recalcAllTotals() {
    document.querySelectorAll('.ent-line-items-table').forEach(function (table) {
        var tbody = table.querySelector('tbody');
        if (!tbody) return;
        var grandTotal = 0;
        var lineCount = 0;

        tbody.querySelectorAll('tr').forEach(function (row) {
            var qty = parseFloat(row.querySelector('.line-qty')?.value) || 0;
            var price = parseFloat(row.querySelector('.line-price')?.value) || 0;
            var discount = parseFloat(row.querySelector('.line-discount')?.value) || 0;
            var lineTotal = (qty * price) - discount;

            var totalCell = row.querySelector('.line-total');
            if (totalCell) totalCell.textContent = lineTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

            if (row.querySelector('.line-material')?.value) {
                grandTotal += lineTotal;
                lineCount++;
            }
        });

        var wrap = table.closest('.ent-line-items-wrap');
        if (wrap) {
            var countEl = wrap.querySelector('.line-count');
            if (countEl) countEl.textContent = lineCount;
            var totalEl = wrap.querySelector('.grand-total');
            if (totalEl) totalEl.textContent = grandTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }

        var form = table.closest('form');
        if (form) {
            var totalInput = form.querySelector('input[name="Order.Amount"], input[name="Requisition.TotalAmount"], input[name="Order.TotalAmount"]');
            if (totalInput) totalInput.value = grandTotal.toFixed(2);
            var countInput = form.querySelector('input[name="Order.ItemCount"], input[name="Requisition.ItemCount"], input[name="Order.ItemCount"]');
            if (countInput) countInput.value = lineCount;
        }
    });
}

document.addEventListener('input', function (e) {
    if (e.target.matches('.line-qty, .line-price, .line-discount')) {
        recalcAllTotals();
    }
});
