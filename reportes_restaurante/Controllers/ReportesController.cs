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
        public IActionResult ReportesPdf()
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

                // Obtener facturas del mes
                var facturas = _context.Factura
                    .Where(f => f.fecha.Date >= primerDia && f.fecha.Date <= ultimoDia)
                    .OrderBy(f => f.fecha)
                    .ToList();

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
                                    _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre :
                                    _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre
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
                        Empleado = empleado,
                        Mesa = mesa
                    });
                }

                // Agrupar por día
                var ventasAgrupadas = ventasPorDia
                    .GroupBy(v => v.Factura.fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Ventas = g.ToList(),
                        TotalDia = g.Sum(v => (decimal)v.Factura.total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                if (!ventasAgrupadas.Any())
                {
                    return Content("No hay ventas para este período");
                }

                // Crear modelo completo para la vista
                var model = new
                {
                    VentasPorDia = ventasAgrupadas,
                    Año = año,
                    Mes = mes,
                    NombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mes),
                    TotalMes = ventasAgrupadas.Sum(d => d.TotalDia),
                    FechaReporte = DateTime.Now.ToString("dd/MM/yyyy")
                };

                // Generar PDF
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = RenderViewToString("ReporteVentaMesLocal", model),
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

                // Consulta optimizada con conversiones explícitas
                var facturas = _context.Factura
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
                                    // Conversión explícita para cantidad
                                    cantidad = (int)dp.cantidad,
                                    tipo_item = dp.tipo_item,
                                    item_id = dp.item_id,
                                    subtotal = dp.subtotal,
                                    comentarios = dp.comentarios,
                                    NombreItem = dp.tipo_item == "Plato"
                                        ? _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre
                                        : _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre,
                                    PrecioUnitario = dp.subtotal / (decimal)dp.cantidad // Conversión explícita
                                })
                            .ToList(),
                        Empleado = _context.empleados.FirstOrDefault(e => e.id == f.empleado_id),
                        Mesa = f.tipo_venta == "LOCAL"
                            ? (int?)_context.Pedido_Local
                                .Where(p => p.id_pedido == f.id_pedido)
                                .Join(_context.mesas,
                                    p => p.id_mesa,
                                    m => m.id,
                                    (p, m) => m.numero)
                                .FirstOrDefault()
                            : null
                    })
                    .ToList();

                // Agrupar por día con conversiones correctas
                var ventasAgrupadas = facturas
                    .GroupBy(v => v.Factura.fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Ventas = g.ToList(),
                        TotalDia = g.Sum(v => (decimal)v.Factura.total) // Conversión explícita
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                if (!ventasAgrupadas.Any())
                {
                    return Content("No hay ventas para este período");
                }

                // Preparar modelo con totales correctamente tipados
                var model = new
                {
                    VentasPorDia = ventasAgrupadas,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    FechaReporte = DateTime.Now.ToString("dd/MM/yyyy"),
                    TotalGeneral = (decimal)facturas.Sum(v => v.Factura.total), // Conversión explícita
                    TotalLocal = (decimal)facturas
                        .Where(v => v.Factura.tipo_venta == "LOCAL")
                        .Sum(v => v.Factura.total), // Conversión explícita
                    TotalOnline = (decimal)facturas
                        .Where(v => v.Factura.tipo_venta == "ONLINE")
                        .Sum(v => v.Factura.total) // Conversión explícita
                };

                // Generar PDF
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = RenderViewToString("ReporteVentaPorRango", model),
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
                                    _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre :
                                    _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre,
                                PrecioUnitario = dp.subtotal / (decimal)dp.cantidad // Conversión explícita
                            })
                        .ToList();

                    ventasDelDia.Add(new
                    {
                        Factura = f,
                        Detalles = detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            cantidad = (int)d.DetallePedido.cantidad, // Conversión explícita
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            d.PrecioUnitario
                        }),
                        Cliente = f.cliente_nombre
                    });
                }

                // Calcular total del día
                decimal totalDia = ventasDelDia.Sum(v => (decimal)v.Factura.total); // Conversión explícita

                // Preparar modelo para la vista
                var model = new
                {
                    Ventas = ventasDelDia,
                    Fecha = fecha,
                    TotalDia = totalDia,
                    FechaReporte = DateTime.Now.ToString("dd/MM/yyyy")
                };

                // Generar PDF usando vista Razor
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = RenderViewToString("ReporteVentasEnLinea", model),
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
                                    _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre :
                                    _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre,
                                PrecioUnitario = dp.subtotal / (decimal)dp.cantidad // Conversión explícita
                            })
                        .ToList();

                    ventasPorDia.Add(new
                    {
                        Factura = f,
                        Detalles = detalles.Select(d => new
                        {
                            d.DetallePedido.tipo_item,
                            d.DetallePedido.item_id,
                            cantidad = (int)d.DetallePedido.cantidad, // Conversión explícita
                            d.DetallePedido.subtotal,
                            d.DetallePedido.comentarios,
                            d.NombreItem,
                            d.PrecioUnitario
                        }),
                        Cliente = f.cliente_nombre
                    });
                }

                // Agrupar ventas por día y calcular totales
                var ventasAgrupadas = ventasPorDia
                    .GroupBy(v => v.Factura.fecha.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Ventas = g.ToList(),
                        TotalDia = g.Sum(v => (decimal)v.Factura.total)
                    })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                // Calcular total del mes
                decimal totalMes = ventasAgrupadas.Sum(d => (decimal)d.TotalDia);

                // Preparar modelo para la vista
                var model = new
                {
                    VentasPorDia = ventasAgrupadas,
                    Año = año,
                    Mes = mes,
                    NombreMes = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mes),
                    TotalMes = totalMes,
                    FechaReporte = DateTime.Now.ToString("dd/MM/yyyy")
                };

                // Generar PDF usando vista Razor
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = RenderViewToString("ReporteVentaEnLineaMes", model),
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
                                    _context.platos.FirstOrDefault(p => p.id == dp.item_id).nombre :
                                    dp.tipo_item == "Combo" ?
                                        _context.combos.FirstOrDefault(c => c.id == dp.item_id).nombre :
                                        _context.promociones.FirstOrDefault(pr => pr.id == dp.item_id).nombre,
                                TipoItem = dp.tipo_item,
                                Descuento = dp.tipo_item == "Promocion" ?
                                    _context.promociones.FirstOrDefault(pr => pr.id == dp.item_id).descuento : 0,
                                PrecioOriginal = dp.subtotal / (1 - (dp.tipo_item == "Promocion" ?
                                    (_context.promociones.FirstOrDefault(pr => pr.id == dp.item_id).descuento / 100m) : 0m))
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
                    .ToList();

                // Calcular total general
                decimal totalGeneral = ventasPorItem.Sum(x => (decimal)x.Total);

                // Preparar modelo para la vista
                var model = new
                {
                    VentasPorItem = ventasPorItem,
                    TipoItem = tipoItem,
                    PeriodoTexto = periodoTexto,
                    TotalGeneral = totalGeneral,
                    FechaReporte = DateTime.Now.ToString("dd/MM/yyyy")
                };

                // Generar PDF usando vista Razor
                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = RenderViewToString("ReporteVentasPorItem", model),
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
    }
}
