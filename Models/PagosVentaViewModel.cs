using System.Collections.Generic;

namespace Optica1.Models
{
    public class PagosVentaViewModel
    {
        public Ventum Venta { get; set; }
        public List<VentaPago> Pagos { get; set; } = new();

        public float Total { get; set; }
        public float Abonado { get; set; }
        public float Saldo { get; set; }

        // Campo para el formulario de nuevo abono
        public float NuevoMonto { get; set; }
    }
}
