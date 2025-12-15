using System;

namespace Optica1.Models
{
    /// <summary>
    /// Ítem individual del carrito (solo para vista).
    /// </summary>
    public class CarritoItemViewModel
    {
        public int IdProducto { get; set; }

        public string Nombre { get; set; }

        public string Tipo { get; set; }

        public string Marca { get; set; }

        public float Precio { get; set; }

        public int Cantidad { get; set; }

        public int StockDisponible { get; set; }
    }
}
