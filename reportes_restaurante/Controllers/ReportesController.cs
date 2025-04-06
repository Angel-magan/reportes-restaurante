using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using reportes_restaurante.Models;
using System.Data;
using System.Text;



namespace reportes_restaurante.Controllers
{
    public class ReportesController : Controller
    {
        private readonly restauranteContext _context;
        private readonly IConverter _converter;
        public ReportesController(restauranteContext context, IConverter converter)
        {
            _context = context;
            _converter = converter;
        }
        public IActionResult DashboardReportes()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ObtenerVentasPorPeriodo(int? year, DateTime? fechaInicio, DateTime? fechaFin, string tipoVenta, string periodo, string nombreEmpleado)
        {
            Console.WriteLine($"Nombre del empleado: {nombreEmpleado}");

            IQueryable<Factura> query = _context.Factura.AsQueryable();

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(v => v.fecha >= fechaInicio.Value && v.fecha <= fechaFin.Value);
            }
            else if (year.HasValue)
            {
                int selectedYear = year.Value;
                query = query.Where(v => v.fecha.Year == selectedYear);
            }

            if (tipoVenta != "total")
            {
                query = query.Where(v => v.tipo_venta == tipoVenta);
            }

            if (!string.IsNullOrEmpty(nombreEmpleado))
            {
                query = query.Join(_context.empleados,
                                   factura => factura.empleado_id,
                                   empleado => empleado.id,
                                   (factura, empleado) => new { factura, empleado })
                             .Where(fe => fe.empleado.nombre.Contains(nombreEmpleado))
                             .Select(fe => fe.factura);
            }

            var ventas = new List<object>();
            if (periodo == "mensual")
            {
                var ventasMensuales = query
                    .GroupBy(f => new { Año = f.fecha.Year, Mes = f.fecha.Month })
                    .Select(g => new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + new DateTime(g.Key.Año, g.Key.Mes, 1).ToString("MMM"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    })
                    .ToList();
                ventas.AddRange(ventasMensuales);
            }
            else if (periodo == "semanal")
            {
                var ventasSemanales = query
                    .GroupBy(f => new { Año = f.fecha.Year, Semana = restauranteContext.GetIsoWeek(f.fecha) })
                    .Select(g => new
                    {
                        Periodo = g.Key.Año.ToString() + "-Semana " + g.Key.Semana,
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    })
                    .ToList();
                ventas.AddRange(ventasSemanales);
            }
            else if (periodo == "diario")
            {
                var ventasDiarias = query
                    .GroupBy(f => new { Año = f.fecha.Year, Mes = f.fecha.Month, Dia = f.fecha.Day })
                    .Select(g => new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + g.Key.Mes.ToString("D2") + "-" + g.Key.Dia.ToString("D2"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    })
                    .ToList();
                ventas.AddRange(ventasDiarias);
            }

            var cantidadVentas = query.Count();
            var totalVendido = query.Sum(v => v.total);

            Console.WriteLine($"Cantidad de ventas: {cantidadVentas}, Total vendido: {totalVendido}");

            return Json(new { ventas, cantidadVentas, totalVendido });
        }

        [HttpGet]
        public IActionResult DescargarPdf(int? year, DateTime? fechaInicio, DateTime? fechaFin, string tipoVenta, string periodo, string nombreEmpleado)
        {
            // Obtener los datos filtrados según los parámetros
            var datosFiltrados = ObtenerDatosFiltrados(year, fechaInicio, fechaFin, tipoVenta, periodo, nombreEmpleado);

            // Generar el contenido HTML para el PDF
            var htmlContent = GenerarHtmlParaPdf(datosFiltrados);

            // Configurar el documento PDF
            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = htmlContent,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            try
            {
                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", "ReporteVentas.pdf");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        private string GenerarHtmlParaPdf(dynamic datosFiltrados)
        {
            var ventas = datosFiltrados.ventas;
            var cantidadVentas = datosFiltrados.cantidadVentas;
            var totalVendido = datosFiltrados.totalVendido;

            var html = new StringBuilder();
            html.Append("<html>");
            html.Append("<head>");
            html.Append("<style>");
            html.Append("table { width: 100%; border-collapse: collapse; }");
            html.Append("th, td { border: 1px solid black; padding: 8px; text-align: left; }");
            html.Append("th { background-color: #f2f2f2; }");
            html.Append("</style>");
            html.Append("</head>");
            html.Append("<body>");
            html.Append("<h1>Reporte de Ventas</h1>");
            html.Append($"<p><strong>Cantidad de Ventas:</strong> {cantidadVentas}</p>");
            html.Append($"<p><strong>Total Vendido:</strong> ${totalVendido:F2}</p>");
            html.Append("<table>");
            html.Append("<tr><th>Periodo</th><th>Total Vendido</th><th>Cantidad de Ventas</th></tr>");

            foreach (var venta in ventas)
            {
                html.Append("<tr>");
                html.Append($"<td>{venta.Periodo}</td>");
                html.Append($"<td>${venta.TotalVendido:F2}</td>");
                html.Append($"<td>{venta.CantidadVentas}</td>");
                html.Append("</tr>");
            }

            html.Append("</table>");
            html.Append("</body>");
            html.Append("</html>");

            return html.ToString();
        }

        private dynamic ObtenerDatosFiltrados(int? year, DateTime? fechaInicio, DateTime? fechaFin, string tipoVenta, string periodo, string nombreEmpleado)
        {
            IQueryable<Factura> query = _context.Factura.AsQueryable();

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(v => v.fecha >= fechaInicio.Value && v.fecha <= fechaFin.Value);
            }
            else
            {
                int selectedYear = year ?? DateTime.Now.Year;
                query = query.Where(v => v.fecha.Year == selectedYear);
            }

            if (tipoVenta != "total")
            {
                query = query.Where(v => v.tipo_venta == tipoVenta);
            }

            if (!string.IsNullOrEmpty(nombreEmpleado))
            {
                query = query.Join(_context.empleados,
                                   factura => factura.empleado_id,
                                   empleado => empleado.id,
                                   (factura, empleado) => new { factura, empleado })
                             .Where(fe => fe.empleado.nombre.Contains(nombreEmpleado))
                             .Select(fe => fe.factura);
            }

            var ventas = new List<object>();
            if (periodo == "mensual")
            {
                var ventasMensuales = query
                    .GroupBy(f => new { Año = f.fecha.Year, Mes = f.fecha.Month })
                    .Select(g => new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + new DateTime(g.Key.Año, g.Key.Mes, 1).ToString("MMM"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    })
                    .ToList();
                ventas.AddRange(ventasMensuales);
            }
            else if (periodo == "semanal")
            {
                var ventasSemanales = query
                    .GroupBy(f => new { Año = f.fecha.Year, Semana = restauranteContext.GetIsoWeek(f.fecha) })
                    .Select(g => new
                    {
                        Periodo = g.Key.Año.ToString() + "-Semana " + g.Key.Semana,
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    })
                    .ToList();
                ventas.AddRange(ventasSemanales);
            }
            else if (periodo == "diario")
            {
                var ventasDiarias = query
                    .GroupBy(f => new { Año = f.fecha.Year, Mes = f.fecha.Month, Dia = f.fecha.Day })
                    .Select(g => new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + g.Key.Mes.ToString("D2") + "-" + g.Key.Dia.ToString("D2"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    })
                    .ToList();
                ventas.AddRange(ventasDiarias);
            }

            var cantidadVentas = query.Count();
            var totalVendido = query.Sum(v => v.total);

            return new { ventas, cantidadVentas, totalVendido };
        }
    }
}
