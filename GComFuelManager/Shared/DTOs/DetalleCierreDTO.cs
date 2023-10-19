using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
    public class DetalleCierreDTO
    {
        [DisplayName("Folio")] public string? OC { get;set; } = string.Empty;
        [DisplayName("BOL")] public string? BOL { get; set; } = string.Empty;
        [DisplayName("Folio de referencia")] public string? FolioReferencia { get; set; } = string.Empty;
        [DisplayName("Fecha de cierre")] public string? FchCierre { get; set; } = string.Empty;
        [DisplayName("Destino")] public string? Destino { get; set; } = string.Empty;
        [DisplayName("Producto")] public string? Producto { get; set; } = string.Empty;
        [DisplayName("Precio")] public string? Precio { get; set; } = string.Empty;

        [EpplusIgnore]
        public double? Volumen { get; set; } = 0;
        [DisplayName("Volumen")]
        public string? Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }

        [DisplayName("Unidad")] public string? Unidad { get; set; } = string.Empty;
        [DisplayName("Estatus")] public string? Estatus { get; set; } = string.Empty;
        [DisplayName("Fecha de llegada")] public string? FchLlegada { get; set; } = string.Empty;
        [DisplayName("Observaciones")] public string? Observaciones { get; set; } = string.Empty;
    }
}
