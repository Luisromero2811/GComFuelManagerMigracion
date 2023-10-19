using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
	public class OrdenesDTO
	{
        public string? Referencia { get; set; } = string.Empty;
        [DisplayName("Fecha de programa")]
        public string? FechaPrograma { get; set; } = string.Empty;
        [DisplayName("Estado de la Orden")]
        public string EstatusOrden { get; set; } = string.Empty;
        [DisplayName("Fecha de Carga")]
		public string? FechaCarga { get; set; } = string.Empty;
        public int? Bol { get; set; } = 0;
        //[DisplayName("Tipo de Venta")]
        //public string DeliveryRack { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;

        [DisplayName("Volumen Natural"), EpplusIgnore]
        public double? VolNat { get; set; } = 0;
        [DisplayName("Volumen Natural")]
        public string Vols { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", VolNat); } }

        [DisplayName("Volumen Cargado"), EpplusIgnore]
        public double? VolCar { get; set; } = 0;
        [DisplayName("Volumen Cargado")]
        public string Volms { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", VolCar); } }

        public string Transportista { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public string Operador { get; set; } = string.Empty;
       
    }
}

