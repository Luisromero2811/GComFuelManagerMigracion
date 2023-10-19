using System;
using System.Text;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using GComFuelManager.Shared.Modelos;
using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GComFuelManager.Shared.DTOs
{
	public class ZonaDTO
	{
        public int Cod { get; set; }
        public int? ZonaCod { get; set; }
		public short? codgru { get; set; }
		public int? CteCod { get; set; }
		public int? DesCod { get; set; }
        public bool? Activo { get; set; } = true;

        [EpplusIgnore, NotMapped] public Cliente? Cliente { get; set; } = new Cliente();
        [NotMapped] public Zona? Zona { get; set; } = null!;
        [NotMapped] public Destino? Destino { get; set; } = null!;

        public string? Clientes { get; set; } = string.Empty;
        public string? Zonas { get; set; } = string.Empty;
        public string? Destinos { get; set; } = string.Empty;

    }
}

