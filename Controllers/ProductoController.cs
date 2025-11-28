using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "cliente,empleado,administrador,optometra")]
    public class ProductoController : Controller
    {
        private readonly ProyectoopticaContext _dbContext;

        public ProductoController(ProyectoopticaContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Lista de productos desde la BD
            var productos = await _dbContext.Productos.ToListAsync();
            return View(productos);
        }
    }
}