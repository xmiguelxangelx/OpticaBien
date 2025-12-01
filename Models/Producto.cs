using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; 

namespace Optica1.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public DateOnly? FechaActualizacion { get; set; }

    public string? Nombre { get; set; }

    public int? Stock { get; set; }

    public float? Precio { get; set; }

    // NUEVOS CAMPOS
    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Descripcion { get; set; }

    // 👇 Esta propiedad DEBE llamarse IdProveedorNit (con E)
    // y estar mapeada a la columna id_proveedor_nit
    [Column("id_proveedor_nit")]
    public int? IdProveedorNit { get; set; }

    public int? StockMinimo { get; set; }

    public string? Estado { get; set; }

    public virtual Proveedor? IdProveedorNitNavigation { get; set; }

    public virtual ICollection<ProductoCompra> ProductoCompras { get; set; } = new List<ProductoCompra>();

    public virtual ICollection<ProductoVentum> ProductoVenta { get; set; } = new List<ProductoVentum>();
}
