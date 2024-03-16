using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GComFuelManager.Shared.Modelos;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
    public class EtaDTO
    {
        public string? Referencia { get; set; } = string.Empty;
        [DisplayName("Fecha de Programa")]
        public string? FechaPrograma { get; set; } = string.Empty;
        [DisplayName("Unidad de Negocio")]
        public string? Unidad_Negocio { get; set; } = string.Empty;
        [DisplayName("Estatus de Orden")]
        public string? EstatusOrden { get; set; } = string.Empty;
        [DisplayName("Fecha de Carga")]
        public string? FechaCarga { get; set; } = string.Empty;
        public int? Bol { get; set; } = 0;

        [DisplayName("Tipo de Venta")]
        public string DeliveryRack { get; set; } = string.Empty;

        public string? Cliente { get; set; } = string.Empty;
        public string? Destino { get; set; } = string.Empty;
        public string? Producto { get; set; } = string.Empty;

        [DisplayName("Volumen Natural"), EpplusIgnore]
        public double? VolNat { get; set; } = 0;
        [DisplayName("Volumen Natural")]
        public string Volms { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", VolNat); } }

        [DisplayName("Volumen Cargado"), EpplusIgnore]
        public double? VolCar { get; set; } = 0;
        [DisplayName("Volumen Cargado")]
        public string Vols { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", VolCar); } }

        public string? Transportista { get; set; } = string.Empty;
        public string? Unidad { get; set; } = string.Empty;
        public string? Operador { get; set; } = string.Empty;

        [DisplayName("Fecha Documentacion")]
        public string? FechaDoc { get; set; } = string.Empty;
        [DisplayName("Horas estimadas de viaje")]
        public string? Eta { get; set; } = string.Empty;
        [DisplayName("ETA")]
        public string? FechaEst { get; set; } = string.Empty;
        [DisplayName("Estado de Orden")]
        public string? Trayecto { get; set; } = string.Empty;
        public string? Observaciones { get; set; } = string.Empty;
        [DisplayName("Fecha Real de Llegada")]
        public string? FechaRealEta { get; set; } = string.Empty;

        [DisplayName("Litros Entregados"), EpplusIgnore]
        public double? LitEnt { get; set; } = 0;
        [DisplayName("Litros Entregados")]
        public string EntLit { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", LitEnt); } }

        [EpplusIgnore]
        public Orden? orden { get; set; } 
        [EpplusIgnore]
        public OrdenEmbarque? ordenEmbarque { get; set; }
        [EpplusIgnore]
        public string? FechaCargaEmbarque { get { return ordenEmbarque?.Fchcar.ToString(); } }
        [EpplusIgnore]
        public int? Compartimento { get; set; } = null!;
        [EpplusIgnore]
        public decimal? Capcom { get { return ordenEmbarque?.Tonel?.Capcom; } }
        [EpplusIgnore]
        public decimal? Capcom2 { get { return ordenEmbarque?.Tonel?.Capcom2; } }
        [EpplusIgnore]
        public decimal? Capcom3 { get { return ordenEmbarque?.Tonel?.Capcom3; } }
        [EpplusIgnore]
        public decimal? Capcom4 { get { return ordenEmbarque?.Tonel?.Capcom4; } }
        [EpplusIgnore]
        public string? VolumenN { get { return orden?.Volumenes; } }
    }
}

