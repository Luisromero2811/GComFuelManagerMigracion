using System;
using GComFuelManager.Shared.Modelos;
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace GComFuelManager.Shared.DTOs
{
	public class FolioCierreGrupalDTO
	{
        public string? Folio { get; set; } = null!;
        //[EpplusIgnore]
        //public Cliente? cliente { get; set; } = null!;
        [EpplusIgnore]
        public Destino? destino { get; set; } = null!;
        [EpplusIgnore]
        public Producto? Producto { get; set; } = null!;
        [EpplusIgnore]
        public DateTime? FchCierre { get; set; } = DateTime.MinValue;
        [EpplusIgnore]
        public Grupo? Grupo { get; set; } = null!;
        [EpplusIgnore]
        public Tad? Terminal { get; set; } = null!;
        [NotMapped, EpplusIgnore]
        public string? Estado { get; set; } = null!;
        [NotMapped, EpplusIgnore]
        public OrdenEmbarque? ordenEmbarque { get; set; } = null!;
        [NotMapped, EpplusIgnore]
        public bool? Activa { get; set; } = true;
       

        [DisplayName("Fecha de Cierre")]
        public string FchCie { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:d}", FchCierre); } }
        [DisplayName("Unidad de Negocio")]
        public string Terminales { get; set; } = null!;
        [DisplayName("Grupo")]
        public string NGrupo { get { return Grupo != null ? Grupo.Den! : string.Empty; } }
        //[DisplayName("Cliente")]
        ////public string NCliente { get { return cliente != null ? cliente.Den! : string.Empty; } }
        //[DisplayName("Destino")]
        //public string NDestino { get { return destino != null ? destino.Den! : string.Empty; } }
        [DisplayName("Producto")]
        public string NProducto { get { return Producto != null ? Producto.Den! : string.Empty; } }

        //Volumenes
        //[EpplusIgnore]
        public int? Volumen { get; set; }
        //[DisplayName("Volumen"), NotMapped]
        //public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }
        //Precio
        [NotMapped]
        public double Precio { get; set; } = 0;
        //Observaciones
        public string? Observaciones { get; set; } = string.Empty;

    }
}

