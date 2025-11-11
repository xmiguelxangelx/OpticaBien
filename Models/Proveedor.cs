using System;
using System.Collections.Generic;

namespace Optica1.Models;

public partial class Proveedor
{
    public int IdProveedorNit { get; set; }

    public string? Nombre { get; set; }

    public string? Direccion { get; set; }

    public long? Telefono { get; set; }

    public string? Correo { get; set; }

    public virtual ICollection<ProveedorCompra> ProveedorCompras { get; set; } = new List<ProveedorCompra>();
}
