using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Destino_Tad
    {
        public int Id_Destino { get; set; }
        public short Id_Terminal { get; set; }

        [NotMapped] public Destino? Destino { get; set; } = null!;
        [NotMapped] public Tad? Terminal { get; set; } = null!;
    }
}
