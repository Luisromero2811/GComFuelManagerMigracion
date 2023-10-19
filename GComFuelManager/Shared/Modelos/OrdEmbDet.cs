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
    public class OrdEmbDet
    {
        [JsonProperty("cod"), Key]
        public int Cod { get; set; }
        [JsonProperty("fch")]
        public DateTime? Fch { get; set; } = DateTime.Now;
        [JsonProperty("fchDoc")]
        public DateTime? FchDoc { get; set; } = DateTime.Now;
        [JsonProperty("eta"), MaxLength(5)]
        public string? Eta { get; set; } = string.Empty;

        [JsonProperty("fchlleest")]
        public DateTime? Fchlleest { get; set; } = DateTime.Today;

        [JsonProperty("fchrealledes")]
        public DateTime? Fchrealledes { get; set; } = DateTime.Now;

        [JsonProperty("litent")]
        public double? Litent { get; set; } = 0;
        //[DisplayName("Litros entregados")]
        //public string Litros { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Litent); } }

        [JsonProperty("obs"), MaxLength(256)]
        public string? Obs { get; set; } = string.Empty;
        [JsonProperty("bol")]
        public int? Bol { get; set; } = 0;
        [JsonProperty("codusu")]
        public int? Codusu { get; set; } = 0;
        [JsonProperty("fchmod")]
        public DateTime Fchmod { get; set; } = DateTime.Now;
        [JsonProperty("codusumod")]
        public int? Codusumod { get; set; } = 0;
        [JsonProperty("loc")]
        public string? Loc { get; set; } = string.Empty;

        [NotMapped, EpplusIgnore]
        public int EtaNumber { get; set; } = 0;
        [NotMapped, EpplusIgnore]
        public byte CodEst { get; set; } = 20;
        //Propiedad de navegación
        [NotMapped] public Orden? Orden { get; set; } = null!;
    }
}
