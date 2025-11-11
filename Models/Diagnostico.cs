using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Diagnostico
{
    public int IdDiagnostico { get; set; }

    public DateOnly? FechaEdicion { get; set; }

    public string? Descripcion { get; set; }

    public string? Observaciones { get; set; }

    public int? IdHistoriaclinica { get; set; }

    public virtual Historiaclinica? IdHistoriaclinicaNavigation { get; set; }

    public virtual ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}
