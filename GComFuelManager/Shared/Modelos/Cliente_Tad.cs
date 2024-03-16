using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Cliente_Tad
    {
        public int Id_Cliente { get; set; }
        public short Id_Terminal { get; set; }

        [NotMapped] public Cliente? Cliente { get; set; } = null!;
        [NotMapped] public Tad? Terminal { get; set; } = null!;
    }
}
