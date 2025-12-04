using System.Collections.Generic;

namespace Optica1.Models
{
    public class ClienteDashboardViewModel
    {
        public Usuario Usuario { get; set; }

        // Próximas citas del cliente
        public List<Citas> CitasProximas { get; set; } = new();

        // Resumen de compras del cliente
        public List<VentaClienteResumen> Compras { get; set; } = new();
    }
}
