using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "cliente")]
    public class ClienteController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public ClienteController(ProyectoopticaContext context)
        {
            _context = context;
        }

        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;

            return await _context.Usuarios
                .Include(u => u.IdPersonaNavigation)   // 
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        public async Task<IActionResult> Index()
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            // 📅 Próximas citas
            var hoy = DateTime.Today;

            var citasProximas = await _context.Citas
                .Where(c =>
                    c.IdUsuariopaciente == usuario.IdUsuario &&
                    c.Estado != "Inactiva" &&
                    c.Fecha.Date >= hoy)
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.Hora)
                .Take(5)
                .ToListAsync();

            // 🛒 Últimas compras (resumen con saldos)
            var ventas = await _context.Venta
                .Where(v => v.IdUsuariopaciente == usuario.IdUsuario)
                .OrderByDescending(v => v.Fecha)
                .Take(5)
                .ToListAsync();

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

            var dictAbonos = pagosPorVenta
                .Where(x => x.IdVenta.HasValue)
                .ToDictionary(x => x.IdVenta!.Value, x => x.TotalAbonado);

            var comprasResumen = ventas.Select(v =>
            {
                float total = v.Total ?? 0;
                float abonado = dictAbonos.TryGetValue(v.IdVenta, out var a) ? a : 0;
                float saldo = total - abonado;

                return new VentaClienteResumen
                {
                    IdVenta = v.IdVenta,
                    Fecha = v.Fecha,
                    Total = total,
                    Abonado = abonado,
                    Saldo = saldo
                };
            }).ToList();

            var vm = new ClienteDashboardViewModel
            {
                Usuario = usuario,
                CitasProximas = citasProximas,
                Compras = comprasResumen
            };

            return View(vm);
        }
    }
}
