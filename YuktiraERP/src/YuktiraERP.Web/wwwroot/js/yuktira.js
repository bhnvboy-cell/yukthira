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
        $.get('/api/notifications/unread-count', function(data) {
            if (data.count > 0) {
                $('#notificationBell').show();
                $('#notifCount').text(data.count);
            }
        }).fail(function() { /* Not logged in */ });
    }

    setInterval(loadNotificationCount, 30000);

    $('[data-bs-toggle="tooltip"]').tooltip();
});
