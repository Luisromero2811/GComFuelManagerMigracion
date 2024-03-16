using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ServiceReference8;//prod
//using ServiceReference6;//qa
using System.Diagnostics;

namespace GComFuelManager.Server.Controllers.Cierres
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly VerifyUserId verifyUser;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly User_Terminal _terminal;

        public ClientesController(ApplicationDbContext context, VerifyUserId verifyUser, UserManager<IdentityUsuario> UserManager, User_Terminal _Terminal)
        {
            this.context = context;
            this.verifyUser = verifyUser;
            userManager = UserManager;
            this._terminal = _Terminal;
        }

        private async Task SaveErrors(Exception e)
        {
            context.Add(new Errors()
            {
                Error = JsonConvert.SerializeObject(new Error()
                {
                    Inner = JsonConvert.SerializeObject(e.InnerException),
                    Message = JsonConvert.SerializeObject(e.Message)
                }),
                Accion = "Obtener cargadas"
            });
            await context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] Folio_Activo_Vigente filtro_)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var clientes_filtrados = context.Cliente.IgnoreAutoIncludes().Where(x => x.Terminales.Any(x => x.Cod == id_terminal)).Include(x => x.Terminales).IgnoreAutoIncludes().AsQueryable();

                if (filtro_.ID_Grupo != 0)
                    clientes_filtrados = clientes_filtrados.Where(x => x.Codgru == filtro_.ID_Grupo);

                if (!string.IsNullOrEmpty(filtro_.Cliente_Filtrado))
                    clientes_filtrados = clientes_filtrados.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(filtro_.Cliente_Filtrado));

                //var clientes = context.Cliente.AsEnumerable().Select(x => new CodDenDTO { Cod = x.Cod, Den = x.Den! }).OrderBy(x => x.Den);

                var clientes = clientes_filtrados.Include(x => x.Vendedor).IgnoreAutoIncludes().Include(x => x.Originador).IgnoreAutoIncludes().OrderBy(x => x.Den);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{cod:int}")]
        public ActionResult GetByCod([FromRoute] int cod)
        {
            try
            {
                //activar todos los clientes cuando se borren y vuelvan a insertar los registros
                Cliente? clientes = context.Cliente.FirstOrDefault(x => x.Cod == cod);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("all")]
        public ActionResult GetAll()
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var clientes = context.Cliente.IgnoreAutoIncludes().Where(x => x.Terminales.Any(x => x.Cod == id_terminal)).Include(x => x.Terminales).IgnoreAutoIncludes().OrderBy(x => x.Den);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("allactives")]
        public async Task<ActionResult> GetAllActives()
        {
            try
            {
                var clientes = await context.Cliente.Where(x => x.Activo == true).OrderBy(x => x.Den).ToListAsync();
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtraractivos")]
        public ActionResult Obtener_Grupos_Activos([FromQuery] Cliente cliente)
        {
            try
            {
                var clientes = context.Cliente.IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(cliente.Den))
                    clientes = clientes.Where(x => x.Den!.ToLower().Contains(cliente.Den.ToLower()) && x.Activo == true);

                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("grupo/{cod:int}")]
        public async Task<ActionResult> Get(int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var clientes = context.Cliente.IgnoreAutoIncludes().Where(x => x.Codgru == cod && x.Terminales.Any(x => x.Cod == id_terminal))
                    .Include(x => x.Vendedor).IgnoreAutoIncludes()
                    .Include(x => x.Terminales).IgnoreAutoIncludes().OrderBy(x => x.Den);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("asignar/{cod:int}")]
        public async Task<ActionResult> PostAsignar([FromBody] CodDenDTO codden, [FromRoute] int cod)
        {
            try
            {
                var destino = await context.Destino.FirstOrDefaultAsync(x => x.Cod == codden.Cod);

                if (destino == null)
                {
                    return NotFound();
                }

                destino.Codcte = cod;
                context.Update(destino);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult> PutCliente([FromBody] Cliente cliente)
        {
            try
            {
                if (cliente == null)
                {
                    return BadRequest();
                }

                //cliente.Grupo = null;

                context.Update(cliente);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //[HttpGet("folio/{cod:int}")]
        //public async Task<ActionResult> GetFolio([FromRoute] int cod)
        //{
        //    try
        //    {
        //        var cliente = await context.Cliente.FindAsync(cod);
        //        if (cliente == null)
        //            return NotFound();

        //        cliente.Consecutivo = cliente.Consecutivo != null ? cliente.Consecutivo + 1 : 1;

        //        var folio = cliente.CodCte != null ? cliente.CodCte + Convert.ToString(cliente.Consecutivo) : string.Empty;

        //        //cliente.Grupo = null!;

        //        context.Update(cliente);
        //        await context.SaveChangesAsync();

        //        return Ok(folio);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        [HttpPut("{cod:int}")]
        public async Task<ActionResult> ChangeStatus([FromRoute] int cod, [FromBody] bool status)
        {
            try
            {
                if (cod == 0)
                {
                    return BadRequest();
                }

                var cliente = context.Cliente.Where(x => x.Cod == cod).FirstOrDefault();
                if (cliente == null)
                {
                    return NotFound();
                }
                cliente.Activo = status;
                //cliente.precioSemanal = status;

                context.Update(cliente);

                var state = cliente.Activo! ? 22 : 23;
                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, state);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        //Cambiar la formulacion 
        [HttpPut("status/{cod:int}")]
        public async Task<ActionResult> ChangesStatus([FromRoute] int cod, [FromBody] bool status)
        {
            try
            {
                if (cod == 0)
                {
                    return BadRequest();
                }

                var cliente = context.Cliente.Where(x => x.Cod == cod).FirstOrDefault();
                if (cliente == null)
                {
                    return NotFound();
                }
                //cliente.Activo = status;
                cliente.precioSemanal = status;

                context.Update(cliente);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [Route("service")]
        [HttpGet]
        public async Task<ActionResult> GetClienteService()
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (id_terminal != 1)
                    return BadRequest("No esta permitida esta accion en esta terminal");

                BusinessEntityServiceClient client = new BusinessEntityServiceClient(BusinessEntityServiceClient.EndpointConfiguration.BasicHttpBinding_BusinessEntityService2);
                client.ClientCredentials.UserName.UserName = "energasws";
                client.ClientCredentials.UserName.Password = "Energas23!";
                client.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMinutes(10);
                client.Endpoint.Binding.SendTimeout = TimeSpan.FromMinutes(5);

                try
                {

                    var svc = client.ChannelFactory.CreateChannel();

                    WsGetBusinessEntityAssociationsRequest getReq = new WsGetBusinessEntityAssociationsRequest();

                    getReq.IncludeChildObjects = new NBool();
                    getReq.IncludeChildObjects.Value = false;

                    getReq.BusinessEntityType = new NInt();
                    getReq.BusinessEntityType.Value = 4;

                    getReq.AssociatedBusinessEntityId = new Identifier();
                    getReq.AssociatedBusinessEntityId.Id = new NLong();
                    getReq.AssociatedBusinessEntityId.Id.Value = 51004;

                    getReq.AssociatedBusinessEntityType = new NInt();
                    getReq.AssociatedBusinessEntityType.Value = 1;

                    getReq.ActiveIndicator = new NEnumOfActiveIndicatorEnum();
                    getReq.ActiveIndicator.Value = ActiveIndicatorEnum.ACTIVE;

                    var respuesta = await svc.GetBusinessEntityAssociationsAsync(getReq);

                    //Debug.WriteLine(JsonConvert.SerializeObject(respuesta.BusinessEntityAssociations));
                    //return Ok(respuesta.BusinessEntityAssociations);
                    foreach (var item in respuesta.BusinessEntityAssociations)
                    {
                        //var clienteId = respuesta.BusinessEntityAssociations.FirstOrDefault(x => x.BusinessEntity.BusinessEntityId.Id.Value == item.BusinessEntity.BusinessEntityId.Id.Value);
                        //Construcción del objeto del cliente
                        Cliente cliente = new Cliente()
                        {
                            Den = item.BusinessEntity.BusinessEntityName != null ? item.BusinessEntity.BusinessEntityName : item.BusinessEntity.BusinessEntityShortName,
                            Codsyn = item.BusinessEntity.BusinessEntityId.Id.Value.ToString()
                        };
                        //Obtención de código del cliente
                        Cliente? c = context.Cliente.Where(x => x.Codsyn == cliente.Codsyn)
                            .DefaultIfEmpty()
                            .FirstOrDefault();
                        //Si el cliente no es nulo 
                        if (c != null)
                        {
                            c.Den = cliente.Den;
                            context.Update(c);
                            foreach (var items in item.BusinessEntity.Destinations)
                            {
                                //Construcción del objeto del Destino 
                                Destino destino = new Destino()
                                {
                                    Den = items.DestinationName,
                                    Codsyn = items.DestinationId.Id.Value.ToString(),
                                    Codcte = c.Cod,
                                    Activo = items.ActiveIndicator.Value == ActiveIndicatorEnum.ACTIVE ? true : false,
                                    Dir = items.Address.Address1,
                                    Ciu = items.Address.City,
                                    Est = items.Address.State != null ? items.Address.State : "N/A",
                                    CodGamo = long.Parse(string.IsNullOrEmpty(items.DestinationCode) ? "0" : items.DestinationCode)
                                };
                                //Obtención del Cod del Destino 
                                Destino? d = context.Destino.Where(x => x.Codsyn == destino.Codsyn)
                                    .DefaultIfEmpty()
                                    .FirstOrDefault();
                                //Si el destino esta activo 
                                if (destino.Activo == true)
                                {
                                    //Si el destino no es nulo
                                    if (d != null)
                                    {
                                        //Activa el destino
                                        d.Den = destino.Den;
                                        d.Activo = destino.Activo;
                                        d.Codsyn = string.IsNullOrEmpty(d.Codsyn) ? string.Empty : d.Codsyn;
                                        d.Est = string.IsNullOrEmpty(destino.Est) ? string.Empty : destino.Est;
                                        d.Ciu = string.IsNullOrEmpty(destino.Ciu) ? string.Empty : destino.Ciu;
                                        d.Dir = string.IsNullOrEmpty(destino.Dir) ? string.Empty : destino.Dir;
                                        d.Codcte = destino.Codcte;
                                        d.CodGamo = destino.CodGamo == null ? 0 : destino.CodGamo;
                                        context.Update(d);
                                    }
                                    else
                                    {
                                        //Agrega un nuevo destino 
                                        context.Add(destino);
                                    }
                                }
                                else
                                {
                                    //Actualiza el campo activo del destino

                                    var cod = context.Destino.Where(x => x.Codsyn == destino.Codsyn)
                                        .DefaultIfEmpty()
                                        .FirstOrDefault();
                                    if (cod != null)
                                    {
                                        cod.Activo = false;
                                        context.Update(cod);
                                    }
                                }

                            }
                        }
                        else
                        {
                            context.Add(cliente);
                            await context.SaveChangesAsync();
                            //Obtención del código del cliente
                            Cliente? cli = context.Cliente.Where(x => x.Codsyn == cliente.Codsyn)
                                .DefaultIfEmpty()
                                .FirstOrDefault();
                            foreach (var itemss in item.BusinessEntity.Destinations)
                            {
                                //Construcción del objeto del Destino
                                Destino destino = new Destino()
                                {
                                    Den = itemss.DestinationName,
                                    Codsyn = itemss.DestinationId.Id.Value.ToString(),
                                    Codcte = cli?.Cod,
                                    Activo = itemss.ActiveIndicator.Value == ActiveIndicatorEnum.ACTIVE ? true : false,
                                    Dir = itemss.Address.Address1,
                                    Ciu = itemss.Address.City,
                                    Est = itemss.Address.State != null ? itemss.Address.State : "N/A",
                                    CodGamo = long.Parse(string.IsNullOrEmpty(itemss.DestinationCode) ? "0" : itemss.DestinationCode)
                                };
                                //Obtención del code del destino 
                                Destino? d = context.Destino.Where(x => x.Codsyn == destino.Codsyn && x.Codcte == destino.Codcte)
                                   .DefaultIfEmpty()
                                   .FirstOrDefault();
                                //Si el destino esta activo
                                if (destino.Activo == true)
                                {
                                    //Si el destino no es nulo 
                                    if (d != null)
                                    {
                                        //Activa el destino
                                        d.Den = destino.Den;
                                        d.Activo = destino.Activo;
                                        d.Codsyn = string.IsNullOrEmpty(d.Codsyn) ? string.Empty : d.Codsyn;
                                        d.Est = string.IsNullOrEmpty(destino.Est) ? string.Empty : destino.Est;
                                        d.Ciu = string.IsNullOrEmpty(destino.Ciu) ? string.Empty : destino.Ciu;
                                        d.Dir = string.IsNullOrEmpty(destino.Dir) ? string.Empty : destino.Dir;
                                        d.Codcte = destino.Codcte;
                                        d.CodGamo = destino.CodGamo == null ? 0 : destino.CodGamo;
                                        context.Update(d);
                                    }
                                    else
                                    {
                                        //Agrega un nuevo destino 
                                        context.Add(destino);
                                    }
                                }
                                else
                                {
                                    //Actualiza el campo activo del destino
                                    var cod = context.Destino.Where(x => x.Codsyn == destino.Codsyn)
                                        .DefaultIfEmpty()
                                        .FirstOrDefault();
                                    if (cod != null)
                                    {

                                        cod.Activo = false;
                                        context.Update(cod);
                                    }
                                }
                            }
                            //Se agrega el cliente
                        }
                    }

                    await context.SaveChangesAsync();
                    return Ok(true);
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
                finally
                {
                    client.Close();
                }

                return Ok();
            }
            catch (Exception e)
            {
                await SaveErrors(e);
                return BadRequest(e.Message);
            }

        }

        //[HttpGet("buscar")]
        //public ActionResult GetClienteBusqueda([FromQuery] CodDenDTO cliente)
        //{
        //    try
        //    {
        //        var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
        //        if (id_terminal == 0)
        //            return BadRequest();

        //        var clientes = context.Cliente.IgnoreAutoIncludes().Where(x => x.Terminales.Any(x => x.Cod == id_terminal)).Include(x => x.Terminales).IgnoreAutoIncludes().AsQueryable();

        //        if (string.IsNullOrEmpty(cliente.Den))
        //        {
        //            clientes = clientes.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(cliente.Den.ToLower()));
        //        }

        //        var newclientes = clientes.Select(x => x.Den);

        //        return Ok(newclientes);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        //[HttpGet("buscarGrupo")]
        //public ActionResult GetGrupoBusqueda([FromQuery] CodDenDTO grupo)
        //{
        //    try
        //    {
        //        var grupos = context.Grupo.AsQueryable();

        //        if (string.IsNullOrEmpty(grupo.Den))
        //        {
        //            grupos = grupos.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(grupo.Den.ToLower()));
        //        }

        //        var newgrupos = grupos.Select(x => x.Den);

        //        return Ok(newgrupos);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        [HttpGet("filtrar")]
        public async Task<ActionResult> Filtrar_Clientes([FromQuery] CodDenDTO parametros)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var clientes = context.Cliente.IgnoreAutoIncludes().Where(x => x.Terminales.Any(x => x.Cod == id_terminal)).Include(x => x.Terminales).IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(parametros.Den))
                {
                    clientes = clientes.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(parametros.Den.ToLower()));
                }

                await HttpContext.InsertarParametrosPaginacion(clientes, parametros.tamanopagina, parametros.pagina);

                if (HttpContext.Response.Headers.ContainsKey("pagina"))
                {
                    var pagina = HttpContext.Response.Headers["pagina"];
                    if (pagina != parametros.pagina && !string.IsNullOrEmpty(pagina))
                    {
                        parametros.pagina = int.Parse(pagina);
                    }
                }

                clientes = clientes.Skip((parametros.pagina - 1) * parametros.tamanopagina).Take(parametros.tamanopagina);

                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("ajustar")]
        public async Task<ActionResult> Ajustar_Clientes_Destinos_Duplicados()
        {
            try
            {
                var destinos_en_precios = context.Precio.Select(x => x.CodDes).Distinct().ToList();
                var destinos_en_precios_programados = context.PrecioProgramado.Select(x => x.CodDes).Distinct().ToList();
                var destinos_en_precios_historial = context.PreciosHistorico.Select(x => x.CodDes).Distinct().ToList();

                List<int?> destinos_encontrados = new();

                destinos_encontrados.AddRange(destinos_en_precios);
                destinos_encontrados.AddRange(destinos_en_precios_programados);
                destinos_encontrados.AddRange(destinos_en_precios_historial);

                var destinos_unicos = destinos_encontrados.Distinct();

                var destinos_generales_activos = context.Destino.Where(x => x.Activo == true).ToList();

                foreach (var item in destinos_generales_activos)
                {
                    if (!destinos_unicos.Any(x => item.Cod == x))
                    {
                        item.Activo = false;
                        context.Update(item);
                    }
                }

                await context.SaveChangesAsync();

                await GetClienteService();

                return Ok(destinos_unicos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
