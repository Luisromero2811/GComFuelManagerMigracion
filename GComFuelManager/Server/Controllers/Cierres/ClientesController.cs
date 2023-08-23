﻿using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.ServiceModel;
using ServiceReference6;
//using ServiceReference8;
using System.Drawing;
using System;
using GComFuelManager.Server.Helpers;
using Microsoft.AspNetCore.Identity;
using GComFuelManager.Server.Identity;

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

        public ClientesController(ApplicationDbContext context, VerifyUserId verifyUser, UserManager<IdentityUsuario> UserManager)
        {
            this.context = context;
            this.verifyUser = verifyUser;
            userManager = UserManager;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                var clientes = context.Cliente.AsEnumerable().Select(x => new CodDenDTO { Cod = x.Cod, Den = x.Den! }).OrderBy(x => x.Den);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{cod:int}")]
        public async Task<ActionResult> GetByCod([FromRoute] int cod)
        {
            try
            {
                Cliente? clientes = context.Cliente.FirstOrDefault(x => x.Cod == cod);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var clientes = context.Cliente.AsEnumerable().OrderBy(x => x.Den);
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
                var clientes = context.Cliente.Where(x => x.codgru == cod).AsEnumerable().OrderBy(x => x.Den);
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

        [HttpGet("folio/{cod:int}")]
        public async Task<ActionResult> GetFolio([FromRoute] int cod)
        {
            try
            {
                var cliente = await context.Cliente.FindAsync(cod);
                if (cliente == null)
                    return NotFound();

                cliente.Consecutivo = cliente.Consecutivo != null ? cliente.Consecutivo + 1 : 1;

                var folio = cliente.CodCte != null ? cliente.CodCte + Convert.ToString(cliente.Consecutivo) : string.Empty;

                //cliente.Grupo = null!;

                context.Update(cliente);
                await context.SaveChangesAsync();

                return Ok(folio);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

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
                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

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
                        Cliente? c = context.Cliente.Where(x => x.Den == cliente.Den && x.Codsyn == cliente.Codsyn)
                            .DefaultIfEmpty()
                            .FirstOrDefault();
                        //Si el cliente no es nulo 
                        if (c != null)
                        {
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
                                Destino? d = context.Destino.Where(x => x.Den == destino.Den && x.Codsyn == destino.Codsyn && x.Codcte == destino.Codcte)
                                    .DefaultIfEmpty()
                                    .FirstOrDefault();
                                //Si el destino esta activo 
                                if (destino.Activo == true)
                                {
                                    //Si el destino no es nulo
                                    if (d != null)
                                    {
                                        Debug.WriteLine($"activo: {d.Cod}, nombre {d.Den}");
                                        //Activa el destino
                                        d.Den = destino.Den;
                                        d.Activo = destino.Activo;
                                        d.Codsyn = string.IsNullOrEmpty(d.Codsyn) ? string.Empty : d.Codsyn;
                                        d.Est = string.IsNullOrEmpty(destino.Est) ? string.Empty : destino.Est;
                                        d.Ciu = string.IsNullOrEmpty(destino.Ciu) ? string.Empty : destino.Ciu;
                                        d.Dir = string.IsNullOrEmpty(destino.Dir) ? string.Empty : destino.Dir;
                                        d.Codcte = destino.Codcte;
                                        d.CodGamo = destino.CodGamo == null ? 0 : destino.CodGamo;
                                        Debug.WriteLine($"antes");
                                        context.Update(d);

                                        Debug.WriteLine($"completo");

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
                            await context.SaveChangesAsync(id, 11);
                            //Obtención del código del cliente
                            Cliente? cli = context.Cliente.Where(x => x.Den == cliente.Den && x.Codsyn == cliente.Codsyn)
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
                                Destino? d = context.Destino.Where(x => x.Den == destino.Den && x.Codsyn == destino.Codsyn && x.Codcte == destino.Codcte)
                                   .DefaultIfEmpty()
                                   .FirstOrDefault();
                                //Si el destino esta activo
                                if (destino.Activo == true)
                                {
                                    //Si el destino no es nulo 
                                    if (d != null)
                                    {
                                        Debug.WriteLine($"activo: {d.Cod}, nombre {d.Den}");
                                        //Activa el destino
                                        d.Den = destino.Den;
                                        d.Activo = destino.Activo;
                                        d.Codsyn = string.IsNullOrEmpty(d.Codsyn) ? string.Empty : d.Codsyn;
                                        d.Est = string.IsNullOrEmpty(destino.Est) ? string.Empty : destino.Est;
                                        d.Ciu = string.IsNullOrEmpty(destino.Ciu) ? string.Empty : destino.Ciu;
                                        d.Dir = string.IsNullOrEmpty(destino.Dir) ? string.Empty : destino.Dir;
                                        d.Codcte = destino.Codcte;
                                        d.CodGamo = destino.CodGamo == null ? 0 : destino.CodGamo;
                                        Debug.WriteLine($"antes");

                                        context.Update(d);
                                        Debug.WriteLine($"completo");

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
                                    Debug.WriteLine($"inactivo: {destino.Cod}");
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

                    await context.SaveChangesAsync(id, 11);
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
                return BadRequest(e.Message);
            }

        }

    }
}
