using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.DTOs
{
    public class PreciosDTO
    {
        [DisplayName("PRODUCTO")]
        public string? Producto { get; set; } = string.Empty;
        [DisplayName("ZONA")]
        public string? Zona { get; set; } = string.Empty;
        [DisplayName("CLIENTE")]
        public string? Cliente { get; set; } = string.Empty;
        [DisplayName("DESTINO")]
        public string? Destino { get; set; } = string.Empty;
        [DisplayName("CODIGO SYNTHESIS")]
        public string? CodSyn { get; set; } = string.Empty;
        [DisplayName("CODIGO TUXPAN")]
        public string? CodTux { get; set; } = string.Empty;
        [DisplayName("FECHA")]
        public string? Fecha { get; set; } = string.Empty;
        [DisplayName("PRECIO FINAL")]
        public double Precio { get; set; } = 0;
    }
}
