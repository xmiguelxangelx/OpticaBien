using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    // Catálogo visible para clientes autenticados
    [Authorize(Roles = "cliente")]
    public class CatalogoClienteController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public CatalogoClienteController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ==========================================
        // LISTA DE PRODUCTOS DISPONIBLES (CATÁLOGO)
        // Solo productos ACTIVO y con stock > 0
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string buscar)
        {
            var query = _context.Productos
                .Where(p =>
                    (p.Estado == "Activo" || p.Estado == null) &&
                    (p.Stock ?? 0) > 0)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();

                query = query.Where(p =>
                    (p.Nombre ?? "").ToLower().Contains(b) ||
                    (p.Marca ?? "").ToLower().Contains(b) ||
                    (p.Tipo ?? "").ToLower().Contains(b) ||
                    (p.Descripcion ?? "").ToLower().Contains(b)
                );
            }

            var productos = await query
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewData["BuscarActual"] = buscar;

            // Usa: Views/CatalogoCliente/Index.cshtml
            return View(productos);
        }

        // ==========================================
        // DETALLE DE PRODUCTO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.IdProveedorNitNavigation)
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == id &&
                    (p.Estado == "Activo" || p.Estado == null) &&
                    (p.Stock ?? 0) > 0);

            if (producto == null)
                return NotFound();

            // Usa: Views/CatalogoCliente/Detalle.cshtml
            return View(producto);
        }
    }
}
