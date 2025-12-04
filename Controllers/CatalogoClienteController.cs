using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Optica1.Controllers
{
    [Authorize(Roles = "cliente,empleado,administrador")]
    public class CatalogoClienteController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public CatalogoClienteController(ProyectoopticaContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Where(p => p.Estado == "Activo" || p.Estado == null)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View(productos);
        }
    }
}
