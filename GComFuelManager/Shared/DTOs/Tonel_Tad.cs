using System;
using System.ComponentModel.DataAnnotations.Schema;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Shared.DTOs
{
	public class Tonel_Tad
	{
		public int Id_Tonel { get; set; }
		public short Id_Terminal { get; set; }

		[NotMapped] public Tonel? Tonel { get; set; } = null!;
		[NotMapped] public Tad? Terminal { get; set; } = null!;
	}
}

