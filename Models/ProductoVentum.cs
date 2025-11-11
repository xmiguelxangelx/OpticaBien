using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class ProductoVentum
{
    public int IdProductoventa { get; set; }

    public int? IdProducto { get; set; }

    public int? IdVenta { get; set; }

    public int? Cantidad { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }

    public virtual Ventum? IdVentaNavigation { get; set; }
}
