using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GComFuelManager.Shared.Modelos;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
    public class ParametrosBusquedaPrecios
    {
        public string producto { get; set; } = string.Empty;
        public string destino { get; set; } = string.Empty;
        public string cliente { get; set; } = string.Empty;
        public string zona { get; set; } = string.Empty;
        public int pagina { get; set; } = 1;
        public int tamanopagina { get; set; } = 10;
        public DateTime DateInicio { get; set; } = DateTime.Today.Date;
        public DateTime DateFin { get; set; } = DateTime.Now;
        [EpplusIgnore] public Tad? Tad { get; set; }
    }
}
