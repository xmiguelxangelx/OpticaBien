using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    // Empleado y administrador pueden entrar al controlador,
    // pero las acciones críticas se restringen abajo.
    [Authorize(Roles = "administrador,empleado")]
    public class PersonasssController1 : Controller
    {
        private readonly ProyectoopticaContext _context;

        public PersonasssController1(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ==========================================
        // LISTA PRINCIPAL → SOLO CLIENTES ACTIVOS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Lista(string buscar)
        {
            // Solo personas que tienen usuario ACTIVO con rol CLIENTE
            var query = _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                        .ThenInclude(up => up.IdPerfilNavigation)
                .Where(p => p.Usuarios.Any(u =>
                    u.Estado == "Activo" &&
                    u.UsuarioPerfils.Any(up =>
                        up.IdPerfilNavigation.Descripcion == "cliente")))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim().ToLower();

                query = query.Where(p =>
                    // Documento
                    p.IdPersona.ToString().Contains(buscar) ||

                    // Nombre completo
                    (
                        (p.PrimerNombre ?? "") + " " +
                        (p.SegundoNombre ?? "") + " " +
                        (p.PrimerApellido ?? "") + " " +
                        (p.SegundoApellido ?? "")
                    ).ToLower().Contains(buscar) ||

                    // Correo
                    (p.Correo ?? "").ToLower().Contains(buscar) ||

                    // Usuario (login)
                    p.Usuarios.Any(u => u.NombreUsuario.ToLower().Contains(buscar))
                );
            }

            ViewData["BuscarActual"] = buscar;
            ViewBag.Perfiles = await _context.Perfiles.ToListAsync();

            var personas = await query.ToListAsync();
            return View(personas);
        }

        // ==========================================
        // LISTA DE PERSONAS INACTIVAS (SOLO ADMIN)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> Inactivos()
        {
            var personas = await _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                        .ThenInclude(up => up.IdPerfilNavigation)
                .Where(p => p.Usuarios.Any(u =>
                    u.Estado == "Inactivo" &&
                    u.UsuarioPerfils.Any(up =>
                        up.IdPerfilNavigation.Descripcion == "cliente")))
                .ToListAsync();

            return View(personas);
        }

        // ==========================================
        // NUEVA PERSONA (SOLO ADMIN)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "administrador")]
        public IActionResult Nuevo()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> Nuevo(Persona persona, string nombreUsuario, string clave)
        {
            if (!ModelState.IsValid)
                return View(persona);

            _context.Personas.Add(persona);
            await _context.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Clave = clave,
                IdPersona = persona.IdPersona,
                Estado = "Activo"
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var rolCliente = await _context.Perfiles
                .FirstOrDefaultAsync(p => p.Descripcion == "cliente");

            if (rolCliente != null)
            {
                _context.UsuarioPerfils.Add(new UsuarioPerfil
                {
                    IdUsuario = usuario.IdUsuario,
                    IdPerfil = rolCliente.IdPerfil
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Lista));
        }

        // EDITAR PERSONA (ADMIN Y EMPLEADO)
        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Editar(long id)
        {
            var persona = await _context.Personas
                .FirstOrDefaultAsync(e => e.IdPersona == id);

            if (persona == null)
                return NotFound();

            return View(persona);
        }

        [HttpPost]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Editar(Persona persona)
        {
            if (!ModelState.IsValid)
                return View(persona);

            _context.Personas.Update(persona);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }


        // ==========================================
        // CAMBIAR ROL (SOLO ADMIN)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> CambiarRol(long idPersona, int idPerfil)
        {
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

            if (persona == null)
                return NotFound();

            var usuario = persona.Usuarios.FirstOrDefault(u => u.Estado == "Activo");
            if (usuario == null)
                return NotFound();

            var usuarioPerfil = usuario.UsuarioPerfils.FirstOrDefault();

            if (usuarioPerfil == null)
            {
                usuarioPerfil = new UsuarioPerfil
                {
                    IdUsuario = usuario.IdUsuario,
                    IdPerfil = idPerfil
                };
                _context.UsuarioPerfils.Add(usuarioPerfil);
            }
            else
            {
                usuarioPerfil.IdPerfil = idPerfil;
                _context.UsuarioPerfils.Update(usuarioPerfil);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Lista));
        }

        // ==========================================
        // INACTIVAR PERSONA (SOLO ADMIN)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> Inactivar(long id)
        {
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                .FirstOrDefaultAsync(e => e.IdPersona == id);

            if (persona == null)
                return NotFound();

            foreach (var usuario in persona.Usuarios)
            {
                usuario.Estado = "Inactivo";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        // ==========================================
        // REACTIVAR (SOLO ADMIN)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> Reactivar(long idPersona)
        {
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

            if (persona == null)
                return NotFound();

            foreach (var usuario in persona.Usuarios)
            {
                usuario.Estado = "Activo";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Inactivos));
        }

        // ==========================================
        // ELIMINAR DEFINITIVO (SOLO ADMIN)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador")]
        public async Task<IActionResult> EliminarDefinitivo(long idPersona)
        {
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

            if (persona == null)
                return NotFound();

            var idsUsuarios = persona.Usuarios
                .Select(u => u.IdUsuario)
                .ToList();

            var tieneCitas = await _context.Citas.AnyAsync(c =>
                (c.IdUsuarioempleado.HasValue && idsUsuarios.Contains(c.IdUsuarioempleado.Value)) ||
                (c.IdUsuariopaciente.HasValue && idsUsuarios.Contains(c.IdUsuariopaciente.Value))
            );

            var tieneVentas = await _context.Venta.AnyAsync(v =>
                (v.IdUsuarioempleado.HasValue && idsUsuarios.Contains(v.IdUsuarioempleado.Value)) ||
                (v.IdUsuariopaciente.HasValue && idsUsuarios.Contains(v.IdUsuariopaciente.Value))
            );

            if (tieneCitas || tieneVentas)
            {
                TempData["Error"] = "No se puede eliminar definitivamente este cliente porque tiene citas o ventas asociadas. Solo puede quedar inactivo.";
                return RedirectToAction(nameof(Inactivos));
            }

            foreach (var usuario in persona.Usuarios.ToList())
            {
                _context.UsuarioPerfils.RemoveRange(usuario.UsuarioPerfils);
                _context.Usuarios.Remove(usuario);
            }

            _context.Personas.Remove(persona);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "El cliente ha sido eliminado definitivamente.";
            return RedirectToAction(nameof(Inactivos));
        }

        // ==========================================
        // EXPORTAR A EXCEL (SOLO ADMIN)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "administrador")]
        public IActionResult ExportarExcel()
        {
            var personas = _context.Personas.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Personas");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "Cédula";
                worksheet.Cell(currentRow, 2).Value = "Primer Nombre";
                worksheet.Cell(currentRow, 3).Value = "Segundo Nombre";
                worksheet.Cell(currentRow, 4).Value = "Primer Apellido";
                worksheet.Cell(currentRow, 5).Value = "Segundo Apellido";
                worksheet.Cell(currentRow, 6).Value = "Correo";
                worksheet.Cell(currentRow, 7).Value = "Teléfono";
                worksheet.Cell(currentRow, 8).Value = "Dirección";
                worksheet.Cell(currentRow, 9).Value = "Fecha Nacimiento";

                foreach (var p in personas)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = p.IdPersona;
                    worksheet.Cell(currentRow, 2).Value = p.PrimerNombre;
                    worksheet.Cell(currentRow, 3).Value = p.SegundoNombre;
                    worksheet.Cell(currentRow, 4).Value = p.PrimerApellido;
                    worksheet.Cell(currentRow, 5).Value = p.SegundoApellido;
                    worksheet.Cell(currentRow, 6).Value = p.Correo;
                    worksheet.Cell(currentRow, 7).Value = p.Telefono;
                    worksheet.Cell(currentRow, 8).Value = p.Direccion;
                    worksheet.Cell(currentRow, 9).Value = p.FechaNacimiento?.ToString("yyyy-MM-dd");
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Personas.xlsx"
                    );
                }
            }
        }
    }
}
