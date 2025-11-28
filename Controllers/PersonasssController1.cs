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
    [Authorize(Roles = "administrador")]
    public class PersonasssController1 : Controller
    {
        private readonly ProyectoopticaContext _context;

        public PersonasssController1(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ==========================================
        // LISTA PRINCIPAL → SOLO PERSONAS CON USUARIO ACTIVO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            var personas = await _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                        .ThenInclude(up => up.IdPerfilNavigation)
                .Where(p => p.Usuarios.Any(u => u.Estado == "Activo"))
                .ToListAsync();

            // Todos los perfiles para armar el combo de roles en la vista
            ViewBag.Perfiles = await _context.Perfiles.ToListAsync();

            return View(personas);
        }

        // ==========================================
        // LISTA DE PERSONAS INACTIVAS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Inactivos()
        {
            var personas = await _context.Personas
                .Include(p => p.Usuarios)
                .Where(p => p.Usuarios.Any(u => u.Estado == "Inactivo"))
                .ToListAsync();

            return View(personas);
        }

        // ==========================================
        // NUEVA PERSONA (solo datos básicos, sin usuario)
        // ==========================================
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Nuevo(Persona persona, string nombreUsuario, string clave)
        {
            if (!ModelState.IsValid)
                return View(persona);

            // 1. Guardar Persona
            _context.Personas.Add(persona);
            await _context.SaveChangesAsync();

            // 2. Crear Usuario ligado a la persona
            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Clave = clave,
                IdPersona = persona.IdPersona
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // 3. Asignar rol "cliente"
            var rolCliente = _context.Perfiles.FirstOrDefault(p => p.Descripcion == "cliente");

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



        // ==========================================
        // EDITAR PERSONA
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Editar(long id)
        {
            var persona = await _context.Personas
                .FirstOrDefaultAsync(e => e.IdPersona == id);

            if (persona == null)
                return NotFound();

            return View(persona);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Persona persona)
        {
            if (!ModelState.IsValid)
                return View(persona);

            _context.Personas.Update(persona);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        // ==========================================
        // CAMBIAR ROL DESDE EL DESPLEGABLE EN LA LISTA
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(long idPersona, int idPerfil)
        {
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                    .ThenInclude(u => u.UsuarioPerfils)
                .FirstOrDefaultAsync(p => p.IdPersona == idPersona);

            if (persona == null)
                return NotFound();

            // Tomamos el usuario ACTIVO de esa persona
            var usuario = persona.Usuarios.FirstOrDefault(u => u.Estado == "Activo");
            if (usuario == null)
                return NotFound();

            // Si ya tiene usuario_perfil lo actualizamos, si no lo creamos
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
        // INACTIVAR PERSONA 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Inactivar(long id)
        {
            var persona = await _context.Personas
                .Include(p => p.Usuarios)
                .FirstOrDefaultAsync(e => e.IdPersona == id);

            if (persona == null)
                return NotFound();

            // Marcamos todos sus usuarios como inactivos
            foreach (var usuario in persona.Usuarios)
            {
                usuario.Estado = "Inactivo";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }


        // ==========================================
        // REACTIVAR PERSONA (DESDE LA LISTA DE INACTIVOS)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        // EXPORTAR A EXCEL (puedes filtrar solo activos si quieres)
        // ==========================================
        [HttpGet]
        public IActionResult ExportarExcel()
        {
            var personas = _context.Personas.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Personas");
                var currentRow = 1;

                // Encabezados
                worksheet.Cell(currentRow, 1).Value = "Cédula";
                worksheet.Cell(currentRow, 2).Value = "Primer Nombre";
                worksheet.Cell(currentRow, 3).Value = "Segundo Nombre";
                worksheet.Cell(currentRow, 4).Value = "Primer Apellido";
                worksheet.Cell(currentRow, 5).Value = "Segundo Apellido";
                worksheet.Cell(currentRow, 6).Value = "Correo";
                worksheet.Cell(currentRow, 7).Value = "Teléfono";
                worksheet.Cell(currentRow, 8).Value = "Dirección";
                worksheet.Cell(currentRow, 9).Value = "Fecha Nacimiento";

                // Datos
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
