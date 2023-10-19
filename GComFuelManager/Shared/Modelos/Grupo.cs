using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GComFuelManager.Shared.Modelos
{
	public class Grupo
	{
		[JsonProperty("cod"), Key]
		public Int16 Cod { get; set; }

		[JsonProperty("den"), MaxLength(30)]
		public string? Den { get; set; } = string.Empty;

		[JsonProperty("eje"), MaxLength(50)]
		public string? Eje { get; set; } = string.Empty;

		[JsonProperty("fch")]
		public DateTime? Fch { get; set; } = DateTime.MinValue;

		//[JsonProperty("class")]
		//public int? Clase { get; set; } = 0;

		[JsonProperty("tipven"), MaxLength(16)]
		public string? Tipven { get; set; } = string.Empty;
		public string? MdVenta { get; set; } = string.Empty;

        [ForeignKey("codgru")]
		public List<Cliente> Clientes { get; set; } = new List<Cliente>();
	}
}

