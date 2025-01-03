var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    var urlParams = new URLSearchParams(url);
    var status = urlParams.get('status');
    loadDataTable(status);

    // if (url.includes("inprocess")) {
    //     loadDataTable("inprocess");
    // }
    // else {
    //     if (url.includes("completed")) {
    //         loadDataTable("completed");
    //     }
    //     else {
    //         if (url.includes("pending")) {
    //             loadDataTable("pending");
    //         }
    //         else {
    //             if (url.includes("approved")) {
    //                 loadDataTable("approved");
    //             }
    //             else {
    //                 loadDataTable("all");
    //             }
    //         }
    //     }
    // }
    
});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/order/getall?status=' + status }, 
        "columns": [
            { data: 'id', "width": "5%" },
            { data: 'name', "width": "30%" },
            { data: 'phoneNumber', "width": "15%" },            
            { data: 'applicationUser.email', "width": "20%" },
            { data: 'orderStatus', "width": "10%" },
            { data: 'orderTotal', "width": "10%" },
            { 
                data: 'id',
                "render": function (data) {
                    return `
                    <div class="btn-group w-75" role="group">
                        <a href="/admin/order/details?orderId=${data}" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i> Edit
                        </a>
                    </div>`
                }, 
                "width": "10%" 
            }
        ]
    });
}
