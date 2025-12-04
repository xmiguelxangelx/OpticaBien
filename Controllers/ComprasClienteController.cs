using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Optica1.Controllers
{
    [Authorize(Roles = "cliente")]
    public class ComprasClienteController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public ComprasClienteController(ProyectoopticaContext context)
        {
            _context = context;
        }

        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            // Ventas del cliente
            var query = _context.Venta
                .Include(v => v.IdUsuarioempleadoNavigation)
                .Where(v => v.IdUsuariopaciente == usuario.IdUsuario);

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            // Cálculo de abonos por venta
            var idsVentas = ventas.Select(v => v.IdVenta).ToList();

            var pagosPorVenta = await _context.VentaPagos
                .Where(p => p.IdVenta.HasValue && idsVentas.Contains(p.IdVenta.Value))
                .GroupBy(p => p.IdVenta)
                .Select(g => new
                {
                    IdVenta = g.Key,
                    TotalAbonado = g.Sum(x => x.Monto ?? 0)
                })
                .ToListAsync();

            ViewBag.TotalAbonadoPorVenta = pagosPorVenta
                .Where(x => x.IdVenta.HasValue)
                .ToDictionary(x => x.IdVenta!.Value, x => x.TotalAbonado);

            return View(ventas);
        }
    }
}
