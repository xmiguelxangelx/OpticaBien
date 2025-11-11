using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Compra
{
    public int IdCompra { get; set; }

    public DateOnly? FechaCompra { get; set; }

    public virtual ICollection<ProductoCompra> ProductoCompras { get; set; } = new List<ProductoCompra>();

    public virtual ICollection<ProveedorCompra> ProveedorCompras { get; set; } = new List<ProveedorCompra>();
}
