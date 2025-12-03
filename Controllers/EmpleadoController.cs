using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica1.Models;

namespace Optica1.Controllers
{
    [Authorize(Roles = "empleado")]
    public class EmpleadoController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public EmpleadoController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // Panel principal del empleado
        public IActionResult Index()
        {
            // Más adelante aquí calculamos métricas (ventas hoy, citas, etc.)
            return View();
        }
    }
}
