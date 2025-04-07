using Microsoft.AspNetCore.Mvc;
using reportes_restaurante.Models;


namespace reportes_restaurante.Controllers
{
    public class DashboardController : Controller
    {

        private readonly restauranteContext _context;


        public DashboardController(restauranteContext context)
        {

            _context = context;


        }

        public IActionResult Index()
        {
            var today = DateTime.Today;


            var mesasOcupadas = _context.mesas
                .Where(m => m.disponibilidad == "ocupado")  // Filtrar solo las mesas con disponibilidad 'ocupado'
                .Count();


            var pedidosAbiertos = _context.Pedido_Local
                .Where(p => p.estado == "Abierta" && p.fechaApertura.Date == today)
                .Count();

            
            var pedidosEnProceso = _context.Pedido_Local
                .Where(pl => pl.fechaApertura.Date == today)
                .Join(_context.Detalle_Pedido,
                    pl => pl.id_pedido,    
                    dp => dp.encabezado_id, 
                    (pl, dp) => new { pl, dp })
                .Where(x => x.dp.estado == "En Proceso")
                .Count();

            var pedidosPendientes = _context.Pedido_Local
                .Where(pl => pl.fechaApertura.Date == today)
                .Join(_context.Detalle_Pedido,
                    pl => pl.id_pedido,    
                    dp => dp.encabezado_id, 
                    (pl, dp) => new { pl, dp })
                .Where(x => x.dp.estado == "Pendiente")
                .Count();

         
            var pedidosFinalizados = _context.Pedido_Local
                .Where(pl => pl.fechaApertura.Date == today)
                .Join(_context.Detalle_Pedido,
                    pl => pl.id_pedido,    
                    dp => dp.encabezado_id,
                    (pl, dp) => new { pl, dp })
                .Where(x => x.dp.estado == "Finalizado")
                .Count();


        
            ViewBag.MesasOcupadas = mesasOcupadas;
            ViewBag.PedidosAbiertos = pedidosAbiertos;
            ViewBag.PedidosEnProceso = pedidosEnProceso;
            ViewBag.PedidosPendientes = pedidosPendientes;  
            ViewBag.PedidosFinalizados = pedidosFinalizados;
        


            var ingresos = CalcularIngresos();

         
            ViewBag.IngresosLocales = ingresos.ingresosLocales;
            ViewBag.IngresosEnLinea = ingresos.ingresosEnLinea;
            ViewBag.IngresosTotales = ingresos.ingresosTotales;



            


            return View();


        }

        
        private (decimal ingresosLocales, decimal ingresosEnLinea, decimal ingresosTotales) CalcularIngresos()
        {
            var today = DateTime.Today;  // Obtener la fecha de hoy

            // Consultar ingresos totales locales
            var ingresosLocales = _context.Factura
                .Where(f => f.tipo_venta == "LOCAL" && f.fecha.Date == today)
                .Sum(f => f.total);

            // Consultar ingresos totales en línea
            var ingresosEnLinea = _context.Factura
                .Where(f => f.tipo_venta == "ONLINE" && f.fecha.Date == today)
                .Sum(f => f.total);

            // Consultar ingresos totales ambos
            var ingresosTotales = _context.Factura
                .Where(f => f.fecha.Date == today)
                .Sum(f => f.total);

            return (ingresosLocales, ingresosEnLinea, ingresosTotales);
        }
        

        [HttpGet]
        public IActionResult ObtenerIngresosPorMes(int year, int month)
        {
            var ingresosLocales = _context.Factura
                .Where(f => f.tipo_venta == "LOCAL" && f.fecha.Year == year && f.fecha.Month == month)
                .Sum(f => f.total);

            var ingresosEnLinea = _context.Factura
                .Where(f => f.tipo_venta == "ONLINE" && f.fecha.Year == year && f.fecha.Month == month)
                .Sum(f => f.total);

            var ingresosTotales = ingresosLocales + ingresosEnLinea;

            return Json(new { ingresosLocales, ingresosEnLinea, ingresosTotales });
        }

        [HttpGet]
        public IActionResult ObtenerIngresosPorSemana()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var ingresosLocales = _context.Factura
                .Where(f => f.tipo_venta == "LOCAL" && f.fecha.Date >= startOfWeek && f.fecha.Date <= today)
                .Sum(f => f.total);

