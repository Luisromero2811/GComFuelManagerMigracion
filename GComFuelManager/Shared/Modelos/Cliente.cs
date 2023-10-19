using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Cliente
    {
        [JsonProperty("cod"), Key]
        public int Cod { get; set; }
        [JsonProperty("den"), MaxLength(128)]
        public string? Den { get; set; } = string.Empty;
        [JsonProperty("codusu")]
        public int? Codusu { get; set; } = 0;
        [JsonProperty("codforpag")]
        public int? Codforpag { get; set; } = 0;
        [JsonProperty("tem"), MaxLength(50)]
        public string? Tem { get; set; } = string.Empty;
        [JsonProperty("codgru")]
        public Int16? codgru { get; set; } = null!;

        [JsonProperty("email"), MaxLength(30)]
        public string? Email { get; set; } = string.Empty;
        [JsonProperty("con"), MaxLength(50)]
        public string? Con { get; set; } = string.Empty;
        [JsonProperty("codtad")]
        public Int16? Codtad { get; set; } = 0;
        [JsonProperty("codsyn"), MaxLength(20)]
        public string? Codsyn { get; set; } = string.Empty;
        [JsonProperty("esenergas")]
        public bool? Esenergas { get; set; } = false;
        [JsonProperty("tipven"), MaxLength(16)]
        public string? Tipven { get; set; } = string.Empty;
        [JsonProperty("codCte")]
        public string? CodCte { get; set; } = string.Empty;
        [JsonProperty("consecutivo")]
        public int? Consecutivo { get; set; } = 0;
        [JsonProperty("activo")]
        public bool Activo { get; set; } = true;
        [JsonProperty("precioSemanal")]
        public bool? precioSemanal { get; set; } = false;


        [JsonProperty("mdVenta")]
        public string? MdVenta { get; set; } = string.Empty;
        [NotMapped] public Grupo? grupo { get; set; } = null!;

    }
}
