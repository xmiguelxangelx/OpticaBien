using System;
using System.Collections.Generic;

namespace Optica1.Models
{
    public partial class Historiaclinica
    {


        public int IdHistoriaclinica { get; set; }
        public string? Estado { get; set; }

        // 🔹 Paciente (cliente)
        public int? IdUsuarioPaciente { get; set; }

        // 🔹 Optómetra
        public int? IdUsuarioOptometra { get; set; }

        public DateOnly? FechaCreacion { get; set; }

        public string? MotivoConsulta { get; set; }
        public string? Antecedentes { get; set; }

        public string? AgudezaVisualOd { get; set; }
        public string? AgudezaVisualOi { get; set; }

        public string? RxFinalOd { get; set; }
        public string? RxFinalOi { get; set; }

        public string? Observaciones { get; set; }

        public virtual ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
    }
}
