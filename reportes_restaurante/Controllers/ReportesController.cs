using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using reportes_restaurante.Models;
using System.Data;
using System.Text;
using System.Globalization;



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
                var ventasMensuales = (
                    from f in query
                    group f by new { Año = f.fecha.Year, Mes = f.fecha.Month } into g
                    select new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + new DateTime(g.Key.Año, g.Key.Mes, 1).ToString("MMM"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    }
                ).ToList();

                ventas.AddRange(ventasMensuales);
            }
            else if (periodo == "semanal")
            {
                var ventasSemanales = (
                    from f in query
                    group f by new { Año = f.fecha.Year, Semana = restauranteContext.GetIsoWeek(f.fecha) } into g
                    select new
                    {
                        Periodo = g.Key.Año.ToString() + "-Semana " + g.Key.Semana,
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    }
                ).ToList();

                ventas.AddRange(ventasSemanales);
            }
            else if (periodo == "diario")
            {
                var ventasDiarias = (
                    from f in query
                    group f by new { Año = f.fecha.Year, Mes = f.fecha.Month, Dia = f.fecha.Day } into g
                    select new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + g.Key.Mes.ToString("D2") + "-" + g.Key.Dia.ToString("D2"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    }
                ).ToList();

                ventas.AddRange(ventasDiarias);
            }

            var cantidadVentas = query.Count();
            var totalVendido = query.Sum(v => v.total);

            return Json(new { ventas, cantidadVentas, totalVendido });
        }

        [HttpGet]
        public IActionResult DescargarPdf(int? year, DateTime? fechaInicio, DateTime? fechaFin, string tipoVenta, string periodo, string nombreEmpleado)
        {
            // Obtener los datos filtrados según los parámetros
            var datosFiltrados = ObtenerDatosFiltrados(year, fechaInicio, fechaFin, tipoVenta, periodo, nombreEmpleado);

            // Determinar la vista a usar
            string viewName = string.IsNullOrEmpty(nombreEmpleado) ? "ReporteVentasPdf" : "ReporteVentasEmpleadoPdf";

            // Generar el contenido HTML para el PDF
            var htmlContent = RenderViewToString(viewName, datosFiltrados);

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
            var htmlContent = RenderViewToString("ReporteVentasPdf", datosFiltrados);
            return htmlContent;
        }
        private string RenderViewToString(string viewName, object model)
        {
            //Renderizar manualmente esto para el pdf pa
            var motorVista = HttpContext.RequestServices.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
            var proveedordatosTem = HttpContext.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider;
            var contextAccion = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);

            using (var sw = new StringWriter())
            {
                var viewResult = motorVista.FindView(contextAccion, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"View {viewName} not found");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    contextAccion,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(contextAccion.HttpContext, proveedordatosTem),
                    sw,
                    new HtmlHelperOptions()
                );

                viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                return sw.GetStringBuilder().ToString();
            }
        }
        private dynamic ObtenerDatosFiltrados(int? year, DateTime? fechaInicio, DateTime? fechaFin, string tipoVenta, string periodo, string nombreEmpleado)
        {
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
                var ventasMensuales = (
                    from f in query
                    group f by new { Año = f.fecha.Year, Mes = f.fecha.Month } into g
                    select new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + new DateTime(g.Key.Año, g.Key.Mes, 1).ToString("MMM"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    }
                ).ToList();
                ventas.AddRange(ventasMensuales);
            }
            else if (periodo == "semanal")
            {
                var ventasSemanales = (
                    from f in query
                    group f by new { Año = f.fecha.Year, Semana = restauranteContext.GetIsoWeek(f.fecha) } into g
                    select new
                    {
                        Periodo = g.Key.Año.ToString() + "-Semana " + g.Key.Semana,
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    }
                ).ToList();
                ventas.AddRange(ventasSemanales);
            }
            else if (periodo == "diario")
            {
                var ventasDiarias = (
                    from f in query
                    group f by new { Año = f.fecha.Year, Mes = f.fecha.Month, Dia = f.fecha.Day } into g
                    select new
                    {
                        Periodo = g.Key.Año.ToString() + "-" + g.Key.Mes.ToString("D2") + "-" + g.Key.Dia.ToString("D2"),
                        TotalVendido = g.Sum(v => v.total),
                        CantidadVentas = g.Count()
                    }
                ).ToList();
                ventas.AddRange(ventasDiarias);
            }

            var cantidadVentas = query.Count();
            var totalVendido = query.Sum(v => v.total);

            return new { ventas, cantidadVentas, totalVendido, tipoVenta, nombreEmpleado };
        }

        ////////////////////////////////////////////////////////////////////////////////////////////Parte rafa
        ////////////////////////////////////////////////////////////Métodos para el reporte diario
