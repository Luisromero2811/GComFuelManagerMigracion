using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GComFuelManager.Shared.Modelos;
using Newtonsoft.Json;

namespace GComFuelManager.Shared.DTOs
{
	public class ClienteTadDTO
	{
        //Instancias
         public Cliente? Cliente { get; set; } = null!;
         public Tad? Terminal { get; set; } = null!;
        //Listas
        public List<Tad> Tads { get; set; } = new List<Tad>();
        public List<Cliente> Clientes { get; set; } = new List<Cliente>();
        public List<Destino> Destinos { get; set; } = new List<Destino>();
        public List<GrupoTransportista> GrupoTransportistas { get; set; } = new List<GrupoTransportista>();
        public List<Transportista> Transportistas { get; set; } = new List<Transportista>();
        public List<Chofer> Chofer { get; set; } = new List<Chofer>();
        public List<Tonel> Toneles { get; set; } = new List<Tonel>();
        //Strings
        public string NombreCliente { get; set; } = string.Empty!;
        public string NombreTerminal { get; set; } = string.Empty!;
        //Codigos
        public int CodCte { get; set; }
        public Int16 Id_Tad { get; set; }
    }
}

