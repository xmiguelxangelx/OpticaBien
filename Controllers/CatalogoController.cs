using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Optica1.Controllers
{
    // Alias / puente: redirige todo lo que vaya a /Catalogo -> /CatalogoCliente
    [Authorize(Roles = "cliente")]
    public class CatalogoController : Controller
    {
        [HttpGet]
        public IActionResult Index(string buscar)
        {
            // Redirige al catálogo real
            return RedirectToAction("Index", "CatalogoCliente", new { buscar });
        }

        [HttpGet]
        public IActionResult Detalle(int id)
        {
            // Redirige al detalle real
            return RedirectToAction("Detalle", "CatalogoCliente", new { id });
        }
    }
}
