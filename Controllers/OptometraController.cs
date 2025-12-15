using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System;
using System.Collections.Generic;
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

        // ==========================================
        // DASHBOARD DEL OPTÓMETRA
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Id del usuario logueado (coincide con IdUsuarioempleado)
            var idUsuarioStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int idOptometra = int.Parse(idUsuarioStr);

            // Traemos usuario + persona para armar el nombre real
            var usuario = await _context.Usuarios
                .Include(u => u.IdPersonaNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idOptometra);

            string nombreOptometra;

            if (usuario?.IdPersonaNavigation != null)
            {
                var p = usuario.IdPersonaNavigation;
                nombreOptometra = string.Join(" ",
                    new[]
                    {
                        p.PrimerNombre,
                        p.SegundoNombre,
                        p.PrimerApellido,
                        p.SegundoApellido
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                // Si por alguna razón no hay persona asociada, usamos el nombre de usuario
                nombreOptometra = usuario?.NombreUsuario ?? "Optómetra";
            }

            var hoyDateTime = DateTime.Today;

            // Citas de HOY para este optómetra
            var totalCitasHoy = await _context.Citas
                .CountAsync(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra);

            // Citas PENDIENTES de HOY para este optómetra
            var citasPendientes = await _context.Citas
                .CountAsync(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra &&
                    c.Estado == "Pendiente");

            // Pacientes ATENDIDOS hoy (estado = 'Atendida') por este optómetra
            var pacientesAtendidosHoy = await _context.Citas
                .Where(c =>
                    c.Fecha.Date == hoyDateTime &&
                    c.IdUsuarioempleado == idOptometra &&
                    c.Estado == "Atendida")
                .Select(c => c.IdUsuariopaciente)
                .Distinct()
                .CountAsync();

            // Historias clínicas vinculadas a citas de HOY de este optómetra
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
                FechaHoy = hoyDateTime,
                NombreOptometra = nombreOptometra
            };

            return View(modelo);
        }

        // ==========================================
        // PACIENTES DEL DÍA PARA EL OPTÓMETRA
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MisPacientes()
        {
            var idUsuarioStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int idOptometra = int.Parse(idUsuarioStr);

            var hoy = DateTime.Today;

            // Citas de HOY del optómetra, con usuario y persona
            var citas = await _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                    .ThenInclude(u => u.IdPersonaNavigation)
                .Where(c =>
                    c.Fecha.Date == hoy &&
                    c.IdUsuarioempleado == idOptometra &&
                    c.Estado != "Inactiva")
                .OrderBy(c => c.Hora)
                .ToListAsync();

            var lista = new List<PacienteDelDiaViewModel>();

            foreach (var c in citas)
            {
                var usuarioPaciente = c.IdUsuariopacienteNavigation;
                var persona = usuarioPaciente?.IdPersonaNavigation;

                // Nombre completo
                string nombrePaciente = usuarioPaciente?.NombreUsuario ?? "Sin nombre";

                if (persona != null)
                {
                    nombrePaciente = string.Join(" ",
                        new[]
                        {
                            persona.PrimerNombre,
                            persona.SegundoNombre,
                            persona.PrimerApellido,
                            persona.SegundoApellido
                        }.Where(s => !string.IsNullOrWhiteSpace(s)));
                }

                // Edad aproximada si hay fecha de nacimiento
                int? edad = null;
                if (persona?.FechaNacimiento != null)
                {
                    // fn es DateOnly
                    var fn = persona.FechaNacimiento.Value;

                    // hoy es DateTime → lo convertimos a DateOnly
                    var hoyDateOnly = DateOnly.FromDateTime(hoy);

                    int e = hoyDateOnly.Year - fn.Year;
                    if (fn > hoyDateOnly.AddYears(-e)) e--;

                    edad = e;
                }


                lista.Add(new PacienteDelDiaViewModel
                {
                    IdCita = c.IdCita,
                    IdUsuarioPaciente = c.IdUsuariopaciente,
                    NombrePaciente = nombrePaciente,

                    // Conversión segura a string
                    Telefono = persona?.Telefono != null
         ? persona.Telefono.ToString()
         : string.Empty,

                    Correo = persona?.Correo ?? string.Empty,

                    Edad = edad,
                    FechaCita = c.Fecha,
                    HoraCita = c.Hora,
                    Motivo = c.Motivo,
                    Estado = c.Estado
                });

            }

            return View(lista);
        }
    }
}
