using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class ProveedorCompra
{
    public int IdProveedorcompra { get; set; }

    public int? IdProveedorNit { get; set; }

    public int? IdCompra { get; set; }

    public virtual Compra? IdCompraNavigation { get; set; }

    public virtual Proveedor? IdProveedorNitNavigation { get; set; }
}
