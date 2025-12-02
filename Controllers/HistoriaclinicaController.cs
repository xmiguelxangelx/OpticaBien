using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador,optometra")]
    public class HistoriaclinicaController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public HistoriaclinicaController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ============================
        // HELPER: Usuario actual
        // ============================
        private async Task<Usuario?> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName)) return null;

            return await _context.Usuarios
                .Include(u => u.IdPersonaNavigation)
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        // ============================
        // HELPER: Cargar combos
        // ============================
        private async Task CargarCombosAsync(int? pacienteSeleccionado = null, int? optometraSeleccionado = null)
        {
            // id perfil cliente
            var idPerfilCliente = await _context.Perfiles
                .Where(p => p.Descripcion == "cliente")
                .Select(p => p.IdPerfil)
                .FirstOrDefaultAsync();

            // id perfil optometra
            var idPerfilOptometra = await _context.Perfiles
                .Where(p => p.Descripcion == "optometra")
                .Select(p => p.IdPerfil)
                .FirstOrDefaultAsync();

            // ======================
            // PACIENTES
            // ======================
            var pacientesQuery = await _context.UsuarioPerfils
                .Include(up => up.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdPersonaNavigation)
                .Where(up => up.IdPerfil == idPerfilCliente &&
                             up.IdUsuarioNavigation.Estado == "Activo")
                .ToListAsync();

            var pacientes = pacientesQuery
                .Select(up => new
                {
                    IdUsuario = up.IdUsuarioNavigation.IdUsuario,
                    NombreMostrar =
                        up.IdUsuarioNavigation.IdPersonaNavigation.IdPersona.ToString() // documento
                        + " - " +
                        string.Join(" ",
                            new[]
                            {
                                up.IdUsuarioNavigation.IdPersonaNavigation.PrimerNombre,
                                up.IdUsuarioNavigation.IdPersonaNavigation.SegundoNombre,
                                up.IdUsuarioNavigation.IdPersonaNavigation.PrimerApellido,
                                up.IdUsuarioNavigation.IdPersonaNavigation.SegundoApellido
                            }.Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToList();

            ViewBag.Pacientes = new SelectList(pacientes, "IdUsuario", "NombreMostrar", pacienteSeleccionado);

            // ======================
            // OPTÓMETRAS
            // ======================
            var optometrasQuery = await _context.UsuarioPerfils
                .Include(up => up.IdUsuarioNavigation)
                    .ThenInclude(u => u.IdPersonaNavigation)
                .Where(up => up.IdPerfil == idPerfilOptometra &&
                             up.IdUsuarioNavigation.Estado == "Activo")
                .ToListAsync();

            var optometras = optometrasQuery
                .Select(up => new
                {
                    IdUsuario = up.IdUsuarioNavigation.IdUsuario,
                    NombreMostrar = string.Join(" ",
                        new[]
                        {
                            up.IdUsuarioNavigation.IdPersonaNavigation.PrimerNombre,
                            up.IdUsuarioNavigation.IdPersonaNavigation.SegundoNombre,
                            up.IdUsuarioNavigation.IdPersonaNavigation.PrimerApellido,
                            up.IdUsuarioNavigation.IdPersonaNavigation.SegundoApellido
                        }.Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToList();

            ViewBag.Optometras = new SelectList(optometras, "IdUsuario", "NombreMostrar", optometraSeleccionado);
        }

        // ============================
        // API: Buscar paciente por documento
        // ============================
        [HttpGet]
        public async Task<IActionResult> BuscarPaciente(long documento)
        {
            // Id perfil cliente
            var idPerfilCliente = await _context.Perfiles
                .Where(p => p.Descripcion == "cliente")
                .Select(p => p.IdPerfil)
                .FirstOrDefaultAsync();

            // Buscar persona por documento que tenga un usuario activo con rol cliente
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                .FirstOrDefaultAsync(p =>
                    p.IdPersona == documento &&
                    p.Usuarios.Any(u =>
                        u.Estado == "Activo" &&
                        u.UsuarioPerfils.Any(up => up.IdPerfil == idPerfilCliente)));

            if (persona == null)
            {
                return Json(new
                {
                    encontrado = false,
                    mensaje = "No se encontró un paciente con ese documento o no tiene usuario activo."
                });
            }

            var usuario = persona.Usuarios
                .First(u => u.Estado == "Activo" &&
                            u.UsuarioPerfils.Any(up => up.IdPerfil == idPerfilCliente));

            var nombreCompleto = string.Join(" ",
                new[]
                {
                    persona.PrimerNombre,
                    persona.SegundoNombre,
                    persona.PrimerApellido,
                    persona.SegundoApellido
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return Json(new
            {
                encontrado = true,
                idUsuario = usuario.IdUsuario,
                nombre = nombreCompleto,
                telefono = persona.Telefono,
                correo = persona.Correo
            });
        }

        // ============================
        // LISTA
        // ============================

        public async Task<IActionResult> Index()
        {
            // Solo historias activas (o sin estado, por compatibilidad)
            var historias = await _context.Historiaclinicas
                .Where(h => h.Estado != "Inactiva" || h.Estado == null)
                .OrderByDescending(h => h.FechaCreacion)
                .ToListAsync();

            // Obtener Ids de usuarios
            var idsUsuarios = historias
                .SelectMany(h => new int?[] { h.IdUsuarioPaciente, h.IdUsuarioOptometra })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var usuarios = await _context.Usuarios
                .Include(u => u.IdPersonaNavigation)
                .Where(u => idsUsuarios.Contains(u.IdUsuario))
                .ToListAsync();

            var dictPersonas = usuarios
                .Where(u => u.IdPersonaNavigation != null)
                .ToDictionary(u => u.IdUsuario, u => u.IdPersonaNavigation!);

            ViewBag.PersonasPorUsuario = dictPersonas;

            return View(historias);
        }




        // ============================
        // CREAR - GET
        // ============================
        public async Task<IActionResult> Crear()
        {
            // Usuario actual (si es optómetra, lo usaremos)
            var usuarioActual = await GetUsuarioActualAsync();

            int? optometraSeleccionado = null;
            string optometraNombreActual = "";

            if (User.IsInRole("optometra") &&
                usuarioActual != null &&
                usuarioActual.IdPersonaNavigation != null)
            {
                optometraSeleccionado = usuarioActual.IdUsuario;

                var p = usuarioActual.IdPersonaNavigation;
                optometraNombreActual = string.Join(" ",
                    new[]
                    {
                        p.PrimerNombre,
                        p.SegundoNombre,
                        p.PrimerApellido,
                        p.SegundoApellido
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            // Cargar combos (para admin siguen apareciendo todos los optómetras)
            await CargarCombosAsync(null, optometraSeleccionado);

            // Enviar nombre del optómetra actual a la vista
            ViewBag.OptometraActualNombre = optometraNombreActual;

            var modelo = new Historiaclinica
            {
                FechaCreacion = DateOnly.FromDateTime(DateTime.Today),
                IdUsuarioOptometra = optometraSeleccionado
            };

            return View(modelo);
        }

        // ============================
        // CREAR - POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Historiaclinica model)
        {
            // Si el usuario logueado es optómetra, forzamos su Id en la historia
            var usuarioActual = await GetUsuarioActualAsync();
            if (User.IsInRole("optometra") && usuarioActual != null)
            {
                model.IdUsuarioOptometra = usuarioActual.IdUsuario;
            }

            // Validaciones básicas
            if (model.IdUsuarioPaciente == null || model.IdUsuarioPaciente == 0)
            {
                ModelState.AddModelError("IdUsuarioPaciente", "Debe seleccionar un paciente.");
            }

            if (model.IdUsuarioOptometra == null || model.IdUsuarioOptometra == 0)
            {
                ModelState.AddModelError("IdUsuarioOptometra", "Debe seleccionar un optómetra.");
            }

            // Fecha por defecto si viene nula
            if (!model.FechaCreacion.HasValue)
            {
                model.FechaCreacion = DateOnly.FromDateTime(DateTime.Today);
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model.IdUsuarioPaciente, model.IdUsuarioOptometra);

                if (string.IsNullOrEmpty(model.Estado))
                {
                    model.Estado = "Activa";
                }

                // Si es optómetra, volvemos a mandar su nombre a la vista
                string optometraNombreActual = "";
                if (User.IsInRole("optometra") && usuarioActual?.IdPersonaNavigation != null)
                {
                    var p = usuarioActual.IdPersonaNavigation;
                    optometraNombreActual = string.Join(" ",
                        new[]
                        {
                            p.PrimerNombre,
                            p.SegundoNombre,
                            p.PrimerApellido,
                            p.SegundoApellido
                        }.Where(s => !string.IsNullOrWhiteSpace(s)));
                }
                ViewBag.OptometraActualNombre = optometraNombreActual;

                return View(model);
            }

            _context.Historiaclinicas.Add(model);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Historia clínica creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // EDITAR - GET
        // ============================
        public async Task<IActionResult> Editar(int id)
        {
            var historia = await _context.Historiaclinicas
                .FirstOrDefaultAsync(h => h.IdHistoriaclinica == id);

            if (historia == null)
                return NotFound();

            await CargarCombosAsync(historia.IdUsuarioPaciente, historia.IdUsuarioOptometra);
            return View(historia);
        }

        // ============================
        // EDITAR - POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Historiaclinica model)
        {
            if (id != model.IdHistoriaclinica)
                return NotFound();

            if (model.IdUsuarioPaciente == null || model.IdUsuarioPaciente == 0)
            {
                ModelState.AddModelError("IdUsuarioPaciente", "Debe seleccionar un paciente.");
            }

            if (model.IdUsuarioOptometra == null || model.IdUsuarioOptometra == 0)
            {
                ModelState.AddModelError("IdUsuarioOptometra", "Debe seleccionar un optómetra.");
            }

            if (!ModelState.IsValid)
            {
                await CargarCombosAsync(model.IdUsuarioPaciente, model.IdUsuarioOptometra);
                return View(model);
            }

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                bool existe = await _context.Historiaclinicas
                    .AnyAsync(h => h.IdHistoriaclinica == model.IdHistoriaclinica);

                if (!existe)
                    return NotFound();

                throw;
            }

            TempData["Mensaje"] = "Historia clínica actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // DETALLES
        // ============================
        public async Task<IActionResult> Detalles(int id)
        {
            var historia = await _context.Historiaclinicas
                .FirstOrDefaultAsync(h => h.IdHistoriaclinica == id);

            if (historia == null)
                return NotFound();

            // =======================
            // Datos del paciente
            // =======================
            string pacienteNombre = "";
            long? pacienteDocumento = null;
            string pacienteTelefono = "";
            string pacienteCorreo = "";

            if (historia.IdUsuarioPaciente.HasValue)
            {
                var usuarioPaciente = await _context.Usuarios
                    .Include(u => u.IdPersonaNavigation)
                    .FirstOrDefaultAsync(u => u.IdUsuario == historia.IdUsuarioPaciente.Value);

                if (usuarioPaciente?.IdPersonaNavigation != null)
                {
                    var p = usuarioPaciente.IdPersonaNavigation;
                    pacienteDocumento = p.IdPersona;

                    pacienteNombre = string.Join(" ",
                        new[]
                        {
                            p.PrimerNombre,
                            p.SegundoNombre,
                            p.PrimerApellido,
                            p.SegundoApellido
                        }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    pacienteTelefono = p.Telefono?.ToString() ?? "";
                    pacienteCorreo = p.Correo?.ToString() ?? "";
                }
            }

            // =======================
            // Datos del optómetra
            // =======================
            string optometraNombre = "";

            if (historia.IdUsuarioOptometra.HasValue)
            {
                var usuarioOptometra = await _context.Usuarios
                    .Include(u => u.IdPersonaNavigation)
                    .FirstOrDefaultAsync(u => u.IdUsuario == historia.IdUsuarioOptometra.Value);

                if (usuarioOptometra?.IdPersonaNavigation != null)
                {
                    var o = usuarioOptometra.IdPersonaNavigation;
                    optometraNombre = string.Join(" ",
                        new[]
                        {
                            o.PrimerNombre,
                            o.SegundoNombre,
                            o.PrimerApellido,
                            o.SegundoApellido
                        }.Where(s => !string.IsNullOrWhiteSpace(s)));
                }
            }

            ViewBag.PacienteNombre = pacienteNombre;
            ViewBag.PacienteDocumento = pacienteDocumento;
            ViewBag.PacienteTelefono = pacienteTelefono;
            ViewBag.PacienteCorreo = pacienteCorreo;

            ViewBag.OptometraNombre = optometraNombre;

            return View(historia);
        }

        // ============================
        // ELIMINAR - GET (confirmación)
        // ============================
        public async Task<IActionResult> Eliminar(int id)
        {
            var historia = await _context.Historiaclinicas
                .FirstOrDefaultAsync(h => h.IdHistoriaclinica == id);

            if (historia == null)
                return NotFound();

            return View(historia);
        }

        // ============================
        // ELIMINAR - POST (inactivar)
        // ============================
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var historia = await _context.Historiaclinicas
                .FirstOrDefaultAsync(h => h.IdHistoriaclinica == id);

            if (historia == null)
                return NotFound();

            // En vez de borrar, inactivamos
            historia.Estado = "Inactiva";
            _context.Historiaclinicas.Update(historia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Historia clínica inactivada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        // ============================
        // LISTA DE HISTORIAS INACTIVAS
        // ============================
        public async Task<IActionResult> Inactivas()
        {
            var historias = await _context.Historiaclinicas
                .Where(h => h.Estado == "Inactiva")
                .OrderByDescending(h => h.FechaCreacion)
                .ToListAsync();

            // Reutilizamos la misma lógica de diccionario
            var idsUsuarios = historias
                .SelectMany(h => new int?[] { h.IdUsuarioPaciente, h.IdUsuarioOptometra })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var usuarios = await _context.Usuarios
                .Include(u => u.IdPersonaNavigation)
                .Where(u => idsUsuarios.Contains(u.IdUsuario))
                .ToListAsync();

            var dictPersonas = usuarios
                .Where(u => u.IdPersonaNavigation != null)
                .ToDictionary(u => u.IdUsuario, u => u.IdPersonaNavigation!);

            ViewBag.PersonasPorUsuario = dictPersonas;

            return View(historias);
        }
        // ============================
        // RESTAURAR HISTORIA INACTIVA
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restaurar(int id)
        {
            var historia = await _context.Historiaclinicas
                .FirstOrDefaultAsync(h => h.IdHistoriaclinica == id);

            if (historia == null)
                return NotFound();

            historia.Estado = "Activa";
            _context.Historiaclinicas.Update(historia);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Historia clínica restaurada correctamente.";
            return RedirectToAction(nameof(Inactivas));
        }

    }
}