using System;

namespace Optica1.Models
{
    public partial class Citas
    {
        public int IdCita { get; set; }

        public string Estado { get; set; }

        public DateTime Fecha { get; set; }

        public TimeSpan Hora { get; set; }

        public int? IdHistoriaclinica { get; set; }      // solo el Id, sin navegación

        public int? IdUsuarioempleado { get; set; }      // Optómetra / empleado

        public int? IdUsuariopaciente { get; set; }      // Cliente / paciente

        public string Motivo { get; set; }

        // 🔹 Navegaciones SOLO con Usuario (las que sí usamos)
        public virtual Usuario IdUsuarioempleadoNavigation { get; set; }

        public virtual Usuario IdUsuariopacienteNavigation { get; set; }
    }
}
