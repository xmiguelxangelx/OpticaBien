using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public DateOnly? FechaActualizacion { get; set; }

    public string? Nombre { get; set; }

    public int? Stock { get; set; }

    public float? Precio { get; set; }

    public virtual ICollection<ProductoCompra> ProductoCompras { get; set; } = new List<ProductoCompra>();

    public virtual ICollection<ProductoVentum> ProductoVenta { get; set; } = new List<ProductoVentum>();
}
