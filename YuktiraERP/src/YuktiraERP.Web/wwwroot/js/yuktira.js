var YuktiraFormat = (function() {
    var cfg = window.YuktiraConfig || { currencyCode: 'INR', currencySymbol: '₹', locale: 'en-IN' };

    function getLocale() {
        return cfg.locale || 'en-IN';
    }

    return {
        currency: function(amount, override) {
            var code = (override && override.currencyCode) || cfg.currencyCode;
            var num = parseFloat(amount);
            if (isNaN(num)) return cfg.currencySymbol + '0.00';
            try {
                return new Intl.NumberFormat(getLocale(), {
                    style: 'currency',
                    currency: code,
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2
                }).format(num);
            } catch (e) {
                return cfg.currencySymbol + num.toFixed(2);
            }
        },
        number: function(value, decimals) {
            var num = parseFloat(value);
            if (isNaN(num)) return '0';
            var d = (decimals !== undefined) ? decimals : 2;
            try {
                return new Intl.NumberFormat(getLocale(), {
                    minimumFractionDigits: d,
                    maximumFractionDigits: d
                }).format(num);
            } catch (e) {
                return num.toFixed(d);
            }
        },
        percentage: function(value, decimals) {
            var num = parseFloat(value);
            if (isNaN(num)) return '0%';
            var d = (decimals !== undefined) ? decimals : 1;
            try {
                return new Intl.NumberFormat(getLocale(), {
                    style: 'percent',
                    minimumFractionDigits: d,
                    maximumFractionDigits: d
                }).format(num / 100);
            } catch (e) {
                return num.toFixed(d) + '%';
            }
        },
        date: function(value, options) {
            if (!value) return '';
            var d = new Date(value);
            if (isNaN(d.getTime())) return value;
            var opts = options || { year: 'numeric', month: 'short', day: 'numeric' };
            try {
                return new Intl.DateTimeFormat(getLocale(), opts).format(d);
            } catch (e) {
                return d.toLocaleDateString();
            }
        },
        config: function() { return cfg; }
    };
})();

$(function() {
    loadNotificationCount();

    $('.datatable').each(function() {
        // Basic client-side sorting
        $(this).find('th').click(function() {
            const table = $(this).closest('table');
            const index = $(this).index();
            const rows = table.find('tbody tr').toArray().sort(function(a, b) {
                const aVal = $(a).children('td').eq(index).text();
                const bVal = $(b).children('td').eq(index).text();
                return aVal.localeCompare(bVal, undefined, {numeric: true});
            });
            table.find('tbody').empty().append(rows);
        });
    });

    function loadNotificationCount() {
        $.get('/api/v1/notifications/unread-count', function(data) {
            if (data.count > 0) {
                $('#notificationBell').show();
                $('#notifCount').text(data.count);
            }
        }).fail(function() { /* Not logged in */ });
    }

    setInterval(loadNotificationCount, 30000);

    $('[data-bs-toggle="tooltip"]').tooltip();
});
