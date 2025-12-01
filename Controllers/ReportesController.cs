using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System.IO;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador")]
    public class ReportesController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public ReportesController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ======================================
        // REPORTE DE VENTAS DETALLADAS
        // ======================================
        public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta, bool exportar = false)
        {
            var query = _context.VwVentasDetalladas.AsQueryable();

            if (desde.HasValue)
            {
                var d = DateOnly.FromDateTime(desde.Value);
                query = query.Where(v => v.Fecha >= d);
            }

            if (hasta.HasValue)
            {
                var h = DateOnly.FromDateTime(hasta.Value);
                query = query.Where(v => v.Fecha <= h);
            }

            var lista = await query
                .OrderBy(v => v.Fecha)
                .ThenBy(v => v.IdVenta)
                .ToListAsync();

            // Si piden exportar a Excel
            if (exportar)
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Ventas");

                // Encabezados
                ws.Cell(1, 1).Value = "ID Venta";
                ws.Cell(1, 2).Value = "Fecha";
                ws.Cell(1, 3).Value = "Cliente";
                ws.Cell(1, 4).Value = "Empleado";
                ws.Cell(1, 5).Value = "Producto";
                ws.Cell(1, 6).Value = "Cantidad";
                ws.Cell(1, 7).Value = "Precio";
                ws.Cell(1, 8).Value = "Subtotal";
                ws.Cell(1, 9).Value = "Abono";
                ws.Cell(1, 10).Value = "Total";
                ws.Cell(1, 11).Value = "Fecha entrega";

                int fila = 2;
                foreach (var v in lista)
                {
                    ws.Cell(fila, 1).Value = v.IdVenta;
               
                    ws.Cell(fila, 2).Value = v.Fecha?.ToString("yyyy-MM-dd");
                    ws.Cell(fila, 3).Value = $"{v.NombreCliente} {v.ApellidoCliente}";
                    ws.Cell(fila, 4).Value = $"{v.NombreEmpleado} {v.ApellidoEmpleado}";
                    ws.Cell(fila, 5).Value = v.Producto;
                    ws.Cell(fila, 6).Value = v.Cantidad;
                    ws.Cell(fila, 7).Value = v.Precio;
                    ws.Cell(fila, 8).Value = v.Subtotal;
                    ws.Cell(fila, 9).Value = v.Abono;
                    ws.Cell(fila, 10).Value = v.Total;
                    ws.Cell(fila, 11).Value = v.FechaEntrega?.ToString("yyyy-MM-dd");
                    fila++;
                }

                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                var content = stream.ToArray();

                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Reporte_Ventas.xlsx");
            }

            // Para la vista
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

            return View(lista);
        }
    }
}
