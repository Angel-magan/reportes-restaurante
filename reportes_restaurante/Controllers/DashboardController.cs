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
            var today = DateTime.Today;  // Obtener la fecha de hoy



            // Mesas Ocupadas: contarlas si tienen un pedido abierto hoy
            var mesasOcupadas = _context.Pedido_Local
                .Where(p => p.estado == "Abierta" && p.fechaApertura.Date == today)
                .Select(p => p.id_mesa)
                .Distinct()
                .Count();

            // Pedidos Abiertos: contar los pedidos abiertos hoy
            var pedidosAbiertos = _context.Pedido_Local
                .Where(p => p.estado == "Abierta" && p.fechaApertura.Date == today)
                .Count();

            // Pedidos en Proceso: contar los detalles de pedidos cuyo estado sea "En Proceso"
            var pedidosEnProceso = _context.Pedido_Local
                .Where(pl => pl.fechaApertura.Date == today)
                .Join(_context.Detalle_Pedido,
                    pl => pl.id_pedido,    // Unir por id_pedido
                    dp => dp.encabezado_id, // Unir por encabezado_id (referencia a Pedido_Local)
                    (pl, dp) => new { pl, dp })
                .Where(x => x.dp.estado == "En Proceso")
                .Count();

            // Pedidos Pendientes: contar los detalles de pedidos cuyo estado sea "Pendiente"
            var pedidosPendientes = _context.Pedido_Local
                .Where(pl => pl.fechaApertura.Date == today)
                .Join(_context.Detalle_Pedido,
                    pl => pl.id_pedido,    // Unir por id_pedido
                    dp => dp.encabezado_id, // Unir por encabezado_id (referencia a Pedido_Local)
                    (pl, dp) => new { pl, dp })
                .Where(x => x.dp.estado == "Pendiente")
                .Count();

            // Pedidos Finalizados: contar los detalles de pedidos cuyo estado sea "Finalizado"
            var pedidosFinalizados = _context.Pedido_Local
                .Where(pl => pl.fechaApertura.Date == today)
                .Join(_context.Detalle_Pedido,
                    pl => pl.id_pedido,    // Unir por id_pedido
                    dp => dp.encabezado_id, // Unir por encabezado_id (referencia a Pedido_Local)
                    (pl, dp) => new { pl, dp })
                .Where(x => x.dp.estado == "Finalizado")
                .Count();

            // Cuentas Cerradas: contar las facturas cuya fecha sea hoy
            var cuentasCerradas = _context.Factura
                .Where(f => f.fecha.Date == today)
                .Count();

            // Pasar los resultados a la vista
            ViewBag.MesasOcupadas = mesasOcupadas;
            ViewBag.PedidosAbiertos = pedidosAbiertos;
            ViewBag.PedidosEnProceso = pedidosEnProceso;
            ViewBag.PedidosPendientes = pedidosPendientes;  // Pasar el valor de pedidos pendientes
            ViewBag.PedidosFinalizados = pedidosFinalizados;
            ViewBag.CuentasCerradas = cuentasCerradas;


           // Llamar al método CalcularIngresos para obtener los valores
            var ingresos = CalcularIngresos();

            // Pasar los resultados de ingresos a la vista usando ViewBag
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
        public IActionResult ObtenerTopPlatos()
        {
            // Primero obtenemos todos los detalles de los pedidos con tipo "Plato"
            var topPlatos = _context.Detalle_Pedido
                .Where(dp => dp.tipo_Item == "Plato")
                .Join(_context.platos, dp => dp.item_id, plato => plato.id,
                    (dp, plato) => new
                    {
                        PlatoId = plato.id,
                        Nombre = plato.nombre,
                        CantidadVendida = dp.cantidad
                    })
                .AsEnumerable()  // Convertimos a memoria para hacer la agrupación y la suma en el cliente
                .GroupBy(x => new { x.PlatoId, x.Nombre })
                .Select(g => new
                {
                    PlatoId = g.Key.PlatoId,
                    Nombre = g.Key.Nombre,
                    TotalVendidos = g.Sum(x => x.CantidadVendida)
                })
                .OrderByDescending(x => x.TotalVendidos)  // Ordenamos por el total vendido de forma descendente
                .Take(5)  // Tomamos los 5 primeros
                .ToList();

            return Json(topPlatos);
        }


        [HttpGet]
        public IActionResult ObtenerTopCombos()
        {
        
            var topCombos = _context.Detalle_Pedido
                .Where(dp => dp.tipo_Item == "Combo")
                .Join(_context.combos, dp => dp.item_id, combo => combo.id,
                    (dp, combo) => new
                    {
                        ComboId = combo.id,
                        Nombre = combo.nombre,
                        CantidadVendida = dp.cantidad
                    })
                .AsEnumerable()  
                .GroupBy(x => new { x.ComboId, x.Nombre })
                .Select(g => new
                {
                    ComboId = g.Key.ComboId,
                    Nombre = g.Key.Nombre,
                    TotalVendidos = g.Sum(x => x.CantidadVendida)
                })
                .OrderByDescending(x => x.TotalVendidos) 
                .Take(5)  
                .ToList();

            return Json(topCombos);
        }




    }

}

