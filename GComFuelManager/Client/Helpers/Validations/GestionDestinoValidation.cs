using System;
using FluentValidation;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class GestionDestinoValidation : AbstractValidator<Destino>
	{
		public GestionDestinoValidation()
		{
			RuleFor(x => x.Den).NotEmpty().WithName("Nombre del destino");
		}
	}
}

