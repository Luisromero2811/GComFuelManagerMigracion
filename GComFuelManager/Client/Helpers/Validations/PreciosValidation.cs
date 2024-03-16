using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
    public class PreciosValidation:AbstractValidator<Precio>
    {
        public PreciosValidation()
        {
            RuleFor(x => x.CodGru).NotEmpty().WithName("Grupo");
            RuleFor(x => x.CodZona).NotEmpty().WithName("Zona");
            RuleFor(x => x.CodCte).NotEmpty().WithName("Cliente");
            RuleFor(x => x.CodDes).NotEmpty().WithName("Destino");
            RuleFor(x => x.CodPrd).NotEmpty().WithName("Producto");
            RuleFor(x => x.Pre).NotEmpty().WithName("Precio");
        }
    }
}
