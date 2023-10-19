using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Errors
    {

        [Key] 
        public string Cod { get; set; } = new Guid().ToString();
        public string Error { get; set; } = string.Empty;
        public DateTime Fch { get; set; } = DateTime.Now;
        public string Accion { get; set; } = string.Empty;
    }

    [Keyless]
    public class Error
    {
        public string Inner { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
