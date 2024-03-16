using System;
using GComFuelManager.Shared.Modelos;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeOpenXml.Attributes;
using System.ComponentModel;

namespace GComFuelManager.Shared.DTOs
{
    public class PrecioBolDTO
    {

        [EpplusIgnore]
        public string? Referencia { get; set; } = string.Empty;
        [EpplusIgnore]
        public int? Folio { get; set; }
        [EpplusIgnore]
        public DateTime? Fecha_De_Precio { get; set; } = DateTime.MinValue;
        [EpplusIgnore]
        public string? Destino_Synthesis { get; set; } = string.Empty;
        [EpplusIgnore]
        public string? Producto_Synthesis { get; set; } = string.Empty;
        [EpplusIgnore]
        public bool Es_Cierre { get; set; } = false;
        [EpplusIgnore]
        public bool Es_Precio_De_Creacion { get; set; } = false;
        [EpplusIgnore]
        public bool Precio_Encontrado { get; set; } = false;
        [EpplusIgnore]
        public string Precio_Encontrado_En { get; set; } = string.Empty;
        [EpplusIgnore]
        public double Tipo_De_Cambio { get; set; } = 1;
        [EpplusIgnore]
        public string Moneda { get; set; } = "MXN";
        [EpplusIgnore] public int? Tipo_Moneda { get; set; } = 1;

        [EpplusIgnore]
        public DateTime? Fecha_De_Carga { get; set; } = DateTime.MinValue;
        [DisplayName("Fecha de Carga")]
        public string Fechas
        {
            get
            {
                if (Fecha_De_Carga is not null)
                    return Fecha_De_Carga.Value.ToString();
                return DateTime.MinValue.ToString();
            }
        }
        [DisplayName("BOL")]
        public int? BOL { get; set; } = 0;
        [DisplayName("Unidad de Negocio")]
        public string? Unidad_Negocio { get; set; } = string.Empty;
        [DisplayName("Cliente")]
        public string? Cliente_Original { get; set; } = string.Empty;
        [DisplayName("Destino")]
        public string? Destino_Original { get; set; } = string.Empty;
        [DisplayName("Producto")]
        public string? Producto_Original { get; set; } = string.Empty;
        [DisplayName("Volumen")]
        public double? Volumen_Cargado { get; set; } = 0;
        [DisplayName("Precio")]
        public double? Precio { get; set; } = 0;

        [EpplusIgnore]
        public string Folio_Cierre { get; set; } = string.Empty;
        //[DisplayName("Tipo de venta")]
        ////public string? TipoVenta { get { return OrdenCierre!.TipoPago; } }
        //public string TipoVenta { get; set; } = string.Empty;

        //Grupo-Cliente
        [EpplusIgnore]
        public DateTime FchInicio { get; set; } = DateTime.Now;
        [EpplusIgnore]
        public DateTime FchFin { get; set; } = DateTime.Now;

        //Propiedades de navegación
        [NotMapped, EpplusIgnore] public Orden? Orden { get; set; } = null!;
        [NotMapped, EpplusIgnore] public OrdenCierre? OrdenCierre { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Cliente? Cliente { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Destino? Destino { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Producto? Producto { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Tad? Tad { get; set; } = null!;
    }
}

