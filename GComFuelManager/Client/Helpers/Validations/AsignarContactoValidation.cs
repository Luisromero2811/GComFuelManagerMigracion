using System;
using FluentValidation;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
	public class AsignarContactoValidation : AbstractValidator<Contacto>
	{
		public AsignarContactoValidation()
		{
			RuleFor(x => x.CodCte).NotEmpty().WithName("Cliente");
			RuleFor(x => x.Nombre).NotEmpty().WithName("Nombre");
			RuleFor(x => x.Correo).NotEmpty().WithName("Correo").EmailAddress();
			RuleFor(x => x.Correo).NotEmpty().WithName("Correo").EmailAddress().WithMessage("El correo electrónico no es valido").Must(Correo => !Correo.StartsWith(" ")).WithMessage("El correo no puede comenzar con un espacio en blanco");
		}
	}
}

