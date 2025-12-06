using System;

namespace Optica1.Services
{
    public static class ValidadorHorarioCitas
    {
        // Horario permitido: 10:00 a 19:00 (inclusive)
        public static bool EsHorarioValido(TimeSpan hora)
        {
            var inicioPermitido = new TimeSpan(10, 0, 0); // 10:00
            var finPermitido = new TimeSpan(19, 0, 0); // 19:00

            return hora >= inicioPermitido && hora <= finPermitido;
        }
    }
}
