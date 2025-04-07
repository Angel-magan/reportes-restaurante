
$('#btnDescargarPorRango').on('click', function () {
    const fechaInicio = $('#fechaInicio').val();
    const fechaFin = $('#fechaFin').val();

    if (!fechaInicio || !fechaFin) {
        alert('Seleccione ambas fechas');
        return;
    }

    window.location.href = `/Reportes/DescargarPdfPorRango?fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`;
});
