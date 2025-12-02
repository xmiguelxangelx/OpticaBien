using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "optometra")]
    public class OptometraController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public OptometraController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // GET: /Optometra
        public async Task<IActionResult> Index()
        {
            // Id del usuario logueado (este Id coincide con IdUsuarioempleado / IdUsuariopaciente)
            var idUsuarioStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int idOptometra = int.Parse(idUsuarioStr);

            var hoyDateTime = DateTime.Today;

            // 🔹 Total de citas de HOY para este optometra
            var totalCitasHoy = await _context.Citas
                .CountAsync(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra);

            // 🔹 Citas PENDIENTES de HOY para este optometra
            var citasPendientes = await _context.Citas
                .CountAsync(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra &&
                    c.Estado == "Pendiente");   // ajusta el texto según cómo lo guardes en BD

            // 🔹 Pacientes ATENDIDOS hoy (estado = 'Atendida') por este optometra
            var pacientesAtendidosHoy = await _context.Citas
                .Where(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra &&
                    c.Estado == "Atendida")     // ajusta el texto si usas otro estado
                .Select(c => c.IdUsuariopaciente)
                .Distinct()
                .CountAsync();

            // 🔹 Historias clínicas vinculadas a citas de HOY de este optometra
            var historiasClinicasHoy = await _context.Citas
                .Where(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra &&
                    c.IdHistoriaclinica != null)
                .Select(c => c.IdHistoriaclinica.Value)
                .Distinct()
                .CountAsync();

            var modelo = new OptometraDashboardViewModel
            {
                TotalCitasHoy = totalCitasHoy,
                CitasPendientes = citasPendientes,
                PacientesAtendidosHoy = pacientesAtendidosHoy,
                HistoriasClinicasHoy = historiasClinicasHoy,
                FechaHoy = hoyDateTime
            };

            return View(modelo);
        }
    }
}
