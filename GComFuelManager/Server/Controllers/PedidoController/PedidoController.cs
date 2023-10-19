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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Administrador Sistema, Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Programador, Coordinador, Analista Credito, Contador, Auditor, Comprador, Ejecutivo de Cuenta Operativo")]
    public class PedidoController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserToken verifyUser;
        private readonly string Id = string.Empty;

        public PedidoController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserToken verifyUser)
        {
            this.context = context;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                var pedidos = await context.OrdenEmbarque
                    .Include(x => x.Destino)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .Take(10000)
                    .ToListAsync();
                return Ok(pedidos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        //Realización de pedido
        [HttpPost("list")]
        public async Task<ActionResult> PostList(List<int> list)
        {
            try
            {
                List<OrdenEmbarque> ordenes = new List<OrdenEmbarque>();
                OrdenEmbarque? pedido = new OrdenEmbarque();

                foreach (var item in list)
                {
                    pedido = await context.OrdenEmbarque
                    .Where(x => x.Cod == item)
                    .Include(x => x.Destino)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .FirstOrDefaultAsync();
                    ordenes.Add(pedido);
                }
                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        //Borrar pedido
        [HttpDelete("{cod:int}/cancel")]
        public async Task<ActionResult> PutCancel([FromRoute] int cod)
        {
            try
            {
                OrdenEmbarque? pedido = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Cod == cod);

                if (pedido is null)
                    return NotFound();

                pedido.Codest = 14;
                context.Update(pedido);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 4);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        //Method para obtener pedidos mediante un rango de fechas
        [HttpPost("filtrar")]
        public async Task<ActionResult> GetDate([FromBody] FechasF fechas)
        {
            try
            {
                List<OrdenEmbarque> ordens = new List<OrdenEmbarque>();
                List<OrdenEmbarque> newOrdens = new List<OrdenEmbarque>();
                //órdenes asignadas ordenar por orden compartimento 
                ordens = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                    && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel != null)
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
                    .Take(10000)
                    .ToListAsync();
                //órdenes sin asignar ordenar por BIN
                var ordensSinAsignar = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 3 && x.FchOrd != null
                    && x.Bolguidid == null && x.Folio == null && x.CodordCom != null && x.Tonel == null)
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
                    .Include(x => x.OrdenPedido)
                    .Take(10000)
                    .ToListAsync();

                ordens.AddRange(ordensSinAsignar);

                ordens.OrderByDescending(x => x.Bin);

                foreach (var item in ordens)
                    if (!newOrdens.Contains(item))
                        newOrdens.Add(item);

                return Ok(newOrdens);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("filtrar/pendientes")]
        public async Task<ActionResult> GetOrdenPendiente([FromBody] FechasF fechas)
        {
            try
            {
                List<OrdenEmbarque> ordens = new List<OrdenEmbarque>();

                ordens = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 9 && x.Bolguidid == null
                    && !string.IsNullOrEmpty(x.OrdenCierre.Folio))
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
                    .Take(10000)
                    .ToListAsync();

                ordens.OrderByDescending(x => x.Bin);

                return Ok(ordens);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Method para obtener pedidos mediante rango de fechas y checbox seleccionado
        //REALIZAR TRES CONDICIONES, UNA POR CADA RADIOBUTTON, QUE SEA EN TRUE SE HARÁ EL MISMO FILTRO POR FECHAS EN WHERE AÑADIENDO LOS CAMPOS QUE UTILIZAN CADA CLAUSULA
        [HttpPost("filtro")]
        public async Task<ActionResult> GetDateRadio([FromBody] FechasF fechas)
        {
            try
            {
                //editar registros de orden con el nuevo campo de folio en 0 al remplazar los registros
                if (fechas.Estado == 1)
                {
                    List<Orden> newOrden = new List<Orden>();
                    //Traerme al bolguid is not null, codest =3 y transportista activo en 1 --Ordenes Sin Cargar--
                    var pedidosDate = await context.OrdenEmbarque

                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 3 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Chofer)
                    .Include(x => x.Estado)
                    .Include(x => x.Orden)
                    //.OrderBy(x => x.Bin)
                    .OrderByDescending(x => x.Bin)
                    //.Select(x => new OrdenesDTO() { Referencia = x.Folio })
                    .Select(o => new Orden()
                    {
                        Cod = o.Cod,
                        Ref = "ENER-" + o.Folio.ToString(),
                        //Ref = o.ref
                        Fchcar = o.Fchcar,
                        Estado = o.Estado,
                        Destino = o.Destino,
                        Producto = o.Producto,
                        //Vol2 = o.Compartment == 1 ? o.Tonel?.Capcom : o.Compartment == 2 ? o.Tonel?.Capcom2 : o.Compartment == 3 ? o.Tonel?.Capcom3 : o.Compartment == 4 ? o?.Tonel?.Capcom4 : o.Vol,
                        Vol2 = null!,
                        Vol = o.Vol,
                        Bolguiid = null!,
                        BatchId = null!,
                        Tonel = o.Tonel,
                        Chofer = o.Chofer,
                        Compartimento = o.Compartment
                    })
                    //.OrderBy(x => x.Fchcar)
                    //ordens.OrderByDescending(x => x.Bin);
                    .Take(10000)
                    .ToListAsync();
                    //pedidosDate.OrderByDescending(x => x.Fchcar);

                    foreach (var item in pedidosDate)
                        if (!newOrden.Contains(item))
                            newOrden.Add(item);


                    return Ok(newOrden);
                }
                else if (fechas.Estado == 2)
                {
                    //List<Orden> pedidosDate = new List<Orden>();
                    List<Orden> newOrden = new List<Orden>();
                    //Traerme al transportista activo en 1 y codest = 26 --Ordenes Cargadas--
                    var pedidosDate = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 20 && x.Tonel!.Transportista.Activo == true)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Estado)
                    .Include(x => x.Producto)
                    .Include(x => x.Chofer)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.OrdenEmbarque)
                     .OrderBy(x => x.Fchcar)
                     //.GroupBy(x => new { x.Destino.Den, x.Tonel.Tracto, x.Ref, x.Codprd2, x.BatchId, x.Fchcar, x.Tonel.Placa })
                   //Falta agrupar producto.den, cliente.den, chofer.den-shortden, transportista.den
                 
                    .Take(10000)
                    .ToListAsync();
                    // pedidosDate.OrderByDescending(x => x.Fchcar);


                    foreach (var item in pedidosDate)
                        if (!newOrden.Contains(item))
                            newOrden.Add(item);

                    return Ok(newOrden);
                }
                else if (fechas.Estado == 3)
                {
                    List<Orden> newOrden = new List<Orden>();
                    //Traerme al transportista activo en 1 --Ordenes en trayecto-- 
                    var pedidosDate = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 26)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Estado)
                    .Include(x => x.Chofer)
                    .OrderBy(x => x.Fchcar)
                    .Take(10000)
                    .ToListAsync();
                    //pedidosDate.OrderByDescending(x => x.Fchcar);
                    return Ok(pedidosDate);
                }
                else if (fechas.Estado == 4)
                {
                    //Ordenes canceladas
                    List<Orden> ordenesCanceladas = new List<Orden>();

                    var pedidosDate = await context.Orden
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 14)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.Estado)
                        .Include(x => x.Chofer)
                        .OrderBy(x => x.Fchcar)
                        .Take(10000)
                        .ToListAsync();
                    //pedidosDate.OrderByDescending(x => x.Fchcar);
                    if (pedidosDate is not null)
                        ordenesCanceladas.AddRange(pedidosDate);

                    var ordenes = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 14 && x.Bolguidid != null)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Chofer)
                    .Include(x => x.Estado)
                    //.Select(x => new OrdenesDTO() { Referencia = x.Folio })
                    .Select(o => new Orden()
                    {
                        Cod = o.Cod,
                        Ref = "ENER-" + o.Folio.ToString(),
                        //Ref = o.ref
                        Fchcar = o.Fchcar,
                        Estado = o.Estado,
                        Destino = o.Destino,
                        Producto = o.Producto,
                        Vol2 = o.Vol,
                        Vol = null!,
                        Bolguiid = null!,
                        BatchId = null!,
                        Tonel = o.Tonel,
                        Chofer = o.Chofer,
                        Compartimento = o.Compartment
                    })
                    .OrderBy(x => x.Fchcar)
                    .Take(10000)
                    .ToListAsync();
                    if (ordenes is not null)
                        ordenesCanceladas.AddRange(ordenes);

                    return Ok(ordenes);
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

        [HttpPost("trafico")]
        public async Task<ActionResult> GetTraffic([FromBody] FechasF fechas)
        {
            try
            {

                var pedidosDate = await context.Orden
                  .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && x.Codprd2 != 10157)
                  .Include(x => x.Destino)
                  .ThenInclude(x => x.Cliente)
                  .Include(x => x.Producto)
                  .Include(x => x.Tonel)
                  .ThenInclude(x => x.Transportista)
                  .Include(x => x.Estado)
                  .Include(x => x.Chofer)
                  .OrderBy(x => x.Fchcar)
                  .Take(10000)
                  .ToListAsync();
                return Ok(pedidosDate);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

        }
        [HttpPost("Historial")]
        public async Task<ActionResult> GetHistorial([FromBody] FechasF fechas)
        {

            try
            {
                List<Orden> Ordenes = new List<Orden>();
                var pedidosDate = await context.OrdenEmbarque
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null && x.Tonel.Transportista.Activo == true)
                .Include(x => x.Destino)
                .ThenInclude(x => x.Cliente)
                .Include(x => x.Tad)
                .Include(x => x.Producto)
                .Include(x => x.Tonel)
                .ThenInclude(x => x.Transportista)
                .Include(x => x.Chofer)
                .Include(x => x.Estado)
                .Select(o => new Orden()
                {
                    Cod = o.Cod,
                    Ref = "ENER-" + o.Folio.ToString(),
                    Fchcar = o.Fchcar,
                    Estado = o.Estado,
                    Destino = o.Destino,
                    Producto = o.Producto,
                    Vol2 = null!,
                    Vol = o.Vol,
                    Bolguiid = null!,
                    BatchId = null!,
                    Tonel = o.Tonel,
                    Chofer = o.Chofer,
                    Compartimento = o.Compartment
                })
                .OrderBy(x => x.Fchcar)
                .Take(10000)
                .ToListAsync();
                Ordenes.AddRange(pedidosDate);
                var pedidosDate2 = await context.Orden
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 20)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Estado)
                    .Include(x => x.Producto)
                    .Include(x => x.Chofer)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.OrdenEmbarque)
                    .OrderBy(x => x.Fchcar)
                    .Take(10000)
                    .ToListAsync();
                Ordenes.AddRange(pedidosDate2);

                var pedidosDate3 = await context.Orden
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista.Activo == true && x.Codest == 26)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Estado)
                    .Include(x => x.Chofer)
                    .Include(x => x.OrdenEmbarque)
                    .OrderBy(x => x.Fchcar)
                    .Take(10000)
                    .ToListAsync();
                Ordenes.AddRange(pedidosDate3);
                //Ordenes Canceladas
                List<Orden> ordenesCanceladas = new List<Orden>();
                var pedidosDate4 = await context.Orden
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 14)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Estado)
                    .Include(x => x.Chofer)
                    .Include(x => x.OrdenEmbarque)
                    .Take(10000)
                    .ToListAsync();
                if (pedidosDate4 is not null)
                    ordenesCanceladas.AddRange(pedidosDate4);
                var orden = await context.OrdenEmbarque
                  .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 14 && x.Bolguidid != null)
                  .Include(x => x.Destino)
                  .ThenInclude(x => x.Cliente)
                  .Include(x => x.Tad)
                  .Include(x => x.Producto)
                  .Include(x => x.Tonel)
                  .ThenInclude(x => x.Transportista)
                  .Include(x => x.Chofer)
                  .Include(x => x.Estado)
                  //.Select(x => new OrdenesDTO() { Referencia = x.Folio })
                  .Select(o => new Orden()
                  {
                      Cod = o.Cod,
                      Ref = "ENER-" + o.Folio.ToString(),
                      //Ref = o.ref
                      Fchcar = o.Fchcar,
                      Estado = o.Estado,
                      Destino = o.Destino,
                      Producto = o.Producto,
                      Vol2 = o.Vol,
                      Vol = null!,
                      Bolguiid = null!,
                      BatchId = null!,
                      Tonel = o.Tonel,
                      Chofer = o.Chofer,
                      Compartimento = o.Compartment
                  })
                  .OrderBy(x => x.Fchcar)
                  .Take(10000)
                  .ToListAsync();
                if (orden is not null)
                    ordenesCanceladas.AddRange(orden);
                Ordenes.AddRange(orden);

                return Ok(Ordenes);

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Method para realizar (agregar) pedido
        //[HttpPost]
        private async Task<ActionResult> Post(OrdenEmbarque orden)
        {
            try
            {
                var user = await context.Usuario.FirstOrDefaultAsync(x => x.Usu == HttpContext.User.FindFirstValue(ClaimTypes.Name));
                if (user == null)
                    return NotFound();
                orden.Codusu = user!.Cod;
                context.Add(orden);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();
                await context.SaveChangesAsync(id, 2);

                var NewOrden = await context.OrdenEmbarque.Where(x => x.Cod == orden.Cod)
                    .Include(x => x.Destino)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .Include(x => x.Estado)
                    .FirstOrDefaultAsync();

                return Ok(NewOrden);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Route("binNumber")]
        [HttpGet]
        public async Task<ActionResult> GetLastBin()
        {
            try
            {
                var bin = await context.OrdenEmbarque.Select(x => x.Bin).OrderBy(x => x).LastOrDefaultAsync();
                return Ok(bin);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Confirmar pedido
        [HttpPost("confirm")]
        public async Task<ActionResult<OrdenCompra>> PostConfirm(List<OrdenEmbarque> orden)
        {
            try
            {
                OrdenCompra newFolio = new OrdenCompra();
                var folio = await context.OrdenCompra.Select(x => x.cod).OrderBy(x => x).LastOrDefaultAsync();
                if (folio != 0)
                {
                    ++folio;
                    newFolio = new OrdenCompra() { den = $"ENER_{DateTime.Now:yyyy-MM-dd}_{folio}" };
                    context.Add(newFolio);
                }
                orden.ForEach(x =>
                {
                    x.Destino = null!;
                    x.Estado = null!;
                    x.Tad = null!;
                    x.Chofer = null!;
                    x.Tonel = null!;
                    x.Producto = null;
                    x.OrdenCierre = null!;
                    x.Cliente = null!;
                    x.OrdenPedido = null!;
                    x.Codest = 3;
                    x.CodordCom = folio;
                    x.FchOrd = DateTime.Today.Date;
                });

                context.UpdateRange(orden);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 15);
                return Ok(newFolio);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost("check/chofer")]
        public async Task<ActionResult> PostConfirmChofer([FromBody] CheckChofer checkChofer)
        {
            try
            {
                var chofer = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Codton == checkChofer.Tonel
                && x.Codchf == checkChofer.Chofer && x.CompartmentId == checkChofer.Compartimento && x.Fchcar == checkChofer.FechaCarga
                && x.Bolguidid == null);
                if (chofer == null)
                {
                    return Ok(0);
                }
                return Ok(chofer.Cod);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("desasignar/{code:int}")]
        public async Task<ActionResult> PutAsignacion(int code)
        {
            try
            {
                var orden = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Cod == code);

                orden.Chofer = null;
                orden.Tonel = null;

                orden.Codchf = null;
                orden.Codton = null;
                orden.Compartment = null;
                orden.CompartmentId = null;

                context.Update(orden);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("update")]
        public async Task<ActionResult> PutPedido([FromBody] OrdenEmbarque orden)
        {
            try
            {
                orden.Producto = null;
                orden.Chofer = null;
                orden.Destino = null;
                orden.Tonel = null;
                orden.Tad = null;
                orden.OrdenCompra = null;
                orden.Estado = null;
                orden.Cliente = null!;
                orden.OrdenCierre = null!;

                context.Update(orden);
                await context.SaveChangesAsync();

                var ord = await context.OrdenEmbarque.Where(x => x.Cod == orden.Cod)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Tad)
                    .Include(x => x.Estado)
                    .Include(x => x.OrdenCompra)
                    .Include(x => x.Chofer)
                    .Include(x => x.OrdenCierre)
                    .FirstOrDefaultAsync();

                return Ok(ord);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPut("cierre/update/{cod:int}")]
        public async Task<ActionResult> PutPedidoCierre([FromBody] OrdenCierre orden, [FromRoute] int cod)
        {
            try
            {

                var o = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Cod == cod);
                if (o == null)
                    return NotFound();

                o!.Codprd = orden.CodPrd;
                o!.Coddes = orden.CodDes;
                o!.Pre = orden.Precio;
                o!.Vol = orden.Volumen;

                context.Update(o);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost("historial/envios")]
        public async Task<ActionResult> GetDateHistorico([FromBody] FechasF fechas)
        {
            try
            {
                List<OrdenEmbarque> ordens = new List<OrdenEmbarque>();

                ordens = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin)
                    .Include(x => x.Chofer)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Estado)
                    .Include(x => x.OrdenCompra)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .OrderBy(x => x.Fchpet)
                    .Take(10000)
                    .ToListAsync();

                ordens.OrderByDescending(x => x.Bin);

                return Ok(ordens);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("cierre/{folio?}")]
        public async Task<ActionResult> PostPedidoVolumen([FromBody] OrdenEmbarque orden, [FromRoute] string? folio)
        {
            try
            {

                if (string.IsNullOrEmpty(folio))
                {
                    return await Post(orden);
                    //return Ok(orden);
                }
                else
                {
                    var cierres = context.OrdenCierre.Where(x => x.Folio!.Equals(folio)).ToList();
                    if (cierres is null)
                        return BadRequest("No existe el cierre.");

                    var pedidos = context.OrdenPedido.Where(x => x.Folio!.Equals(folio)).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Orden).ToList();

                    var user = await context.Usuario.FirstOrDefaultAsync(x => x.Usu == HttpContext.User.FindFirstValue(ClaimTypes.Name));
                    if (user == null)
                        return NotFound();

                    if (cierres.Any(x => x.CodPrd == orden.Codprd))
                    {

                        var VolumenDisponible = cierres.Where(x => x.CodPrd == orden.Codprd && x.Estatus is true).Sum(x => x.Volumen);

                        var VolumenCongelado = pedidos.Where(x => x.CodPed == orden.Codprd
                        && x?.OrdenEmbarque?.OrdenCierre?.Estatus is true
                        && x?.OrdenEmbarque?.Folio is not null
                        && x?.OrdenEmbarque?.Orden is null).Sum(item =>
                        item?.OrdenEmbarque?.Compartment == 1 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom?.ToString())
                                        : item?.OrdenEmbarque?.Compartment == 2 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom2?.ToString())
                                        : item?.OrdenEmbarque?.Compartment == 3 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom3?.ToString())
                                        : item?.OrdenEmbarque?.Compartment == 4 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom4?.ToString())
                                        : item?.OrdenEmbarque?.Vol);

                        var VolumenConsumido = pedidos.Where(x => x.OrdenEmbarque?.Codprd == orden.Codprd
                        && x?.OrdenEmbarque?.OrdenCierre?.Estatus is true
                        && x?.OrdenEmbarque?.Folio is not null
                        && x?.OrdenEmbarque?.Orden?.BatchId is not null).Sum(x => x.OrdenEmbarque?.Orden?.Vol);

                        var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado);

                        if (VolumenTotalDisponible < orden.Vol)
                            return BadRequest("No hay suficiente volumen disponible");
                    }

                    orden.Codusu = user!.Cod;

                    context.Add(orden);

                    var id = await verifyUser.GetId(HttpContext, userManager);
                    if (string.IsNullOrEmpty(id))
                        return BadRequest();

                    await context.SaveChangesAsync(id, 2);

                    var NewOrden = await context.OrdenEmbarque.Where(x => x.Cod == orden.Cod)
                    .Include(x => x.Destino)
                    .Include(x => x.Tad)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .Include(x => x.Estado)
                    .FirstOrDefaultAsync();

                    return Ok(NewOrden);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("orden/add")]
        public async Task<ActionResult> PostRelation([FromBody] OrdenPedido ordenPedido)
        {
            try
            {
                ordenPedido.OrdenEmbarque = null!;
                context.Add(ordenPedido);
                await context.SaveChangesAsync();
                return Ok(true);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("verificar/carga")]
        public async Task<ActionResult> VerifyVolumenAsignacion([FromBody] OrdenEmbarque orden)
        {
            try
            {
                return Ok(true);

                if (orden == null)
                    return BadRequest();

                var folio = context.OrdenPedido.FirstOrDefault(x => x.CodPed == orden.Cod);

                var cierres = context.OrdenCierre.Where(x => x.Folio!.Equals(folio.Folio)).ToList();
                if (cierres is null)
                    return BadRequest("No existe el cierre.");

                var pedidos = context.OrdenPedido.Where(x => x.Folio!.Equals(folio.Folio) && x.CodPed != 0 && !string.IsNullOrEmpty(x.Folio))
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Orden)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tonel)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.OrdenCierre)
                    .ToList();


                if (cierres.Any(x => x.CodPrd == orden.Codprd))
                {



                    var VolumenDisponible = cierres.Where(x => x.CodPrd == orden.Codprd && x.Estatus is true).Sum(x => x.Volumen);

                    var VolumenCongelado = pedidos.Where(x => x.OrdenEmbarque.Codprd == orden.Codprd
                    && x.OrdenEmbarque.OrdenCierre.Estatus is true
                    && x?.OrdenEmbarque?.Folio is not null
                    && x?.OrdenEmbarque?.Orden is null).Sum(item =>
                    item?.OrdenEmbarque?.Compartment == 1 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom?.ToString())
                                    : item?.OrdenEmbarque?.Compartment == 2 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom2?.ToString())
                                    : item?.OrdenEmbarque?.Compartment == 3 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom3?.ToString())
                                    : item?.OrdenEmbarque?.Compartment == 4 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom4?.ToString())
                                    : item?.OrdenEmbarque?.Vol);

                    var VolumenConsumido = pedidos.Where(x => x.OrdenEmbarque?.Codprd == orden.Codprd
                    && x?.OrdenEmbarque?.OrdenCierre?.Estatus is true
                    && x?.OrdenEmbarque?.Folio is not null
                    && x?.OrdenEmbarque?.Orden?.BatchId is not null).Sum(x => x.OrdenEmbarque?.Orden?.Vol);

                    var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado);

                    var tonel = context.Tonel.FirstOrDefault(x => x.Cod == orden.Codton);

                    if (tonel is null)
                        return BadRequest("No se encontro la unidad");

                    var volumen = orden?.Compartment == 1 ? double.Parse(tonel?.Capcom.ToString())
                            : orden?.Compartment == 2 ? double.Parse(tonel?.Capcom2.ToString())
                            : orden?.Compartment == 3 ? double.Parse(tonel?.Capcom3.ToString())
                            : double.Parse(tonel?.Capcom4.ToString());

                    if (VolumenTotalDisponible < volumen)
                    {
                        return BadRequest($"No hay suficiente volumen disponible. Volumen Disponible: {VolumenTotalDisponible}. Intento de carga: {volumen}");
                    }
                }

                await context.SaveChangesAsync();
                return Ok(true);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("orden/{folio?}")]
        public async Task<ActionResult> GetOrdens([FromRoute] int? folio)
        {
            try
            {
                List<OrdenEmbarque> ordenes = new List<OrdenEmbarque>();

                if (folio != null && folio != 0)
                    ordenes = await context.OrdenEmbarque.Where(x => x.Folio == folio)
                        .Include(x => x.Producto)
                        .Include(x => x.Estado)
                        .Include(x => x.Destino)
                        .Include(x => x.Tonel)
                        .Include(x => x.Chofer)
                        .Include(x => x.OrdenCierre)
                        .Include(x => x.Orden)
                        .ThenInclude(x => x.Estado)
                        .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("cancel/{cod:int}")]
        public async Task<ActionResult> CancelPedido([FromRoute] int cod)
        {
            try
            {
                OrdenEmbarque? pedido = context.OrdenEmbarque.FirstOrDefault(x => x.Cod == cod);

                if (pedido is null)
                    return NotFound(pedido);

                OrdenCierre? cierre = context.OrdenCierre.FirstOrDefault(x => x.CodPed == pedido.Cod);

                pedido.Codest = 14;

                if (cierre is not null)
                    cierre.Estatus = false;

                context.Update(pedido);

                if (cierre is not null)
                    context.Update(cierre);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 4);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}