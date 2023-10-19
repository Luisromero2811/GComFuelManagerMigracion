using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Porcentaje
    {
        [Key] public int? Cod { get; set; }
        public string? Accion { get; set; } = string.Empty;
        public double Porcen { get; set; } = 1;
    }
}
