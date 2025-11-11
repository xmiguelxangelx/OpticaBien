using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.IO;



namespace Optica1.Controllers
{
    public class PersonasssController1 : Controller
    {
        private readonly ProyectoopticaContext _context;
        public PersonasssController1(ProyectoopticaContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            List<Persona> lista = await _context.Personas.ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Nuevo()
        {

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Nuevo(Persona persona)
        {

            await _context.Personas.AddAsync(persona);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Persona persona = await _context.Personas.FirstAsync(e => e.IdPersona == id);

            return View(persona);
        }


        [HttpPost]
        public async Task<IActionResult> Editar(Persona persona)
        {

            _context.Personas.Update(persona);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));
        }



        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            Persona persona = await _context.Personas.FirstAsync(e => e.IdPersona == id);
            _context.Personas.Remove(persona);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Lista));

        }

        [HttpGet]
        public IActionResult ExportarExcel()
        {
            // Supón que tienes un contexto de base de datos llamado _context
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

                // Ajuste de ancho automático
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Personas.xlsx");
                }
            }
        }

    }

    }



    
