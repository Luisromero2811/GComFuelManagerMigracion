using System;
using FluentValidation;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class AsignarGrupoValidation : AbstractValidator<Grupo>
	{
		public AsignarGrupoValidation()
		{
			RuleFor(x => x.Den).NotEmpty().WithName("Nombre");
			RuleFor(x => x.Tipven).NotEmpty().WithName("Tipo de Venta");
			RuleFor(x => x.MdVenta).NotEmpty().WithName("Modelo de Venta");
		}
	}
}

