namespace Optica1.Models
{
    public class VentaClienteResumen
    {
        public int IdVenta { get; set; }
        public DateOnly? Fecha { get; set; }
        public float Total { get; set; }
        public float Abonado { get; set; }
        public float Saldo { get; set; }
    }
}
