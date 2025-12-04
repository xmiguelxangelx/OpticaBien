using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "cliente")]
    public class HistoriaClinicaClienteController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public HistoriaClinicaClienteController(ProyectoopticaContext context)
        {
            _context = context;
        }

        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;

            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        // LISTA DE HISTORIAS DEL CLIENTE
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            var historias = await _context.Historiaclinicas
                .Where(h => h.IdUsuarioPaciente == usuario.IdUsuario)
                .OrderByDescending(h => h.FechaCreacion)
                .ToListAsync();

            return View(historias);
        }

        // DETALLE DE UNA HISTORIA (validando que sea del cliente)
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            var historia = await _context.Historiaclinicas
                .FirstOrDefaultAsync(h =>
                    h.IdHistoriaclinica == id &&
                    h.IdUsuarioPaciente == usuario.IdUsuario);

            if (historia == null)
                return NotFound();

            return View(historia);
        }
    }
}
