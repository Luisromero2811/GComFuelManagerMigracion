using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using GComFuelManager.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using System.Diagnostics;
using System.ServiceModel;
using ServiceReference6;
using GComFuelManager.Shared.Modelos;
using ServiceReference3;

namespace GComFuelManager.Server.Controllers.AsignacionUnidadesController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TransportistaController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public TransportistaController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                var transportistas = await context.Transportista.Where(x => x.Activo == true && x.Gru != null)
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(transportistas);
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
                BusinessEntityServiceClient client = new BusinessEntityServiceClient(BusinessEntityServiceClient.EndpointConfiguration.BasicHttpBinding_BusinessEntityService);
                client.ClientCredentials.UserName.UserName = "energasws";
                client.ClientCredentials.UserName.Password = "Energas23!";
                client.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                TruckCarrierServiceClient truck = new TruckCarrierServiceClient(TruckCarrierServiceClient.EndpointConfiguration.BasicHttpBinding_TruckCarrierService);
                truck.ClientCredentials.UserName.UserName = "energasws";
                truck.ClientCredentials.UserName.Password = "Energas23!";
                truck.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                try
                {

                    List<Transportista> transportistas = new List<Transportista>();
                    //ServiceReference6.BusinessEntityServiceChannel svc = svcTruck.CreateChannel();
                    var svc = client.ChannelFactory.CreateChannel();

                    WsGetBusinessEntityAssociationsRequest getReq = new WsGetBusinessEntityAssociationsRequest();

                    getReq.IncludeChildObjects = new ServiceReference6.NBool();
                    getReq.IncludeChildObjects.Value = true;

                    getReq.BusinessEntityType = new ServiceReference6.NInt();
                    getReq.BusinessEntityType.Value = 8;//truck carrier

                    getReq.AssociatedBusinessEntityId = new ServiceReference6.Identifier();
                    getReq.AssociatedBusinessEntityId.Id = new ServiceReference6.NLong();
                    getReq.AssociatedBusinessEntityId.Id.Value = 51004;//energas

                    getReq.AssociatedBusinessEntityType = new ServiceReference6.NInt();
                    getReq.AssociatedBusinessEntityType.Value = 1;

                    var respuesta = await svc.GetBusinessEntityAssociationsAsync(getReq);

                    WsGetTruckCarriersRequest truckRequest = new WsGetTruckCarriersRequest();
                    var truckResponse = await truck.GetTruckCarriersAsync(truckRequest);

                    foreach (var item in respuesta.BusinessEntityAssociations)
                    {
                        var carrid = truckResponse.TruckCarriers.FirstOrDefault(x => x.BusinessEntityId.Id.Value == item.BusinessEntity.BusinessEntityId.Id.Value);
                        Transportista transportista = new Transportista()
                        {
                            Den = item.BusinessEntity.BusinessEntityName,
                            Busentid = item.BusinessEntity.BusinessEntityId.Id.Value.ToString(),
                            Activo = item.BusinessEntity.ActiveIndicator.Value == ServiceReference6.ActiveIndicatorEnum.ACTIVE ? true : false,
                            CarrId = carrid == null ? string.Empty : carrid.CarrierId.Id.Value.ToString()
                        };

                        if (transportista.Activo == true)
                        {
                            Debug.WriteLine($"activo: {transportista.Busentid}");
                            Transportista? t = context.Transportista.Where(x => x.Busentid == transportista.Busentid)
                                .DefaultIfEmpty()
                                .FirstOrDefault();

                            if (t != null)
                            {
                                t.Den = transportista.Den;
                                t.Activo = transportista.Activo;
                                t.CarrId = string.IsNullOrEmpty(t.CarrId) ? string.Empty : t.CarrId;
                                context.Update(t);
                            }
                            else
                                context.Add(transportista);
                        }
                        else
                        {
                            Debug.WriteLine($"inactivo:{transportista.Busentid}");
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
                return BadRequest(e.Message);
            }
        }
    }

}