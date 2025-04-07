using Microsoft.AspNetCore.Mvc;
using reportes_restaurante.Models;

namespace reportes_restaurante.Controllers
{
    public class DetalleMesasPedidosController : Controller
    {
        private readonly restauranteContext _context;
        public DetalleMesasPedidosController(restauranteContext restauranteContext)
        {
            _context = restauranteContext;
        }
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult DetalleMesas(string estado = "Pendiente")
        {
            DateTime fecha = DateTime.Today;
            if (estado.Equals("Todos"))
            {
                var pedido = (from p in _context.Pedido_Local
                              join dp in _context.Detalle_Pedido
                              on p.id_pedido equals dp.encabezado_id
                              join m in _context.mesas
                              on p.id_mesa equals m.id
                              //where p.fechaApertura >= fecha && p.fechaApertura <= DateTime.Now
                              select new
                              {
                                  Mesa = m.numero,
                                  Estado = dp.estado
                              }).OrderBy(x => x.Estado).ToList();
                ViewBag.Pedido = pedido;
            }
            else if (estado.Equals("MesasDisp"))
            {
                var mesas = (from m in _context.mesas
                             where m.disponibilidad == "libre"
                             select new
                             {
                                 Mesa = m.numero,
                                 Estado = m.disponibilidad
                             }).OrderBy(x => x.Estado).ToList();
                ViewBag.Pedido = mesas;
            }
            else
            {
                var pedido = (from p in _context.Pedido_Local
                              join dp in _context.Detalle_Pedido
                              on p.id_pedido equals dp.encabezado_id
                              join m in _context.mesas
                              on p.id_mesa equals m.id
                              where dp.estado == estado /*p.fechaApertura >= fecha && p.fechaApertura <= DateTime.Now*/
                              select new
                              {
                                  Mesa = m.numero,
                                  Estado = dp.estado
                              }).ToList();
                ViewBag.Pedido = pedido;
            }

            return PartialView("~/Views/DetalleMesasPedidos/_DetalleMesas.cshtml");  // Vista parcial para mesas
        }

        [HttpPost]
        public ActionResult DetallePedidos(string estado = "Abierta")
        {
            if (estado.Equals("Todos"))
            {
                var pedido = (from p in _context.Pedido_Local
                              join m in _context.mesas
                              on p.id_mesa equals m.id
                              select new
                              {
                                  Id = p.id_pedido,
                                  Mesa = m.numero,
                                  Cliente = p.nombre_cliente,
                                  Estado = p.estado
                              }).OrderBy(x => x.Estado).ToList();
                ViewBag.Pedido = pedido;
            }
            else
            {
                var pedido = (from p in _context.Pedido_Local
                              join m in _context.mesas
                              on p.id_mesa equals m.id
                              where p.estado == estado
                              select new
                              {
                                  Id = p.id_pedido,
                                  Mesa = m.numero,
                                  Cliente = p.nombre_cliente,
                                  Estado = p.estado
                              }).ToList();
                ViewBag.Pedido = pedido;
            }

            return PartialView("~/Views/DetalleMesasPedidos/_DetallePedidos.cshtml");  // Vista parcial para pedidos
        }

        public JsonResult ObtenerDetPedidoPlatos(int id_pedido)
        {
            var pedidoPlato = (from p in _context.Pedido_Local
                               join dp in _context.Detalle_Pedido
                               on p.id_pedido equals dp.encabezado_id
                               join pl in _context.platos
                               on dp.item_id equals pl.id
                               where p.id_pedido == id_pedido
                               select new
                               {
                                   Estado = dp.estado,
                                   Plato = pl.nombre,
                                   Cantidad = dp.cantidad,
                                   Subtotal = dp.subtotal
                               }).ToList();

            return Json(pedidoPlato);
        }
        public JsonResult ObtenerDetPedidoCombos(int id_pedido)
        {
            var pedidoCombo = (from p in _context.Pedido_Local
                               join dp in _context.Detalle_Pedido
                               on p.id_pedido equals dp.encabezado_id
                               join c in _context.combos
                               on dp.item_id equals c.id
                               where p.id_pedido == id_pedido
                               select new
                               {
                                   Estado = dp.estado,
                                   Combo = c.nombre,
                                   Cantidad = dp.cantidad,
                                   Subtotal = dp.subtotal
                               }).ToList();

            return Json(pedidoCombo);
        }
        public JsonResult ObtenerEncabezadoPedido(int id_pedido)
        {
            var encabezado = (from p in _context.Pedido_Local
                               join m in _context.mesas
                               on p.id_mesa equals m.id
                               join mr in _context.empleados
                               on p.id_mesero equals mr.codigo
                               where p.id_pedido == id_pedido
                               select new
                               {
                                   Mesa = m.numero,
                                   Cuenta = p.estado,
                                   Mesero = mr.nombre
                               }).ToList();

            return Json(encabezado);
        }
    }
}
