using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador")]
    public class PerfilControlador : Controller
    {
        private readonly ProyectoopticaContext _dbContext;

        public PerfilControlador(ProyectoopticaContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Acción para obtener la lista de perfiles
        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            List<Perfil> perfiles = await _dbContext.Perfiles.ToListAsync();  // Asegúrate de usar 'Perfiles' y no 'Perfils'
            return View(perfiles);  // Pasa los datos a la vista (una lista de Perfiles)
        }

        // Acción para mostrar el formulario de nuevo perfil
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();  // Pasa un objeto Perfil vacío a la vista
        }

        // Acción para guardar el nuevo perfil
        [HttpPost]
        public async Task<IActionResult> Nuevo(Perfil perfil)
        {
            await _dbContext.Perfiles.AddAsync(perfil);  // Agregar nuevo perfil
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Lista));
        }

        // Acción para obtener un perfil específico y mostrarlo en el formulario de edición
        [HttpGet]
        public async Task<IActionResult> editar(int id)
        {
            Perfil perfil = await _dbContext.Perfiles.FirstOrDefaultAsync(e => e.IdPerfil == id); // Mejor usar FirstOrDefault en lugar de FirstAsync para evitar errores si no se encuentra el perfil
            if (perfil == null)
            {
                return NotFound();  // Si no se encuentra el perfil, retorna 404
            }
            return View(perfil);
        }

        // Acción para guardar las actualizaciones del perfil
        [HttpPost]
        public async Task<IActionResult> editar(Perfil perfil)
        {
            if (ModelState.IsValid)  // Verifica si el modelo es válido antes de proceder
            {
                _dbContext.Perfiles.Update(perfil);  // Actualiza el perfil
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Lista));  // Redirige a la lista de perfiles
            }
            return View(perfil);  // Si hay errores en el modelo, vuelve a mostrar el formulario con los errores
        }
        [HttpGet]
        public async Task<IActionResult> eliminar(int id)
        {
            Perfil perfil = await _dbContext.Perfiles.FirstAsync(p => p.IdPerfil == id);
            _dbContext.Perfiles.Remove(perfil);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Lista));
        }

        // ✅ Exportar a Excel (CORREGIDO Y GARANTIZANDO NEGRITA EN EL ENCABEZADO)
        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            var proveedores = await _dbContext.Proveedors.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Proveedores");
                var currentRow = 1;

                // 1. Definición de Encabezados
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Nombre";
                worksheet.Cell(currentRow, 3).Value = "Dirección";
                worksheet.Cell(currentRow, 4).Value = "Teléfono";
                worksheet.Cell(currentRow, 5).Value = "Correo";

                // ✨ Mejora: Aplicar estilos a la Fila 1 (Encabezados) de forma explícita
                worksheet.Row(currentRow).Style.Font.Bold = true;
                worksheet.Row(currentRow).Style.Fill.BackgroundColor = XLColor.LightGray;

                // 2. Datos
                foreach (var p in proveedores)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = p.IdProveedorNit;
                    worksheet.Cell(currentRow, 2).Value = p.Nombre;
                    worksheet.Cell(currentRow, 3).Value = p.Direccion;
                    worksheet.Cell(currentRow, 4).Value = p.Telefono;
                    worksheet.Cell(currentRow, 5).Value = p.Correo;
                }

                // 3. Ajustar y devolver
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Proveedores.xlsx"
                    );
                }
            }
        }
    }
    
}




