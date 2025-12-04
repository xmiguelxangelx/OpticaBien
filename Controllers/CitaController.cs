using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Optica1.Controllers
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

        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        // Horas cada 30 minutos entre 10:00 y 19:00
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

            // Perfil optometra
            var idPerfilOptometra = await _context.Perfiles
                .Where(p => p.Descripcion == "optometra")
                .Select(p => p.IdPerfil)
                .FirstOrDefaultAsync();

            if (idPerfilOptometra == 0)
            {
                ViewBag.Optometras = new SelectList(Enumerable.Empty<SelectListItem>());
                return;
            }

            var optometras = await _context.UsuarioPerfils
                .Include(up => up.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdPersonaNavigation)
                .Where(up => up.IdPerfil == idPerfilOptometra)
                .Select(up => up.IdUsuarioNavigation)
                .ToListAsync();

            var listaOptometras = optometras
                .Where(o => o != null)
                .Select(o => new
                {
                    o.IdUsuario,
                    NombreMostrar = o.IdPersonaNavigation == null
                        ? o.NombreUsuario
                        : string.Join(" ",
                            new[]
                            {
                                o.IdPersonaNavigation.PrimerNombre,
                                o.IdPersonaNavigation.SegundoNombre,
                                o.IdPersonaNavigation.PrimerApellido,
                                o.IdPersonaNavigation.SegundoApellido
                            }.Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToList();

            ViewBag.Optometras = new SelectList(
                listaOptometras,
                "IdUsuario",
                "NombreMostrar",
                optometraSeleccionado
            );
        }

        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            var query = _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                .Include(c => c.IdUsuarioempleadoNavigation)
                .AsQueryable();

            // solo citas activas
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
        // CITAS INACTIVAS
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> Inactivas()
        {
            var lista = await _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                .Include(c => c.IdUsuarioempleadoNavigation)
                .Where(c => c.Estado == "Inactiva")
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.Hora)
                .ToListAsync();

            return View(lista);
        }

        // RESTAURAR CITA INACTIVA
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> Restaurar(int id)
        {
            var cita = await _context.Citas
                .FirstOrDefaultAsync(c => c.IdCita == id);

            if (cita == null)
            {
                TempData["Error"] = "La cita no existe.";
                return RedirectToAction(nameof(Inactivas));
            }

            if (cita.Estado != "Inactiva")
            {
                TempData["Mensaje"] = "La cita ya se encuentra activa.";
                return RedirectToAction(nameof(Index));
            }

            bool choque = await _context.Citas.AnyAsync(c =>
                c.IdCita != cita.IdCita &&
                c.IdUsuarioempleado == cita.IdUsuarioempleado &&
                c.Fecha == cita.Fecha &&
                c.Hora == cita.Hora &&
                c.Estado != "Inactiva");

            if (choque)
            {
                TempData["Error"] =
                    "No se puede restaurar la cita porque el optómetra ya tiene otra cita activa en ese horario.";
                return RedirectToAction(nameof(Inactivas));
            }

            cita.Estado = "Pendiente";

            _context.Update(cita);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La cita se restauró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CREAR
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "administrador,cliente,empleado")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,cliente,empleado")]
        public async Task<IActionResult> Crear(Citas model, string horaSeleccionada)
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            ModelState.Remove("Estado");
            ModelState.Remove("IdUsuarioempleadoNavigation");
            ModelState.Remove("IdUsuariopacienteNavigation");

            model.Estado = "Pendiente";

            if (User.IsInRole("cliente"))
            {
                model.IdUsuariopaciente = usuario.IdUsuario;
            }

            // Hora desde select
            if (!string.IsNullOrEmpty(horaSeleccionada) &&
                TimeSpan.TryParse(horaSeleccionada, out var horaSpan))
            {
                model.Hora = horaSpan;
            }
            else
            {
                ModelState.AddModelError("Hora", "La hora es obligatoria.");
            }

            // Validaciones de fecha/hora
            if (model.Hora != default(TimeSpan))
            {
                var inicio = new TimeSpan(10, 0, 0);
                var fin = new TimeSpan(19, 0, 0);

                if (model.Hora < inicio || model.Hora > fin || model.Hora.Minutes % 30 != 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "El horario permitido para citas es entre 10:00 AM y 7:00 PM, en intervalos de 30 minutos.");
                }

                // Fecha no puede ser pasada
                if (model.Fecha.Date < DateTime.Today)
                {
                    ModelState.AddModelError("Fecha", "No puedes agendar citas en fechas que ya pasaron.");
                }

                // Si es hoy, hora no puede ser pasada
                if (model.Fecha.Date == DateTime.Today)
                {
                    var ahora = DateTime.Now.TimeOfDay;
                    if (model.Hora < ahora)
                    {
                        ModelState.AddModelError("Hora", "No puedes agendar una cita en una hora que ya pasó.");
                    }
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

        [HttpGet]
        [Authorize(Roles = "administrador,cliente,empleado")]
        public async Task<IActionResult> Editar(int id)
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

            await CargarCombosAsync(cita.IdUsuarioempleado);
            return View(cita);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,cliente,empleado")]
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

            // 🔹 Nueva regla para EMPLEADO:
            if (User.IsInRole("empleado"))
            {
                // No permitir editar citas atendidas o de fechas pasadas
                if (citaDb.Estado == "Atendida" || citaDb.Fecha.Date < DateTime.Today)
                {
                    TempData["Error"] = "No se pueden editar citas atendidas o de fechas pasadas.";
                    return RedirectToAction(nameof(Index));
                }
            }

            ModelState.Remove("Estado");
            ModelState.Remove("IdUsuarioempleadoNavigation");
            ModelState.Remove("IdUsuariopacienteNavigation");

            // mantener paciente y estado original
            model.IdUsuariopaciente = citaDb.IdUsuariopaciente;
            model.Estado = citaDb.Estado;

            // Hora desde select
            if (!string.IsNullOrEmpty(horaSeleccionada) &&
                TimeSpan.TryParse(horaSeleccionada, out var horaSpan))
            {
                model.Hora = horaSpan;
            }
            else
            {
                ModelState.AddModelError("Hora", "La hora es obligatoria.");
            }

            // Validaciones de fecha/hora (igual que en Crear)
            if (model.Hora != default(TimeSpan))
            {
                var inicio = new TimeSpan(10, 0, 0);
                var fin = new TimeSpan(19, 0, 0);

                if (model.Hora < inicio || model.Hora > fin || model.Hora.Minutes % 30 != 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "El horario permitido para citas es entre 10:00 AM y 7:00 PM, en intervalos de 30 minutos.");
                }

                if (model.Fecha.Date < DateTime.Today)
                {
                    ModelState.AddModelError("Fecha", "No puedes agendar citas en fechas que ya pasaron.");
                }

                if (model.Fecha.Date == DateTime.Today)
                {
                    var ahora = DateTime.Now.TimeOfDay;
                    if (model.Hora < ahora)
                    {
                        ModelState.AddModelError("Hora", "No puedes agendar una cita en una hora que ya pasó.");
                    }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,cliente,empleado")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();

            var usuario = await GetUsuarioActualAsync();
            if (usuario == null) return Unauthorized();

            // Cliente: solo puede cancelar sus propias citas
            if (User.IsInRole("cliente") && cita.IdUsuariopaciente != usuario.IdUsuario)
                return Forbid();

            // Por simplicidad y tiempo, permitimos que el empleado/admin cancelen cualquier cita
            // (si luego quieres, volvemos a poner reglas por fecha/estado)
            cita.Estado = "Inactiva";

            _context.Citas.Update(cita);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La cita se inactivó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CAMBIAR ESTADO DE LA CITA
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "administrador,empleado,optometra")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var cita = await _context.Citas
                .Include(c => c.IdUsuariopacienteNavigation)
                .Include(c => c.IdUsuarioempleadoNavigation)
                .FirstOrDefaultAsync(c => c.IdCita == id);

            if (cita == null) return NotFound();

            if (cita.Estado == "Inactiva")
            {
                TempData["Error"] = "No se puede cambiar el estado de una cita inactiva.";
                return RedirectToAction(nameof(Index));
            }

            // Opciones de estado permitidas
            ViewBag.Estados = new List<string> { "Pendiente", "Atendida", "Cancelada" };

            return View(cita);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,empleado,optometra")]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null) return NotFound();

            if (cita.Estado == "Inactiva")
            {
                TempData["Error"] = "No se puede cambiar el estado de una cita inactiva.";
                return RedirectToAction(nameof(Index));
            }

            var estadosValidos = new[] { "Pendiente", "Atendida", "Cancelada" };
            if (!estadosValidos.Contains(nuevoEstado))
            {
                TempData["Error"] = "El estado seleccionado no es válido.";
                return RedirectToAction(nameof(CambiarEstado), new { id });
            }

            cita.Estado = nuevoEstado;

            _context.Citas.Update(cita);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "El estado de la cita se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }



    }
}
