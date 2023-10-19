using System;
using System.Text;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.DTOs
{
	public class AsignarContactoDTO
	{
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public Accion? accion { get; set; }

        public List<Accion> accione { get; set; } = new List<Accion>();

        public List<Accion> acciones { get; set; } = null!;
        public Cliente? cliente { get; set; } = null!;
       
    }
}

