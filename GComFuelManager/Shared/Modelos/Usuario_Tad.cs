using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Usuario_Tad
    {
        public string Id_Usuario { get; set; } = string.Empty;
        public short Id_Terminal { get; set; }
    }
}
