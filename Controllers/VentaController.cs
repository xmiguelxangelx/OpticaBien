using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "administrador")]
    public class VentaController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public VentaController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ============================
        // HELPER: USUARIO ACTUAL
        // ============================
        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        // ============================
        // LISTAR VENTAS
        // ============================
        public async Task<IActionResult> Index()
        {
            // Traer ventas con cliente, empleado y productos
            var ventas = await _context.Venta
                .Include(v => v.IdUsuariopacienteNavigation)
                .Include(v => v.IdUsuarioempleadoNavigation)
                .Include(v => v.ProductoVenta)
                    .ThenInclude(pv => pv.IdProductoNavigation)
                .ToListAsync();

            // Traer pagos agrupados por venta
            var pagosPorVenta = await _context.VentaPagos
                .GroupBy(p => p.IdVenta)
                .Select(g => new
                {
                    IdVenta = g.Key,
                    TotalAbonado = g.Sum(x => x.Monto ?? 0)
                })
                .ToListAsync();

            // Pasar un diccionario VentaId -> TotalAbonado a la vista
            ViewBag.TotalAbonadoPorVenta = pagosPorVenta
                .Where(x => x.IdVenta.HasValue)
                .ToDictionary(x => x.IdVenta!.Value, x => x.TotalAbonado);

            return View(ventas);
        
        }

        // ============================
        // CREAR VENTA - GET
        // ============================
        public async Task<IActionResult> Crear()
        {
            // Clientes: por ahora todos los usuarios (luego podemos filtrar solo rol "cliente")
            ViewBag.Clientes = new SelectList(
                await _context.Usuarios.ToListAsync(),
                "IdUsuario",
                "NombreUsuario"
            );

            ViewBag.Productos = new SelectList(
                await _context.Productos.ToListAsync(),
                "IdProducto",
                "Nombre"
            );

            return View();
        }

        // ============================
        // CREAR VENTA - POST
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            DateOnly fecha,
            DateOnly? fechaEntrega,
            int idUsuariopaciente,
            float total,
            float abono,
            List<int> productoId,
            List<int> cantidad)
        {
            var empleado = await GetUsuarioActualAsync();
            if (empleado == null)
            {
                TempData["Error"] = "No se pudo obtener el usuario empleado actual.";
                return RedirectToAction(nameof(Crear));
            }

            if (productoId == null || cantidad == null || productoId.Count == 0)
            {
                TempData["Error"] = "Debe agregar al menos un producto a la venta.";
                return RedirectToAction(nameof(Crear));
            }

            if (total <= 0)
            {
                TempData["Error"] = "El total de la venta debe ser mayor a 0.";
                return RedirectToAction(nameof(Crear));
            }

            if (abono < 0)
            {
                TempData["Error"] = "El abono no puede ser negativo.";
                return RedirectToAction(nameof(Crear));
            }

            if (abono > total)
            {
                TempData["Error"] = "El abono no puede ser mayor que el total.";
                return RedirectToAction(nameof(Crear));
            }

            // 1️⃣ Validar stock de todos los productos antes de registrar la venta
            for (int i = 0; i < productoId.Count; i++)
            {
                int idProd = productoId[i];
                int cant = cantidad[i];

                if (cant <= 0)
                {
                    TempData["Error"] = "Las cantidades deben ser mayores que 0.";
                    return RedirectToAction(nameof(Crear));
                }

                var prod = await _context.Productos.FirstOrDefaultAsync(p => p.IdProducto == idProd);
                if (prod == null)
                    continue;

                int stockActual = prod.Stock ?? 0;

                if (stockActual < cant)
                {
                    TempData["Error"] = $"No hay stock suficiente para el producto {prod.Nombre}. Stock actual: {stockActual}.";
                    return RedirectToAction(nameof(Crear));
                }
            }

            // 2️⃣ Crear la venta
            var venta = new Ventum
            {
                Fecha = fecha,
                FechaEntrega = fechaEntrega,
                Total = total,
                Abono = abono,
                IdUsuariopaciente = idUsuariopaciente,
                IdUsuarioempleado = empleado.IdUsuario
            };

            _context.Venta.Add(venta);
            await _context.SaveChangesAsync();

            // 3️⃣ Registrar productos vendidos + actualizar stock
            for (int i = 0; i < productoId.Count; i++)
            {
                int idProd = productoId[i];
                int cant = cantidad[i];

                var prod = await _context.Productos.FirstOrDefaultAsync(p => p.IdProducto == idProd);
                if (prod == null) continue;

                var detalle = new ProductoVentum
                {
                    IdVenta = venta.IdVenta,
                    IdProducto = idProd,
                    Cantidad = cant
                };

                _context.ProductoVenta.Add(detalle);

                prod.Stock = (prod.Stock ?? 0) - cant;
                prod.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);
                _context.Productos.Update(prod);
            }

            // 🔹 Aquí podríamos crear un registro en VentaPagos con el abono inicial,
            // pero como no vimos el modelo VentaPago, por ahora solo guardamos el Abono en Ventum.
            // Más adelante, cuando me pases VentaPago.cs, dejamos esto full.

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Venta registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
