using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GComFuelManager.Shared.Modelos
{
    public class Transportista
    {
        [Key]
        public int Cod { get; set; } = 0;
        [MaxLength(256)]
        public string? Den { get; set; } = string.Empty;
        [AllowNull, DefaultValue("")]
        public string? CarrId { get; set; } = string.Empty;
        [MaxLength(15)]
        public string? Busentid { get; set; } = string.Empty;
        public bool? Activo { get; set; } = true;
        public bool? Simsa { get; set; } = true;
        public string? Gru { get; set; } = string.Empty;

        [EpplusIgnore, NotMapped] public List<Tad> Terminales { get; set; } = new();
        [EpplusIgnore, NotMapped, JsonIgnore] public List<Transportista_Tad> Transportista_Tads { get; set; } = new();

        public int? Codgru { get; set; }
        public short? Id_Tad { get; set; }
        [NotMapped]
        public GrupoTransportista? GrupoTransportista { get; set; } = null!;
        [NotMapped]
        public Tad? Tad { get; set; } = null!;
    }
}

