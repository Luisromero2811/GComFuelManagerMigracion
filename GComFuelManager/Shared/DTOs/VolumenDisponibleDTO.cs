using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.DTOs
{
    public class VolumenDisponibleDTO
    {
        public int VolumenTotal { get; set; } = 0;
        public List<ProductoVolumen> Productos { get; set; } = new List<ProductoVolumen>();
    }

    public class ProductoVolumen
    {
        public string? Nombre { get; set; } = string.Empty;
        public double? Total { get; set; } = 0;
        public double? Disponible { get; set; } = 0;
        public double? Congelado { get; set; } = 0;
        public double? Consumido { get; set; } = 0;
        public double? Remanente { get; set; } = 0;
        public double? PromedioCarga { get; set; } = 0;
        public double? Programado { get; set; } = 0;
        public double? Solicitud { get; set; } = 0;
    }
}
