using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.Modelos
{
	//5
	public class Tad
	{
		[JsonProperty("cod"), Key]
		public Int16 Cod { get; set; }

		[JsonProperty("den"), MaxLength(128)]
		public string? Den { get; set; } = string.Empty;

		[JsonProperty("nro")]
		public Int16? Nro { get; set; } = 0;

		[JsonProperty("activo")]
		public bool? Activo { get; set; } = true;
		//public List<OrdenEmbarque> OrdenEmbarque { get; set; } = null!;
		[NotMapped] public List<Cliente> Clientes { get; set; } = new();
		[NotMapped] public List<Cliente_Tad> Cliente_Tads { get; set; } = new();
		[NotMapped] public List<Destino> Destinos { get; set; } = new();
		[NotMapped] public List<Destino_Tad> Destino_Tads { get; set; } = new();
		[NotMapped] public List<Transportista> Transportistas { get; set; } = new();
		[NotMapped] public List<Transportista_Tad> Transportista_Tads { get; set; } = new();
		[NotMapped] public List<Chofer> Choferes { get; set; } = new();
		[NotMapped] public List<Chofer_Tad> Chofer_Tads { get; set; } = new();
		[NotMapped] public List<Tonel> Unidades { get; set; } = new();
		[NotMapped] public List<Unidad_Tad> Unidad_Tads { get; set; } = new();

		[NotMapped] public List<GrupoTransportista> GruposTransportes { get; set; } = new();
		[NotMapped] public List<GrupoTransportista_Tad> GrupoTransportista_Tads { get; set; } = new();

 		[NotMapped] public List<Tad> Terminales { get; set; } = new List<Tad>();
    }
}

