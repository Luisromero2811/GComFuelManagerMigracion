using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Shared.DTOs
{
	public class GrupoClienteDTO
	{
		public Cliente? cliente { get; set; }
		public Grupo? grupo { get; set; }
        public string? Tipven { get; set; } 
        public string? Mdpven { get; set; }
        public string? Den { get; set; } = string.Empty;

        [NotMapped]
        public string? MdVenta { get; set; } = string.Empty;
        [NotMapped]
        public string? MVenta { get { return Tipven == "Rack" ? "Rack" : "Delivery"; } }

    }
}

