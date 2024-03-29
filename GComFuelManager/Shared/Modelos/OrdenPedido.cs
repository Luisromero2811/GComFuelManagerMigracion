﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class OrdenPedido
    {
        [Key] public int? Cod { get; set; }
        public int? CodPed { get; set; }
        public string? Folio { get; set; } = string.Empty;
        [NotMapped] public OrdenEmbarque? OrdenEmbarque { get; set; } = new OrdenEmbarque();
    }
}
