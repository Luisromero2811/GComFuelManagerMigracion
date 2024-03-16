using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Unidad_Tad
    {
        public int Id_Unidad { get; set; }
        public short Id_Terminal { get; set; }

        [NotMapped] public Tonel? Tonel { get; set; } = null!;
        [NotMapped] public Tad? Terminal { get; set; } = null!;
    }
}
