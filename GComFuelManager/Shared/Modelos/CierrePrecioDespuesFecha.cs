using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class CierrePrecioDespuesFecha
    {
        [Key]
        public int? Cod { get; set; }
        public int? CodCie { get; set; }
        public double? Precio { get; set; }
        public int? CodCte { get; set; }
        public short? CodPrd { get; set; }
    }
}