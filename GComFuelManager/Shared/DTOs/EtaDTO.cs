﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
	public class EtaDTO
	{
        public string? Referencia { get; set; } = string.Empty;
        [DisplayName("Fecha de Programa")]
        public string? FechaPrograma { get; set; } = string.Empty;
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
        public string Volms { get { return VolNat.Value.ToString("N2"); } }

        [DisplayName("Volumen Cargado"), EpplusIgnore]
        public double? VolCar { get; set; } = 0;
        [DisplayName("Volumen Cargado")]
        public string Vols { get { return VolCar.Value.ToString("N2"); } }

        public string? Transportista { get; set; } = string.Empty;
        public string? Unidad { get; set; } = string.Empty;
        public string? Operador { get; set; } = string.Empty;

        [DisplayName("Fecha Documentacion")]
        public string? FechaDoc { get; set; } = string.Empty;
        //public string? Eta { get; set; } = string.Empty;
        [DisplayName("Fecha Estimada")]
        public string? FechaEst { get; set; } = string.Empty;
        [DisplayName("Estado de Orden")]
        public string? Trayecto { get; set; } = string.Empty;
        public string? Observaciones { get; set; } = string.Empty;
        [DisplayName("Fecha Real de Llegada")]
        public string? FechaRealEta { get; set; } = string.Empty;
        [DisplayName("Litros Entregados")]
        public double? LitEnt { get; set; } = 0;

    }
}

