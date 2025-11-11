using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Historiaclinica
{
    public int IdHistoriaclinica { get; set; }

    public DateOnly? FechaCreacion { get; set; }

    public virtual ICollection<Citum> Cita { get; set; } = new List<Citum>();

    public virtual ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
}
