using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using ClosedXML.Excel;
using System.IO;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador")]
    public class ProductoController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public ProductoController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ============================
        // LISTADO INVENTARIO (ACTIVOS)
        // ============================
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Include(p => p.IdProveedorNitNavigation)
                .Where(p => p.Estado == "Activo" || p.Estado == null)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View(productos);
        }

        // ============================
        // LISTADO INACTIVOS
        // ============================
        public async Task<IActionResult> Inactivos()
        {
            var productos = await _context.Productos
                .Include(p => p.IdProveedorNitNavigation)
                .Where(p => p.Estado == "Inactivo")
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View(productos);
        }

        // ============================
        // CREAR - GET
        // ============================
        public async Task<IActionResult> Crear()
        {
            ViewBag.Proveedores = new SelectList(
                await _context.Proveedors.ToListAsync(),
                "IdProveedorNit",
                "Nombre"
            );

            return View();
        }

        // ============================
        // CREAR - POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = new SelectList(
                    await _context.Proveedors.ToListAsync(),
                    "IdProveedorNit",
                    "Nombre",
                    model.IdProveedorNit
                );
                return View(model);
            }

            model.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);
            model.Estado = "Activo";

            _context.Productos.Add(model);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Producto creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // EDITAR - GET
        // ============================
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Proveedores = new SelectList(
                await _context.Proveedors.ToListAsync(),
                "IdProveedorNit",
                "Nombre",
                producto.IdProveedorNit
            );

            return View(producto);
        }

        // ============================
        // EDITAR - POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Producto model)
        {
            if (id != model.IdProducto) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Proveedores = new SelectList(
                    await _context.Proveedors.ToListAsync(),
                    "IdProveedorNit",
                    "Nombre",
                    model.IdProveedorNit
                );
                return View(model);
            }

            var productoDb = await _context.Productos.FindAsync(id);
            if (productoDb == null) return NotFound();

            // Actualizar campos
            productoDb.Nombre = model.Nombre;
            productoDb.Tipo = model.Tipo;
            productoDb.Marca = model.Marca;
            productoDb.Descripcion = model.Descripcion;
            productoDb.Precio = model.Precio;
            productoDb.Stock = model.Stock;
            productoDb.StockMinimo = model.StockMinimo;
            productoDb.IdProveedorNit = model.IdProveedorNit;
            productoDb.Estado = model.Estado;
            productoDb.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);

            _context.Productos.Update(productoDb);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // INACTIVAR
        // ============================
        public async Task<IActionResult> Inactivar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Estado = "Inactivo";
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Producto inactivado.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // ACTIVAR
        // ============================
        public async Task<IActionResult> Activar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Estado = "Activo";
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Producto activado.";
            return RedirectToAction(nameof(Inactivos));
        }

        // ============================
        // EXPORTAR INVENTARIO A EXCEL
        // ============================
        [HttpGet]
        public async Task<IActionResult> ExportarInventario()
        {
            var productos = await _context.Productos
                .Include(p => p.IdProveedorNitNavigation)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var hoja = workbook.Worksheets.Add("Inventario");

                // Encabezados
                hoja.Cell(1, 1).Value = "Nombre";
                hoja.Cell(1, 2).Value = "Tipo";
                hoja.Cell(1, 3).Value = "Marca";
                hoja.Cell(1, 4).Value = "Proveedor";
                hoja.Cell(1, 5).Value = "Precio";
                hoja.Cell(1, 6).Value = "Stock";
                hoja.Cell(1, 7).Value = "Stock mínimo";
                hoja.Cell(1, 8).Value = "Estado";
                hoja.Cell(1, 9).Value = "Fecha actualización";

                // Estilo encabezados
                var headerRange = hoja.Range(1, 1, 1, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int fila = 2;

                foreach (var p in productos)
                {
                    hoja.Cell(fila, 1).Value = p.Nombre;
                    hoja.Cell(fila, 2).Value = p.Tipo;
                    hoja.Cell(fila, 3).Value = p.Marca;
                    hoja.Cell(fila, 4).Value = p.IdProveedorNitNavigation?.Nombre;
                    hoja.Cell(fila, 5).Value = p.Precio;
                    hoja.Cell(fila, 6).Value = p.Stock ?? 0;
                    hoja.Cell(fila, 7).Value = p.StockMinimo ?? 0;
                    hoja.Cell(fila, 8).Value = string.IsNullOrEmpty(p.Estado) ? "Activo" : p.Estado;
                    hoja.Cell(fila, 9).Value = p.FechaActualizacion?.ToString("yyyy-MM-dd");

                    fila++;
                }

                hoja.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var nombreArchivo = $"InventarioProductos_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        nombreArchivo
                    );
                }
            }
        }

    }
}
