using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador")]
    public class CompraController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public CompraController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTAR COMPRAS
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var compras = await _context.Compras
                .Include(c => c.ProveedorCompras)
                    .ThenInclude(pc => pc.IdProveedorNitNavigation)
                .Include(c => c.ProductoCompras)
                    .ThenInclude(pc => pc.IdProductoNavigation)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();

            return View(compras);
        }

        // =====================================================
        // CREAR COMPRA - GET
        // =====================================================
        public async Task<IActionResult> Crear()
        {
            ViewBag.Proveedores = new SelectList(
                await _context.Proveedors.ToListAsync(),
                "IdProveedorNit",
                "Nombre"
            );

            ViewBag.Productos = new SelectList(
                await _context.Productos.ToListAsync(),
                "IdProducto",
                "Nombre"
            );

            return View();
        }

        // =====================================================
        // CREAR COMPRA - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            DateOnly fechaCompra,
            int proveedorId,
            List<int> productoId,
            List<int> cantidad)
        {
            if (productoId == null || cantidad == null || productoId.Count == 0)
            {
                TempData["Error"] = "Debe agregar al menos un producto.";
                return RedirectToAction(nameof(Crear));
            }

            // 1. Crear la compra
            var compra = new Compra
            {
                FechaCompra = fechaCompra
            };

            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();

            // 2. Registrar proveedor_compra
            var proveedorCompra = new ProveedorCompra
            {
                IdCompra = compra.IdCompra,
                IdProveedorNit = proveedorId
            };

            _context.ProveedorCompras.Add(proveedorCompra);

            // 3. Registrar productos en ProductoCompra + actualizar stock
            for (int i = 0; i < productoId.Count; i++)
            {
                int idProd = productoId[i];
                int cant = cantidad[i];

                var prod = await _context.Productos.FirstOrDefaultAsync(p => p.IdProducto == idProd);
                if (prod == null) continue;

                // Registrar detalle de compra
                var detalle = new ProductoCompra
                {
                    IdCompra = compra.IdCompra,
                    IdProducto = idProd,
                    Cantidad = cant
                };
                _context.ProductoCompras.Add(detalle);

                // Actualizar stock
                prod.Stock = (prod.Stock ?? 0) + cant;
                prod.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);

                _context.Productos.Update(prod);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Compra registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
