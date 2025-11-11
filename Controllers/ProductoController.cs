using Microsoft.AspNetCore.Mvc;
using Optica1.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
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