using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GComFuelManager.Shared.Modelos
{
    public class Chofer
    {
        [Key]
        public int Cod { get; set; }
        [MaxLength(128)]
        public string? Den { get; set; } = string.Empty;
        public int? Codtransport { get; set; } = 0;
        [MaxLength(6)]
        public string? Dricod { get; set; } = string.Empty;
        [MaxLength(128)]
        public string? Shortden { get; set; } = string.Empty;
        public bool? Activo { get; set; } = true;
        public bool? Activo_Permanente { get; set; } = true;
        public short? Id_Tad { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                if (!string.IsNullOrEmpty(Den) && !string.IsNullOrEmpty(Shortden) && Den.ToLower().Equals(Shortden.ToLower()))
                    return Shortden;
                else
                    return $"{Den} {Shortden}";
            }
        }

        [NotMapped] public Transportista? Transportista { get; set; } = null!;
        [NotMapped] public Tonel? Tonel { get; set; } = null!;
        [NotMapped] public List<Tad> Terminales { get; set; } = new();
        [NotMapped, JsonIgnore] public List<Chofer_Tad> Chofer_Tads { get; set; } = new();
    }
}

