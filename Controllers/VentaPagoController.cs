using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica1.Models;

namespace Optica1.Controllers
{
    [Authorize]
    public class VentaPagoController : Controller
    {
        private readonly ProyectoopticaContext _context;

        public VentaPagoController(ProyectoopticaContext context)
        {
            _context = context;
        }

        // ============================
        // LISTA DE PAGOS POR VENTA
        // ============================
        public async Task<IActionResult> Index(int idVenta)
        {
            var venta = await _context.Venta
                .Include(v => v.IdUsuariopacienteNavigation)
                .Include(v => v.IdUsuarioempleadoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);

            if (venta == null)
                return NotFound();

            var pagos = await _context.VentaPagos
                .Include(p => p.IdMedioDePagoNavigation)
                .Where(p => p.IdVenta == idVenta)
                .OrderByDescending(p => p.IdVentapago)
                .ToListAsync();

            ViewBag.Venta = venta;
            ViewBag.TotalAbonado = pagos.Sum(p => p.Monto ?? 0);

            return View(pagos);
        }

        // ============================
        // FORM PARA AGREGAR PAGO
        // ============================
        public async Task<IActionResult> Crear(int idVenta)
        {
            var venta = await _context.Venta.FindAsync(idVenta);
            if (venta == null)
                return NotFound();

            ViewBag.IdVenta = idVenta;
            ViewBag.MediosPago = await _context.MedioDePagos.ToListAsync();

            return View();
        }

        // ============================
        // REGISTRAR EL PAGO
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(VentaPago model)
        {
            var venta = await _context.Venta.FindAsync(model.IdVenta);
            if (venta == null)
                return NotFound();

            // Total abonado a la fecha
            var totalAbonado = await _context.VentaPagos
                .Where(p => p.IdVenta == model.IdVenta)
                .SumAsync(p => p.Monto ?? 0);

            var saldo = (venta.Total ?? 0) - totalAbonado;

            if (model.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0.");
            }
            else if (model.Monto > saldo)
            {
                ModelState.AddModelError("Monto", $"El abono excede el saldo pendiente: {saldo:C2}");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IdVenta = model.IdVenta;
                ViewBag.MediosPago = await _context.MedioDePagos.ToListAsync();
                return View(model);
            }

            _context.VentaPagos.Add(model);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Abono registrado correctamente.";

            return RedirectToAction("Index", new { idVenta = model.IdVenta });
        }
    }
}
