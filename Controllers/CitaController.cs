using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using Optica1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Optical.Controllers
{
    [Authorize]
    public class CitaController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public CitaController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        // Usuario actual según NombreUsuario
        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        // Horas cada 30 min entre 10:00 y 19:00
        private IEnumerable<SelectListItem> GetHorasSelectList()
        {
            var inicio = new TimeSpan(10, 0, 0);
            var fin = new TimeSpan(19, 0, 0);
            var lista = new List<SelectListItem>();

            for (var hora = inicio; hora <= fin; hora = hora.Add(TimeSpan.FromMinutes(30)))
            {
                var valor = hora.ToString(@"hh\:mm");
                lista.Add(new SelectListItem
                {
                    Value = valor,
                    Text = valor
                });
            }

            return lista;
        }

        // Cargar combos (horas + optómetras)
        private async Task CargarCombosAsync(int? optometraSeleccionado = null)
        {
            ViewBag.HorasDisponibles = GetHorasSelectList();

            // 👉 Por ahora mostramos todos los usuarios como posibles optómetras
            var optometras = await _context.Usuarios.ToListAsync();

            ViewBag.Optometras = new SelectList(
                optometras,
                "IdUsuario",
                "NombreUsuario",
                optometraSeleccionado
            );
        }

        // =====================================================
        // INDEX
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            var query = _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                .Include(c => c.IdUsuarioempleadoNavigation)
                .AsQueryable();

            // solo citas que no estén inactivas
            query = query.Where(c => c.Estado != "Inactiva");

            if (User.IsInRole("cliente"))
            {
                query = query.Where(c => c.IdUsuariopaciente == usuario.IdUsuario);
            }
            else if (User.IsInRole("optometra"))
            {
                query = query.Where(c => c.IdUsuarioempleado == usuario.IdUsuario);
            }

            var lista = await query
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.Hora)
                .ToListAsync();

            return View(lista);
        }

        // =====================================================
        // CREAR
        // =====================================================

        // GET: Cita/Crear
        [Authorize(Roles = "administrador,cliente")]
        public async Task<IActionResult> Crear()
        {
            await CargarCombosAsync();

            var modelo = new Citas
            {
                Fecha = DateTime.Today,
                Estado = "Pendiente"
            };

            return View(modelo);
        }

        // POST: Cita/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,cliente")]
        public async Task<IActionResult> Crear(Citas model, string horaSeleccionada)
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            // Estas propiedades no vienen del formulario
            ModelState.Remove("Estado");
            ModelState.Remove("IdUsuarioempleadoNavigation");
            ModelState.Remove("IdUsuariopacienteNavigation");

            // Estado por defecto
            model.Estado = "Pendiente";

            // Cliente se asigna a sí mismo como paciente
            if (User.IsInRole("cliente"))
            {
                model.IdUsuariopaciente = usuario.IdUsuario;
            }

            // === Hora desde el select ===
            if (!string.IsNullOrEmpty(horaSeleccionada) &&
                TimeSpan.TryParse(horaSeleccionada, out var horaSpan))
            {
                model.Hora = horaSpan;
            }
            else
            {
                ModelState.AddModelError("Hora", "La hora es obligatoria.");
            }

            // Validar rango 10–19 y cada 30 minutos
            if (model.Hora != default(TimeSpan))
            {
                var inicio = new TimeSpan(10, 0, 0);
                var fin = new TimeSpan(19, 0, 0);

                if (model.Hora < inicio || model.Hora > fin || model.Hora.Minutes % 30 != 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "El horario permitido para citas es entre 10:00 AM y 7:00 PM, en intervalos de 30 minutos.");
                }
            }

            // Motivo obligatorio
            if (string.IsNullOrWhiteSpace(model.Motivo))
            {
                ModelState.AddModelError("Motivo", "El motivo es obligatorio.");
            }

            // Optómetra obligatorio
            if (!model.IdUsuarioempleado.HasValue || model.IdUsuarioempleado == 0)
            {
                ModelState.AddModelError("IdUsuarioempleado", "Debe seleccionar un optómetra.");
            }

            // Validación cruzada: mismo optómetra no puede tener otra cita ese día/hora
            if (model.IdUsuarioempleado.HasValue && model.Hora != default(TimeSpan))
            {
                bool choque = await _context.Citas.AnyAsync(c =>
                    c.IdUsuarioempleado == model.IdUsuarioempleado &&
                    c.Fecha == model.Fecha &&
                    c.Hora == model.Hora &&
                    c.Estado != "Inactiva");

                if (choque)
                {
                    ModelState.AddModelError(string.Empty,
                        "El optómetra ya tiene una cita asignada en ese horario.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model.IdUsuarioempleado);
                return View(model);
            }

            _context.Citas.Add(model);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La cita se creó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDITAR
        // =====================================================

        // GET: Cita/Editar/5
        [Authorize(Roles = "administrador,cliente")]
        public async Task<IActionResult> Editar(int id)
        {
            var cita = await _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                .Include(c => c.IdUsuarioempleadoNavigation)
                .FirstOrDefaultAsync(c => c.IdCita == id);

            if (cita == null) return NotFound();

            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            // el cliente solo puede editar sus propias citas
            if (User.IsInRole("cliente") && cita.IdUsuariopaciente != usuario.IdUsuario)
                return Forbid();

            await CargarCombosAsync(cita.IdUsuarioempleado);
            return View(cita);
        }

        // POST: Cita/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,cliente")]
        public async Task<IActionResult> Editar(int id, Citas model, string horaSeleccionada)
        {
            if (id != model.IdCita) return NotFound();

            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            var citaDb = await _context.Citas
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCita == id);

            if (citaDb == null) return NotFound();

            if (User.IsInRole("cliente") && citaDb.IdUsuariopaciente != usuario.IdUsuario)
                return Forbid();

            // Quitar validación de propiedades que llenamos nosotros
            ModelState.Remove("Estado");
            ModelState.Remove("IdUsuarioempleadoNavigation");
            ModelState.Remove("IdUsuariopacienteNavigation");

            // Mantener paciente y estado originales
            model.IdUsuariopaciente = citaDb.IdUsuariopaciente;
            model.Estado = citaDb.Estado;

            // === Hora desde select ===
            if (!string.IsNullOrEmpty(horaSeleccionada) &&
                TimeSpan.TryParse(horaSeleccionada, out var horaSpan))
            {
                model.Hora = horaSpan;
            }
            else
            {
                ModelState.AddModelError("Hora", "La hora es obligatoria.");
            }

            // Validar rango
            if (model.Hora != default(TimeSpan))
            {
                var inicio = new TimeSpan(10, 0, 0);
                var fin = new TimeSpan(19, 0, 0);

                if (model.Hora < inicio || model.Hora > fin || model.Hora.Minutes % 30 != 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "El horario permitido para citas es entre 10:00 AM y 7:00 PM, en intervalos de 30 minutos.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.Motivo))
            {
                ModelState.AddModelError("Motivo", "El motivo es obligatorio.");
            }

            if (!model.IdUsuarioempleado.HasValue || model.IdUsuarioempleado == 0)
            {
                ModelState.AddModelError("IdUsuarioempleado", "Debe seleccionar un optómetra.");
            }

            // Validación cruzada (ignorando la propia cita)
            if (model.IdUsuarioempleado.HasValue && model.Hora != default(TimeSpan))
            {
                bool choque = await _context.Citas.AnyAsync(c =>
                    c.IdCita != model.IdCita &&
                    c.IdUsuarioempleado == model.IdUsuarioempleado &&
                    c.Fecha == model.Fecha &&
                    c.Hora == model.Hora &&
                    c.Estado != "Inactiva");

                if (choque)
                {
                    ModelState.AddModelError(string.Empty,
                        "El optómetra ya tiene una cita asignada en ese horario.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model.IdUsuarioempleado);
                return View(model);
            }

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Citas.Any(c => c.IdCita == model.IdCita))
                    return NotFound();
                throw;
            }

            TempData["Mensaje"] = "La cita se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ELIMINAR (INACTIVAR)
        // =====================================================

        // GET: Cita/Eliminar/5
        [Authorize(Roles = "administrador,cliente")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cita = await _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                .Include(c => c.IdUsuarioempleadoNavigation)
                .FirstOrDefaultAsync(c => c.IdCita == id);

            if (cita == null) return NotFound();

            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            if (User.IsInRole("cliente") && cita.IdUsuariopaciente != usuario.IdUsuario)
                return Forbid();

            return View(cita);
        }

        // POST: Cita/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,cliente")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();

            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            if (User.IsInRole("cliente") && cita.IdUsuariopaciente != usuario.IdUsuario)
                return Forbid();

            cita.Estado = "Inactiva";
            _context.Update(cita);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La cita se inactivó correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
