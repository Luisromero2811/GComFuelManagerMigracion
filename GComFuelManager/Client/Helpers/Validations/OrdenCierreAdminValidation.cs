using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
    public class OrdenCierreAdminValidation : AbstractValidator<OrdenCierre>
    {
        public OrdenCierreAdminValidation()
        {
            RuleFor(o => o.CodGru).NotEmpty().WithName("Grupo");
            RuleFor(o => o.CodCte).NotEmpty().WithName("Cliente");
            RuleFor(o => o.Volumen).NotEmpty().WithName("Volumen");
            RuleFor(o => o.CodDes).NotEmpty().WithName("Destino");
            RuleFor(o => o.CodPrd).NotEmpty().WithName("Producto");
            RuleFor(o => o.TipoPago).NotEmpty().WithName("Tipo de pago");
        }
    }
}
