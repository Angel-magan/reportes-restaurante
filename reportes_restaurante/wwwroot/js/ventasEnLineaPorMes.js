document.addEventListener('DOMContentLoaded', function () {
    const btnDescargarVentasLineaMes = document.getElementById('btnDescargarVentasLineaMes');

    if (btnDescargarVentasLineaMes) {
        btnDescargarVentasLineaMes.addEventListener('click', function () {
            const mes = document.getElementById('selectMesLinea').value;
            const anio = document.getElementById('selectAnioLinea').value;

            if (!mes) {
                alert('Por favor seleccione un mes');
                return;
            }

            if (!anio) {
                alert('Por favor seleccione un año');
                return;
            }

            // Validar que no sea un mes futuro
            const fechaActual = new Date();
            const mesActual = fechaActual.getMonth() + 1;
            const anioActual = fechaActual.getFullYear();

            if (anio > anioActual || (anio == anioActual && mes > mesActual)) {
                if (!confirm('El mes seleccionado es futuro. ¿Desea continuar?')) {
                    return;
                }
            }

            window.location.href = `/Reportes/DescargarPdfVentasEnLineaMes?año=${anio}&mes=${mes}`;
        });
    }
});