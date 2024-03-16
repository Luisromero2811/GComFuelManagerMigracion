using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RedireccionController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserToken verifyUser;
        private readonly User_Terminal _terminal;

        public RedireccionController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserToken verifyUser, User_Terminal _Terminal)
        {
            this.context = context;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
            this._terminal = _Terminal;
        }

        [HttpGet]
        public ActionResult Obtener_Redireccionamientos([FromQuery] Folio_Activo_Vigente filtro_)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var redireccionamientos = context.Redireccionamientos.Where(x => x.Fecha_Red >= filtro_.Fecha_Inicio && x.Fecha_Red <= filtro_.Fecha_Fin)
                    .Include(x => x.Grupo)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.Orden)
                    .ThenInclude(x => x.Producto)
                    .Include(x => x.Orden)
                    .ThenInclude(x => x.OrdenEmbarque)
                    .Include(x => x.Orden)
                    .ThenInclude(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .IgnoreAutoIncludes()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(filtro_.Cliente_Filtrado))
                    redireccionamientos = redireccionamientos.Where(x => x.Cliente != null && !string.IsNullOrEmpty(x.Cliente.Den) && x.Cliente.Den.ToLower().Contains(filtro_.Cliente_Filtrado.ToLower()));

                if (!string.IsNullOrEmpty(filtro_.Cliente_Original))
                    redireccionamientos = redireccionamientos.Where(x => x.Orden != null && x.Orden.Destino != null && x.Orden.Destino.Cliente != null
                    && !string.IsNullOrEmpty(x.Orden.Destino.Cliente.Den) && x.Orden.Destino.Cliente.Den.ToLower().Contains(filtro_.Cliente_Original.ToLower()));

                if (!string.IsNullOrEmpty(filtro_.Destino_Filtrado))
                    redireccionamientos = redireccionamientos.Where(x => x.Destino != null && !string.IsNullOrEmpty(x.Destino.Den) && x.Destino.Den.ToLower().Contains(filtro_.Destino_Filtrado.ToLower()));

                if (!string.IsNullOrEmpty(filtro_.Destino_Original))
                    redireccionamientos = redireccionamientos.Where(x => x.Orden != null && x.Orden.Destino != null && !string.IsNullOrEmpty(x.Orden.Destino.Den)
                    && x.Orden.Destino.Den.ToLower().Contains(filtro_.Destino_Original.ToLower()));

                if (!string.IsNullOrEmpty(filtro_.Producto_Filtrado))
                    redireccionamientos = redireccionamientos.Where(x => x.Orden != null && x.Orden.Producto != null && !string.IsNullOrEmpty(x.Orden.Producto.Den)
                    && x.Orden.Producto.Den.ToLower().Contains(filtro_.Producto_Filtrado.ToLower()));

                if (filtro_.BOL is not null)
                    redireccionamientos = redireccionamientos.Where(x => x.Orden != null && x.Orden.BatchId != null && x.Orden.BatchId.ToString().Contains(filtro_.BOL.ToString()));

                var redirecciones_con_precio = redireccionamientos.ToList();

                redirecciones_con_precio.ForEach(x =>
                {
                    if (x.Orden is not null)
                        if (x.Orden.OrdenEmbarque is not null)
                            if (x.Orden.OrdenEmbarque.Pre is not null)
                                x.Orden.OrdenEmbarque.Pre = Obtener_Precio_Del_Dia_De_Orden_Synthesis(x.Orden.Cod, id_terminal).Precio;
                });

                return Ok(redireccionamientos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Guardar_Redireccionamientos(Redireccionamiento redireccionamiento)
        {
            try
            {

                if (redireccionamiento is null)
                    return BadRequest();

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                var orden_synthesis = context.Orden.Find(redireccionamiento.Id_Orden);

                if (orden_synthesis is not null)
                {
                    var orden_enviada = context.OrdenEmbarque.IgnoreAutoIncludes().FirstOrDefault(x => x.FolioSyn == orden_synthesis.Ref && x.Codest != 14);
                    if (orden_enviada is not null)
                    {
                        var pertenece_a_cierre = context.OrdenPedido.Any(x => x.CodPed == orden_enviada.Cod && x.Pedido_Original == 0 && string.IsNullOrEmpty(x.Folio_Cierre_Copia));
                        if (pertenece_a_cierre)
                        {
                            var orden_pedido = context.OrdenPedido.IgnoreAutoIncludes().FirstOrDefault(x => x.CodPed == orden_enviada.Cod);

                            if (orden_pedido is not null && !string.IsNullOrEmpty(orden_pedido.Folio))
                            {
                                var cierre = context.OrdenCierre.IgnoreAutoIncludes().FirstOrDefault(x => x.Folio == orden_pedido.Folio && x.CodPrd == orden_enviada.Codprd && x.Estatus == true && x.Activa == true);

                                var cierre_destino_redireccionado = context.OrdenCierre.Where(x => x.CodPed == 0 && x.CodGru == redireccionamiento.Grupo_Red && x.Estatus == true && x.Activa == true)
                                    .OrderByDescending(x => x.FchCierre).IgnoreAutoIncludes().ToList();

                                if (cierre_destino_redireccionado is not null)
                                {
                                    var cierre_destino_redireccionado_individual = cierre_destino_redireccionado.FirstOrDefault(x => x.CodDes == redireccionamiento.Destino_Red && x.CodCte == redireccionamiento.Cliente_Red);

                                    if (cierre_destino_redireccionado_individual is not null)
                                    {
                                        OrdenEmbarque nueva_orden_embarque = orden_enviada.HardCopy();

                                        nueva_orden_embarque.Cod = 0;
                                        nueva_orden_embarque.Fchpet = DateTime.Now;
                                        nueva_orden_embarque.Coddes = redireccionamiento.Destino_Red;

                                        context.Add(nueva_orden_embarque);

                                        await context.SaveChangesAsync(id, 35);

                                        var cierre_orden_embarque = context.OrdenCierre.FirstOrDefault(x => x.CodPed == orden_enviada.Cod);
                                        if (cierre_orden_embarque is not null)
                                        {
                                            OrdenCierre nuevo_cierre_orden_embarque = cierre_orden_embarque.HardCopy();
                                            nuevo_cierre_orden_embarque.Cod = 0;
                                            nuevo_cierre_orden_embarque.CodPed = nueva_orden_embarque.Cod;
                                            nuevo_cierre_orden_embarque.CodGru = redireccionamiento.Grupo_Red;
                                            nuevo_cierre_orden_embarque.CodCte = redireccionamiento.Cliente_Red;
                                            nuevo_cierre_orden_embarque.CodDes = redireccionamiento.Destino_Red;

                                            context.Add(nuevo_cierre_orden_embarque);
                                            await context.SaveChangesAsync(id, 35);

                                            OrdenPedido nueva_orden_pedido = new()
                                            {
                                                Folio = cierre_destino_redireccionado_individual.Folio,
                                                CodCierre = cierre_destino_redireccionado_individual.Cod,
                                                CodPed = nueva_orden_embarque.Cod
                                            };

                                            context.Add(nueva_orden_pedido);
                                            await context.SaveChangesAsync(id, 35);
                                        }
                                    }
                                    else
                                    {
                                        var cierre_destino_redireccionado_grupal = cierre_destino_redireccionado.OrderByDescending(x => x.FchCierre).FirstOrDefault();
                                        if (cierre_destino_redireccionado_grupal is not null)
                                        {
                                            OrdenEmbarque nueva_orden_embarque = orden_enviada.ShallowCopy();

                                            nueva_orden_embarque.Cod = 0;
                                            nueva_orden_embarque.Coddes = redireccionamiento.Grupo_Red;
                                            nueva_orden_embarque.Coddes = redireccionamiento.Cliente_Red;
                                            nueva_orden_embarque.Coddes = redireccionamiento.Destino_Red;

                                            context.Add(nueva_orden_embarque);

                                            await context.SaveChangesAsync(id, 35);

                                            var cierre_orden_embarque = context.OrdenCierre.FirstOrDefault(x => x.CodPed == orden_enviada.Cod);
                                            if (cierre_orden_embarque is not null)
                                            {
                                                OrdenCierre nuevo_cierre_orden_embarque = cierre_orden_embarque.ShallowCopy();
                                                nuevo_cierre_orden_embarque.Cod = 0;
                                                nuevo_cierre_orden_embarque.CodPed = nueva_orden_embarque.Cod;
                                                nuevo_cierre_orden_embarque.CodGru = redireccionamiento.Grupo_Red;
                                                nuevo_cierre_orden_embarque.CodCte = redireccionamiento.Cliente_Red;
                                                nuevo_cierre_orden_embarque.CodDes = redireccionamiento.Destino_Red;

                                                context.Add(nuevo_cierre_orden_embarque);
                                                await context.SaveChangesAsync(id, 35);

                                                OrdenPedido nueva_orden_pedido = new()
                                                {
                                                    Folio = cierre_destino_redireccionado_grupal.Folio,
                                                    CodCierre = cierre_destino_redireccionado_grupal.Cod,
                                                    CodPed = nueva_orden_embarque.Cod
                                                };

                                                context.Add(nueva_orden_pedido);
                                                await context.SaveChangesAsync(id, 35);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        orden_enviada.Codest = 41;
                        orden_enviada.FolioSyn = string.Empty;
                        orden_enviada.Bol = orden_synthesis.BatchId;
                        orden_enviada.Vol = orden_synthesis.Vol;

                        context.Update(orden_enviada);

                        context.Add(redireccionamiento);
                        await context.SaveChangesAsync(id, 35);
                    }
                }

                //if (redireccionamiento.Id != 0)
                //{
                //    context.Update(redireccionamiento);
                //    await context.SaveChangesAsync(id, 36);

                //}
                //else
                //{
                //    context.Add(redireccionamiento);
                //    await context.SaveChangesAsync(id, 35);
                //}

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("checar")]
        public ActionResult Checar_Redireccion([FromQuery] Folio_Activo_Vigente redireccionamiento)
        {
            try
            {
                if (redireccionamiento is null)
                    return NotFound();

                bool redireccion = context.Redireccionamientos.Any(x => x.Id_Orden == redireccionamiento.ID_Orden);

                return Ok(redireccion);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        private PrecioBolDTO Obtener_Precio_Del_Dia_De_Orden_Synthesis(long? Id, int id_terminal)
        {
            try
            {
                var orden = context.Orden.Where(x => x.Cod == Id)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.OrdenCierre)
                    .IgnoreAutoIncludes()
                    .FirstOrDefault();

                if (orden is null)
                    return new PrecioBolDTO();

                PrecioBolDTO precio = new();

                var precioVig = context.Precio.Where(x => orden != null && x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd && x.Id_Tad == id_terminal)
                    .OrderByDescending(x => x.FchActualizacion)
                    .FirstOrDefault();

                var precioPro = context.PrecioProgramado.Where(x => orden != null && x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd && x.Id_Tad == id_terminal)
                    .OrderByDescending(x => x.FchActualizacion)
                    .FirstOrDefault();

                var precioHis = context.PreciosHistorico.Where(x => orden != null && x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd && x.FchDia <= orden.Fchcar && x.Id_Tad == id_terminal)
                    .OrderByDescending(x => x.FchActualizacion)
                    .FirstOrDefault();

                if (precioHis is not null)
                    precio.Precio = precioHis.pre;

                if (orden != null && precioVig is not null && precioVig.FchDia == DateTime.Today)
                    precio.Precio = precioVig.Pre;

                if (orden != null && precioPro is not null && precioPro.FchDia == DateTime.Today && context.PrecioProgramado.Any())
                    precio.Precio = precioPro.Pre;

                if (orden != null && orden.OrdenEmbarque is not null && context.OrdenPedido.Any(x => x.CodPed == orden.OrdenEmbarque.Cod && x.Pedido_Original == 0 && string.IsNullOrEmpty(x.Folio_Cierre_Copia)))
                {
                    var ordenepedido = context.OrdenPedido.Where(x => x.CodPed == orden.OrdenEmbarque.Cod && !string.IsNullOrEmpty(x.Folio) && x.Pedido_Original == 0 && string.IsNullOrEmpty(x.Folio_Cierre_Copia)).FirstOrDefault();

                    if (ordenepedido is not null)
                    {
                        var cierre = context.OrdenCierre.Where(x => x.Folio == ordenepedido.Folio
                         && x.CodPrd == orden.Codprd).FirstOrDefault();

                        if (cierre is not null)
                            precio.Precio = cierre.Precio;
                    }
                }

                if (orden is not null && orden.OrdenEmbarque is not null && precioHis is null && precioPro is null && precioVig is null && !precio.Es_Cierre)
                {
                    precio.Precio = orden.OrdenEmbarque.Pre;

                    if (orden.OrdenCierre is not null)
                        precio.Fecha_De_Precio = orden.OrdenCierre.fchPrecio;
                }

                return precio;
            }
            catch (Exception e)
            {
                return new PrecioBolDTO();
            }
        }
    }
}
