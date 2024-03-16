using System;
using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class GestionEmpresaTransportesValidation : AbstractValidator<Transportista>
	{
		public GestionEmpresaTransportesValidation()
		{
			RuleFor(x => x.GrupoTransportista).NotEmpty().WithName("Grupo de Transporte");
			RuleFor(x => x.Den).NotEmpty().WithName("Empresa transportista");
		}
	}
}

