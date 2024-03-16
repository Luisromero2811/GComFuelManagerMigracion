using System;
using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class GestionUnidadValidation : AbstractValidator<Tonel>
	{
		public GestionUnidadValidation()
		{
			RuleFor(x => x.Transportista).NotEmpty().WithName("Empresa transportista");
			RuleFor(x => x.Placa).NotEmpty().WithName("Placa");
			RuleFor(x => x.Nrocom).NotEmpty().WithName("Nº Compartimento");
			RuleFor(x => x.Capcom).NotEmpty().WithName("Capacidad");
			RuleFor(x => x.Tracto).NotEmpty().WithName("Tracto");
			RuleFor(x => x.Placatracto).NotEmpty().WithName("Placa-Tracto");
		}
	}
}

