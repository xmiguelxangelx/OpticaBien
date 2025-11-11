using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class VentaPago
{
    public int IdVentapago { get; set; }

    public int? IdMedioDePago { get; set; }

    public int? IdVenta { get; set; }

    public virtual MedioDePago? IdMedioDePagoNavigation { get; set; }

    public virtual Ventum? IdVentaNavigation { get; set; }
}
