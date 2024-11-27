var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/company/getall' }, 
        "columns": [
            { data: 'name', "width": "20%" },
            { data: 'streetAddress', "width": "20%" },
            { data: 'city', "width": "12%" },
            { data: 'state', "width": "12%" },
            { data: 'phoneNumber', "width": "12%" },
            { 
                data: 'id',
                "render": function (data) {
                    return `
                    <div class="btn-group w-75" role="group">
                        <a href="/admin/company/createorupdate?id=${data}" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i> Edit
                        </a>
                        <a onClick=Delete('/admin/company/delete?id=${data}') class="btn btn-danger mx-2">
                            <i class="bi bi-trash-fill"></i> Delete
                        </a>
                    </div>`
                }, 
                "width": "24%" 
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure to delete this company?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
      }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE', 
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();

                        toastr.options = {
                            "closeButton": true,
                            "progressBar": true,
                            "positionClass": "toast-bottom-right",
                            "showDuration": "300"
                        };
                        toastr.success(data.message);

                    } else {
                        toastr.options = {
                            "closeButton": true,
                            "progressBar": true,
                            "positionClass": "toast-bottom-right",
                            "showDuration": "300"
                        };
                        toastr.error(data.message);
                    }                    
                }
            });
        }
      });
}
