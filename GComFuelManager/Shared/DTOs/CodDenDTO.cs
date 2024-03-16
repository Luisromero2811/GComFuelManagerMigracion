using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GComFuelManager.Shared.Modelos;
using OfficeOpenXml.Attributes;

namespace GComFuelManager.Shared.DTOs
{
    public class CodDenDTO
    {
        [EpplusIgnore]
        public int Cod { get; set; } = 0;
        [DisplayName("Nombre del destino")]
        public string Den { get; set; } = string.Empty;
        [EpplusIgnore]
        public bool Activo { get; set; } = true;
        [EpplusIgnore]
        public Cliente? cliente { get; set; }
        [EpplusIgnore]
        public Destino? destino { get; set; }
        [EpplusIgnore]
        public Grupo? grupo { get; set; }
        [EpplusIgnore]
        public int pagina { get; set; } = 1;
        [EpplusIgnore]
        public int tamanopagina { get; set; } = 10;
        [EpplusIgnore] public List<Tad> Terminales { get; set; } = new();
    }
}
