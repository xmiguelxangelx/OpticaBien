using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Ventum
{
    public int IdVenta { get; set; }

    public DateOnly? Fecha { get; set; }

    public float? Total { get; set; }

    public float? Abono { get; set; }

    public DateOnly? FechaEntrega { get; set; }

    public int? IdUsuariopaciente { get; set; }

    public int? IdUsuarioempleado { get; set; }

    public virtual Usuario? IdUsuarioempleadoNavigation { get; set; }

    public virtual Usuario? IdUsuariopacienteNavigation { get; set; }

    public virtual ICollection<ProductoVentum> ProductoVenta { get; set; } = new List<ProductoVentum>();

    public virtual ICollection<VentaPago> VentaPagos { get; set; } = new List<VentaPago>();
}
