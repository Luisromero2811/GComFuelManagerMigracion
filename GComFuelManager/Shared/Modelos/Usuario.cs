using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
namespace GComFuelManager.Shared.Modelos
{
    public class Usuario
    {
        [JsonProperty("cod"), Key]
        public int Cod { get; set; }
        [JsonProperty("den"),MaxLength(64)]
        public string? Den { get; set; } = string.Empty;
        [JsonProperty("usu"), MaxLength(16)]
        public string? Usu { get; set; } = string.Empty;
        [JsonProperty("cve"), MaxLength(16)]
        public string? Cve { get; set; } = string.Empty;
        [JsonProperty("fch")]
        public DateTime? Fch { get; set; } = DateTime.Now;
        [JsonProperty("tip")]
        public byte? Tip { get; set; } = 0;
        [JsonProperty("est")]
        public byte? Est { get; set; } = 0;
        [JsonProperty("privilegio"), MaxLength(256)]
        public string? Privilegio { get; set; } = string.Empty;
        [JsonProperty("activo")]
        public bool Activo { get; set; } = true;
        [JsonProperty("codCte")]
        public int? CodCte { get; set; }
        [JsonProperty("codGru")]
        public Int16? CodGru { get; set; }
        [JsonProperty("isClient")]
        public bool IsClient { get; set; } = false;
        [NotMapped] public Cliente? Cliente { get; set; } = null!;
    }
}