[HttpGet]
public IActionResult ObtenerVentasPorDia(DateTime fecha)
        {
            try
            {

                // Consulta principal optimizada
                var ventasDelDia = _context.Factura
                    .Where(f => f.fecha.Date == fecha.Date)
                    .AsEnumerable()
                    .Select(f => new
                    {
                        Factura = f,
                        Detalles = _context.Detalle_Factura
                            .Where(df => df.factura_id == f.factura_id)
                            .Join(_context.Detalle_Pedido,
                                df => df.detalle_pedido_id,
                                dp => dp.id_detalle_pedido,
                                (df, dp) => new
                                {
                                    DetallePedido = dp,
                                    NombreItem = dp.tipo_item == "Plato" ?
                                        _context.platos
                                            .Where(p => p.id == dp.item_id)
                                            .Select(p => p.nombre)
                                            .FirstOrDefault() :
                                        _context.combos
                                            .Where(c => c.id == dp.item_id)
                                            .Select(c => c.nombre)
                                            .FirstOrDefault()
                                })
                            .ToList(),
                        Empleado = _context.empleados
                            .Where(e => e.id == f.empleado_id)
                            .Select(e => new { e.nombre, e.apellido })
                            .FirstOrDefault(),
                        Mesa = f.tipo_venta == "LOCAL" ?
                            _context.Pedido_Local
                                .Where(p => p.id_pedido == f.id_pedido)
                                .Join(_context.mesas,
                                    p => p.id_mesa,
                                    m => m.id,
                                    (p, m) => (int?)m.numero)
                                .FirstOrDefault() : (int?)null
                    })
                    .ToList();

                if (!ventasDelDia.Any())
                {
                    return Json(new { error = "No hay ventas para la fecha seleccionada" });
                }

                return Json(new
                {
                    ventas = ventasDelDia.Select(v => new
                    {
                        v.Factura.factura_id,
                        v.Factura.codigo_factura,
                        v.Factura.cliente_nombre,
                        v.Factura.fecha,
                        v.Factura.total,
                        v.Factura.tipo_venta,
                        Detalles = v.Detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            d.DetallePedido.cantidad,
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            PrecioUnitario = d.DetallePedido.subtotal / d.DetallePedido.cantidad
                        }),
                        Empleado = v.Empleado,
                        v.Mesa
                    }),
                    totalDia = ventasDelDia.Sum(v => v.Factura.total),
                    fechaReporte = fecha.ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener las ventas: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DescargarPdfVentasDia(DateTime fecha)
        {
            try
            {
                // 1. Obtener datos (igual a tu código original)
                var facturas = _context.Factura
                    .Where(f => f.fecha.Date == fecha.Date)
                    .ToList();

                var ventasDelDia = new List<dynamic>();

                foreach (var f in facturas)
                {
                    var detalles = _context.Detalle_Factura
                        .Where(df => df.factura_id == f.factura_id)
                        .Join(_context.Detalle_Pedido,
                            df => df.detalle_pedido_id,
                            dp => dp.id_detalle_pedido,
                            (df, dp) => new
                            {
                                DetallePedido = dp,
                                NombreItem = dp.tipo_item == "Plato"
                                    ? _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre
                                    : _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre
                            })
                        .ToList();

                    var empleado = _context.empleados
                        .AsNoTracking()
                        .FirstOrDefault(e => e.id == f.empleado_id);

                    int? mesa = null;
                    if (f.tipo_venta == "LOCAL")
                    {
                        mesa = _context.Pedido_Local
                            .Where(p => p.id_pedido == f.id_pedido)
                            .Join(_context.mesas,
                                p => p.id_mesa,
                                m => m.id,
                                (p, m) => (int?)m.numero)
                            .FirstOrDefault();
                    }

                    ventasDelDia.Add(new
                    {
                        Factura = f,
                        Detalles = detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            d.DetallePedido.cantidad,
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            PrecioUnitario = d.DetallePedido.subtotal / d.DetallePedido.cantidad
                        }),
                        Empleado = empleado != null ? new { empleado.nombre, empleado.apellido } : null,
                        Mesa = mesa
                    });
                }

                if (!ventasDelDia.Any())
                {
                    return Content("No hay ventas para esta fecha");
                }

                // 2. Calcular total del día
                decimal totalDia = ventasDelDia.Sum(v => (decimal)v.Factura.total);

                // 3. Preparar modelo para la vista
                var model = new
                {
                    Ventas = ventasDelDia,
                    Fecha = fecha,
                    TotalDia = totalDia,
                    FechaReporte = DateTime.Now.ToString("dd/MM/yyyy")
                };

                // 4. Nombre correcto de la vista: "ReportesVentaDiaLocal"

                string viewName = "ReporteVentaDiaLocal";
                string htmlContent = RenderViewToString(viewName, model);

                // 5. Generar PDF
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
                };

                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", $"VentasDia_{fecha:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el PDF: {ex.Message}");
            }
        }

        private string ObtenerNombreItem(Detalle_Pedido detalle)
        {
            if (detalle.tipo_item == "Plato")
            {
                var plato = _context.platos.FirstOrDefault(p => p.id == detalle.item_id);
                return plato?.nombre ?? "Plato no encontrado";
            }
            else if (detalle.tipo_item == "Combo")
            {
                var combo = _context.combos.FirstOrDefault(c => c.id == detalle.item_id);
                return combo?.nombre ?? "Combo no encontrado";
            }
            return "Ítem desconocido";
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////Metodos para el reporte por mes

        [HttpGet]
        public IActionResult DescargarPdfVentasMes(int año, int mes)
        {
            try
            {
                // Validar parámetros
                if (año < 2000 || año > DateTime.Now.Year + 1 || mes < 1 || mes > 12)
                {
                    return Content("Parámetros de fecha no válidos");
                }

                // Obtener el primer y último día del mes
                var primerDia = new DateTime(año, mes, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

                // Primero obtener todas las facturas del mes
                var facturas = _context.Factura
                    .Where(f => f.fecha.Date >= primerDia && f.fecha.Date <= ultimoDia)
                    .OrderBy(f => f.fecha)
                    .ToList();

                // Luego obtener los detalles para cada factura
                var ventasPorDia = new List<dynamic>();

                foreach (var f in facturas)
                {
                    // Obtener detalles de la factura
                    var detalles = _context.Detalle_Factura
                        .Where(df => df.factura_id == f.factura_id)
                        .Join(_context.Detalle_Pedido,
                            df => df.detalle_pedido_id,
                            dp => dp.id_detalle_pedido,
                            (df, dp) => new
                            {
                                DetallePedido = dp,
                                NombreItem = dp.tipo_item == "Plato" ?
                                    _context.platos
                                        .Where(p => p.id == dp.item_id)
                                        .Select(p => p.nombre)
                                        .FirstOrDefault() :
                                    _context.combos
                                        .Where(c => c.id == dp.item_id)
                                        .Select(c => c.nombre)
                                        .FirstOrDefault()
                            })
                        .ToList();

                    // Obtener empleado
                    var empleado = _context.empleados
                        .AsNoTracking()
                        .FirstOrDefault(e => e.id == f.empleado_id);

                    ventasPorDia.Add(new
                    {
                        Factura = f,
                        Detalles = detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            d.DetallePedido.cantidad,
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            PrecioUnitario = d.DetallePedido.subtotal / d.DetallePedido.cantidad
                        }),
                        Empleado = empleado
                    });
                }

                // Agrupar por día para el reporte
                var ventasAgrupadas = ventasPorDia
    .GroupBy(v => v.Factura.fecha.Date)
    .Select(g => new
    {
        Fecha = g.Key,
        Ventas = g.ToList(),
        TotalDia = g.Sum(v => (decimal)v.Factura.total) // Conversión explícita
    })
    .OrderBy(x => x.Fecha)
    .ToList<dynamic>();

                var htmlContent = GenerarHtmlPdfVentasMes(ventasAgrupadas, año, mes);

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
                };

                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", $"VentasMes_{año}_{mes:00}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el PDF: {ex.Message}");
            }
        }

        private string GenerarHtmlPdfVentasMes(List<dynamic> ventasPorDia, int año, int mes)
        {
            var nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mes);
            var html = new StringBuilder();

            html.Append($@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            padding: 20px;
            color: #333;
        }}
        h1, h2 {{
            color: #af1717;
            border-bottom: 2px solid #af1717;
            padding-bottom: 5px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
        }}
        th {{
            background-color: #af1717;
            color: white;
            padding: 8px;
            text-align: left;
        }}
        td {{
            padding: 8px;
            border-bottom: 1px solid #ddd;
        }}
        .total-cuenta {{
            text-align: right;
            font-weight: bold;
            margin: 10px 0;
            padding: 5px;
            background-color: #f5f5f5;
        }}
        .separador {{
            border-top: 2px dashed #af1717;
            margin: 20px 0;
        }}
        .encabezado-reporte {{
            text-align: center;
            margin-bottom: 30px;
        }}
    </style>
