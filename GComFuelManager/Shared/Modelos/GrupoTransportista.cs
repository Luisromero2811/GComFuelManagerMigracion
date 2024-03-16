using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GComFuelManager.Shared.DTOs;
using Newtonsoft.Json;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.Modelos
{
	public class GrupoTransportista
	{
        [Key, JsonProperty("cod")] public int cod { get; set; } = 0;
        //   public int Cod { get; set; } = 0;

        [JsonProperty("den"), MaxLength(255)]
        public string? den { get; set; } = string.Empty;

        public short? Id_Tad { get; set; }

        [NotMapped] public Tad? Tad { get; set; }
        [NotMapped] public List<Tad> Terminales { get; set; } = new();
        [NotMapped] public List<GrupoTransportista_Tad> GrupoTransportista_Tads { get; set; } = new();
    }
}

