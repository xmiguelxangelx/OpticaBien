using System;

namespace Optica1.Models
{
    public class PacienteDelDiaViewModel
    {
        public int IdCita { get; set; }
        public int? IdUsuarioPaciente { get; set; }

        public string NombrePaciente { get; set; }
        public string Documento { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }

        public int? Edad { get; set; }

        public DateTime FechaCita { get; set; }
        public TimeSpan HoraCita { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
    }
}
