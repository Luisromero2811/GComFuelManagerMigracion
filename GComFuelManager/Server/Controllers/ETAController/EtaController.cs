using System;
using GComFuelManager.Server.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Linq;
using System.Linq.Dynamic.Core;
using GComFuelManager.Server.Helpers;

namespace GComFuelManager.Server.Controllers.ETAController
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Administrador Sistema, Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Programador, Coordinador, Analista Suministros, Auditor, Capturista Recepcion Producto, Comprador")]
    public class EtaController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserId verifyUser;
        private readonly User_Terminal _terminal;

        public EtaController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserId verifyUser, User_Terminal _Terminal)
        {
            this.context = context;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
            this._terminal = _Terminal;
        }
        //Filtro para ordenes por Bol 
        [HttpPost("Filtro")]
        public ActionResult EtaGet([FromBody] EtaDTO etaDTO)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var eta = context.OrdEmbDet
                    .Where(x => x.Bol == etaDTO.Bol && x.Orden != null && x.Orden.Id_Tad == id_terminal)
                    .Include(x => x.Orden)
                    .ThenInclude(x => x.Tonel)
                    .Include(x => x.Orden)
                    .ThenInclude(x => x.Estado)
                    .FirstOrDefault();

                if (eta == null)
                    return NotFound("No se ha encontrado ningun registro.");
                return Ok(eta);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        //Method para enviar la fecha ESTIMADA de llegada al destino 1era parte
        [HttpPost]
        public async Task<ActionResult> SendEta([FromBody] OrdEmbDet ordEmb)
        {
            try
            {
                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                var user = await userManager.FindByIdAsync(id);

                if (user == null) return BadRequest("No existe el usuario.");

                var acc = 0;
                if (ordEmb.Cod == 0)
                {
                    var Eta = context.Orden.FirstOrDefault(x => x.BatchId == ordEmb.Bol);
                    if (Eta is null)
                        return BadRequest("No se encontro la orden.");
                    //Como por ahora solo documentan la primera parte del ETA, entonces si el ETA tiene un número mayor a cero, al momento de registrar lo anexamos con estatus 26 de Trayecto
                    if (ordEmb.EtaNumber > 0)
                    {
                        ordEmb.Orden!.Codest = 26;
                    }
                    else
                    {
                        //ordEmb.Orden!.Codest = ordEmb.CodEst;
                    }
                    Eta.Codest = ordEmb.Orden!.Codest;

                    context.Update(Eta);
                    ordEmb.Orden = null!;

                    ordEmb.Codusu = user.UserCod;
                    // ordEmb.Eta = ordEmb.EtaNumber.ToString();

                    context.Add(ordEmb);
                    acc = 29;
                }

                else
                {
                    //Cuando los litros esten entregados, el estado pasa a Entregado
                    if (ordEmb.Litent > 0)
                        ordEmb.Orden!.Codest = 10;
                    else
                        //ordEmb.Orden!.Codest = ordEmb.CodEst;

                        ordEmb.Orden!.Estado = null!;
                    ordEmb.Fchmod = DateTime.Now;
                    ordEmb.Codusumod = user.UserCod;

                    context.Update(ordEmb.Orden);

                    ordEmb.Orden = null!;
                    //TimeSpan? time = ordEmb.Fchlleest?.Subtract(ordEmb.FchDoc!.Value);

                    //ordEmb.Eta = time?.ToString("HHmm") ?? string.Empty;
                    //ordEmb.Eta = ordEmb.EtaNumber.ToString();

                    context.Update(ordEmb);
                    acc = 30;
                }

                await context.SaveChangesAsync(id, acc);
                ordEmb.Orden = context.Orden.Where(x => x.BatchId == ordEmb.Bol).Include(x => x.Estado).Include(x => x.Tonel).FirstOrDefault();
                return Ok(ordEmb);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{bol:int}")]
        public ActionResult GetEta(int? bol)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (bol == null)
                    return BadRequest("El bol no puede ir vacio.");

                var orden = context.Orden.Where(x => x.BatchId == bol && x.Id_Tad == id_terminal).Include(x => x.Tonel).Include(x => x.Estado).FirstOrDefault();

                if (orden == null)
                    return BadRequest($"No se contro el bol {bol} en las ordenes cargadas");

                var ordembdet = context.OrdEmbDet.Include(x => x.Orden).IgnoreAutoIncludes().FirstOrDefault(x => x.Bol == bol && x.Orden != null && x.Orden.Id_Tad == id_terminal) ?? new OrdEmbDet();

                if (ordembdet.Cod == 0)
                    ordembdet.Bol = bol;

                ordembdet.Orden = orden;

                return Ok(ordembdet);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Reporte")]
        public async Task<ActionResult> GetReportes([FromBody] FechasF fechas)
        {
            try
            {
                List<EtaDTO> Ordenes = new List<EtaDTO>();

                //Órdenes sin asignación de transporte-chofer-vehiculo
                //órdenes sin asignar ordenar por BIN
                var ordensSinAsignar = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                    && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                    && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && string.IsNullOrEmpty(fechas.TipVenta)
                    //Internas-Externas
                    //||  x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                    //&& x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && fechas.TipVenta.Contains("Ambas") == x.Destino.Cliente.Tipven.StartsWith("Int")
                    //   || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                    //&& x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && fechas.TipVenta.Contains("Ambas") == x.Destino.Cliente.Tipven.StartsWith("Ext")
                    )
                    .Include(x => x.Chofer)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Estado)
                    .Include(x => x.OrdenCompra)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.OrdenCierre)
                     .Include(x => x.OrdenPedido)
                    .OrderBy(x => x.Fchpet)
                       .Select(e => new EtaDTO()
                       {
                           Referencia = "ENER-" + e.Folio.ToString(),
                           FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                           EstatusOrden = "Pendiente de Asignar",
                           FechaCarga = null!,
                           Bol = null!,
                           DeliveryRack = e.Destino.Cliente.Tipven,
                           Cliente = e.Destino.Cliente.Den,
                           Destino = e.Destino.Den,
                           Producto = e.Producto.Den,
                           VolNat = null!,
                           VolCar = null!,
                           Transportista = null!,
                           Unidad = null!,
                           Operador = null!,
                           FechaDoc = null!,
                           Eta = null!,
                           FechaEst = null!,
                           Trayecto = null!,
                           Observaciones = null!,
                           FechaRealEta = null!,
                           LitEnt = null!
                       })
                .Take(10000)
                .ToListAsync();
                Ordenes.AddRange(ordensSinAsignar);

                //Órdenes programadas
                var ordens = await context.OrdenEmbarque
                 .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                 && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                 || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                 && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && string.IsNullOrEmpty(fechas.TipVenta))
                 .Include(x => x.Chofer)
                 .Include(x => x.Destino)
                 .ThenInclude(x => x.Cliente)
                 .Include(x => x.Estado)
                 .Include(x => x.OrdenCompra)
                 .Include(x => x.Tad)
                 .Include(x => x.Producto)
                 .Include(x => x.Tonel)
                 .ThenInclude(x => x.Transportista)
                 .Include(x => x.OrdenCierre)
                 .OrderBy(x => x.Fchpet)
                 .ThenBy(x => x.Tonel!.Tracto)
                 .Include(x => x.OrdenPedido)
                   .Select(e => new EtaDTO()
                   {
                       Referencia = "ENER-" + e.Folio.ToString(),
                       FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                       EstatusOrden = e.Estado.den,
                       FechaCarga = null!,
                       Bol = null!,
                       DeliveryRack = e.Destino.Cliente.Tipven,
                       Cliente = e.Destino.Cliente.Den,
                       Destino = e.Destino.Den,
                       Producto = e.Producto.Den,
                       VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                        e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                        e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                        e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                       VolCar = null!,
                       Transportista = e.Tonel.Transportista.Den,
                       Unidad = e.Tonel.Veh,
                       Operador = e.Chofer.Den,
                       FechaDoc = null!,
                       Eta = null!,
                       FechaEst = null!,
                       Trayecto = null!,
                       Observaciones = null!,
                       FechaRealEta = null!,
                       LitEnt = null!
                   })
                 .Take(10000)
                 .ToListAsync();

                Ordenes.AddRange(ordens);

                //Órdenes sin carga-Pedientes de carga
                var pedidosDate = await context.OrdenEmbarque
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.TipVenta)
                || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && string.IsNullOrEmpty(fechas.TipVenta))
                .Include(x => x.Destino)
                .ThenInclude(x => x.Cliente)
                .Include(x => x.Tad)
                .Include(x => x.Producto)
                .Include(x => x.Tonel)
                .ThenInclude(x => x.Transportista)
                .Include(x => x.Chofer)
                .Include(x => x.Estado)
                .OrderBy(x => x.Fchcar)
                //    Vol2 = null!,
                //    Vol = o.Vol,
                    .Select(e => new EtaDTO()
                    {
                        Referencia = "ENER-" + e.Folio.ToString(),
                        FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                        EstatusOrden = e.Estado.den,
                        FechaCarga = null!,
                        Bol = null!,
                        DeliveryRack = e.Destino.Cliente.Tipven,
                        Cliente = e.Destino.Cliente.Den,
                        Destino = e.Destino.Den,
                        Producto = e.Producto.Den,
                        VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                        e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                        e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                        e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                        VolCar = null!,
                        Transportista = e.Tonel.Transportista.Den,
                        Unidad = e.Tonel.Veh,
                        Operador = e.Chofer.Den,
                        FechaDoc = null!,
                        Eta = null!,
                        FechaEst = null!,
                        Trayecto = null!,
                        Observaciones = null!,
                        FechaRealEta = null!,
                        LitEnt = null!
                    })
                .Take(10000)
                .ToListAsync();
                Ordenes.AddRange(pedidosDate);
                //Órdenes cargadas
                var pedidosDate2 = await context.Orden
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && string.IsNullOrEmpty(fechas.TipVenta)
                //Internas-Externas
                //|| x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && fechas.TipVenta.Contains("Ambas") == x.Destino.Cliente.Tipven.StartsWith("Int")
                //|| x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && fechas.TipVenta.Contains("Ambas") == x.Destino.Cliente.Tipven.StartsWith("Ext")
                )
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Estado)
                    .Include(x => x.Producto)
                    .Include(x => x.Chofer)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.OrdenEmbarque)
                    .Include(x => x.OrdEmbDet)
                    .OrderBy(x => x.Fchcar)
                      .Select(e => new EtaDTO()
                      {
                          Referencia = "ENER-" + e.Folio.ToString(),
                          FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                          EstatusOrden = e.Estado.den,
                          FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                          Bol = e.BatchId,
                          DeliveryRack = e.Destino.Cliente.Tipven,
                          Cliente = e.Destino.Cliente.Den,
                          Destino = e.Destino.Den,
                          Producto = e.Producto.Den,
                          VolNat = e.Vol2,
                          VolCar = e.Vol,
                          Transportista = e.Tonel.Transportista.Den,
                          Unidad = e.Tonel.Veh,
                          Operador = e.Chofer.Den,
                          FechaDoc = null!,
                          Eta = null!,
                          FechaEst = null!,
                          Trayecto = null!,
                          Observaciones = null!,
                          FechaRealEta = null!,
                          LitEnt = null!
                      })
                    .Take(10000)
                    .ToListAsync();
                Ordenes.AddRange(pedidosDate2);

                //Órdenes ETA-Trayecto
                List<EtaDTO> newOrden = new List<EtaDTO>();
                var Eta = await context.Orden
                      .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                          || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.TipVenta)
                      )
                      .Include(x => x.OrdEmbDet)
                      .Include(x => x.Destino)
                      .ThenInclude(x => x.Cliente)
                      .Include(x => x.Estado)
                      .Include(x => x.Producto)
                      .Include(x => x.Chofer)
                      .Include(x => x.Tonel)
                      .ThenInclude(x => x.Transportista)
                      .Include(x => x.OrdEmbDet)
                      .OrderBy(x => x.Fchcar)
                       .Select(e => new EtaDTO()
                       {
                           Referencia = e.Ref,
                           FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                           EstatusOrden = e.Estado.den,
                           FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                           Bol = e.BatchId,
                           DeliveryRack = e.Destino.Cliente.Tipven,
                           Cliente = e.Destino.Cliente.Den,
                           Destino = e.Destino.Den,
                           Producto = e.Producto.Den,
                           VolNat = e.Vol2,
                           VolCar = e.Vol,
                           Transportista = e.Tonel.Transportista.Den,
                           Unidad = e.Tonel.Veh,
                           Operador = e.Chofer.Den,
                           FechaDoc = e.OrdEmbDet.FchDoc.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                           Eta = e.OrdEmbDet.FchDoc!.Value.Subtract(e.OrdEmbDet.Fchlleest.Value!).ToString("hh\\:mm"),
                           FechaEst = e.OrdEmbDet.Fchlleest.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                           Trayecto = "ENTREGADO",
                           Observaciones = e.OrdEmbDet!.Obs,
                           FechaRealEta = e.OrdEmbDet.Fchrealledes.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                           LitEnt = e.OrdEmbDet.Litent
                       })
                      .Take(10000)
                      .ToListAsync();
                foreach (var item in Eta)
                    if (!newOrden.Contains(item))
                        newOrden.Add(item);


                Ordenes.AddRange(newOrden);

                return Ok(Ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Reportes")]
        public async Task<ActionResult> Etas([FromBody] FechasF fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                //Si seleccionan los dos tipos de delivery
                if (fechas.Estado == 1)
                {
                    List<EtaDTO> Ordenes = new List<EtaDTO>();

                    //Órdenes sin asignación de transporte-chofer-vehiculo
                    //órdenes sin asignar ordenar por BIN
                    var ordensSinAsignar = await context.OrdenEmbarque
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                        || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && fechas.Estado.Equals(1) == x.Destino!.Cliente!.Tipven!.Contains("terno") && x.Codtad == id_terminal)
                        .Include(x => x.Chofer)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Estado)
                        .Include(x => x.OrdenCompra)
                        .Include(x => x.Tad)
                        .Include(x => x.Producto)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenCierre)
                         .Include(x => x.OrdenPedido)
                        .OrderBy(x => x.Fchpet)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = "ENER-" + e.Folio.ToString(),
                               FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Tad.Den,
                               EstatusOrden = "Pendiente de Asignar",
                               FechaCarga = null!,
                               Bol = null!,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = null!,
                               VolCar = null!,
                               Transportista = null!,
                               Unidad = null!,
                               Operador = null!,
                               FechaDoc = null!,
                               Eta = null!,
                               FechaEst = null!,
                               Trayecto = null!,
                               Observaciones = null!,
                               FechaRealEta = null!,
                               LitEnt = null!
                           })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(ordensSinAsignar);

                    //Órdenes programadas
                    var ordens = await context.OrdenEmbarque
                     .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                     || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && fechas.Estado.Equals(1) == x.Destino!.Cliente!.Tipven!.Contains("terno") && x.Codtad == id_terminal)
                     .Include(x => x.Chofer)
                     .Include(x => x.Destino)
                     .ThenInclude(x => x.Cliente)
                     .Include(x => x.Estado)
                     .Include(x => x.OrdenCompra)
                     .Include(x => x.Tad)
                     .Include(x => x.Producto)
                     .Include(x => x.Tonel)
                     .ThenInclude(x => x.Transportista)
                     .Include(x => x.OrdenCierre)
                     .OrderBy(x => x.Fchpet)
                     .ThenBy(x => x.Tonel!.Tracto)
                     .Include(x => x.OrdenPedido)
                       .Select(e => new EtaDTO()
                       {
                           Referencia = "ENER-" + e.Folio.ToString(),
                           FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                           Unidad_Negocio = e.Tad.Den,
                           EstatusOrden = e.Estado.den,
                           FechaCarga = null!,
                           Bol = null!,
                           DeliveryRack = e.Destino.Cliente.Tipven,
                           Cliente = e.Destino.Cliente.Den,
                           Destino = e.Destino.Den,
                           Producto = e.Producto.Den,
                           VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                           VolCar = null!,
                           Transportista = e.Tonel.Transportista.Den,
                           Unidad = e.Tonel.Veh,
                           Operador = e.Chofer.Den,
                           FechaDoc = null!,
                           Eta = null!,
                           FechaEst = null!,
                           Trayecto = null!,
                           Observaciones = null!,
                           FechaRealEta = null!,
                           LitEnt = null!
                       })
                     .Take(10000)
                     .ToListAsync();
                    Ordenes.AddRange(ordens);
                    //Órdenes sin carga-Pedientes de carga
                    var pedidosDate = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && fechas.Estado.Equals(1) == x.Destino!.Cliente!.Tipven!.Contains("terno") && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && fechas.Estado.Equals(1) == x.Destino!.Cliente!.Tipven!.Contains("terno") && x.Codtad == id_terminal)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Chofer)
                    .Include(x => x.Estado)
                    .OrderBy(x => x.Fchcar)
                        .Select(e => new EtaDTO()
                        {
                            Referencia = "ENER-" + e.Folio.ToString(),
                            FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                            Unidad_Negocio = e.Tad.Den,
                            EstatusOrden = e.Estado.den,
                            FechaCarga = null!,
                            Bol = null!,
                            DeliveryRack = e.Destino.Cliente.Tipven,
                            Cliente = e.Destino.Cliente.Den,
                            Destino = e.Destino.Den,
                            Producto = e.Producto.Den,
                            VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                            VolCar = null!,
                            Transportista = e.Tonel.Transportista.Den,
                            Unidad = e.Tonel.Veh,
                            Operador = e.Chofer.Den,
                            FechaDoc = null!,
                            Eta = null!,
                            FechaEst = null!,
                            Trayecto = null!,
                            Observaciones = null!,
                            FechaRealEta = null!,
                            LitEnt = null!
                        })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(pedidosDate);
                    //Órdenes cargadas
                    var pedidosDate2 = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Id_Tad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && fechas.Estado.Equals(1) == x.Destino!.Cliente!.Tipven!.Contains("terno") && x.Id_Tad == id_terminal
                    //Internas-Externas
                    )
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Terminal)
                        .Include(x => x.Estado)
                        .Include(x => x.Producto)
                        .Include(x => x.Chofer)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenEmbarque)
                        .Include(x => x.OrdEmbDet)
                        .OrderBy(x => x.Fchcar)
                          .Select(e => new EtaDTO()
                          {
                              Referencia = "ENER-" + e.Folio.ToString(),
                              FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                              Unidad_Negocio = e.Terminal.Den,
                              EstatusOrden = e.Estado.den,
                              FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                              Bol = e.BatchId,
                              DeliveryRack = e.Destino.Cliente.Tipven,
                              Cliente = e.Destino.Cliente.Den,
                              Destino = e.Destino.Den,
                              Producto = e.Producto.Den,
                              VolNat = e.Vol2,
                              VolCar = e.Vol,
                              Transportista = e.Tonel.Transportista.Den,
                              Unidad = e.Tonel.Veh,
                              Operador = e.Chofer.Den,
                              FechaDoc = null!,
                              Eta = null!,
                              FechaEst = null!,
                              Trayecto = null!,
                              Observaciones = null!,
                              FechaRealEta = null!,
                              LitEnt = null!
                          })
                        .Take(10000)
                        .ToListAsync();
                    Ordenes.AddRange(pedidosDate2);
                    //Órdenes ETA-Trayecto
                    List<EtaDTO> newOrden = new List<EtaDTO>();
                    var Eta = await context.Orden
                          .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Id_Tad == id_terminal
                          //Pruebas de Filtro Interno - Externo
                          || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true
                          && fechas.Estado.Equals(1) == x.Destino!.Cliente!.Tipven!.Contains("terno") && x.Id_Tad == id_terminal)
                          .Include(x => x.OrdEmbDet)
                          .Include(x => x.Destino)
                          .ThenInclude(x => x.Cliente)
                          .Include(x => x.Terminal)
                          .Include(x => x.Estado)
                          .Include(x => x.Producto)
                          .Include(x => x.Chofer)
                          .Include(x => x.Tonel)
                          .ThenInclude(x => x.Transportista)
                          .Include(x => x.OrdEmbDet)
                          .OrderBy(x => x.Fchcar)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = e.Ref,
                               FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Terminal.Den,
                               EstatusOrden = e.Estado.den,
                               FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Bol = e.BatchId,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = e.Vol2,
                               VolCar = e.Vol,
                               Transportista = e.Tonel.Transportista.Den,
                               Unidad = e.Tonel.Veh,
                               Operador = e.Chofer.Den,
                               FechaDoc = e.OrdEmbDet.FchDoc.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Eta = e.OrdEmbDet.FchDoc!.Value.Subtract(e.OrdEmbDet.Fchlleest.Value!).ToString("hh\\:mm"),
                               FechaEst = e.OrdEmbDet.Fchlleest.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Trayecto = "ENTREGADO",
                               Observaciones = e.OrdEmbDet!.Obs,
                               FechaRealEta = e.OrdEmbDet.Fchrealledes.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               LitEnt = e.OrdEmbDet.Litent
                           })
                          .Take(10000)
                          .ToListAsync();
                    foreach (var item in Eta)
                        if (!newOrden.Contains(item))
                            newOrden.Add(item);


                    Ordenes.AddRange(newOrden);

                    return Ok(Ordenes);
                }
                else if (fechas.Estado == 2)
                {
                    List<EtaDTO> Ordenes = new List<EtaDTO>();
                    //Órdenes sin asignación de transporte-chofer-vehiculo
                    //órdenes sin asignar ordenar por BIN
                    var ordensSinAsignar = await context.OrdenEmbarque
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                        || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && fechas.Estado.Equals(2) == x.Destino!.Cliente!.Tipven!.StartsWith("Interno") && x.Codtad == id_terminal
                        )
                        .Include(x => x.Chofer)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Estado)
                        .Include(x => x.OrdenCompra)
                        .Include(x => x.Tad)
                        .Include(x => x.Producto)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenCierre)
                         .Include(x => x.OrdenPedido)
                        .OrderBy(x => x.Fchpet)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = "ENER-" + e.Folio.ToString(),
                               FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Tad.Den,
                               EstatusOrden = "Pendiente de Asignar",
                               FechaCarga = null!,
                               Bol = null!,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = null!,
                               VolCar = null!,
                               Transportista = null!,
                               Unidad = null!,
                               Operador = null!,
                               FechaDoc = null!,
                               Eta = null!,
                               FechaEst = null!,
                               Trayecto = null!,
                               Observaciones = null!,
                               FechaRealEta = null!,
                               LitEnt = null!
                           })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(ordensSinAsignar);

                    //Órdenes programadas
                    var ordens = await context.OrdenEmbarque
                     .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                     || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && fechas.Estado.Equals(2) == x.Destino!.Cliente!.Tipven!.StartsWith("Interno") && x.Codtad == id_terminal)
                     .Include(x => x.Chofer)
                     .Include(x => x.Destino)
                     .ThenInclude(x => x.Cliente)
                     .Include(x => x.Estado)
                     .Include(x => x.OrdenCompra)
                     .Include(x => x.Tad)
                     .Include(x => x.Producto)
                     .Include(x => x.Tonel)
                     .ThenInclude(x => x.Transportista)
                     .Include(x => x.OrdenCierre)
                     .OrderBy(x => x.Fchpet)
                     .ThenBy(x => x.Tonel!.Tracto)
                     .Include(x => x.OrdenPedido)
                       .Select(e => new EtaDTO()
                       {
                           Referencia = "ENER-" + e.Folio.ToString(),
                           FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                           Unidad_Negocio = e.Tad.Den,
                           EstatusOrden = e.Estado.den,
                           FechaCarga = null!,
                           Bol = null!,
                           DeliveryRack = e.Destino.Cliente.Tipven,
                           Cliente = e.Destino.Cliente.Den,
                           Destino = e.Destino.Den,
                           Producto = e.Producto.Den,
                           VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                           VolCar = null!,
                           Transportista = e.Tonel.Transportista.Den,
                           Unidad = e.Tonel.Veh,
                           Operador = e.Chofer.Den,
                           FechaDoc = null!,
                           Eta = null!,
                           FechaEst = null!,
                           Trayecto = null!,
                           Observaciones = null!,
                           FechaRealEta = null!,
                           LitEnt = null!
                       })
                     .Take(10000)
                     .ToListAsync();

                    Ordenes.AddRange(ordens);

                    //Órdenes sin carga-Pedientes de carga
                    var pedidosDate = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && fechas.Estado.Equals(2) == x.Destino!.Cliente!.Tipven!.Contains("Interno") && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && fechas.Estado.Equals(2) == x.Destino!.Cliente!.Tipven!.Contains("Interno") && x.Codtad == id_terminal)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Chofer)
                    .Include(x => x.Estado)
                    .OrderBy(x => x.Fchcar)
                        .Select(e => new EtaDTO()
                        {
                            Referencia = "ENER-" + e.Folio.ToString(),
                            FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                            Unidad_Negocio = e.Tad.Den,
                            EstatusOrden = e.Estado.den,
                            FechaCarga = null!,
                            Bol = null!,
                            DeliveryRack = e.Destino.Cliente.Tipven,
                            Cliente = e.Destino.Cliente.Den,
                            Destino = e.Destino.Den,
                            Producto = e.Producto.Den,
                            VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                            VolCar = null!,
                            Transportista = e.Tonel.Transportista.Den,
                            Unidad = e.Tonel.Veh,
                            Operador = e.Chofer.Den,
                            FechaDoc = null!,
                            Eta = null!,
                            FechaEst = null!,
                            Trayecto = null!,
                            Observaciones = null!,
                            FechaRealEta = null!,
                            LitEnt = null!
                        })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(pedidosDate);
                    //Órdenes cargadas
                    var pedidosDate2 = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Id_Tad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && fechas.Estado.Equals(2) == x.Destino!.Cliente!.Tipven!.StartsWith("Interno") && x.Id_Tad == id_terminal
                    //Internas-Externas
                     )
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Terminal)
                        .Include(x => x.Estado)
                        .Include(x => x.Producto)
                        .Include(x => x.Chofer)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenEmbarque)
                        .Include(x => x.OrdEmbDet)
                        .OrderBy(x => x.Fchcar)
                          .Select(e => new EtaDTO()
                          {
                              Referencia = "ENER-" + e.Folio.ToString(),
                              FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                              Unidad_Negocio = e.Terminal.Den,
                              EstatusOrden = e.Estado.den,
                              FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                              Bol = e.BatchId,
                              DeliveryRack = e.Destino.Cliente.Tipven,
                              Cliente = e.Destino.Cliente.Den,
                              Destino = e.Destino.Den,
                              Producto = e.Producto.Den,
                              VolNat = e.Vol2,
                              VolCar = e.Vol,
                              Transportista = e.Tonel.Transportista.Den,
                              Unidad = e.Tonel.Veh,
                              Operador = e.Chofer.Den,
                              FechaDoc = null!,
                              Eta = null!,
                              FechaEst = null!,
                              Trayecto = null!,
                              Observaciones = null!,
                              FechaRealEta = null!,
                              LitEnt = null!
                          })
                        .Take(10000)
                        .ToListAsync();
                    Ordenes.AddRange(pedidosDate2);
                    //Órdenes ETA-Trayecto
                    List<EtaDTO> newOrden = new List<EtaDTO>();
                    var Eta = await context.Orden
                          .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Id_Tad == id_terminal
                          //Pruebas de Filtro Interno - Externo
                          || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true
                          && fechas.Estado.Equals(2) == x.Destino!.Cliente!.Tipven!.StartsWith("Interno") && x.Id_Tad == id_terminal
                          )
                          .Include(x => x.OrdEmbDet)
                          .Include(x => x.Destino)
                          .ThenInclude(x => x.Cliente)
                          .Include(x => x.Terminal)
                          .Include(x => x.Estado)
                          .Include(x => x.Producto)
                          .Include(x => x.Chofer)
                          .Include(x => x.Tonel)
                          .ThenInclude(x => x.Transportista)
                          .Include(x => x.OrdEmbDet)
                          .OrderBy(x => x.Fchcar)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = e.Ref,
                               FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Terminal.Den,
                               EstatusOrden = e.Estado.den,
                               FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Bol = e.BatchId,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = e.Vol2,
                               VolCar = e.Vol,
                               Transportista = e.Tonel.Transportista.Den,
                               Unidad = e.Tonel.Veh,
                               Operador = e.Chofer.Den,
                               FechaDoc = e.OrdEmbDet.FchDoc.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Eta = e.OrdEmbDet.FchDoc!.Value.Subtract(e.OrdEmbDet.Fchlleest.Value!).ToString("hh\\:mm"),
                               FechaEst = e.OrdEmbDet.Fchlleest.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Trayecto = "ENTREGADO",
                               Observaciones = e.OrdEmbDet!.Obs,
                               FechaRealEta = e.OrdEmbDet.Fchrealledes.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               LitEnt = e.OrdEmbDet.Litent
                           })
                          .Take(10000)
                          .ToListAsync();
                    foreach (var item in Eta)
                        if (!newOrden.Contains(item))
                            newOrden.Add(item);


                    Ordenes.AddRange(newOrden);

                    return Ok(Ordenes);
                }
                else if (fechas.Estado == 3)
                {
                    List<EtaDTO> Ordenes = new List<EtaDTO>();

                    //Órdenes sin asignación de transporte-chofer-vehiculo
                    //órdenes sin asignar ordenar por BIN
                    var ordensSinAsignar = await context.OrdenEmbarque
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                        || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.StartsWith("Externo") && x.Codtad == id_terminal
                        )
                        .Include(x => x.Chofer)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Estado)
                        .Include(x => x.OrdenCompra)
                        .Include(x => x.Tad)
                        .Include(x => x.Producto)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenCierre)
                         .Include(x => x.OrdenPedido)
                        .OrderBy(x => x.Fchpet)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = "ENER-" + e.Folio.ToString(),
                               FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Tad.Den,
                               EstatusOrden = "Pendiente de Asignar",
                               FechaCarga = null!,
                               Bol = null!,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = null!,
                               VolCar = null!,
                               Transportista = null!,
                               Unidad = null!,
                               Operador = null!,
                               FechaDoc = null!,
                               Eta = null!,
                               FechaEst = null!,
                               Trayecto = null!,
                               Observaciones = null!,
                               FechaRealEta = null!,
                               LitEnt = null!
                           })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(ordensSinAsignar);

                    //Órdenes programadas
                    var ordens = await context.OrdenEmbarque
                     .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                     || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.StartsWith("Externo") && x.Codtad == id_terminal)
                     .Include(x => x.Chofer)
                     .Include(x => x.Destino)
                     .ThenInclude(x => x.Cliente)
                     .Include(x => x.Estado)
                     .Include(x => x.OrdenCompra)
                     .Include(x => x.Tad)
                     .Include(x => x.Producto)
                     .Include(x => x.Tonel)
                     .ThenInclude(x => x.Transportista)
                     .Include(x => x.OrdenCierre)
                     .OrderBy(x => x.Fchpet)
                     .ThenBy(x => x.Tonel!.Tracto)
                     .Include(x => x.OrdenPedido)
                       .Select(e => new EtaDTO()
                       {
                           Referencia = "ENER-" + e.Folio.ToString(),
                           FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                           Unidad_Negocio = e.Tad.Den,
                           EstatusOrden = e.Estado.den,
                           FechaCarga = null!,
                           Bol = null!,
                           DeliveryRack = e.Destino.Cliente.Tipven,
                           Cliente = e.Destino.Cliente.Den,
                           Destino = e.Destino.Den,
                           Producto = e.Producto.Den,
                           VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                           VolCar = null!,
                           Transportista = e.Tonel.Transportista.Den,
                           Unidad = e.Tonel.Veh,
                           Operador = e.Chofer.Den,
                           FechaDoc = null!,
                           Eta = null!,
                           FechaEst = null!,
                           Trayecto = null!,
                           Observaciones = null!,
                           FechaRealEta = null!,
                           LitEnt = null!
                       })
                     .Take(10000)
                     .ToListAsync();

                    Ordenes.AddRange(ordens);

                    //Órdenes sin carga-Pedientes de carga
                    var pedidosDate = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.Contains("Externo") && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.Contains("Externo") && x.Codtad == id_terminal)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Chofer)
                    .Include(x => x.Estado)
                    .OrderBy(x => x.Fchcar)

                        .Select(e => new EtaDTO()
                        {
                            Referencia = "ENER-" + e.Folio.ToString(),
                            FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                            Unidad_Negocio = e.Tad.Den,
                            EstatusOrden = e.Estado.den,
                            FechaCarga = null!,
                            Bol = null!,
                            DeliveryRack = e.Destino.Cliente.Tipven,
                            Cliente = e.Destino.Cliente.Den,
                            Destino = e.Destino.Den,
                            Producto = e.Producto.Den,
                            VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                            VolCar = null!,
                            Transportista = e.Tonel.Transportista.Den,
                            Unidad = e.Tonel.Veh,
                            Operador = e.Chofer.Den,
                            FechaDoc = null!,
                            Eta = null!,
                            FechaEst = null!,
                            Trayecto = null!,
                            Observaciones = null!,
                            FechaRealEta = null!,
                            LitEnt = null!
                        })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(pedidosDate);
                    //Órdenes cargadas
                    var pedidosDate2 = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Id_Tad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.StartsWith("Externo") && x.Id_Tad == id_terminal
                    //Internas-Externas
                     )
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Terminal)
                        .Include(x => x.Estado)
                        .Include(x => x.Producto)
                        .Include(x => x.Chofer)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenEmbarque)
                        .Include(x => x.OrdEmbDet)
                        .OrderBy(x => x.Fchcar)
                          .Select(e => new EtaDTO()
                          {
                              Referencia = "ENER-" + e.Folio.ToString(),
                              FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                              Unidad_Negocio = e.Terminal.Den,
                              EstatusOrden = e.Estado.den,
                              FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                              Bol = e.BatchId,
                              DeliveryRack = e.Destino.Cliente.Tipven,
                              Cliente = e.Destino.Cliente.Den,
                              Destino = e.Destino.Den,
                              Producto = e.Producto.Den,
                              VolNat = e.Vol2,
                              VolCar = e.Vol,
                              Transportista = e.Tonel.Transportista.Den,
                              Unidad = e.Tonel.Veh,
                              Operador = e.Chofer.Den,
                              FechaDoc = null!,
                              Eta = null!,
                              FechaEst = null!,
                              Trayecto = null!,
                              Observaciones = null!,
                              FechaRealEta = null!,
                              LitEnt = null!
                          })
                        .Take(10000)
                        .ToListAsync();
                    Ordenes.AddRange(pedidosDate2);

                    //Órdenes ETA-Trayecto
                    List<EtaDTO> newOrden = new List<EtaDTO>();
                    var Eta = await context.Orden
                          .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.Estado.ToString()) && x.Id_Tad == id_terminal
                          //Pruebas de Filtro Interno - Externo
                          || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true
                          && fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.StartsWith("Externo") && x.Id_Tad == id_terminal)
                          .Include(x => x.OrdEmbDet)
                          .Include(x => x.Destino)
                          .ThenInclude(x => x.Cliente)
                          .Include(x => x.Terminal)
                          .Include(x => x.Estado)
                          .Include(x => x.Producto)
                          .Include(x => x.Chofer)
                          .Include(x => x.Tonel)
                          .ThenInclude(x => x.Transportista)
                          .Include(x => x.OrdEmbDet)
                          .OrderBy(x => x.Fchcar)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = e.Ref,
                               FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Terminal.Den,
                               EstatusOrden = e.Estado.den,
                               FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Bol = e.BatchId,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = e.Vol2,
                               VolCar = e.Vol,
                               Transportista = e.Tonel.Transportista.Den,
                               Unidad = e.Tonel.Veh,
                               Operador = e.Chofer.Den,
                               FechaDoc = e.OrdEmbDet.FchDoc.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Eta = e.OrdEmbDet.FchDoc!.Value.Subtract(e.OrdEmbDet.Fchlleest.Value!).ToString("hh\\:mm"),
                               FechaEst = e.OrdEmbDet.Fchlleest.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Trayecto = "ENTREGADO",
                               Observaciones = e.OrdEmbDet!.Obs,
                               FechaRealEta = e.OrdEmbDet.Fchrealledes.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               LitEnt = e.OrdEmbDet.Litent
                           })
                          .Take(10000)
                          .ToListAsync();
                    foreach (var item in Eta)
                        if (!newOrden.Contains(item))
                            newOrden.Add(item);
                    Ordenes.AddRange(newOrden);

                    return Ok(Ordenes);
                }
                else if (fechas.Estado == 4)
                {
                    List<EtaDTO> Ordenes = new List<EtaDTO>();

                    //Órdenes sin asignación de transporte-chofer-vehiculo
                    //órdenes sin asignar ordenar por BIN
                    var ordensSinAsignar = await context.OrdenEmbarque
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                        && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null && x.Codtad == id_terminal)
                        .Include(x => x.Chofer)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Estado)
                        .Include(x => x.OrdenCompra)
                        .Include(x => x.Tad)
                        .Include(x => x.Producto)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenCierre)
                         .Include(x => x.OrdenPedido)
                        .OrderBy(x => x.Fchpet)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = "ENER-" + e.Folio.ToString(),
                               FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Tad.Den,
                               EstatusOrden = "Pendiente de Asignar",
                               FechaCarga = null!,
                               Bol = null!,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = null!,
                               VolCar = null!,
                               Transportista = null!,
                               Unidad = null!,
                               Operador = null!,
                               FechaDoc = null!,
                               Eta = null!,
                               FechaEst = null!,
                               Trayecto = null!,
                               Observaciones = null!,
                               FechaRealEta = null!,
                               LitEnt = null!
                           })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(ordensSinAsignar);

                    //Órdenes programadas
                    var ordens = await context.OrdenEmbarque
                     .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                     && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null && x.Codtad == id_terminal)
                     .Include(x => x.Chofer)
                     .Include(x => x.Destino)
                     .ThenInclude(x => x.Cliente)
                     .Include(x => x.Estado)
                     .Include(x => x.OrdenCompra)
                     .Include(x => x.Tad)
                     .Include(x => x.Producto)
                     .Include(x => x.Tonel)
                     .ThenInclude(x => x.Transportista)
                     .Include(x => x.OrdenCierre)
                     .OrderBy(x => x.Fchpet)
                     .ThenBy(x => x.Tonel!.Tracto)
                     .Include(x => x.OrdenPedido)
                       .Select(e => new EtaDTO()
                       {
                           Referencia = "ENER-" + e.Folio.ToString(),
                           FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                           Unidad_Negocio = e.Tad.Den,
                           EstatusOrden = e.Estado.den,
                           FechaCarga = null!,
                           Bol = null!,
                           DeliveryRack = e.Destino.Cliente.Tipven,
                           Cliente = e.Destino.Cliente.Den,
                           Destino = e.Destino.Den,
                           Producto = e.Producto.Den,
                           VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                           VolCar = null!,
                           Transportista = e.Tonel.Transportista.Den,
                           Unidad = e.Tonel.Veh,
                           Operador = e.Chofer.Den,
                           FechaDoc = null!,
                           Eta = null!,
                           FechaEst = null!,
                           Trayecto = null!,
                           Observaciones = null!,
                           FechaRealEta = null!,
                           LitEnt = null!
                       })
                     .Take(10000)
                     .ToListAsync();

                    Ordenes.AddRange(ordens);

                    //Órdenes sin carga-Pedientes de carga
                    var pedidosDate = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true && x.Codtad == id_terminal)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Chofer)
                    .Include(x => x.Estado)
                    .OrderBy(x => x.Fchcar)
                        .Select(e => new EtaDTO()
                        {
                            Referencia = "ENER-" + e.Folio.ToString(),
                            FechaPrograma = e.Fchcar.Value.ToString("yyyy-MM-dd"),
                            Unidad_Negocio = e.Tad.Den,
                            EstatusOrden = e.Estado.den,
                            FechaCarga = null!,
                            Bol = null!,
                            DeliveryRack = e.Destino.Cliente.Tipven,
                            Cliente = e.Destino.Cliente.Den,
                            Destino = e.Destino.Den,
                            Producto = e.Producto.Den,
                            VolNat = e.Compartment == 1 ? Convert.ToDouble(e.Tonel.Capcom) :
                            e.Compartment == 2 ? Convert.ToDouble(e.Tonel.Capcom2) :
                            e.Compartment == 3 ? Convert.ToDouble(e.Tonel.Capcom3) :
                            e.Compartment == 4 ? e.Tonel.Capcom4 : e.Vol,
                            VolCar = null!,
                            Transportista = e.Tonel.Transportista.Den,
                            Unidad = e.Tonel.Veh,
                            Operador = e.Chofer.Den,
                            FechaDoc = null!,
                            Eta = null!,
                            FechaEst = null!,
                            Trayecto = null!,
                            Observaciones = null!,
                            FechaRealEta = null!,
                            LitEnt = null!
                        })
                    .Take(10000)
                    .ToListAsync();
                    Ordenes.AddRange(pedidosDate);
                    //Órdenes cargadas
                    var pedidosDate2 = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20 && x.Id_Tad == id_terminal)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Terminal)
                        .Include(x => x.Estado)
                        .Include(x => x.Producto)
                        .Include(x => x.Chofer)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.OrdenEmbarque)
                        .Include(x => x.OrdEmbDet)
                        .OrderBy(x => x.Fchcar)
                          .Select(e => new EtaDTO()
                          {
                              Referencia = "ENER-" + e.Folio.ToString(),
                              FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                              Unidad_Negocio = e.Terminal.Den,
                              EstatusOrden = e.Estado.den,
                              FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                              Bol = e.BatchId,
                              DeliveryRack = e.Destino.Cliente.Tipven,
                              Cliente = e.Destino.Cliente.Den,
                              Destino = e.Destino.Den,
                              Producto = e.Producto.Den,
                              VolNat = e.Vol2,
                              VolCar = e.Vol,
                              Transportista = e.Tonel.Transportista.Den,
                              Unidad = e.Tonel.Veh,
                              Operador = e.Chofer.Den,
                              FechaDoc = null!,
                              Eta = null!,
                              FechaEst = null!,
                              Trayecto = null!,
                              Observaciones = null!,
                              FechaRealEta = null!,
                              LitEnt = null!
                          })
                        .Take(10000)
                        .ToListAsync();
                    Ordenes.AddRange(pedidosDate2);

                    //Órdenes ETA-Trayecto
                    List<EtaDTO> newOrden = new List<EtaDTO>();
                    var Eta = await context.Orden
                          .Where(x =>
                              //x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta)
                              //||
                              x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && x.Id_Tad == id_terminal
                          //&& string.IsNullOrEmpty(fechas.Estado.ToString())
                          //Pruebas de Filtro Interno - Externo
                          //|| x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true
                          //&& fechas.Estado.Equals(3) == x.Destino!.Cliente!.Tipven!.StartsWith("Externo")
                          )
                          .Include(x => x.OrdEmbDet)
                          .Include(x => x.Destino)
                          .ThenInclude(x => x.Cliente)
                          .Include(x => x.Terminal)
                          .Include(x => x.Estado)
                          .Include(x => x.Producto)
                          .Include(x => x.Chofer)
                          .Include(x => x.Tonel)
                          .ThenInclude(x => x.Transportista)
                          .Include(x => x.OrdEmbDet)
                          .OrderBy(x => x.Fchcar)
                           .Select(e => new EtaDTO()
                           {
                               Referencia = e.Ref,
                               FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                               Unidad_Negocio = e.Terminal.Den,
                               EstatusOrden = e.Estado.den,
                               FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Bol = e.BatchId,
                               DeliveryRack = e.Destino.Cliente.Tipven,
                               Cliente = e.Destino.Cliente.Den,
                               Destino = e.Destino.Den,
                               Producto = e.Producto.Den,
                               VolNat = e.Vol2,
                               VolCar = e.Vol,
                               Transportista = e.Tonel.Transportista.Den,
                               Unidad = e.Tonel.Veh,
                               Operador = e.Chofer.Den,
                               FechaDoc = e.OrdEmbDet.FchDoc.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Eta = e.OrdEmbDet.FchDoc!.Value.Subtract(e.OrdEmbDet.Fchlleest.Value!).ToString("hh\\:mm"),
                               FechaEst = e.OrdEmbDet.Fchlleest.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               Trayecto = "ENTREGADO",
                               Observaciones = e.OrdEmbDet!.Obs,
                               FechaRealEta = e.OrdEmbDet.Fchrealledes.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                               LitEnt = e.OrdEmbDet.Litent
                           })
                          .Take(10000)
                          .ToListAsync();
                    foreach (var item in Eta)
                        if (!newOrden.Contains(item))
                            newOrden.Add(item);


                    Ordenes.AddRange(newOrden);

                    return Ok(Ordenes);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Method para exportación de reportes mediante lapso de fechas
        [HttpPost("Etareporte")]
        public async Task<ActionResult> GetEta([FromBody] FechasF fechas)
        {
            try
            {
                List<EtaDTO> newOrden = new List<EtaDTO>();
                var Eta = await context.Orden
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && fechas.TipVenta == x.Destino.Cliente.Tipven && !string.IsNullOrEmpty(fechas.TipVenta) || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && string.IsNullOrEmpty(fechas.TipVenta))
                        .Include(x => x.OrdEmbDet)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Estado)
                        .Include(x => x.Producto)
                        .Include(x => x.Chofer)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)

                        .OrderBy(x => x.Fchcar)

                         .Select(e => new EtaDTO()
                         {
                             Referencia = e.Ref,
                             FechaPrograma = e.OrdenEmbarque.Fchcar.Value.ToString("yyyy-MM-dd"),
                             EstatusOrden = "CLOSED",
                             FechaCarga = e.Fchcar.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                             Bol = e.BatchId,
                             DeliveryRack = e.Destino.Cliente.Tipven,
                             Cliente = e.Destino.Cliente.Den,
                             Destino = e.Destino.Den,
                             Producto = e.Producto.Den,
                             VolNat = e.Vol2,
                             VolCar = e.Vol,
                             Transportista = e.Tonel.Transportista.Den,
                             Unidad = e.Tonel.Veh,
                             Operador = e.Chofer.Den,
                             FechaDoc = e.OrdEmbDet.FchDoc.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                             Eta = e.OrdEmbDet.FchDoc!.Value.Subtract(e.OrdEmbDet.Fchlleest.Value!).ToString("hh\\:mm") ?? string.Empty,
                             FechaEst = e.OrdEmbDet.Fchlleest.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                             Trayecto = "ENTREGADO",
                             Observaciones = e.OrdEmbDet!.Obs,
                             FechaRealEta = e.OrdEmbDet.Fchrealledes.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                             LitEnt = e.OrdEmbDet.Litent
                         })
                        .Take(10000)
                        .ToListAsync();

                foreach (var item in Eta)
                    if (!newOrden.Contains(item))
                        newOrden.Add(item);

                return Ok(newOrden);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}

