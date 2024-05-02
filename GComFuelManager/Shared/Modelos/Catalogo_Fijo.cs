﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Catalogo_Fijo
    {
        public int Id { get; set; }
        [StringLength(250)]
        public string Valor { get; set; } = string.Empty;
        public short Catalogo { get; set; }
        public bool Activo { get; set; } = true;
    }
}
