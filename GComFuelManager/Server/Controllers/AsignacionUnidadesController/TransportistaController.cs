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
//using ServiceReference6;
//using ServiceReference3;
using ServiceReference10;
using ServiceReference8;

namespace GComFuelManager.Server.Controllers.AsignacionUnidadesController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TransportistaController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly RequestToFile toFile;
        private readonly VerifyUserId verifyUser;
        private readonly UserManager<IdentityUsuario> UserManager;
        private readonly User_Terminal _terminal;

        public TransportistaController(ApplicationDbContext context, RequestToFile toFile, VerifyUserId verifyUser, UserManager<IdentityUsuario> UserManager, User_Terminal _Terminal)
        {
            this.context = context;
            this.toFile = toFile;
            this.verifyUser = verifyUser;
            this.UserManager = UserManager;
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

        [HttpPost("save")]
        public async Task<ActionResult> PostGroups([FromBody] GrupoTransportista grupoTransportista)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (grupoTransportista is null)
                {
                    return NotFound();
                }
                if (grupoTransportista.cod == 0)
                {
                    grupoTransportista.Id_Tad = id_terminal;
                    context.Add(grupoTransportista);
                }
                else
                {
                    context.Update(grupoTransportista);
                }
                await context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("relacion")]
        public async Task<ActionResult> PostClienteTerminal([FromBody] ClienteTadDTO clienteTadDTO)
        {
            try
            {
                //Si el cliente es nulo, retornamos un notfound
                if (clienteTadDTO is null)
                    return NotFound();

                foreach (var terminal in clienteTadDTO.Tads)
                {
                    foreach (var grupotransportes in clienteTadDTO.GrupoTransportistas)
                    {
                        if (!context.GrupoTransportista_Tad.Any(x => x.Id_Terminal == terminal.Cod && x.Id_GrupoTransportista == grupotransportes.cod))
                        {
                            GrupoTransportista_Tad grupotransportetad = new()
                            {
                                Id_GrupoTransportista = grupotransportes.cod,
                                Id_Terminal = terminal.Cod
                            };
                            context.Add(grupotransportetad);
                        }
                    }
                }
                await context.SaveChangesAsync();

                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        
        [HttpPost("crearTransportista")]
        public async Task<ActionResult> PostTransportista([FromBody] Transportista transportista)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (transportista is null)
                    return BadRequest();

                if (transportista.Cod == 0)
                {
                    transportista.Id_Tad = id_terminal;
                    transportista.Codgru = transportista.GrupoTransportista!.cod!;
                    context.Add(transportista);
                }
                else
                {
                    context.Update(transportista);
                }
                await context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("relacionempresa")]
        public async Task<ActionResult> PostTransportistaTerminal([FromBody] ClienteTadDTO clienteTadDTO)
        {
            try
            {
                //Si el cliente es nulo, retornamos un notfound
                if (clienteTadDTO is null)
                    return NotFound();

                foreach (var terminal in clienteTadDTO.Tads)
                {
                    foreach (var transportista in clienteTadDTO.Transportistas)
                    {
                        if (!context.Transportista_Tad.Any(x => x.Id_Terminal == terminal.Cod && x.Id_Transportista == transportista.Cod))
                        {
                            Transportista_Tad transportista_Tad = new()
                            {
                                Id_Transportista = transportista.Cod,
                                Id_Terminal = terminal.Cod
                            };
                            context.Add(transportista_Tad);
                        }
                    }
                }
                await context.SaveChangesAsync();

                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("gruposactivos")]
        public async Task<ActionResult> GetGrupos()
        {
            try
            {
                var grupostransporte = await context.GrupoTransportista
                     .Include(x => x.Terminales)
                    .OrderBy(x => x.den)
                    .ToListAsync();
                return Ok(grupostransporte);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtraractivos")]
        public ActionResult Obtener_Grupos_Activos([FromQuery] GrupoTransportista grupo)
        {
            try
            {
                var grupos = context.GrupoTransportista
                     .Include(x => x.Terminales)
                    .IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(grupo.den))
                    grupos = grupos.Where(x => x.den!.ToLower().Contains(grupo.den.ToLower()));

                return Ok(grupos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtrarempresa")]
        public ActionResult Obtener_Empresa_Activa([FromQuery] Transportista transportista)
        {
            try
            {
                var transportistas = context.Transportista.IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(transportista.Den))
                    transportistas = transportistas.Where(x => x.Den!.ToLower().Contains(transportista.Den.ToLower()) && x.Activo == true);

                return Ok(transportistas);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("Grupo/{grupo:int}")]
        public async Task<ActionResult> GetTransportistas(int grupo)
        {
            try
            {
                var transportistas = await context.Transportista
                    .Include(x => x.Terminales)
                    .Where(x => x.Codgru == grupo && x.Activo == true)
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(transportistas);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("lista")]
        public async Task<ActionResult> GetList()
        {
            try
            {
                var transportistas = await context.Transportista.Where(x => x.Activo == true)
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(transportistas);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var transportistas = context.Transportista.IgnoreAutoIncludes().Where(x => x.Activo == true && x.Terminales.Any(y => y.Cod == id_terminal))
                    .Include(x => x.Terminales).IgnoreAutoIncludes()
                    .OrderBy(x => x.Den)
                    .ToList();

                return Ok(transportistas);
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
                var transportistas = context.Transportista
                    .OrderBy(x => x.Den)
                    .ToList();
                return Ok(transportistas);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("borrar/relacion")]
        public async Task<ActionResult> Borrar_Relacion([FromBody] Transportista_Tad transportista_Tad)
        {
            try
            {
                if (transportista_Tad is null)
                    return NotFound();

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                context.Remove(transportista_Tad);
                await context.SaveChangesAsync();

                return Ok(transportista_Tad);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("borrar/relaciones")]
        public async Task<ActionResult> Borrar_Relaciones([FromBody] GrupoTransportista transportista_Tad)
        {
            try
            {
                if (transportista_Tad is null)
                    return NotFound();

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                context.Remove(transportista_Tad);
                await context.SaveChangesAsync();

                return Ok(transportista_Tad);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Route("service")]
        [HttpGet]
        public async Task<ActionResult> GetTransportistasService()
        {
            try
            {
                ServiceReference8.BusinessEntityServiceClient client = new(BusinessEntityServiceClient.EndpointConfiguration.BasicHttpBinding_BusinessEntityService);
                client.ClientCredentials.UserName.UserName = "energasws";
                client.ClientCredentials.UserName.Password = "Energas23!";
                client.Endpoint.Binding.SendTimeout = TimeSpan.FromMinutes(5);
                client.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                TruckCarrierServiceClient truck = new(TruckCarrierServiceClient.EndpointConfiguration.BasicHttpBinding_TruckCarrierService);
                truck.ClientCredentials.UserName.UserName = "energasws";
                truck.ClientCredentials.UserName.Password = "Energas23!";
                client.Endpoint.Binding.SendTimeout = TimeSpan.FromMinutes(5);
                truck.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                try
                {

                    List<Transportista> transportistas = new();
                    //ServiceReference6.BusinessEntityServiceChannel svc = svcTruck.CreateChannel();
                    var svc = client.ChannelFactory.CreateChannel();
                    //Conexión a WebService para obtener el transportista
                    WsGetBusinessEntityAssociationsRequest getReq = new();

                    getReq.IncludeChildObjects = new ServiceReference8.NBool();
                    getReq.IncludeChildObjects.Value = true;

                    getReq.BusinessEntityType = new ServiceReference8.NInt();
                    getReq.BusinessEntityType.Value = 8;//truck carrier

                    getReq.AssociatedBusinessEntityId = new ServiceReference8.Identifier();
                    getReq.AssociatedBusinessEntityId.Id = new ServiceReference8.NLong();
                    getReq.AssociatedBusinessEntityId.Id.Value = 51004;//energas

                    getReq.AssociatedBusinessEntityType = new ServiceReference8.NInt();
                    getReq.AssociatedBusinessEntityType.Value = 1;

                    //toFile.GenerateFile(JsonConvert.SerializeObject(getReq), $"Request_Transportistas_{DateTime.Now.ToString("ddMMyyyyHHmmss")}", $"{DateTime.Now.ToString("ddMMyyyy")}");

                    var respuesta = await svc.GetBusinessEntityAssociationsAsync(getReq);

                    //toFile.GenerateFile(JsonConvert.SerializeObject(respuesta), $"Response_Transportistas_{DateTime.Now.ToString("ddMMyyyyHHmmss")}", $"{DateTime.Now.ToString("ddMMyyyy")}");

                    //Conexion a WebService para obtener carrId del transportista 
                    WsGetTruckCarriersRequest truckRequest = new();

                    //toFile.GenerateFile(JsonConvert.SerializeObject(truckRequest), $"Request_Transportistas_ID_{DateTime.Now.ToString("ddMMyyyyHHmmss")}", $"{DateTime.Now.ToString("ddMMyyyy")}");

                    var truckResponse = await truck.GetTruckCarriersAsync(truckRequest);

                    //toFile.GenerateFile(JsonConvert.SerializeObject(truckResponse), $"Response_Transportistas_ID_{DateTime.Now.ToString("ddMMyyyyHHmmss")}", $"{DateTime.Now.ToString("ddMMyyyy")}");


                    foreach (var item in respuesta.BusinessEntityAssociations)
                    {
                        if (!string.IsNullOrEmpty(item.BusinessEntity.BusinessEntityName) && !string.IsNullOrWhiteSpace(item.BusinessEntity.BusinessEntityName))
                        {
                            var carrid = truckResponse.TruckCarriers.FirstOrDefault(x => x.BusinessEntityId.Id.Value == item.BusinessEntity.BusinessEntityId.Id.Value);
                            //Creacion del objeto del transportista
                            Transportista transportista = new()
                            {
                                Den = item.BusinessEntity.BusinessEntityName,
                                Busentid = item.BusinessEntity.BusinessEntityId.Id.Value.ToString(),
                                Activo = item.BusinessEntity.ActiveIndicator.Value == ServiceReference8.ActiveIndicatorEnum.ACTIVE ? true : false,
                                CarrId = carrid == null ? string.Empty : carrid.CarrierId.Id.Value.ToString()
                            };
                            //Si el transportista esta activo 
                            if (transportista.Activo == true)
                            {
                                Transportista? t = context.Transportista.Where(x => x.Busentid == transportista.Busentid)
                                    .DefaultIfEmpty()
                                    .FirstOrDefault();
                                //Si el transportista no es nulo 
                                if (t != null)
                                {
                                    //Lo actualiza
                                    t.Den = transportista.Den;
                                    t.Activo = transportista.Activo;
                                    t.CarrId = string.IsNullOrEmpty(t.CarrId) ? string.Empty : t.CarrId;
                                    context.Update(t);
                                }
                                else
                                    //Agrega un nuevo transportista 
                                    context.Add(transportista);
                            }
                            else
                            {
                                //Actualiza el campo de activo 
                                var cod = context.Transportista.Where(x => x.Busentid == transportista.Busentid && string.IsNullOrEmpty(x.CarrId)).DefaultIfEmpty().FirstOrDefault();
                                if (cod != null)
                                {
                                    var tinactivo = context.Transportista.Find(cod.Cod);
                                    if (tinactivo != null)
                                    {
                                        tinactivo.Activo = false;
                                        context.Update(tinactivo);
                                    }
                                }
                            }
                        }
                    }

                    //Guarda los cambios
                    await context.SaveChangesAsync();

                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
                finally
                {
                    //svcTruck.Close();
                    client.Close();
                }

                return Ok(true);
            }
            catch (Exception e)
            {
                await SaveErrors(e);
                return BadRequest(e.Message);
            }
        }
    }

}