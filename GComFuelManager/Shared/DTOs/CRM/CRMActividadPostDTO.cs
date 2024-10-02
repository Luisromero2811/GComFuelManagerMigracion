﻿using System;
namespace GComFuelManager.Shared.DTOs.CRM
{
	public class CRMActividadPostDTO
	{
        public int Id { get; set; }
        public int Asunto { get; set; }
        public DateTime Fecha_Creacion { get; set; } = DateTime.Now;
        public DateTime Fecha_Mod { get; set; }
        public DateTime Fch_Inicio { get; set; } = DateTime.Now;
        public DateTime Fecha_Fin { get; set; }
        public DateTime Fecha_Ven { get; set; } = DateTime.Now;
        public int Prioridad { get; set; }
        public int Asignado { get; set; }
        public string Desccripcion { get; set; } = string.Empty;
        public int Estatus { get; set; }
        public int Contacto_Rel { get; set; }
        public int Recordatorio { get; set; }
        public bool Activo { get; set; } = true;
        public int EquipoId { get; set; }
        public int CuentaId { get; set; }

        //Documento
        public int DocumentoId { get; set; }
        public string NombreDocumento { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime FechaCaducidad { get; set; } = DateTime.Today;
        public string Descripcion { get; set; } = string.Empty;
        public int DocumentoRelacionado { get; set; }
        public int DocumentoRevision { get; set; }
    }
}