</head>
<body>
    <div class='encabezado-reporte'>
        <h1>Reporte del mes de {nombreMes}</h1>
    </div>");

            foreach (var dia in ventasPorDia)
            {
                html.Append($@"
<h2>Día {dia.Fecha.Day}</h2>");

                // Agrupar ventas por factura/cliente - VERSIÓN CORREGIDA
                var ventasPorCliente = ((IEnumerable<dynamic>)dia.Ventas)
                    .GroupBy(v => (string)v.Factura.cliente_nombre)
                    .ToList();

                foreach (var grupoCliente in ventasPorCliente)
                {
                    html.Append(@"
<table>
    <thead>
        <tr>
            <th>Cliente</th>
            <th>Platillo</th>
            <th>Tipo</th>
            <th>Cantidad</th>
            <th>Precio</th>
        </tr>
    </thead>
    <tbody>");

                    var primerItem = true;
                    decimal totalCliente = 0;

                    foreach (var venta in grupoCliente)
                    {
                        foreach (var detalle in venta.Detalles)
                        {
                            html.Append($@"
        <tr>
            <td>{(primerItem ? grupoCliente.Key : "")}</td>
            <td>{(string)detalle.NombreItem}</td>
            <td>{(string)detalle.tipo_item}</td>
            <td>{(int)detalle.cantidad}</td>
            <td>{((decimal)detalle.PrecioUnitario).ToString("N2")}</td>
        </tr>");

                            totalCliente += (decimal)detalle.subtotal;
                            primerItem = false;
                        }
                    }

                    html.Append($@"
    </tbody>
</table>
<div class='total-cuenta'>Total cuenta: {totalCliente.ToString("N2")}</div>
<div class='separador'></div>");
                }
            }

            // Total mensual
            decimal totalMes = ventasPorDia.Sum(d => (decimal)d.TotalDia);
            html.Append($@"
    <div style='margin-top: 30px; text-align: right; font-weight: bold; font-size: 1.2em;'>
        TOTAL DEL MES: {totalMes.ToString("N2")}
    </div>
</body>
</html>");

            return html.ToString();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////Metodos para las reporte de rango
        [HttpGet]
        public IActionResult DescargarPdfPorRango(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                if (fechaInicio > fechaFin)
                {
                    return Content("La fecha de inicio no puede ser mayor a la fecha final");
                }

                // Consulta optimizada para obtener todos los datos necesarios
                var ventas = _context.Factura
                    .Where(f => f.fecha.Date >= fechaInicio.Date && f.fecha.Date <= fechaFin.Date)
                    .OrderBy(f => f.fecha)
                    .Select(f => new
                    {
                        Factura = f,
                        Detalles = _context.Detalle_Factura
                            .Where(df => df.factura_id == f.factura_id)
                            .Join(_context.Detalle_Pedido,
                                df => df.detalle_pedido_id,
                                dp => dp.id_detalle_pedido,
                                (df, dp) => new
                                {
                                    TipoItem = dp.tipo_item,
                                    ItemId = dp.item_id,
                                    Cantidad = dp.cantidad,
                                    Subtotal = dp.subtotal,
                                    Comentarios = dp.comentarios,
                                    NombreItem = dp.tipo_item == "Plato" ?
                                        _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre :
                                        _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre,
                                    PrecioUnitario = dp.subtotal / dp.cantidad
                                })
                            .ToList()
                    })
                    .ToList();

                // Agrupar por día
                var ventasPorDia = ventas
                    .GroupBy(v => v.Factura.fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Ventas = g.ToList(),
                        TotalDia = g.Sum(v => v.Factura.total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                var htmlContent = GenerarHtmlPdfPorRango(ventasPorDia, fechaInicio, fechaFin);

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
                };

                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", $"Ventas_{fechaInicio:yyyyMMdd}_a_{fechaFin:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el PDF: {ex.Message}");
            }
        }

        private string GenerarHtmlPdfPorRango(IEnumerable<dynamic> ventasPorDia, DateTime fechaInicio, DateTime fechaFin)
        {
            var culture = new CultureInfo("es-ES");
            var html = new StringBuilder();

            html.Append($@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            padding: 20px;
            color: #333;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
            border-bottom: 2px solid #af1717;
            padding-bottom: 10px;
        }}
        h1 {{
            color: #af1717;
            margin: 0;
        }}
        h2 {{
            color: #333;
            margin: 15px 0 5px 0;
        }}
        .report-info {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .rango-fechas {{
            text-align: center;
            margin-bottom: 20px;
            font-weight: bold;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
        }}
        th {{
            background-color: #af1717;
            color: white;
            padding: 8px;
            text-align: left;
        }}
        td {{
            padding: 8px;
            border-bottom: 1px solid #ddd;
        }}
        .total-section {{
            text-align: right;
            margin-top: 20px;
            padding-top: 10px;
            border-top: 2px solid #af1717;
        }}
        .total-general {{
            font-weight: bold;
            font-size: 1.1em;
        }}
        .tipo-venta {{
            font-weight: bold;
        }}
        .tipo-local {{
            color: #1e88e5;
        }}
        .tipo-online {{
            color: #43a047;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            font-size: 0.9em;
            color: #666;
        }}
        .resumen-ventas {{
            margin: 20px 0;
            padding: 10px;
            background-color: #f5f5f5;
            border-radius: 5px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Restaurante Foodie</h1>
        <h2>Reporte de Ventas por Rango</h2>
    </div>
    
    <div class='report-info'>
        <p>Reporte generado el {DateTime.Now.ToString("dd/MM/yyyy", culture)}</p>
    </div>
    
    <div class='rango-fechas'>
        <p>Rango: {fechaInicio.ToString("dd/MM/yyyy", culture)} - {fechaFin.ToString("dd/MM/yyyy", culture)}</p>
    </div>");

            decimal totalGeneral = 0;
            decimal totalLocal = 0;
            decimal totalOnline = 0;

            foreach (var dia in ventasPorDia)
            {
                html.Append($@"
    <h2>Día: {((DateTime)dia.Fecha).ToString("dddd dd 'de' MMMM", culture)}</h2>
    <table>
        <thead>
            <tr>
                <th>Factura</th>
                <th>Cliente</th>
                <th>Tipo Venta</th>
                <th>Producto</th>
                <th>Cantidad</th>
                <th>P. Unitario</th>
                <th>Subtotal</th>
            </tr>
        </thead>
        <tbody>");

                decimal totalDia = 0;
                decimal diaLocal = 0;
                decimal diaOnline = 0;

                foreach (var venta in (IEnumerable<dynamic>)dia.Ventas)
                {
                    var tipoVenta = venta.Factura.tipo_venta;
                    var claseTipoVenta = tipoVenta == "LOCAL" ? "tipo-local" : "tipo-online";
                    var textoTipoVenta = tipoVenta == "LOCAL" ? "Local" : "En Línea";

                    var esPrimerItem = true;
                    decimal subtotalFactura = 0;

                    foreach (var detalle in venta.Detalles)
                    {
                        html.Append($@"
            <tr>
                <td>{(esPrimerItem ? venta.Factura.codigo_factura : "")}</td>
                <td>{(esPrimerItem ? venta.Factura.cliente_nombre : "")}</td>
                <td class='tipo-venta {claseTipoVenta}'>{(esPrimerItem ? textoTipoVenta : "")}</td>
                <td>{detalle.NombreItem}</td>
                <td>{detalle.Cantidad}</td>
                <td>{detalle.PrecioUnitario.ToString("C", culture)}</td>
                <td>{detalle.Subtotal.ToString("C", culture)}</td>
            </tr>");

                        subtotalFactura += detalle.Subtotal;
                        esPrimerItem = false;
                    }

                    // Acumular totales
                    if (tipoVenta == "LOCAL")
                    {
                        diaLocal += subtotalFactura;
                    }
                    else
                    {
                        diaOnline += subtotalFactura;
                    }

                    totalDia += subtotalFactura;
                }

                totalLocal += diaLocal;
                totalOnline += diaOnline;
                totalGeneral += totalDia;

                html.Append($@"
        </tbody>
    </table>
    <div style='text-align: right; margin: 10px 0 20px 0;'>
        <p><strong>Total día:</strong> {totalDia.ToString("C", culture)}</p>
        <p>Total Local: {diaLocal.ToString("C", culture)}</p>
        <p>Total En Línea: {diaOnline.ToString("C", culture)}</p>
    </div>");
            }

            // Resumen general
            html.Append($@"
    <div class='resumen-ventas'>
        <h3>Resumen General del Rango</h3>
        <p><strong>Total Ventas Locales:</strong> {totalLocal.ToString("C", culture)}</p>
        <p><strong>Total Ventas en Línea:</strong> {totalOnline.ToString("C", culture)}</p>
    </div>

    <div class='total-section'>
        <div class='total-general'>TOTAL GENERAL: {totalGeneral.ToString("C", culture)}</div>
    </div>

    <div class='footer'>
        Este reporte ha sido generado automáticamente por el sistema del Restaurante Dulce Sabor
    </div>
</body>
</html>");

            return html.ToString();
        }
        public IActionResult ReportesPdf()
        {
            return View();
        }

        //////////////////////////////////////////////////////reporte diario de ventas en linea
        [HttpGet]
        public IActionResult ObtenerVentasEnLineaPorDia(DateTime fecha)
        {
            try
            {
                var ventasDelDia = _context.Factura
                    .Where(f => f.fecha.Date == fecha.Date && f.tipo_venta == "ONLINE")
                    .AsEnumerable()
                    .Select(f => new
                    {
                        Factura = f,
                        Detalles = _context.Detalle_Factura
                            .Where(df => df.factura_id == f.factura_id)
                            .Join(_context.Detalle_Pedido,
                                df => df.detalle_pedido_id,
                                dp => dp.id_detalle_pedido,
                                (df, dp) => new
                                {
                                    DetallePedido = dp,
                                    NombreItem = dp.tipo_item == "Plato" ?
                                        _context.platos
                                            .Where(p => p.id == dp.item_id)
                                            .Select(p => p.nombre)
                                            .FirstOrDefault() :
                                        _context.combos
                                            .Where(c => c.id == dp.item_id)
                                            .Select(c => c.nombre)
                                            .FirstOrDefault()
                                })
                            .ToList(),
                        Cliente = f.cliente_nombre
                    })
                    .ToList();

                if (!ventasDelDia.Any())
                {
                    return Json(new { error = "No hay ventas en línea para la fecha seleccionada" });
                }

                return Json(new
                {
                    ventas = ventasDelDia.Select(v => new
                    {
                        v.Factura.factura_id,
                        v.Factura.codigo_factura,
                        v.Factura.cliente_nombre,
                        v.Factura.fecha,
                        v.Factura.total,
                        v.Factura.tipo_venta,
                        Detalles = v.Detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            d.DetallePedido.cantidad,
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            PrecioUnitario = d.DetallePedido.subtotal / d.DetallePedido.cantidad
                        })
                    }),
                    totalDia = ventasDelDia.Sum(v => v.Factura.total),
                    fechaReporte = fecha.ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener las ventas: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DescargarPdfVentasEnLineaDia(DateTime fecha)
        {
            try
            {
                var facturas = _context.Factura
                    .Where(f => f.fecha.Date == fecha.Date && f.tipo_venta == "ONLINE")
                    .ToList();

                var ventasDelDia = new List<dynamic>();

                foreach (var f in facturas)
                {
                    var detalles = _context.Detalle_Factura
                        .Where(df => df.factura_id == f.factura_id)
                        .Join(_context.Detalle_Pedido,
                            df => df.detalle_pedido_id,
                            dp => dp.id_detalle_pedido,
                            (df, dp) => new
                            {
                                DetallePedido = dp,
                                NombreItem = dp.tipo_item == "Plato" ?
                                    _context.platos
                                        .Where(p => p.id == dp.item_id)
                                        .Select(p => p.nombre)
                                        .FirstOrDefault() :
                                    _context.combos
                                        .Where(c => c.id == dp.item_id)
                                        .Select(c => c.nombre)
                                        .FirstOrDefault()
                            })
                        .ToList();

                    ventasDelDia.Add(new
                    {
                        Factura = f,
                        Detalles = detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            d.DetallePedido.cantidad,
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            PrecioUnitario = d.DetallePedido.subtotal / d.DetallePedido.cantidad
                        }),
                        Cliente = f.cliente_nombre
                    });
                }

                var htmlContent = GenerarHtmlPdfVentasEnLineaDia(ventasDelDia, fecha);

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
                };

                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", $"VentasEnLineaDia_{fecha:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el PDF: {ex.Message}");
            }
        }

        private string GenerarHtmlPdfVentasEnLineaDia(List<dynamic> ventas, DateTime fecha)
        {
            var html = new StringBuilder();
            html.Append($@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            padding: 20px;
            color: #333;
        }}
        .header {{ 
            text-align: center; 
            margin-bottom: 30px;
            border-bottom: 2px solid #003653;
            padding-bottom: 10px;
        }}
        h1, h2 {{ 
            color: #003653;
            margin: 5px 0;
        }}
        .report-info {{ 
            text-align: center;
            margin-bottom: 20px;
            font-size: 0.9em;
            color: #666;
        }}
        .venta {{ 
            margin-bottom: 30px; 
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 15px;
            background-color: #f9f9f9;
        }}
        .cliente-header {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
            padding-bottom: 5px;
            border-bottom: 1px solid #eee;
        }}
        table {{
            width: 100%; 
            border-collapse: collapse; 
            margin: 10px 0;
            font-size: 0.9em;
        }}
        th {{
            background-color: #003653;
            color: white; 
            padding: 8px;
            text-align: left;
        }}
        td {{
            padding: 8px;
            border-bottom: 1px solid #ddd;
        }}
        .total-venta {{
            text-align: right; 
            font-weight: bold; 
            margin-top: 10px;
            padding-top: 5px;
            border-top: 1px solid #003653;
        }}
        .total-dia {{
            text-align: right; 
            font-weight: bold; 
            font-size: 1.2em; 
            margin-top: 30px;
            padding: 10px;
            background-color: #003653;
            color: white;
            border-radius: 5px;
        }}
        .comentarios {{
            font-style: italic; 
            color: #666;
            font-size: 0.8em;
            margin-top: 5px;
        }}
        .badge {{
            background-color: #003653;
            color: white;
            padding: 3px 8px;
            border-radius: 3px;
            font-size: 0.8em;
        }}
        .footer {{
            text-align: center;
            margin-top: 40px;
            font-size: 0.8em;
            color: #999;
            border-top: 1px solid #eee;
            padding-top: 10px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Restaurante Foodie</h1>
        <h2>Ventas en línea por día</h2>
    </div>
    
    <div class='report-info'>
        <p>Reporte generado el {DateTime.Now.ToString("dd/MM/yyyy")} | Fecha del reporte: {fecha.ToString("dd/MM/yyyy")}</p>
    </div>");

            decimal totalDia = 0;

            foreach (var venta in ventas)
            {
                var factura = venta.Factura as Factura;
                totalDia += factura.total;

                html.Append($@"
    <div class='venta'>
        <div class='cliente-header'>
            <div><strong>Cliente:</strong> {factura.cliente_nombre}</div>
            <div><span class='badge'>EN LÍNEA</span></div>
        </div>
        
        <table>
            <thead>
                <tr>
                    <th>Platillo</th>
                    <th>Tipo</th>
                    <th>Cantidad</th>
                    <th>Precio Unitario</th>
                    <th>Subtotal</th>
                </tr>
            </thead>
            <tbody>");

                foreach (var detalle in venta.Detalles)
                {
                    html.Append($@"
                <tr>
                    <td>{detalle.NombreItem}</td>
                    <td>{detalle.tipo_item}</td>
                    <td>{detalle.cantidad}</td>
                    <td>${detalle.PrecioUnitario.ToString("N2")}</td>
                    <td>${detalle.subtotal.ToString("N2")}</td>
                </tr>");

                    if (!string.IsNullOrEmpty(detalle.comentarios))
                    {
                        html.Append($@"
                <tr>
                    <td colspan='5' class='comentarios'>
                        <strong>Notas:</strong> {detalle.comentarios}
                    </td>
                </tr>");
                    }
                }

                html.Append($@"
            </tbody>
        </table>
        <div class='total-venta'>TOTAL FACTURA: ${factura.total.ToString("N2")}</div>
    </div>");
            }

            html.Append($@"
    <div class='total-dia'>TOTAL DEL DÍA (EN LÍNEA): ${totalDia.ToString("N2")}</div>
    
    <div class='footer'>
        Este reporte ha sido generado automáticamente por el sistema del Restaurante Foodie
    </div>
</body>
</html>");

            return html.ToString();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////ventas en linea por mes
        [HttpGet]
        public IActionResult DescargarPdfVentasEnLineaMes(int año, int mes)
        {
            try
            {
                if (año < 2000 || año > DateTime.Now.Year + 1 || mes < 1 || mes > 12)
                {
                    return Content("Parámetros de fecha no válidos");
                }

                var primerDia = new DateTime(año, mes, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

                var facturas = _context.Factura
                    .Where(f => f.fecha.Date >= primerDia && f.fecha.Date <= ultimoDia && f.tipo_venta == "ONLINE")
                    .OrderBy(f => f.fecha)
                    .ToList();

                var ventasPorDia = new List<dynamic>();

                foreach (var f in facturas)
                {
                    var detalles = _context.Detalle_Factura
                        .Where(df => df.factura_id == f.factura_id)
                        .Join(_context.Detalle_Pedido,
                            df => df.detalle_pedido_id,
                            dp => dp.id_detalle_pedido,
                            (df, dp) => new
                            {
                                DetallePedido = dp,
                                NombreItem = dp.tipo_item == "Plato" ?
                                    _context.platos
                                        .Where(p => p.id == dp.item_id)
                                        .Select(p => p.nombre)
                                        .FirstOrDefault() :
                                    _context.combos
                                        .Where(c => c.id == dp.item_id)
                                        .Select(c => c.nombre)
                                        .FirstOrDefault()
                            })
                        .ToList();

                    ventasPorDia.Add(new
                    {
                        Factura = f,
                        Detalles = detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            d.DetallePedido.cantidad,
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            PrecioUnitario = d.DetallePedido.subtotal / d.DetallePedido.cantidad
                        }),
                        Cliente = f.cliente_nombre
                    });
                }

                var ventasAgrupadas = ventasPorDia
                    .GroupBy(v => v.Factura.fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Ventas = g.ToList(),
                        TotalDia = g.Sum(v => (decimal)v.Factura.total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList<dynamic>();

                var htmlContent = GenerarHtmlPdfVentasEnLineaMes(ventasAgrupadas, año, mes);

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
                };

                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", $"VentasEnLineaMes_{año}_{mes:00}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el PDF: {ex.Message}");
            }
        }

        private string GenerarHtmlPdfVentasEnLineaMes(List<dynamic> ventasPorDia, int año, int mes)
        {
            var nombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mes);
            var html = new StringBuilder();

            html.Append($@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            padding: 20px;
            color: #333;
        }}
        .header {{ 
            text-align: center; 
            margin-bottom: 20px;
            border-bottom: 2px solid #003653;
            padding-bottom: 10px;
        }}
        h1, h2 {{ 
            color: #003653;
            margin: 5px 0;
        }}
        .report-info {{ 
            text-align: center;
            margin-bottom: 30px;
            font-size: 0.9em;
            color: #666;
        }}
        .dia-section {{
            margin-bottom: 30px;
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 15px;
            background-color: #f9f9f9;
        }}
        .dia-header {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 15px;
            padding-bottom: 5px;
            border-bottom: 1px solid #eee;
        }}
        table {{
            width: 100%; 
            border-collapse: collapse; 
            margin: 15px 0;
            font-size: 0.9em;
        }}
        th {{
            background-color: #003653;
            color: white; 
            padding: 8px;
            text-align: left;
        }}
        td {{
            padding: 8px;
            border-bottom: 1px solid #ddd;
        }}
        .total-cuenta {{
            text-align: right; 
            font-weight: bold; 
            margin: 10px 0;
            padding: 5px;
            background-color: #f5f5f5;
            border-top: 1px solid #003653;
        }}
        .total-dia {{
            text-align: right; 
            font-weight: bold; 
            margin: 15px 0;
            padding: 8px;
            background-color: #e8f5e9;
            border-radius: 5px;
        }}
        .total-mes {{
            text-align: right; 
            font-weight: bold; 
            font-size: 1.2em; 
            margin-top: 30px;
            padding: 10px;
            background-color: #003653;
            color: white;
            border-radius: 5px;
        }}
        .comentarios {{
            font-style: italic; 
            color: #666;
            font-size: 0.8em;
            margin-top: 5px;
        }}
        .badge {{
            background-color: #003653;
            color: white;
            padding: 3px 8px;
            border-radius: 3px;
            font-size: 0.8em;
        }}
        .footer {{
            text-align: center;
            margin-top: 40px;
            font-size: 0.8em;
            color: #999;
            border-top: 1px solid #eee;
            padding-top: 10px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Restaurante Foodie</h1>
        <h2>Ventas en línea - {nombreMes} {año}</h2>
    </div>
    
    <div class='report-info'>
        <p>Reporte generado el {DateTime.Now.ToString("dd/MM/yyyy")}</p>
    </div>");

            decimal totalMes = 0;

            foreach (var dia in ventasPorDia)
            {
                html.Append($@"
    <div class='dia-section'>
        <div class='dia-header'>
            <h3>Día {dia.Fecha.Day}</h3>
            <span class='badge'>EN LÍNEA</span>
        </div>");

                var ventasPorCliente = ((IEnumerable<dynamic>)dia.Ventas)
                    .GroupBy(v => (string)v.Factura.cliente_nombre)
                    .ToList();

                foreach (var grupoCliente in ventasPorCliente)
                {
                    html.Append(@"
        <table>
            <thead>
                <tr>
                    <th>Cliente</th>
                    <th>Platillo</th>
                    <th>Tipo</th>
                    <th>Cantidad</th>
                    <th>Precio Unitario</th>
                    <th>Subtotal</th>
                </tr>
            </thead>
            <tbody>");

                    var primerItem = true;
                    decimal totalCliente = 0;

                    foreach (var venta in grupoCliente)
                    {
                        foreach (var detalle in venta.Detalles)
                        {
                            html.Append($@"
                <tr>
                    <td>{(primerItem ? grupoCliente.Key : "")}</td>
                    <td>{(string)detalle.NombreItem}</td>
                    <td>{(string)detalle.tipo_item}</td>
                    <td>{(int)detalle.cantidad}</td>
                    <td>{((decimal)detalle.PrecioUnitario).ToString("N2")}</td>
                    <td>{((decimal)detalle.subtotal).ToString("N2")}</td>
                </tr>");

                            if (!string.IsNullOrEmpty(detalle.comentarios))
                            {
                                html.Append($@"
                <tr>
                    <td colspan='6' class='comentarios'>
                        <strong>Notas:</strong> {detalle.comentarios}
                    </td>
                </tr>");
                            }

                            totalCliente += (decimal)detalle.subtotal;
                            primerItem = false;
                        }
                    }

                    html.Append($@"
            </tbody>
        </table>
        <div class='total-cuenta'>Total cuenta: {totalCliente.ToString("N2")}</div>");
                }

                html.Append($@"
        <div class='total-dia'>TOTAL DEL DÍA: {((decimal)dia.TotalDia).ToString("N2")}</div>
    </div>");

                totalMes += (decimal)dia.TotalDia;
            }

            html.Append($@"
    <div class='total-mes'>TOTAL DEL MES (EN LÍNEA): {totalMes.ToString("N2")}</div>
    
    <div class='footer'>
        Este reporte ha sido generado automáticamente por el sistema del Foodie
    </div>
</body>
</html>");

            return html.ToString();
        }


        [HttpGet]
        public IActionResult DescargarPdfVentasPorItem(string tipoItem, string periodo, DateTime? fecha, int? mes, int? año, DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                // Validar parámetros
                if (string.IsNullOrEmpty(tipoItem) || (!fecha.HasValue && !mes.HasValue && !fechaInicio.HasValue))
                {
                    return Content("Parámetros no válidos");
                }

                // Determinar el rango de fechas según el periodo seleccionado
                DateTime startDate, endDate;
                string periodoTexto = "";

                switch (periodo.ToLower())
                {
                    case "día":
                        if (!fecha.HasValue) return Content("Fecha no válida");
                        startDate = fecha.Value.Date;
                        endDate = fecha.Value.Date.AddDays(1).AddSeconds(-1);
                        periodoTexto = $"Día: {fecha.Value.ToString("dddd dd 'de' MMMM 'de' yyyy", new CultureInfo("es-ES"))}";
                        break;

                    case "mes":
                        if (!mes.HasValue || !año.HasValue) return Content("Mes o año no válidos");
                        startDate = new DateTime(año.Value, mes.Value, 1);
                        endDate = startDate.AddMonths(1).AddSeconds(-1);
                        periodoTexto = $"Mes: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mes.Value)} {año.Value}";
                        break;

                    case "rango":
                        if (!fechaInicio.HasValue || !fechaFin.HasValue) return Content("Rango de fechas no válido");
                        startDate = fechaInicio.Value.Date;
                        endDate = fechaFin.Value.Date.AddDays(1).AddSeconds(-1);
                        periodoTexto = $"Rango Seleccionado: {fechaInicio.Value.ToString("dd/MM/yyyy")} - {fechaFin.Value.ToString("dd/MM/yyyy")}";
                        break;

                    default:
                        return Content("Tipo de período no válido");
                }

                // Obtener las facturas en el rango de fechas
                var facturas = _context.Factura
                    .Where(f => f.fecha >= startDate && f.fecha <= endDate)
                    .OrderBy(f => f.fecha)
                    .ToList();

                // Obtener los detalles agrupados por item
                var ventasPorItem = facturas
                    .SelectMany(f => _context.Detalle_Factura
                        .Where(df => df.factura_id == f.factura_id)
                        .Join(_context.Detalle_Pedido,
                            df => df.detalle_pedido_id,
                            dp => dp.id_detalle_pedido,
                            (df, dp) => new
                            {
                                DetallePedido = dp,
                                NombreItem = dp.tipo_item == "Plato" ?
                                    _context.platos
                                        .Where(p => p.id == dp.item_id)
                                        .Select(p => p.nombre)
                                        .FirstOrDefault() :
                                    dp.tipo_item == "Combo" ?
                                        _context.combos
                                            .Where(c => c.id == dp.item_id)
                                            .Select(c => c.nombre)
                                            .FirstOrDefault() :
                                    _context.promociones
                                        .Where(pr => pr.id == dp.item_id)
                                        .Select(pr => pr.nombre)
                                        .FirstOrDefault(),
                                TipoItem = dp.tipo_item,
                                Descuento = dp.tipo_item == "Promocion" ?
                                    _context.promociones
                                        .Where(pr => pr.id == dp.item_id)
                                        .Select(pr => pr.descuento)
                                        .FirstOrDefault() : 0,
                                PrecioOriginal = dp.subtotal / (1 - (dp.tipo_item == "Promocion" ?
                                    (_context.promociones
                                        .Where(pr => pr.id == dp.item_id)
                                        .Select(pr => pr.descuento)
                                        .FirstOrDefault() / 100) : 0))
                            }))
                    .Where(x => x.TipoItem.ToLower() == tipoItem.ToLower())
                    .GroupBy(x => new { x.NombreItem, x.PrecioOriginal })
                    .Select(g => new
                    {
                        NombreItem = g.Key.NombreItem,
                        PrecioUnitario = g.Key.PrecioOriginal,
                        CantidadVendida = g.Sum(x => x.DetallePedido.cantidad),
                        Total = g.Sum(x => x.DetallePedido.subtotal),
                        DescuentoPromocion = tipoItem == "Promocion" ? g.First().Descuento : 0
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList<dynamic>();

                // Generar el HTML para el PDF
                var htmlContent = GenerarHtmlPdfVentasPorItem(ventasPorItem, tipoItem, periodoTexto);

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
                };

                var pdfBytes = _converter.Convert(pdf);
                return File(pdfBytes, "application/pdf", $"VentasPor{tipoItem}_{periodo}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"Error al generar el PDF: {ex.Message}");
            }
        }

        private string GenerarHtmlPdfVentasPorItem(List<dynamic> ventasPorItem, string tipoItem, string periodoTexto)
        {
            var html = new StringBuilder();

            html.Append($@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            padding: 20px;
            color: #333;
        }}
        .header {{ 
            text-align: center; 
            margin-bottom: 20px;
            border-bottom: 2px solid #003653;
            padding-bottom: 10px;
        }}
        h1, h2 {{ 
            color: #003653;
            margin: 5px 0;
        }}
        .report-info {{ 
            text-align: center;
            margin-bottom: 30px;
            font-size: 0.9em;
            color: #666;
        }}
        table {{
            width: 100%; 
            border-collapse: collapse; 
            margin: 15px 0;
            font-size: 0.9em;
        }}
        th {{
            background-color: #003653;
            color: white; 
            padding: 8px;
            text-align: left;
        }}
        td {{
            padding: 8px;
            border-bottom: 1px solid #ddd;
        }}
        .total {{
            text-align: right; 
            font-weight: bold; 
            margin: 15px 0;
            padding: 8px;
            background-color: #003653;
            color: white;
            border-radius: 5px;
        }}
        .footer {{
            text-align: center;
            margin-top: 40px;
            font-size: 0.8em;
            color: #999;
            border-top: 1px solid #eee;
            padding-top: 10px;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Restaurante Foodie</h1>
        <h2>REPORTE VENTAS POR {tipoItem.ToUpper()}</h2>
    </div>
    
    <div class='report-info'>
        <p>{periodoTexto}</p>
        <p>Reporte generado el {DateTime.Now.ToString("dd/MM/yyyy")}</p>
    </div>

    <table>
        <thead>
            <tr>
                <th>Nombre del {tipoItem.ToLower()}</th>
                <th>Precio unitario</th>
                <th>Cantidad vendida</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>");

            decimal totalGeneral = 0;

            foreach (var item in ventasPorItem)
            {
                html.Append($@"
            <tr>
                <td>{(string)item.NombreItem}</td>
                <td>{((decimal)item.PrecioUnitario).ToString("C")}</td>
                <td>{(int)item.CantidadVendida}</td>
                <td>{((decimal)item.Total).ToString("C")}</td>
            </tr>");

                totalGeneral += (decimal)item.Total;
            }

            html.Append($@"
        </tbody>
    </table>

    <div class='total'>TOTAL GENERAL: {totalGeneral.ToString("C")}</div>
    
    <div class='footer'>
        Este reporte ha sido generado automáticamente por el sistema del Foodie
    </div>
</body>
</html>");

            return html.ToString();
        }
    }
}
