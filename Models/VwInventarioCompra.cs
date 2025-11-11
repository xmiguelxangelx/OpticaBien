using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class VwInventarioCompra
{
    public int IdProducto { get; set; }

    public string? Nombre { get; set; }

    public int? Stock { get; set; }

    public float? Precio { get; set; }

    public DateOnly? UltimaCompra { get; set; }

    public decimal? TotalComprado { get; set; }
}
