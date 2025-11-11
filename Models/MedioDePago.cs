using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class MedioDePago
{
    public int IdMedioDePago { get; set; }

    public string? Descripcion { get; set; }

    public virtual ICollection<VentaPago> VentaPagos { get; set; } = new List<VentaPago>();
}
