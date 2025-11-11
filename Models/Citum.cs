using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Citum
{
    public int IdCita { get; set; }

    public DateOnly? Fecha { get; set; }

    public TimeOnly? Hora { get; set; }

    public string? Motivo { get; set; }

    public string? Estado { get; set; }

    public int? IdUsuariopaciente { get; set; }

    public int? IdUsuarioempleado { get; set; }

    public int? IdHistoriaclinica { get; set; }

    public virtual Historiaclinica? IdHistoriaclinicaNavigation { get; set; }

    public virtual Usuario? IdUsuarioempleadoNavigation { get; set; }

    public virtual Usuario? IdUsuariopacienteNavigation { get; set; }
}
