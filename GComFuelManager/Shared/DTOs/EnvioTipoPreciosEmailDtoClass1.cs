using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.DTOs
{
    public class EnvioTipoPreciosEmailDtoClass1
    {
        public Int16 TipoEnvio { get; set; } = 0;
        public List<string> Clientes { get; set; } = new List<string>();
    }
}
