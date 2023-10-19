using GComFuelManager.Shared.DTOs;
using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GComFuelManager.Shared.Modelos
{
    public class OrdenCierre
    {
        [Key, JsonPropertyName("cod"), EpplusIgnore]
        public int Cod { get; set; }
        
        [JsonPropertyName("fchCierre"),EpplusIgnore]
        public DateTime? FchCierre { get; set; } = DateTime.Today;
        [JsonPropertyName("fchVencimiento"), EpplusIgnore]
        public DateTime? FchVencimiento { get; set; } = DateTime.Today;

        [DisplayName("Fecha de cierre"), NotMapped]
        public string? Fch { get { return FchCierre!.Value.ToString("dd/MM/yyyy"); } }

        [DisplayName("BOL")]
        public string? BOL { get { return OrdenEmbarque is not null ? OrdenEmbarque.Orden is not null ? OrdenEmbarque.Orden.BatchId.ToString() : string.Empty : string.Empty; } }

        [DisplayName("Fecha de vencimiento"), NotMapped]
        public string? FchVen { get { return FchVencimiento?.ToString("dd/MM/yyyy"); } }

        [JsonPropertyName("folio"), DisplayName("Folio")]
        public string? Folio { get; set; } = string.Empty;
        
        [JsonPropertyName("contacto"), EpplusIgnore]
        public string? Contacto { get { return ContactoN != null ? ContactoN.Nombre : string.Empty; } }
        
        [JsonPropertyName("email"), EpplusIgnore]
        public string? Email { get { return ContactoN != null ? ContactoN.Correo : string.Empty; } }
        
        [JsonPropertyName("codPrd"), EpplusIgnore]
        public byte? CodPrd { get; set; }
        
        [NotMapped, EpplusIgnore] 
        public Producto? Producto { get; set; } = null!;
        [DisplayName("producto")]
        public string? Pro { get { return Producto != null ? Producto.Den : string.Empty; } }
        
        [JsonPropertyName("codCte"), EpplusIgnore]
        public int? CodCte { get; set; }
        
        [NotMapped, EpplusIgnore] 
        public Cliente? Cliente { get; set; } = null!;
        
        [JsonPropertyName("modeloVenta"), EpplusIgnore]
        public string? ModeloVenta { get; set; } = string.Empty;

        [JsonPropertyName("tipoVenta"), DisplayName("Tipo de Venta")]
        public string? TipoVenta { get; set; } = string.Empty;

        [JsonPropertyName("tipoPago"), DisplayName("Tipo de Pago")]
        public string? TipoPago { get; set; } = string.Empty;

        [JsonPropertyName("precio"), DisplayName("Precio")]
        public double Precio { get; set; } = 0;
        
        //[JsonPropertyName("temperatura"), DisplayName("Temperatura")]
        //[NotMapped] public double? Temperatura { get; set; } = null!;
        
        [JsonPropertyName("vendedor"), DisplayName("Vendedor")]
        public string? Vendedor { get; set; } = string.Empty;
        
        [JsonPropertyName("codDes"), EpplusIgnore]
        public int? CodDes { get; set; }
        
        [NotMapped, EpplusIgnore] 
        public Destino? Destino { get; set; } = null!;

        [DisplayName("destino")]
        public string? Des { get { return Destino != null ? Destino.Den : string.Empty; } }

        [DisplayName("cliente")]
        public string? Cli { get { return Cliente != null ? Cliente.Den : string.Empty; } }

        [JsonPropertyName("volumen"), DisplayName("Volumen"), EpplusIgnore]
        public int? Volumen { get; set; }
        [DisplayName("Volumen")]
        public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }

        [JsonPropertyName("observaciones"), DisplayName("Observaciones")]
        public string? Observaciones { get; set; } = string.Empty;
        
        [JsonPropertyName("estatus"), EpplusIgnore]
        public bool? Estatus { get; set; } = true;

        [JsonPropertyName("Activa"), EpplusIgnore]
        public bool? Activa { get; set; } = true;

        [JsonPropertyName("confirmada"), EpplusIgnore]
        public bool? Confirmada { get; set; } = true;

        [JsonProperty("codCon"), EpplusIgnore]
        public int? CodCon { get; set; }

        [JsonProperty("codPed"), EpplusIgnore]
        public int? CodPed { get; set; } = 0;

        [EpplusIgnore, NotMapped]
        public OrdenEmbarque? OrdenEmbarque { get; set; } = null!;

        [NotMapped, EpplusIgnore]
        public Contacto? ContactoN { get; set; } = null!;

        [EpplusIgnore, JsonProperty("fchLlegada")]
        public DateTime? FchLlegada { get; set; } = DateTime.Now;

        [DisplayName("Fecha de llegada"), NotMapped]
        public string? FechaLlegada { get { return FchLlegada!.Value.ToString("D"); } }

        [DisplayName("Turno"), JsonPropertyName("turno")]
        public string? Turno { get; set; } = string.Empty;

        [EpplusIgnore, JsonPropertyName("codGru")]
        public Int16? CodGru { get; set; }

        [EpplusIgnore, NotMapped]
        public Grupo? Grupo { get; set; } = null!;

        [EpplusIgnore, NotMapped]
        public bool IsCierreVolumen { get; set; } = true;

        [EpplusIgnore, NotMapped]
        public bool IsDifferentVol { get; set; } = false;
        //Turno cambiado
        [EpplusIgnore, NotMapped]
        public bool IsDifferentTurn { get; set; } = false;

        [EpplusIgnore, NotMapped]
        public DateTime? FchCar { get; set; } = DateTime.Today;
        [EpplusIgnore, NotMapped]
        public Int16? CodTad { get; set; } = 1;
        [EpplusIgnore, NotMapped]
        public bool IsAutoPrecio { get; set; } = true;

        [EpplusIgnore, NotMapped]
        public VolumenDisponibleDTO? VolumenDisponible { get; set; } = new VolumenDisponibleDTO();
        [EpplusIgnore, NotMapped]
        public bool isGroup { get; set; } = false;
        [EpplusIgnore, NotMapped]
        public bool PrecioOverDate { get; set; }

        [DisplayName("fchPrecio"), EpplusIgnore]
        public DateTime? fchPrecio { get; set; } = DateTime.Now;
        [DisplayName("Fecha de Precio"), NotMapped]
        public string? FchPre { get { return fchPrecio?.ToString("dd/MM/yyyy"); } }

        //Props de navegación
      
        [NotMapped] public Cliente? Cllientes { get; set; } = null!;
        [NotMapped] public Destino? Destinos { get; set; } = null!;
        [NotMapped] public Producto? Prods { get; set; } = null!;
        [NotMapped] public Grupo? Grups { get; set; } = null!;
    } 
}
