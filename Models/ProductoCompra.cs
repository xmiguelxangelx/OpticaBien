using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class ProductoCompra
{
    public int IdProductocompra { get; set; }

    public int? IdProducto { get; set; }

    public int? IdCompra { get; set; }

    public int? Cantidad { get; set; }

    public virtual Compra? IdCompraNavigation { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
