
    document.addEventListener('DOMContentLoaded', function() {
        const btnDescargarVentasDia = document.getElementById('btnDescargarVentasDia');

        if (btnDescargarVentasDia) {
            btnDescargarVentasDia.addEventListener('click', function () {
                const fechaSeleccionada = document.getElementById('fechaReporte').value;

                if (!fechaSeleccionada) {
                    alert('Por favor seleccione una fecha válida');
                    return;
                }

                // Validar que la fecha no sea futura
                const fechaActual = new Date().toISOString().split('T')[0];
                if (fechaSeleccionada > fechaActual) {
                    if (!confirm('La fecha seleccionada es futura. ¿Desea continuar?')) {
                        return;
                    }
                }

                window.location.href = `/Reportes/DescargarPdfVentasDia?fecha=${fechaSeleccionada}`;
            });
        }
    });

    

