document.addEventListener('DOMContentLoaded', function () {
    const btnDescargarVentasLineaDia = document.getElementById('btnDescargarVentasLineaDia');
    const fechaReporteInput = document.getElementById('fechaReporteLineaDiario');

    // Si el input no existe, lo creamos dinámicamente (opcional)
    if (!fechaReporteInput) {
        const contenedor = document.querySelector('.card-body');
        if (contenedor) {
            const divInput = document.createElement('div');
            divInput.className = 'mb-3';
            divInput.innerHTML = `
                <label for="fechaReporteLineaDiario" class="form-label">Fecha del reporte:</label>
                <input type="date" id="fechaReporteLineaDiario" class="form-control" 
                       value="${new Date().toISOString().split('T')[0]}">
            `;
            contenedor.prepend(divInput);
        }
    }

    if (btnDescargarVentasLineaDia) {
        btnDescargarVentasLineaDia.addEventListener('click', function () {
            const fechaSeleccionada = document.getElementById('fechaReporteLineaDiario').value;

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

            window.location.href = `/Reportes/DescargarPdfVentasEnLineaDia?fecha=${fechaSeleccionada}`;
        });
    }
});