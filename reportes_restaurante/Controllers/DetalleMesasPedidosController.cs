using Microsoft.AspNetCore.Mvc;

namespace reportes_restaurante.Controllers
{
    public class DetalleMesasPedidosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public ActionResult DetalleMesas()
        {
            return PartialView("~/Views/DetalleMesasPedidos/_DetalleMesas.cshtml");  // Vista parcial para mesas
        }

        public ActionResult DetallePedidos()
        {
            return PartialView("~/Views/DetalleMesasPedidos/_DetallePedidos.cshtml");  // Vista parcial para pedidos
        }
    }
}
