using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
    public class CierreGrupoValidation : AbstractValidator<OrdenCierre>
    {
        public CierreGrupoValidation()
        {
            RuleFor(o=>o.CodGru).NotEmpty().WithName("Grupo");
            RuleFor(o => o.CodPrd).NotEmpty().WithName("Producto");
            RuleFor(o => o.Volumen).NotEmpty().WithName("Volumen");
            RuleFor(o => o.TipoPago).NotEmpty().WithName("Tipo de pago");
        }
    }
}
