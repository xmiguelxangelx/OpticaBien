using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class VwHistoriaClinicaCompletum
{
    public int IdHistoriaclinica { get; set; }

    public DateOnly? FechaCreacion { get; set; }

    public string PrimerNombre { get; set; } = null!;

    public string PrimerApellido { get; set; } = null!;

    public string? Diagnostico { get; set; }

    public string? Observaciones { get; set; }

    public string? DetallesTratamiento { get; set; }

    public string? Indicaciones { get; set; }
}
