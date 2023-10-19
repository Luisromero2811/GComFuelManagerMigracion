using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Orden
    {
        [JsonProperty("cod"), Key]
        public Int64? Cod { get; set; } = null!;
        [JsonProperty("fch")]
        public DateTime? Fch { get; set; } = null!;
        [JsonProperty("ref"), MaxLength(32)]
        public string? Ref { get; set; } = string.Empty;
        [JsonProperty("coddes")]
        public int? Coddes { get; set; } = 0;
        //
        [JsonProperty("codprd")]
        public byte? Codprd { get; set; } = 0; //PK

        //VOL
        [JsonProperty("vol"), EpplusIgnore]
        public double? Vol { get; set; } = null!;
        //[JsonProperty("Volumen Cargado")]
        [NotMapped]
        public string Vols { get { return Vol != null ? Vol?.ToString("N2") : string.Empty; } }

        [JsonProperty("fchcar")]
        public DateTime? Fchcar { get; set; } = DateTime.MinValue;
        //Prueba
        [JsonProperty("codest")]
        public byte? Codest { get; set; } = 0; //PK

        [JsonProperty("coduni")]
        public int? Coduni { get; set; } = 0;
        [JsonProperty("codchf")]
        public int? Codchf { get; set; } = 0;
        [JsonProperty("bolguiid"), MaxLength(256)]
        public string? Bolguiid { get; set; } = string.Empty;
        [JsonProperty("liniteid")]
        public Int64? Liniteid { get; set; } = null!;
        [JsonProperty("codprd2")]
        public int? Codprd2 { get; set; } = 0;
        [JsonProperty("dendes"), MaxLength(256)]
        public string? Dendes { get; set; } = string.Empty;

        //VOL2
        [JsonProperty("vol2"), EpplusIgnore]
        public double? Vol2 { get; set; } = null!;
        //[JsonProperty("Volumen Natural")]
        [NotMapped]
        public string? Volumenes { get { return Vol2 != null ? Vol2?.ToString("N2") : string.Empty; } }

        [JsonProperty("batchId")]
        public int? BatchId { get; set; }
        [JsonProperty("CompartmentId")]
        public int? CompartmentId { get; set; } = null!;
        [JsonProperty("SealNumber"), MaxLength(128)]
        public string? SealNumber { get; set; } = string.Empty;
        [NotMapped] public long? Codprdsyn { get; set; } = 0;
        [NotMapped] public long? Codprd2syn { get; set; } = 0;
        [NotMapped] public long? Codchfsyn { get; set; } = 0;

        //Prop de nav Estado
        [NotMapped] public Estado? Estado { get; set; } = null!;
        [NotMapped] public Destino? Destino { get; set; } = null!;
        [NotMapped] public Producto? Producto { get; set; } = null!;
        [NotMapped] public Transportista? Transportista { get; set; } = null!;
        [NotMapped] public Tonel? Tonel { get; set; } = null!;
        [NotMapped] public Chofer? Chofer { get; set; } = null!;
        [NotMapped] public OrdEmbDet? OrdEmbDet { get; set; } = null!;
        [NotMapped] public OrdenEmbarque? OrdenEmbarque { get; set; } = null!;
        public int? Folio { get; set; } = 0;
        [NotMapped] public int? Compartimento { get; set; } = null!;
    }
}
