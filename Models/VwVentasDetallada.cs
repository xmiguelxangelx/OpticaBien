using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class VwVentasDetallada
{
    public int IdVenta { get; set; }

    public DateOnly? Fecha { get; set; }

    public string NombreCliente { get; set; } = null!;

    public string ApellidoCliente { get; set; } = null!;

    public string? NombreEmpleado { get; set; }

    public string? ApellidoEmpleado { get; set; }

    public string? Producto { get; set; }

    public int? Cantidad { get; set; }

    public float? Precio { get; set; }

    public double? Subtotal { get; set; }

    public float? Total { get; set; }

    public float? Abono { get; set; }

    public DateOnly? FechaEntrega { get; set; }
}