            var ingresosEnLinea = _context.Factura
                .Where(f => f.tipo_venta == "ONLINE" && f.fecha.Date >= startOfWeek && f.fecha.Date <= today)
                .Sum(f => f.total);

            var ingresosTotales = ingresosLocales + ingresosEnLinea;

            return Json(new { ingresosLocales, ingresosEnLinea, ingresosTotales });
        }

        [HttpGet]
        public IActionResult ObtenerIngresosPorDia()
        {
            var today = DateTime.Today;

            var ingresosLocales = _context.Factura
                .Where(f => f.tipo_venta == "LOCAL" && f.fecha.Date == today)
                .Sum(f => f.total);

            var ingresosEnLinea = _context.Factura
                .Where(f => f.tipo_venta == "ONLINE" && f.fecha.Date == today)
                .Sum(f => f.total);

            var ingresosTotales = ingresosLocales + ingresosEnLinea;

            return Json(new { ingresosLocales, ingresosEnLinea, ingresosTotales });
        }


        [HttpGet]
        public IActionResult ObtenerIngresosPorRango(DateTime startDate, DateTime endDate)
        {
            var ingresosLocales = _context.Factura
                .Where(f => f.tipo_venta == "LOCAL" && f.fecha.Date >= startDate && f.fecha.Date <= endDate)
                .Sum(f => f.total);

            var ingresosEnLinea = _context.Factura
                .Where(f => f.tipo_venta == "ONLINE" && f.fecha.Date >= startDate && f.fecha.Date <= endDate)
                .Sum(f => f.total);

            var ingresosTotales = ingresosLocales + ingresosEnLinea;

            return Json(new { ingresosLocales, ingresosEnLinea, ingresosTotales });
        }



        [HttpGet]
        public IActionResult ObtenerVentasPorDia(int year, int month)
        {
            // Obtener las ventas del mes y año actuales
            var ventas = _context.Factura
                .Where(f => f.fecha.Year == year && f.fecha.Month == month)  // Filtrar por mes y año
                .GroupBy(f => f.fecha.Day)  // Agrupar por día
                .Select(g => new
                {
                    Fecha = g.Key,  // Día del mes
                    TotalVendido = g.Sum(f => f.total)  // Sumar las ventas por día
                })
                .OrderBy(v => v.Fecha)  // Ordenar por fecha
                .ToList();

            // Verifica si las ventas se están recuperando correctamente
            Console.WriteLine("Ventas obtenidas:");
            foreach (var venta in ventas)
            {
                Console.WriteLine($"Día: {venta.Fecha}, Total Vendido: {venta.TotalVendido}");
            }

            return Json(new { ventas });
        }


