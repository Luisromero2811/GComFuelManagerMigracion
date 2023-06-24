﻿using System;
using FluentValidation;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class AsignarGrupoClienteValidation : AbstractValidator<GrupoClienteDTO>
	{
		public AsignarGrupoClienteValidation()
		{
			RuleFor(x => x.grupo).NotEmpty().WithName("Grupo");
			RuleFor(x => x.cliente).NotEmpty().WithName("Cliente");
		}
	}
}

