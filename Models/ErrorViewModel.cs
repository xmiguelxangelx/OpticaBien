namespace Optica1.Models
{
    public class ValidadorHorarioCitas
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
