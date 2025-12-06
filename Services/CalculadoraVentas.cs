using System.Collections.Generic;
using System.Linq;

namespace Optica1.Services
{
    public class DetalleSimpleVenta
    {
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public static class CalculadoraVentas
    {
        public static decimal CalcularTotal(IEnumerable<DetalleSimpleVenta> detalles)
        {
            if (detalles == null || !detalles.Any())
                return 0;

            return detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        }
    }
}
