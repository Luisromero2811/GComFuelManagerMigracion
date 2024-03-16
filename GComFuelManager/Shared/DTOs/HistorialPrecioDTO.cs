using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml.Attributes;


namespace GComFuelManager.Shared.DTOs
{
    public class HistorialPrecioDTO
    {
        [DisplayName("Fecha")]
        public string? Fecha { get; set; } = string.Empty;

        [DisplayName("Precio")]
        public double? Pre { get; set; } = 0;

        [DisplayName("Producto")]
        public string? Producto { get; set; } = string.Empty;

        [DisplayName("Destino")]
        public string? Destino { get; set; } = string.Empty;

        [DisplayName("Zona")]
        public string? Zona { get; set; } = string.Empty;

        [DisplayName("Cliente")]
        public string? Cliente { get; set; } = string.Empty;

        [DisplayName("Moneda")]
        public string? Moneda { get; set; } = string.Empty;

        [DisplayName("Usuario")]
        public string? Usuario { get; set; } = string.Empty;
        
        [DisplayName("Fecha de subida")]
        public string? Fecha_De_Subida { get; set; } = string.Empty;

        [DisplayName("Unidad de Negocio")]
        public string? Unidad_Negocio { get; set; } = string.Empty;
    }
}

