using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Optica1.Controllers
{
    [Authorize(Roles = "cliente")]
    public class CarritoController : Controller
    {
        private readonly ProyectoopticaContext _context;
        private const string CarritoSessionKey = "CarritoCliente";

        public CarritoController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ======================================================
        //  Clases internas para guardar el carrito en sesión
        // ======================================================
        private class CarritoItemSession
        {
            public int IdProducto { get; set; }
            public int Cantidad { get; set; }
        }

        private List<CarritoItemSession> ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(CarritoSessionKey);
            if (string.IsNullOrEmpty(json))
                return new List<CarritoItemSession>();

            try
            {
                var lista = JsonSerializer.Deserialize<List<CarritoItemSession>>(json);
                return lista ?? new List<CarritoItemSession>();
            }
            catch
            {
                // Si algo falla al deserializar, empezamos un carrito vacío
                return new List<CarritoItemSession>();
            }
        }

        private void GuardarCarrito(List<CarritoItemSession> items)
        {
            var json = JsonSerializer.Serialize(items);
            HttpContext.Session.SetString(CarritoSessionKey, json);
        }

        // ======================================================
        //  VER CARRITO
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                // Vista esperando List<CarritoItemViewModel>
                return View(new List<CarritoItemViewModel>());
            }

            var ids = carrito.Select(c => c.IdProducto).ToList();

            var productos = await _context.Productos
                .Where(p =>
                    ids.Contains(p.IdProducto) &&
                    (p.Estado == "Activo" || p.Estado == null))
                .ToListAsync();

            var modelo = new List<CarritoItemViewModel>();

            foreach (var p in productos)
            {
                var itemCarrito = carrito.First(c => c.IdProducto == p.IdProducto);
                var cant = itemCarrito.Cantidad;
                float precio = p.Precio ?? 0;

                modelo.Add(new CarritoItemViewModel
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre,
                    Tipo = p.Tipo,
                    Marca = p.Marca,
                    Precio = precio,
                    Cantidad = cant,
                    StockDisponible = p.Stock ?? 0
                });
            }

            return View(modelo);
        }

        // ======================================================
        //  AGREGAR PRODUCTO (desde catálogo)
        //  Se llama con: /Carrito/Agregar?idProducto=XX
        // ======================================================
        [HttpGet]
        public IActionResult Agregar(int idProducto, int cantidad = 1)
        {
            if (cantidad <= 0)
                cantidad = 1;

            var carrito = ObtenerCarrito();

            var item = carrito.FirstOrDefault(c => c.IdProducto == idProducto);
            if (item == null)
            {
                carrito.Add(new CarritoItemSession
                {
                    IdProducto = idProducto,
                    Cantidad = cantidad
                });
            }
            else
            {
                item.Cantidad += cantidad;
            }

            GuardarCarrito(carrito);

            TempData["Mensaje"] = "Producto añadido al carrito.";
            return RedirectToAction("Index", "CatalogoCliente");
        }

        // ======================================================
        //  QUITAR PRODUCTO DEL CARRITO
        // ======================================================
        [HttpGet]
        public IActionResult Quitar(int idProducto)
        {
            var carrito = ObtenerCarrito();
            var item = carrito.FirstOrDefault(c => c.IdProducto == idProducto);

            if (item != null)
            {
                carrito.Remove(item);
                GuardarCarrito(carrito);
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        //  VACIAR CARRITO
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vaciar()
        {
            HttpContext.Session.Remove(CarritoSessionKey);
            TempData["Mensaje"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Index));
        }

        // ======================================================
        //  MINI-CARRITO (parcial en el panel del cliente)
        // ======================================================
        [HttpGet]
        public async Task<IActionResult> Mini()
        {
            var carrito = ObtenerCarrito();

            if (!carrito.Any())
                return PartialView("_CarritoMini", new List<CarritoItemViewModel>());

            var ids = carrito.Select(c => c.IdProducto).ToList();

            var productos = await _context.Productos
                .Where(p =>
                    ids.Contains(p.IdProducto) &&
                    (p.Estado == "Activo" || p.Estado == null))
                .ToListAsync();

            var modelo = new List<CarritoItemViewModel>();

            foreach (var p in productos)
            {
                var itemCarrito = carrito.First(c => c.IdProducto == p.IdProducto);
                var cant = itemCarrito.Cantidad;
                float precio = p.Precio ?? 0;

                modelo.Add(new CarritoItemViewModel
                {
                    IdProducto = p.IdProducto,
                    Nombre = p.Nombre,
                    Tipo = p.Tipo,
                    Marca = p.Marca,
                    Precio = precio,
                    Cantidad = cant,
                    StockDisponible = p.Stock ?? 0
                });
            }

            return PartialView("_CarritoMini", modelo);
        }
    }
}
