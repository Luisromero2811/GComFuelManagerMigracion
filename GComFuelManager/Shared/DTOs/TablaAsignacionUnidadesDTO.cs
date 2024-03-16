using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
    public class TablaAsignacionUnidadesDTO
    {
        [DisplayName("Orden de compra")]
        public string? OrdenCompra { get; set; } = string.Empty;
        public string? Referencia { get; set; } = string.Empty;
        [DisplayName("Unidad de Negocio")]
        public string? Unidad_Negocio { get; set; } = string.Empty;
        public string? Cliente { get; set; } = string.Empty;
        public string? Destino { get; set; } = string.Empty;
        public string? Producto { get; set; } = string.Empty;
        [EpplusIgnore]
        public double? Volumen { get; set; } = 0;
        [DisplayName("Volumen")]
        public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }
        [DisplayName("Fecha de carga")]
        public string? FechaCarga { get; set; } = string.Empty;
        public string? Transportista { get; set; } = string.Empty;
        public string? Unidad { get; set; } = string.Empty;
        public int? Compartimento { get; set; } = 0;
        public string? Operador { get; set; } = string.Empty;
        public int? Bin { get; set; } = 0;
        [DisplayName("Fecha estimada")]
        public string? Fecha { get; set; } = string.Empty;
        public string? Turno { get; set; } = string.Empty;
        //public string? Status { get; set; } = string.Empty;
    }
}

