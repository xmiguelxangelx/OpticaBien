using System;

namespace Optica1.Models
{
    public class OptometraDashboardViewModel
    {
        public int TotalCitasHoy { get; set; }
        public int CitasPendientes { get; set; }
        public int PacientesAtendidosHoy { get; set; }
        public int HistoriasClinicasHoy { get; set; }

        // Opcional: rango de fechas mostrado
        public DateTime FechaHoy { get; set; } = DateTime.Today;
    }
}
