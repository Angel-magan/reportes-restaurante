using Microsoft.AspNetCore.Mvc;

namespace reportes_restaurante.Controllers
{
    public class ReportesController : Controller
    {
        public IActionResult DashboardReportes()
        {
            return View();
        }
    }
}
