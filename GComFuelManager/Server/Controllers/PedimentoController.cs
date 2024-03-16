using GComFuelManager.Client.Shared;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PedimentoController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public PedimentoController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public ActionResult Index([FromQuery] CierreFiltroDTO filtroDTO)
        {
            try
            {
                if (filtroDTO is null)
                    return BadRequest("valores no permitidos");

                List<Pedimento> pedimentos = context.Pedimentos.Where(x => x.Fecha_Actual >= filtroDTO.Fecha_Inicio && x.Fecha_Actual <= filtroDTO.Fecha_Fin && x.Activo == true)
                    .Include(x => x.Producto)
                    .OrderByDescending(x => x.Fecha_Actual)
                    .ToList();

                return Ok(pedimentos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{Id:int}/detalle")]
        public ActionResult Obtener_Pedimento([FromRoute] int Id)
        {
            try
            {
                Pedimento? pedimento = context.Pedimentos.Include(x => x.Producto).FirstOrDefault(x => x.Id == Id);

                if (pedimento is null)
                    return BadRequest("No se encontro el pedimento");

                List<OrdenEmbarque> Ordenes_Pedimento = new();
                var Ordenes_Pedimento_Query = context.OrdenEmbarque.Where(x => (x.Orden == null && x.Fchcar >= pedimento.Fecha_Actual && x.Fchcar <= pedimento.Fecha_Actual)
                                                                            || (x.Orden != null && x.Orden.Fchcar >= pedimento.Fecha_Actual && x.Orden.Fchcar <= pedimento.Fecha_Actual))
                    .Include(x => x.Orden).Include(x => x.Estado).Include(x => x.OrdenCierre).ThenInclude(x => x.Cliente).Include(x => x.Destino).Include(x => x.Producto)
                    .Include(x => x.Orden).ThenInclude(x => x.Destino)
                    .Include(x => x.Orden).Include(x => x.Producto)
                    .IgnoreAutoIncludes()
                    .AsQueryable();

                Ordenes_Pedimento_Query = Ordenes_Pedimento_Query.Where(x => (x.Codest == 20 && x.Orden != null) || (x.Codest == 22) || (x.Codest == 26 && x.Orden != null));

                Ordenes_Pedimento = Ordenes_Pedimento_Query.ToList();

                Ordenes_Pedimento.ForEach(x =>
                {
                    x.Pre = Obtener_Precio_Del_Dia_De_Orden(x.Cod).Precio;
                    x.Costo = pedimento.Costo;
                    pedimento.Litros_Totales += x.Obtener_Volumen_De_Orden();
                });

                pedimento.Ordens = Ordenes_Pedimento;

                return Ok(pedimento);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("ordenes")]
        public ActionResult Obtener_Ordenes_De_Pedimento([FromQuery] Folio_Activo_Vigente filtroDTO)
        {
            try
            {
                List<OrdenEmbarque> Ordenes_Pedimento = new();

                var Ordenes_Pedimento_Query = context.OrdenEmbarque.Where(x => (x.Orden == null && x.Fchcar >= filtroDTO.Fecha_Inicio && x.Fchcar <= filtroDTO.Fecha_Fin)
                                                                            || (x.Orden != null && x.Orden.Fchcar >= filtroDTO.Fecha_Inicio && x.Orden.Fchcar <= filtroDTO.Fecha_Fin))
                    .Include(x => x.Orden).Include(x => x.Estado).Include(x => x.OrdenCierre).ThenInclude(x => x.Cliente).Include(x => x.Destino).Include(x => x.Producto)
                    .Include(x => x.Orden).ThenInclude(x => x.Destino)
                    .Include(x => x.Orden).Include(x => x.Producto)
                    .IgnoreAutoIncludes()
                    .OrderByDescending(x=>x.Fchcar)
                    .AsQueryable();

                Ordenes_Pedimento_Query = Ordenes_Pedimento_Query.Where(x => (x.Codest == 20 && x.Orden != null) || (x.Codest == 22) || (x.Codest == 26 && x.Orden != null));

                Ordenes_Pedimento = Ordenes_Pedimento_Query.ToList();

                Ordenes_Pedimento.ForEach(x =>
                {
                    x.Pre = Obtener_Precio_Del_Dia_De_Orden(x.Cod).Precio;
                    x.Costo = Obtener_Costo_De_Pedimento(x).Costo;
                });

                return Ok(Ordenes_Pedimento);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Desactivar_Pedimento([FromRoute] int id)
        {
            try
            {
                Pedimento? pedimento = context.Pedimentos.Find(id);
                if (pedimento is null)
                    return NotFound();

                pedimento.Activo = false;

                context.Update(pedimento);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Crear_Pedimento([FromBody] Pedimento pedimento)
        {
            try
            {
                if (pedimento is null)
                    return BadRequest("Los valores no pueden estar vacios");

                if (pedimento.Id == 0)
                    context.Add(pedimento);
                else
                {
                    pedimento.Producto = null;
                    context.Update(pedimento);
                }

                await context.SaveChangesAsync();

                var Nuevo_Pedimento = context.Pedimentos.Where(x => x.Id == pedimento.Id).Include(x => x.Producto).FirstOrDefault();

                return Ok(Nuevo_Pedimento);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        private PrecioBolDTO Obtener_Precio_Del_Dia_De_Orden(int Id)
        {
            try
            {
                var orden = context.OrdenEmbarque.Where(x => x.Cod == Id)
                    .Include(x => x.Orden)
                    .Include(x => x.OrdenCierre)
                    .IgnoreAutoIncludes()
                    .FirstOrDefault();

                if (orden is null)
                    return new PrecioBolDTO();

                PrecioBolDTO precio = new();

                var precioVig = context.Precio.Where(x => orden != null && x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd)
                    .OrderByDescending(x => x.FchDia)
                    .FirstOrDefault();

                var precioPro = context.PrecioProgramado.Where(x => orden != null && x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd)
                    .OrderByDescending(x => x.FchDia)
                    .FirstOrDefault();

                var precioHis = context.PreciosHistorico.Where(x => orden != null && x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd
                    && orden.Fchcar != null && x.FchDia <= DateTime.Today)
                    .OrderByDescending(x => x.FchDia)
                    .FirstOrDefault();

                if (precioHis is not null)
                    precio.Precio = precioHis.pre;

                if (orden != null && precioVig is not null && orden.Fchcar is not null && orden.Fchcar.Value.Date == DateTime.Today)
                    precio.Precio = precioVig.Pre;

                if (orden != null && precioPro is not null && orden.Fchcar is not null && orden.Fchcar.Value.Date == DateTime.Today && context.PrecioProgramado.Any())
                    precio.Precio = precioPro.Pre;

                if (orden != null && context.OrdenPedido.Any(x => x.CodPed == orden.Cod))
                {
                    var ordenepedido = context.OrdenPedido.Where(x => x.CodPed == orden.Cod && !string.IsNullOrEmpty(x.Folio)).FirstOrDefault();

                    if (ordenepedido is not null)
                    {
                        var cierre = context.OrdenCierre.Where(x => x.Folio == ordenepedido.Folio
                         && x.CodPrd == orden.Codprd).FirstOrDefault();

                        if (cierre is not null)
                            precio.Precio = cierre.Precio;
                    }
                }

                if (orden is not null && precioHis is null && precioPro is null && precioVig is null && !precio.Es_Cierre)
                {
                    precio.Precio = orden.Pre;

                    if (orden.OrdenCierre is not null)
                        precio.Fecha_De_Precio = orden.OrdenCierre.fchPrecio;
                }

                precio.Moneda = !string.IsNullOrEmpty(precio.Moneda) ? precio.Moneda : "MXN";

                return precio;
            }
            catch (Exception e)
            {
                return new PrecioBolDTO();
            }
        }

        private Pedimento Obtener_Costo_De_Pedimento(OrdenEmbarque orden)
        {
            try
            {
                Pedimento? pedimento = new();

                pedimento = context.Pedimentos.Where(x => x.ID_Producto == orden.Codprd && x.Fecha_Actual <= orden.Fchcar).OrderByDescending(x => x.Fecha_Actual).FirstOrDefault();

                if (pedimento is not null)
                    return pedimento;

                return new Pedimento();
            }
            catch (Exception)
            {
                return new Pedimento();
            }
        }
    }
}
