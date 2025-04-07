document.getElementById('btnDescargarVentasMes').addEventListener('click', function() {
        const mes = document.getElementById('selectMes').value;
        const anio = document.getElementById('selectAnio').value;

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
        const mesActual = fechaActual.getMonth() + 1; // Los meses son 0-11 en JS
        const anioActual = fechaActual.getFullYear();

        if (anio > anioActual || (anio == anioActual && mes > mesActual)) {
            if (!confirm('El mes seleccionado es futuro. ¿Desea continuar?')) {
                return;
            }
        }

        window.location.href = `/Reportes/DescargarPdfVentasMes?año=${anio}&mes=${mes}`;
    });

    

