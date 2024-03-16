using FluentValidation;
using GComFuelManager.Shared.Modelos;

namespace GComFuelManager.Client.Helpers.Validations
{
    public class PedidoOrdenValidation : AbstractValidator<OrdenCierre>
    {
        public PedidoOrdenValidation()
        {
            RuleFor(o => o.CodGru).NotEmpty().WithName("Grupo");
            RuleFor(o => o.CodCte).NotEmpty().WithName("Cliente");
            RuleFor(o => o.CodDes).NotEmpty().WithName("Estacion");
            RuleFor(o => o.CodPrd).NotEmpty().WithName("Producto");
            RuleFor(o => o.FchLlegada).NotEmpty().WithName("Fecha de llegada estimada");
            RuleFor(o => o.Turno).NotEmpty().WithName("Turno");
            RuleFor(o => o.Volumen).NotEmpty().WithName("Volumen");
            RuleFor(o => o.Id_Tad).NotEmpty().WithName("Terminal");
            RuleFor(o => o.FchCar).NotEmpty().WithName("Fecha de carga");
            RuleFor(o => o.Folio_Perteneciente).NotNull().WithName("Folio de pedido");
        }
    }
}
