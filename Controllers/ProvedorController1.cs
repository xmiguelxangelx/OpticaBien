using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.IO;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador,empleado")]
    public class ProvedorController1 : Controller
    {
        private readonly ProyectoopticaContext _dbContext;

        public ProvedorController1(ProyectoopticaContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ✅ Lista de proveedores
        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            List<Proveedor> lista = await _dbContext.Proveedors.ToListAsync();
            return View(lista);
        }

        // ✅ Crear nuevo
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Nuevo(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                await _dbContext.Proveedors.AddAsync(proveedor);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Lista));
            }
            return View(proveedor);
        }

        // ✅ Editar
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var proveedor = await _dbContext.Proveedors.FirstOrDefaultAsync(p => p.IdProveedorNit == id);
            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Proveedors.Update(proveedor);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Lista));
            }
            return View(proveedor);
        }

        // ✅ Eliminar
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var proveedor = await _dbContext.Proveedors.FirstOrDefaultAsync(p => p.IdProveedorNit == id);
            if (proveedor == null) return NotFound();

            _dbContext.Proveedors.Remove(proveedor);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        // ✅ Exportar a Excel
        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            var proveedores = await _dbContext.Proveedors.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Proveedores");
                var currentRow = 1;

                // Encabezados
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Nombre";
                worksheet.Cell(currentRow, 3).Value = "Dirección";
                worksheet.Cell(currentRow, 4).Value = "Teléfono";
                worksheet.Cell(currentRow, 5).Value = "Correo";

                // Datos
                foreach (var p in proveedores)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = p.IdProveedorNit;
                    worksheet.Cell(currentRow, 2).Value = p.Nombre;
                    worksheet.Cell(currentRow, 3).Value = p.Direccion;
                    worksheet.Cell(currentRow, 4).Value = p.Telefono;
                    worksheet.Cell(currentRow, 5).Value = p.Correo;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Proveedores.xlsx");
                }
            }
        }
    }
}