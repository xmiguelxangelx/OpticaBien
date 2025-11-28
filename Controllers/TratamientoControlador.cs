using ClosedXML.Excel;   
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador,optometra")]
    public class TratamientoControlador : Controller
    {
        private readonly ProyectoopticaContext _dbContext;
        public TratamientoControlador(ProyectoopticaContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            List<Tratamiento> lista = await _dbContext.Tratamientos.ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Nuevo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Nuevo(Tratamiento tratamiento)
        {
            await _dbContext.Tratamientos.AddAsync(tratamiento);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Tratamiento tratamiento = await _dbContext.Tratamientos.FirstAsync(t => t.IdTratamiento == id);
            return View(tratamiento);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Tratamiento tratamiento)
        {
            _dbContext.Tratamientos.Update(tratamiento);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            Tratamiento tratamiento = await _dbContext.Tratamientos.FirstAsync(t => t.IdTratamiento == id);

            _dbContext.Tratamientos.Remove(tratamiento);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        // 📌 NUEVO: Exportar a Excel
        [HttpGet]
        public async Task<IActionResult> ExportarExcel()
        {
            var tratamientos = await _dbContext.Tratamientos.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Tratamientos");
                var fila = 1;

                // Encabezados
                worksheet.Cell(fila, 1).Value = "Id Tratamiento";
                worksheet.Cell(fila, 2).Value = "Fecha Edición";
                worksheet.Cell(fila, 3).Value = "Detalles Tratamiento";
                worksheet.Cell(fila, 4).Value = "Indicaciones";
                worksheet.Cell(fila, 5).Value = "Id Diagnóstico";

                // Datos
                foreach (var t in tratamientos)
                {
                    fila++;
                    worksheet.Cell(fila, 1).Value = t.IdTratamiento;
                    worksheet.Cell(fila, 2).Value = t.FechaEdicion?.ToString("yyyy-MM-dd");
                    worksheet.Cell(fila, 3).Value = t.DetallesTratamiento;
                    worksheet.Cell(fila, 4).Value = t.Indicaciones;
                    worksheet.Cell(fila, 5).Value = t.IdDiagnostico;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Tratamientos.xlsx"
                    );
                }
            }
        }
    }
}