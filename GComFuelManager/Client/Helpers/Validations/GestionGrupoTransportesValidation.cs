using System;
using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class GestionGrupoTransportesValidation : AbstractValidator<GrupoTransportista>
	{
		public GestionGrupoTransportesValidation()
		{
			RuleFor(x => x.den).NotEmpty().WithName("Grupo de Transporte");
		}
	}
}

