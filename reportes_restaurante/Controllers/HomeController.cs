using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using reportes_restaurante.Models;

namespace reportes_restaurante.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public ActionResult DetalleMesas()
        {
            return PartialView("~/Views/DetalleMesasPedidos/_DetalleMesas.cshtml"); 
        }

        public ActionResult DetallePedidos()
        {
            return PartialView("~/Views/DetalleMesasPedidos/_DetallePedidos.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
