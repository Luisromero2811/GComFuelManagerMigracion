using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace GComFuelManager.Shared.DTOs
{
    public class UsuarioInfo
    {
        public string Id { get; set; } = string.Empty;
        public int UserCod { get; set; }
        //Nombre de persona
        public string Nombre { get; set; } = string.Empty;
        //Nombre de usuario en sistema 
        public string UserName { get; set; } = string.Empty;
        //Roles asignados
        public List<string> Roles { get; set; } = new List<string>();
        //Contraseña
        public string Password { get; set; } = string.Empty;
        //Estado
        public bool Activo { get; set; } = true;
        public int? CodCte { get; set; }
        public short? CodGru { get; set; }
        public bool IsClient { get; set; } = false;
        public bool ShowPassword { get; set; } = false;
        [NotMapped]
        public bool passwordView { get; set; } = true;
        [NotMapped]
        public bool ShowUsersActions { get; set; } = false;
        public string Terminal { get; set; } = string.Empty;
    }
}
