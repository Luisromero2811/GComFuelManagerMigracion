using System;
using GComFuelManager.Shared.Modelos;
using OfficeOpenXml.Attributes;
using System.ComponentModel;

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


        [DisplayName("Fecha de Cierre")]
        public string FchCie { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:d}", FchCierre); } }
        [DisplayName("Grupo")]
        public string NGrupo { get { return Grupo != null ? Grupo.Den! : string.Empty; } }
        //[DisplayName("Cliente")]
        ////public string NCliente { get { return cliente != null ? cliente.Den! : string.Empty; } }
        //[DisplayName("Destino")]
        //public string NDestino { get { return destino != null ? destino.Den! : string.Empty; } }
        [DisplayName("Producto")]
        public string NProducto { get { return Producto != null ? Producto.Den! : string.Empty; } }
    }
}