        [HttpGet]
        public IActionResult ObtenerVentasPorSemana()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Inicio de la semana (domingo)
            var ventas = _context.Factura
                .Where(f => f.fecha.Date >= startOfWeek && f.fecha.Date <= today)
                .GroupBy(f => f.fecha.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    TotalVendido = g.Sum(f => f.total)
                })
                .OrderBy(v => v.Fecha)
                .ToList();

            return Json(new { ventas });
        }

        [HttpGet]
        public IActionResult ObtenerVentasPorHora()
        {
            var today = DateTime.Today;
            var ventas = _context.Factura
                .Where(f => f.fecha.Date == today)
                .GroupBy(f => f.fecha.Hour)
                .Select(g => new
                {
                    Hora = g.Key,
                    TotalVendido = g.Sum(f => f.total)
                })
                .OrderBy(v => v.Hora)
                .ToList();

            return Json(new { ventas });
        }

        [HttpGet]
        public IActionResult ObtenerVentasPorRango(DateTime startDate, DateTime endDate)
        {
            var ventas = _context.Factura
                .Where(f => f.fecha.Date >= startDate && f.fecha.Date <= endDate)
                .GroupBy(f => f.fecha.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    TotalVendido = g.Sum(f => f.total)
                })
                .OrderBy(v => v.Fecha)
                .ToList();

            return Json(new { ventas });
        }



        [HttpGet]
        public IActionResult ObtenerTopPlatosMensual(int year, int month)
        {
            var topPlatos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Plato" && x.pedido.fechaApertura.Year == year && x.pedido.fechaApertura.Month == month)
                .Join(_context.platos,
                    dp => dp.detalle.item_id,
                    plato => plato.id,
                    (dp, plato) => new { PlatoId = plato.id, Nombre = plato.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.PlatoId, x.Nombre })
                .Select(g => new { g.Key.PlatoId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topPlatos);
        }

        [HttpGet]
        public IActionResult ObtenerTopPlatosSemanal()
        {
            // Calcular el rango semanal
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek); // Esto debería calcular el domingo como inicio de la semana

            var topPlatos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Plato" && x.pedido.fechaApertura.Date >= startOfWeek && x.pedido.fechaApertura.Date <= today) // Filtrar por el rango semanal
                .Join(_context.platos,
                    dp => dp.detalle.item_id,
                    plato => plato.id,
                    (dp, plato) => new { PlatoId = plato.id, Nombre = plato.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.PlatoId, x.Nombre })
                .Select(g => new { g.Key.PlatoId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topPlatos);
        }


        [HttpGet]
        public IActionResult ObtenerTopPlatosHoy()
        {
            var today = DateTime.Today;

            var topPlatos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Plato" && x.pedido.fechaApertura.Date == today) // Filtrar por día actual
                .Join(_context.platos,
                    dp => dp.detalle.item_id,
                    plato => plato.id,
                    (dp, plato) => new { PlatoId = plato.id, Nombre = plato.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.PlatoId, x.Nombre })
                .Select(g => new { g.Key.PlatoId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topPlatos);
        }

        [HttpGet]
        public IActionResult ObtenerTopPlatosRango(DateTime startDate, DateTime endDate)
        {
            var topPlatos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Plato" && x.pedido.fechaApertura.Date >= startDate && x.pedido.fechaApertura.Date <= endDate)
                .Join(_context.platos,
                    dp => dp.detalle.item_id,
                    plato => plato.id,
                    (dp, plato) => new { PlatoId = plato.id, Nombre = plato.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.PlatoId, x.Nombre })
                .Select(g => new { g.Key.PlatoId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topPlatos);
        }




        [HttpGet]
        public IActionResult ObtenerTopCombosMensual(int year, int month)
        {
            var topCombos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Combo" && x.pedido.fechaApertura.Year == year && x.pedido.fechaApertura.Month == month)
                .Join(_context.combos,
                    dp => dp.detalle.item_id,
                    combo => combo.id,
                    (dp, combo) => new { ComboId = combo.id, Nombre = combo.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.ComboId, x.Nombre })
                .Select(g => new { g.Key.ComboId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topCombos);
        }

        [HttpGet]
        public IActionResult ObtenerTopCombosSemanal()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            var topCombos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Combo" && x.pedido.fechaApertura.Date >= startOfWeek && x.pedido.fechaApertura.Date <= today)
                .Join(_context.combos,
                    dp => dp.detalle.item_id,
                    combo => combo.id,
                    (dp, combo) => new { ComboId = combo.id, Nombre = combo.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.ComboId, x.Nombre })
                .Select(g => new { g.Key.ComboId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topCombos);
        }

        [HttpGet]
        public IActionResult ObtenerTopCombosHoy()
        {
            var today = DateTime.Today;

            var topCombos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Combo" && x.pedido.fechaApertura.Date == today)
                .Join(_context.combos,
                    dp => dp.detalle.item_id,
                    combo => combo.id,
                    (dp, combo) => new { ComboId = combo.id, Nombre = combo.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.ComboId, x.Nombre })
                .Select(g => new { g.Key.ComboId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topCombos);
        }


        [HttpGet]
        public IActionResult ObtenerTopCombosRango(DateTime startDate, DateTime endDate)
        {
            var topCombos = _context.Detalle_Pedido
                .Join(_context.Pedido_Local,
                    detalle => detalle.encabezado_id,
                    pedido => pedido.id_pedido,
                    (detalle, pedido) => new { detalle, pedido })
                .Where(x => x.detalle.tipo_Item == "Combo" && x.pedido.fechaApertura.Date >= startDate && x.pedido.fechaApertura.Date <= endDate)
                .Join(_context.combos,
                    dp => dp.detalle.item_id,
                    combo => combo.id,
                    (dp, combo) => new { ComboId = combo.id, Nombre = combo.nombre, CantidadVendida = dp.detalle.cantidad })
                .GroupBy(x => new { x.ComboId, x.Nombre })
                .Select(g => new { g.Key.ComboId, g.Key.Nombre, TotalVendidos = g.Sum(x => x.CantidadVendida) })
                .OrderByDescending(x => x.TotalVendidos)
                .Take(5)
                .ToList();

            return Json(topCombos);
        }

    }

}

