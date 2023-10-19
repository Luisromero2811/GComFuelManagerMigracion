using System;
namespace GComFuelManager.Shared.DTOs
{
    public class CierreDiarioDTO
    {
        public int? codCte { get; set; }
        public short? codGru { get; set; }
        public DateTime FchInicio { get; set; } = DateTime.Now;
        public DateTime FchFin { get; set; } = DateTime.Now;
    }
}

