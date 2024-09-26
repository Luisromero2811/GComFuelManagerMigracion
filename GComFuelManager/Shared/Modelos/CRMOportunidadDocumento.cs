﻿namespace GComFuelManager.Shared.Modelos
{
    public class CRMOportunidadDocumento
    {
        public int OportunidadId { get; set; }
        public int DocumentoId { get; set; }

        public CRMOportunidad? Oportunidad { get; set; } = null!;
        public CRMDocumento? Documento { get; set; } = null!;
    }
}
