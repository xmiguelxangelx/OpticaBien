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
    [Authorize(Roles = "administrador,empleado")]
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
        // LISTADO DE VENTAS (CON FILTRO POR ID)
        // ============================
        [HttpGet]
        public async Task<IActionResult> Index(int? idBusqueda)
        {
            var usuarioActual = await GetUsuarioActualAsync();

            // Base: todas las ventas con sus relaciones
            var query = _context.Venta
                .Include(v => v.IdUsuariopacienteNavigation)
                    .ThenInclude(u => u.IdPersonaNavigation)
                .Include(v => v.IdUsuarioempleadoNavigation)
                .Include(v => v.ProductoVenta)
                    .ThenInclude(pv => pv.IdProductoNavigation)
                .AsQueryable();

            // Si es empleado, filtramos solo sus ventas
            if (User.IsInRole("empleado") && usuarioActual != null)
            {
                query = query.Where(v => v.IdUsuarioempleado == usuarioActual.IdUsuario);
            }

            // Filtro por ID de venta
            if (idBusqueda.HasValue)
            {
                query = query.Where(v => v.IdVenta == idBusqueda.Value);
            }

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            // ==========
            // PAGOS POR VENTA
            // ==========
            var idsVentas = ventas.Select(v => v.IdVenta).ToList();

            var pagosPorVenta = await _context.VentaPagos
                .Where(p => p.IdVenta.HasValue && idsVentas.Contains(p.IdVenta.Value))
                .GroupBy(p => p.IdVenta)
                .Select(g => new
                {
                    IdVenta = g.Key,
                    TotalAbonado = g.Sum(x => x.Monto ?? 0)
                })
                .ToListAsync();

            ViewBag.TotalAbonadoPorVenta = pagosPorVenta
                .Where(x => x.IdVenta.HasValue)
                .ToDictionary(x => x.IdVenta!.Value, x => x.TotalAbonado);

            ViewBag.IdBusqueda = idBusqueda;

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
                TempData["ErrorVenta"] = "No se pudo obtener el usuario empleado actual.";
                return RedirectToAction(nameof(Crear));
            }

            if (productoId == null || cantidad == null || productoId.Count == 0)
            {
                TempData["ErrorVenta"] = "Debe agregar al menos un producto a la venta.";
                return RedirectToAction(nameof(Crear));
            }

            if (total <= 0)
            {
                TempData["ErrorVenta"] = "El total de la venta debe ser mayor a 0.";
                return RedirectToAction(nameof(Crear));
            }

            if (abono < 0)
            {
                TempData["ErrorVenta"] = "El abono no puede ser negativo.";
                return RedirectToAction(nameof(Crear));
            }

            if (abono > total)
            {
                TempData["ErrorVenta"] = "El abono no puede ser mayor que el total.";
                return RedirectToAction(nameof(Crear));
            }

            // 1️⃣ Validar stock de todos los productos antes de registrar la venta
            for (int i = 0; i < productoId.Count; i++)
            {
                int idProd = productoId[i];
                int cant = cantidad[i];

                if (cant <= 0)
                {
                    TempData["ErrorVenta"] = "Las cantidades deben ser mayores que 0.";
                    return RedirectToAction(nameof(Crear));
                }

                var prod = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == idProd);
                if (prod == null)
                    continue;

                int stockActual = prod.Stock ?? 0;

                if (stockActual < cant)
                {
                    TempData["ErrorVenta"] =
                        $"No hay stock suficiente para el producto {prod.Nombre}. Stock actual: {stockActual}.";
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

                var prod = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == idProd);
                if (prod == null)
                    continue;

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

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Venta registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // CANCELAR / ELIMINAR VENTA
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            // Usa aquí el mismo DbSet que tengas definido en el DbContext.
            // En tu caso es public virtual DbSet<Ventum> Venta { get; set; }
            var venta = await _context.Venta.FindAsync(id);
            if (venta == null)
                return NotFound();

            // Por ahora la eliminamos físicamente.
            _context.Venta.Remove(venta);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La venta fue eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // BUSCAR CLIENTE POR DOCUMENTO (IdPersona)
        // ============================
        [HttpGet]
        public async Task<IActionResult> BuscarClientePorDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return Json(new { encontrado = false });

            // Intentamos convertir el documento a long (porque IdPersona es long)
            if (!long.TryParse(documento, out long docNumber))
                return Json(new { encontrado = false });

            // Aquí el documento ES el IdPersona
            var persona = await _context.Personas
                .FirstOrDefaultAsync(p => p.IdPersona == docNumber);

            if (persona == null)
                return Json(new { encontrado = false });

            // Buscamos el usuario ligado a esa persona
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdPersona == persona.IdPersona);

            if (usuario == null)
                return Json(new { encontrado = false });

            // Devolvemos datos básicos
            return Json(new
            {
                encontrado = true,
                idUsuario = usuario.IdUsuario,
                nombreCompleto = $"{persona.PrimerNombre} {persona.SegundoNombre} {persona.PrimerApellido} {persona.SegundoApellido}".Trim(),
                correo = persona.Correo,
                telefono = persona.Telefono?.ToString()
            });
        }

        // ============================
        // BUSCAR PRODUCTO POR CÓDIGO (IdProducto)
        // ============================
        [HttpGet]
        public async Task<IActionResult> BuscarProductoPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return Json(new { encontrado = false });

            // Suponemos que el código es el IdProducto (int)
            if (!int.TryParse(codigo, out int idProducto))
                return Json(new { encontrado = false });

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.IdProducto == idProducto
                                       && (p.Estado == "Activo" || p.Estado == null));

            if (producto == null)
                return Json(new { encontrado = false });

            return Json(new
            {
                encontrado = true,
                idProducto = producto.IdProducto,
                nombre = producto.Nombre,
                precio = producto.Precio ?? 0,
                stock = producto.Stock ?? 0
            });
        }
    }
}
