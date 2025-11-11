using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Tratamiento
{
    public int IdTratamiento { get; set; }

    public DateOnly? FechaEdicion { get; set; }

    public string? DetallesTratamiento { get; set; }

    public string? Indicaciones { get; set; }

    public int? IdDiagnostico { get; set; }

    public virtual Diagnostico? IdDiagnosticoNavigation { get; set; }
}
