using GComFuelManager.Shared.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.DTOs
{
    public class FolioDetalleDTO
    {
        public string? Folio { get; set; } = string.Empty;
        public Cliente? Cliente { get; set; } = null!;
        public Destino? Destino { get; set; } = null!;
        public Producto? Producto { get; set; } = null!;
        public DateTime? FchCierre { get; set; } = null!;
        public string? Comentarios { get; set; } = null!;
    }
}
