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
    [Authorize] // Autenticado en general, los roles se controlan por acción
    public class VentaController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public VentaController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ============================
        // HELPERS
        // ============================
        private async Task<Usuario> GetUsuarioActualAsync()
        {
            var userName = User.Identity?.Name;
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == userName);
        }

        private async Task<Dictionary<int, float>> GetPagosPorVentaAsync(List<int> idsVentas)
        {
            var resultado = new Dictionary<int, float>();

            if (idsVentas == null || !idsVentas.Any())
                return resultado;

            var pagosPorVenta = await _context.VentaPagos
                .Where(p => p.IdVenta.HasValue && idsVentas.Contains(p.IdVenta.Value))
                .GroupBy(p => p.IdVenta!.Value)
                .Select(g => new
                {
                    IdVenta = g.Key,
                    TotalAbonado = g.Sum(x => x.Monto ?? 0)
                })
                .ToListAsync();

            return pagosPorVenta.ToDictionary(x => x.IdVenta, x => x.TotalAbonado);
        }

        // ============================
        // LISTADO DE VENTAS (ADMIN/EMPLEADO)
        // ============================
        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Index(int? idBusqueda)
        {
            var usuarioActual = await GetUsuarioActualAsync();

            var query = _context.Venta
                .Include(v => v.IdUsuariopacienteNavigation)
                    .ThenInclude(u => u.IdPersonaNavigation)
                .Include(v => v.IdUsuarioempleadoNavigation)
                .Include(v => v.ProductoVenta)
                    .ThenInclude(pv => pv.IdProductoNavigation)
                .AsQueryable();

            // Si es empleado, solo ve sus ventas
            if (User.IsInRole("empleado") && usuarioActual != null)
            {
                query = query.Where(v => v.IdUsuarioempleado == usuarioActual.IdUsuario);
            }

            // Filtro por Id de venta
            if (idBusqueda.HasValue)
            {
                query = query.Where(v => v.IdVenta == idBusqueda.Value);
            }

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            var idsVentas = ventas.Select(v => v.IdVenta).ToList();
            var dictAbonos = await GetPagosPorVentaAsync(idsVentas);

            ViewBag.TotalAbonadoPorVenta = dictAbonos;
            ViewBag.IdBusqueda = idBusqueda;

            return View(ventas);
        }

        // ============================
        // CREAR VENTA (ADMIN/EMPLEADO)
        // ============================
        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Crear(int? idProducto)
        {
            // Clientes activos con rol "cliente"
            var clientesQuery = _context.Usuarios
                .Include(u => u.IdPersonaNavigation)
                .Include(u => u.UsuarioPerfils)
                    .ThenInclude(up => up.IdPerfilNavigation)
                .Where(u =>
                    u.Estado == "Activo" &&
                    u.UsuarioPerfils.Any(up => up.IdPerfilNavigation.Descripcion == "cliente"));

            var listaClientes = await clientesQuery
                .Select(u => new
                {
                    u.IdUsuario,
                    NombreMostrar = u.IdPersonaNavigation == null
                        ? u.NombreUsuario
                        : string.Join(" ",
                            new[]
                            {
                                u.IdPersonaNavigation.PrimerNombre,
                                u.IdPersonaNavigation.SegundoNombre,
                                u.IdPersonaNavigation.PrimerApellido,
                                u.IdPersonaNavigation.SegundoApellido
                            }.Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .OrderBy(x => x.NombreMostrar)
                .ToListAsync();

            ViewBag.Clientes = new SelectList(
                listaClientes,
                "IdUsuario",
                "NombreMostrar"
            );

            // Productos activos
            var productos = await _context.Productos
                .Where(p => p.Estado == "Activo" || p.Estado == null)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewBag.Productos = new SelectList(
                productos,
                "IdProducto",
                "Nombre"
            );

            // Si venimos desde el catálogo con un idProducto,
            // preparamos datos para autocompletar la primera línea en la vista.
            if (idProducto.HasValue)
            {
                var prod = await _context.Productos
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == idProducto.Value &&
                        (p.Estado == "Activo" || p.Estado == null));

                if (prod != null)
                {
                    ViewBag.ProductoInicialId = prod.IdProducto;
                    ViewBag.ProductoInicialNombre = prod.Nombre;
                    ViewBag.ProductoInicialPrecio = prod.Precio ?? 0;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,empleado")]
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

            // Validar stock
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

            // Detalles + actualización de stock
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
        // EDITAR VENTA (ADMIN/EMPLEADO)
        // ============================
        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Editar(int id)
        {
            var venta = await _context.Venta
                .Include(v => v.IdUsuariopacienteNavigation)
                .Include(v => v.IdUsuarioempleadoNavigation)
                .Include(v => v.ProductoVenta)
                    .ThenInclude(pv => pv.IdProductoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null)
                return NotFound();

            return View(venta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Editar(
            int id,
            DateOnly? fechaEntrega,
            int[] productosAEliminar,
            int[] productosIds,
            int[] cantidadesNuevas)
        {
            var venta = await _context.Venta
                .Include(v => v.ProductoVenta)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null)
                return NotFound();

            // Actualizar fecha de entrega
            venta.FechaEntrega = fechaEntrega;

            // Estructuras de apoyo
            var eliminarSet = (productosAEliminar != null)
                ? new HashSet<int>(productosAEliminar)
                : new HashSet<int>();

            var nuevasCantidades = new Dictionary<int, int>();
            if (productosIds != null && cantidadesNuevas != null &&
                productosIds.Length == cantidadesNuevas.Length)
            {
                for (int i = 0; i < productosIds.Length; i++)
                {
                    nuevasCantidades[productosIds[i]] = cantidadesNuevas[i];
                }
            }

            // Abonos ya realizados
            float abonadoActual = await _context.VentaPagos
                .Where(p => p.IdVenta == id)
                .SumAsync(p => (float?)(p.Monto ?? 0) ?? 0);

            // Aplicar cambios en detalles y stock
            var detalles = venta.ProductoVenta.ToList();

            foreach (var det in detalles)
            {
                int idProd = det.IdProducto ?? 0;
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == idProd);

                if (producto == null)
                    continue;

                int cantidadActual = det.Cantidad ?? 0;

                // Eliminar producto completo
                if (eliminarSet.Contains(idProd))
                {
                    producto.Stock = (producto.Stock ?? 0) + cantidadActual;
                    producto.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);
                    _context.Productos.Update(producto);

                    _context.ProductoVenta.Remove(det);
                    continue;
                }

                // Cambiar cantidad
                if (nuevasCantidades.TryGetValue(idProd, out int nuevaCant)
                    && nuevaCant != cantidadActual)
                {
                    if (nuevaCant <= 0)
                    {
                        TempData["ErrorVenta"] = "Las cantidades deben ser mayores que 0.";
                        return RedirectToAction(nameof(Editar), new { id });
                    }

                    int delta = nuevaCant - cantidadActual;

                    if (delta > 0)
                    {
                        // Aumentar cantidad → consumir stock
                        int stockActual = producto.Stock ?? 0;
                        if (stockActual < delta)
                        {
                            TempData["ErrorVenta"] =
                                $"No hay stock suficiente para aumentar el producto {producto.Nombre}. Stock actual: {stockActual}.";
                            return RedirectToAction(nameof(Editar), new { id });
                        }

                        producto.Stock = stockActual - delta;
                    }
                    else if (delta < 0)
                    {
                        // Disminuir cantidad → devolver stock
                        int devolver = -delta;
                        producto.Stock = (producto.Stock ?? 0) + devolver;
                    }

                    producto.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);
                    _context.Productos.Update(producto);

                    det.Cantidad = nuevaCant;
                    _context.ProductoVenta.Update(det);
                }
            }

            // Recalcular total con los productos que quedan
            float nuevoTotal = 0;

            var detallesRestantes = venta.ProductoVenta
                .Where(d => !(d.IdProducto.HasValue && eliminarSet.Contains(d.IdProducto.Value)))
                .ToList();

            foreach (var det in detallesRestantes)
            {
                int idProd = det.IdProducto ?? 0;
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == idProd);

                if (producto == null)
                    continue;

                float precio = producto.Precio ?? 0;
                int cant = det.Cantidad ?? 0;

                nuevoTotal += (float)(precio * cant);
            }

            // Validar contra los abonos
            if (nuevoTotal < abonadoActual)
            {
                TempData["ErrorVenta"] =
                    "No se pueden aplicar los cambios porque los abonos superarían el nuevo total de la venta.";
                return RedirectToAction(nameof(Editar), new { id });
            }

            venta.Total = nuevoTotal;
            _context.Venta.Update(venta);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La venta se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // CANCELAR / ELIMINAR VENTA (ADMIN/EMPLEADO)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var venta = await _context.Venta
                .Include(v => v.ProductoVenta)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null)
                return NotFound();

            // Revertir stock
            foreach (var detalle in venta.ProductoVenta)
            {
                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.IdProducto == detalle.IdProducto);

                if (producto != null)
                {
                    producto.Stock = (producto.Stock ?? 0) + (detalle.Cantidad ?? 0);
                    producto.FechaActualizacion = DateOnly.FromDateTime(DateTime.Now);
                    _context.Productos.Update(producto);
                }
            }

            // Eliminar detalles
            if (venta.ProductoVenta != null && venta.ProductoVenta.Any())
            {
                _context.ProductoVenta.RemoveRange(venta.ProductoVenta);
            }

            // Eliminar pagos
            var pagos = await _context.VentaPagos
                .Where(p => p.IdVenta == id)
                .ToListAsync();

            if (pagos.Any())
            {
                _context.VentaPagos.RemoveRange(pagos);
            }

            // Eliminar venta
            _context.Venta.Remove(venta);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "La venta fue cancelada y se revirtió el stock correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================
        // PAGOS (ADMIN/EMPLEADO)
        // ============================
        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> Pagos(int id)
        {
            var venta = await _context.Venta
                .Include(v => v.IdUsuariopacienteNavigation)
                .Include(v => v.IdUsuarioempleadoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == id);

            if (venta == null)
                return NotFound();

            var pagos = await _context.VentaPagos
                .Where(p => p.IdVenta == id)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            float total = venta.Total ?? 0;
            float abonado = pagos.Sum(p => p.Monto ?? 0);
            float saldo = total - abonado;

            var model = new PagosVentaViewModel
            {
                Venta = venta,
                Pagos = pagos,
                Total = total,
                Abonado = abonado,
                Saldo = saldo
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> AgregarPago(int idVenta, float monto)
        {
            var venta = await _context.Venta
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);

            if (venta == null)
                return NotFound();

            if (monto <= 0)
            {
                TempData["ErrorPago"] = "El monto del abono debe ser mayor a 0.";
                return RedirectToAction(nameof(Pagos), new { id = idVenta });
            }

            float abonadoActual = await _context.VentaPagos
                .Where(p => p.IdVenta == idVenta)
                .SumAsync(p => (float?)(p.Monto ?? 0) ?? 0);

            float total = venta.Total ?? 0;
            float nuevoTotalAbonado = abonadoActual + monto;

            if (nuevoTotalAbonado > total)
            {
                TempData["ErrorPago"] = "El abono supera el saldo pendiente.";
                return RedirectToAction(nameof(Pagos), new { id = idVenta });
            }

            var pago = new VentaPago
            {
                IdVenta = idVenta,
                Monto = monto,
                FechaPago = DateOnly.FromDateTime(DateTime.Now)
            };

            _context.VentaPagos.Add(pago);
            await _context.SaveChangesAsync();

            TempData["MensajePago"] = "Abono registrado correctamente.";
            return RedirectToAction(nameof(Pagos), new { id = idVenta });
        }

        // ============================
        // BÚSQUEDAS AJAX (ADMIN/EMPLEADO)
        // ============================
        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> BuscarClientePorDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
                return Json(new { encontrado = false });

            if (!long.TryParse(documento, out long docNumber))
                return Json(new { encontrado = false });

            var persona = await _context.Personas
                .FirstOrDefaultAsync(p => p.IdPersona == docNumber);

            if (persona == null)
                return Json(new { encontrado = false });

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdPersona == persona.IdPersona);

            if (usuario == null)
                return Json(new { encontrado = false });

            return Json(new
            {
                encontrado = true,
                idUsuario = usuario.IdUsuario,
                nombreCompleto = $"{persona.PrimerNombre} {persona.SegundoNombre} {persona.PrimerApellido} {persona.SegundoApellido}".Trim(),
                correo = persona.Correo,
                telefono = persona.Telefono?.ToString()
            });
        }

        [HttpGet]
        [Authorize(Roles = "administrador,empleado")]
        public async Task<IActionResult> BuscarProductoPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return Json(new { encontrado = false });

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

        // ============================
        // MÓDULO CLIENTE: MIS COMPRAS
        // ============================
        [HttpGet]
        [Authorize(Roles = "cliente")]
        public async Task<IActionResult> MisCompras()
        {
            var usuario = await GetUsuarioActualAsync();
            if (usuario == null)
                return Unauthorized();

            var ventas = await _context.Venta
                .Include(v => v.IdUsuarioempleadoNavigation)
                .Include(v => v.ProductoVenta)
                    .ThenInclude(pv => pv.IdProductoNavigation)
                .Where(v => v.IdUsuariopaciente == usuario.IdUsuario)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            var idsVentas = ventas.Select(v => v.IdVenta).ToList();
            var dictAbonos = await GetPagosPorVentaAsync(idsVentas);

            var modelo = ventas.Select(v =>
            {
                float total = v.Total ?? 0;
                // Si no hay registros en VentaPagos, se puede usar el abono directo de la venta
                float abonado = dictAbonos.TryGetValue(v.IdVenta, out var a)
                    ? a
                    : (v.Abono ?? 0);

                return new MisCompraViewModel
                {
                    Venta = v,
                    Total = total,
                    Abonado = abonado,
                    Saldo = total - abonado
                };
            }).ToList();

            return View(modelo);
        }
    }
}
