
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GComFuelManager.Shared.Modelos
{
    public class Cliente
    {
        [Key, EpplusIgnore]
        public int Cod { get; set; }
        [MaxLength(128), DisplayName("Nombre del cliente")]
        public string? Den { get; set; } = string.Empty;
        [EpplusIgnore]
        public int? Codusu { get; set; } = 0;
        [EpplusIgnore]
        public int? Codforpag { get; set; } = 0;
        [MaxLength(50), EpplusIgnore]
        public string? Tem { get; set; } = string.Empty;
        [EpplusIgnore]
        public Int16? Codgru { get; set; } = null!;
        [MaxLength(30), EpplusIgnore]
        public string? Email { get; set; } = string.Empty;
        [MaxLength(50), EpplusIgnore]
        public string? Con { get; set; } = string.Empty;
        [EpplusIgnore]
        public Int16? Codtad { get; set; } = 0;
        [MaxLength(20), EpplusIgnore]
        public string? Codsyn { get; set; } = string.Empty;
        [EpplusIgnore]
        public bool? Esenergas { get; set; } = false;
        [MaxLength(16), DisplayName("Tipo de Venta")]
        public string? Tipven { get; set; } = string.Empty;
        [EpplusIgnore]
        public string? CodCte { get; set; } = string.Empty;
        [EpplusIgnore]
        public int? Consecutivo { get; set; } = 0;
        [EpplusIgnore]
        public bool Activo { get; set; } = true;
        [EpplusIgnore]
        public bool? precioSemanal { get; set; } = false;
        [EpplusIgnore] public int? Id_Vendedor { get; set; } = 0;
        [EpplusIgnore] public int? Id_Originador { get; set; } = 0;
        public short? Id_Tad { get; set; } = 0;
        [DisplayName("Modelo de venta")]
        public string? MdVenta { get; set; } = string.Empty;
        [EpplusIgnore] DateTime? Fecha_Registro { get; set; } = DateTime.Now;
        [EpplusIgnore] bool? Es_Meta { get; set; } = true;


        [NotMapped, EpplusIgnore] public bool IsEditing { get; set; } = false;
        [NotMapped, EpplusIgnore] public string Nuevo_Codigo { get; set; } = string.Empty;

        [NotMapped, EpplusIgnore] public Grupo? grupo { get; set; } = null!;
        [NotMapped, EpplusIgnore, JsonIgnore] public Vendedor? Vendedor { get; set; } = null!;
        [NotMapped, EpplusIgnore, JsonIgnore] public Originador? Originador { get; set; } = null!;
        [EpplusIgnore, NotMapped] public List<Tad> Terminales { get; set; } = new();
        [EpplusIgnore, NotMapped, JsonIgnore] public List<Cliente_Tad> Cliente_Tads { get; set; } = new();

        [EpplusIgnore, NotMapped, JsonIgnore] public Cliente_Tad? cliente_Tad { get; set; }
        [EpplusIgnore, NotMapped, JsonIgnore] public Tad? Tad { get; set; }

        [NotMapped]
        public string Obtener_Nombre_Vendedor
        {
            get
            {
                if (Vendedor is not null)
                    if (!string.IsNullOrEmpty(Vendedor.Nombre))
                        return Vendedor.Nombre;
                return string.Empty;
            }
        }
        [NotMapped]
        public string Obtener_Nombre_Originador
        {
            get
            {
                if (Originador is not null)
                    if (!string.IsNullOrEmpty(Originador.Nombre))
                        return Originador.Nombre;
                return string.Empty;
            }
        }

    }
}
