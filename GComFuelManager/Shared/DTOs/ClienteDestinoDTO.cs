using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Shared.DTOs
{
	public class ClienteDestinoDTO
	{
		public Cliente? cliente { get; set; }
		public CodDenDTO? destino { get; set; }
		public Destino? destinos { get; set; }
        public string Den { get; set; } = string.Empty;
    }
}

