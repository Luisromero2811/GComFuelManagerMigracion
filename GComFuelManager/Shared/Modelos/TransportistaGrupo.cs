using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System;
using OfficeOpenXml.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace GComFuelManager.Shared.Modelos
{
	public class TransportistaGrupo
	{
		[Key]
		public int Cod { get; set; }
		public int Codtra { get; set; } = 0;
		public int Codgru { get; set; } = 0;
		public DateTime Fch { get; set; } = DateTime.MinValue;
		public short? Id_Tad { get; set; } = 0;

        [NotMapped, EpplusIgnore] public Tad? Terminal { get; set; } = null!;
    }
}

