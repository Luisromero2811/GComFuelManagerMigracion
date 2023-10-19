using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.DTOs
{
    public class CierreFiltroDTO
    {
        public string Folio { get; set; } = string.Empty;
        public DateTime? FchInicio { get; set; } = DateTime.Now;
        public DateTime? FchFin{ get; set; } = DateTime.Now;
        public int? codCte { get; set; }
        public short? codGru { get; set; }
        public bool forFolio { get; set; } = false;
        public bool byMonth { get; set; } = false;
    }
}
