// Agregar este script al final de tu sección de scripts existente
$(document).ready(function () {
    // Configuración de fechas por defecto para el rango
    const hoy = new Date();
    const primerDiaMes = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
    $('#fechaInicioItem').val(primerDiaMes.toISOString().split('T')[0]);
    $('#fechaFinItem').val(hoy.toISOString().split('T')[0]);

    // Mostrar/ocultar secciones según el período seleccionado
    $('#selectPeriodoItem').on('change', function () {
        $('.periodo-item-section').hide();
        const periodo = $(this).val();

        if (periodo === 'Día') {
            $('#diaItemSection').show();
        } else if (periodo === 'Mes') {
            $('#mesItemSection').show();
        } else if (periodo === 'Rango') {
            $('#rangoItemSection').show();
        }
    });

    // Descargar reporte por día
    $('#btnDescargarItemDia').on('click', function () {
        const tipoItem = $('#selectTipoItem').val();
        const fecha = $('#fechaReporteItemDia').val();

        if (!tipoItem) {
            alert('Por favor seleccione un tipo de ítem');
            return;
        }

        if (!fecha) {
            alert('Por favor seleccione una fecha');
            return;
        }

        window.location.href = `/Reportes/DescargarPdfVentasPorItem?tipoItem=${tipoItem}&periodo=Día&fecha=${fecha}`;
    });

    // Descargar reporte por mes
    $('#btnDescargarItemMes').on('click', function () {
        const tipoItem = $('#selectTipoItem').val();
        const mes = $('#selectMesItem').val();
        const año = $('#selectAnioItem').val();

        if (!tipoItem) {
            alert('Por favor seleccione un tipo de ítem');
            return;
        }

        if (!mes) {
            alert('Por favor seleccione un mes');
            return;
        }

        window.location.href = `/Reportes/DescargarPdfVentasPorItem?tipoItem=${tipoItem}&periodo=Mes&mes=${mes}&año=${año}`;
    });

    // Descargar reporte por rango
    $('#btnDescargarItemRango').on('click', function () {
        const tipoItem = $('#selectTipoItem').val();
        const fechaInicio = $('#fechaInicioItem').val();
        const fechaFin = $('#fechaFinItem').val();

        if (!tipoItem) {
            alert('Por favor seleccione un tipo de ítem');
            return;
        }

        if (!fechaInicio || !fechaFin) {
            alert('Por favor seleccione ambas fechas');
            return;
        }

        if (fechaInicio > fechaFin) {
            alert('La fecha de inicio no puede ser mayor a la fecha final');
            return;
        }

        window.location.href = `/Reportes/DescargarPdfVentasPorItem?tipoItem=${tipoItem}&periodo=Rango&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}`;
    });
});