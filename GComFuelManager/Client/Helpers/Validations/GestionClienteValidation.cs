using System;
using FluentValidation;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class GestionClienteValidation : AbstractValidator<Cliente>
	{
		public GestionClienteValidation()
		{
			RuleFor(x => x.grupo).NotEmpty().WithName("Grupo empresarial");
			RuleFor(x => x.Den).NotEmpty().WithName("Nombre del cliente");
			RuleFor(x => x.MdVenta).NotEmpty().WithName("Modelo de Venta");
            RuleFor(x => x.Tipven).NotEmpty().WithName("Tipo de Venta");
        }
	}
}

