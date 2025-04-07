
    document.addEventListener('DOMContentLoaded', function() {
        const btnDescargarVentasDia = document.getElementById('btnDescargarVentasDia');
        const fechaReporteInput = document.getElementById('fechaReporte'); // Nuevo input para fecha

        // Si no existe el input de fecha, lo creamos dinámicamente
        if (!fechaReporteInput) {
            const divContenedor = document.querySelector('.d-flex.flex-column.flex-md-row.me-md-3.w-100.w-md-auto');
            const nuevoInput = document.createElement('div');
            nuevoInput.className = 'me-md-3 mb-2 mb-md-0';
            nuevoInput.innerHTML = `
            <label for="fechaReporte">Fecha del reporte:</label>
            <input type="date" id="fechaReporte" class="form-control" value="${new Date().toISOString().split('T')[0]}">
        `;
            divContenedor.prepend(nuevoInput);
        }

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

    

