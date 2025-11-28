using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Historiaclinica
{
    public int IdHistoriaclinica { get; set; }

    public DateOnly? FechaCreacion { get; set; }

   // public virtual ICollection<Citas> Cita { get; set; } = new List<Citas>();

    public virtual ICollection<Diagnostico> Diagnosticos { get; set; } = new List<Diagnostico>();
}
