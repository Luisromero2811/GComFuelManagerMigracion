using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Lectura Asignacion, Administrador Sistema, Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Programador, Coordinador, Analista Credito, Contador, Auditor, Comprador, Ejecutivo de Cuenta Operativo")]
    public class PedidoController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserToken verifyUser;
        private readonly User_Terminal _terminal;
        private readonly string Id = string.Empty;

        public PedidoController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserToken verifyUser, User_Terminal _Terminal)
        {
            this.context = context;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
            this._terminal = _Terminal;
        }

        //[HttpGet]
        //public async Task<ActionResult> Get()
        //{
        //    try
        //    {
        //        var pedidos = await context.OrdenEmbarque
        //            .Include(x => x.Destino)
        //            .Include(x => x.Tad)
        //            .Include(x => x.Producto)
        //            .Include(x => x.Tonel)
        //            .Take(10000)
        //            .ToListAsync();
        //        return Ok(pedidos);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}
        //Realización de pedido
        [HttpPost("list")]//TODO: checar utilidad
        public async Task<ActionResult> PostList(List<int> list)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenEmbarque> ordenes = new();
                OrdenEmbarque? pedido = new();

                foreach (var item in list)
                {
                    pedido = await context.OrdenEmbarque
                    .Where(x => x.Cod == item && x.Codtad == id_terminal)
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                OrdenEmbarque? pedido = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Cod == cod && x.Codtad == id_terminal);

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
        [HttpPost("filtrar")]//TODO: checar utilidad
        public async Task<ActionResult> GetDate([FromBody] FechasF fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenEmbarque> ordens = new();
                List<OrdenEmbarque> newOrdens = new();
                //órdenes asignadas ordenar por orden compartimento 
                ordens = await context.OrdenEmbarque
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
                    .Include(x => x.Tad)
                    .OrderBy(x => x.Fchpet)
                    .ThenBy(x => x.Tonel!.Tracto)
                    .Include(x => x.OrdenPedido)
                    .Take(10000)
                    .ToListAsync();
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenEmbarque> ordens = new();

                ordens = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 9 && x.Bolguidid == null
                    && x.OrdenCierre != null && !string.IsNullOrEmpty(x.OrdenCierre.Folio) && x.Codtad == id_terminal)
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                //editar registros de orden con el nuevo campo de folio en 0 al remplazar los registros
                if (fechas.Estado == 1)
                {
                    List<Orden> newOrden = new();
                    //Traerme al bolguid is not null, codest =3 y transportista activo en 1 --Ordenes Sin Cargar--
                    var pedidosDate = await context.OrdenEmbarque.IgnoreAutoIncludes()
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 3 && x.Bolguidid != null
                    && x.Tonel != null && x.Tonel.Transportista != null && x.Tonel.Transportista.Activo == true && x.Codtad == id_terminal
                    || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null
                    && x.Tonel != null && x.Tonel.Transportista != null && x.Tonel.Transportista.Activo == true && x.Codtad == id_terminal)
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
                        Terminal = o.Tad,
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
                    List<Orden> newOrden = new();
                    //Traerme al transportista activo en 1 y codest = 26 --Ordenes Cargadas--
                    var pedidosDate = await context.Orden.IgnoreAutoIncludes()
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 20 && x.Id_Tad == id_terminal)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Estado)
                    .Include(x => x.Producto)
                    .Include(x => x.Chofer)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.OrdenEmbarque)
                    .Include(x => x.Terminal)
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
                    List<Orden> newOrden = new();
                    //Traerme al transportista activo en 1 --Ordenes en trayecto-- 
                    var pedidosDate = await context.Orden.IgnoreAutoIncludes()
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel != null && x.Tonel.Transportista != null
                    && x.Tonel.Transportista.Activo == true && x.Codest == 26 && x.Id_Tad == id_terminal)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .Include(x => x.Estado)
                    .Include(x => x.Chofer)
                    .Include(x => x.Terminal)
                    .OrderBy(x => x.Fchcar)
                    .Take(10000)
                    .ToListAsync();
                    //pedidosDate.OrderByDescending(x => x.Fchcar);
                    return Ok(pedidosDate);
                }
                else if (fechas.Estado == 4)
                {
                    //Ordenes canceladas
                    List<Orden> ordenesCanceladas = new();

                    var pedidosDate = await context.Orden.IgnoreAutoIncludes()
                        .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 14 && x.Id_Tad == id_terminal)
                        .Include(x => x.Destino)
                        .ThenInclude(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Tonel)
                        .ThenInclude(x => x.Transportista)
                        .Include(x => x.Estado)
                        .Include(x => x.Chofer)
                        .Include(x => x.Terminal)
                        .OrderBy(x => x.Fchcar)
                        .Take(10000)
                        .ToListAsync();
                    //pedidosDate.OrderByDescending(x => x.Fchcar);
                    if (pedidosDate is not null)
                        ordenesCanceladas.AddRange(pedidosDate);

                    var ordenes = await context.OrdenEmbarque.IgnoreAutoIncludes()
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 14 && x.Bolguidid != null && x.Codtad == id_terminal)
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
                        Terminal = o.Tad,
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

        [HttpPost("trafico")]//TODO: checar utilidad
        public async Task<ActionResult> GetTraffic([FromBody] FechasF fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var pedidosDate = await context.Orden
                  .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel!.Transportista!.Activo == true && x.Codprd2 != 10157 && x.Id_Tad == id_terminal)
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<Orden> Ordenes = new();
                var pedidosDate = await context.OrdenEmbarque.IgnoreAutoIncludes()
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Bolguidid != null && x.FchOrd != null && x.Codest == 3 && x.Tonel!.Transportista!.Activo == true || x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 22 && x.Bolguidid != null
                && x.Tonel != null && x.Tonel.Transportista != null && x.Tonel.Transportista.Activo == true && x.Codtad == id_terminal)
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
                var pedidosDate2 = await context.Orden.IgnoreAutoIncludes()
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel != null && x.Tonel.Transportista != null
                && x.Tonel.Transportista.Activo == true && x.Codest == 20 && x.Id_Tad == id_terminal)
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

                var pedidosDate3 = await context.Orden.IgnoreAutoIncludes()
                .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Tonel != null && x.Tonel.Transportista != null
                && x.Tonel.Transportista.Activo == true && x.Codest == 26)
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
                List<Orden> ordenesCanceladas = new();
                var pedidosDate4 = await context.Orden.IgnoreAutoIncludes()
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codest == 14 && x.Id_Tad == id_terminal)
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
                var orden = await context.OrdenEmbarque.IgnoreAutoIncludes()
                  .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.FchOrd != null && x.Codest == 14 && x.Bolguidid != null && x.Codtad == id_terminal)
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var user = await context.Usuario.FirstOrDefaultAsync(x => x.Usu == HttpContext.User.FindFirstValue(ClaimTypes.Name));
                if (user == null)
                    return NotFound();

                orden.Codusu = user!.Cod;
                orden.Codtad = id_terminal;
                context.Add(orden);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();
                await context.SaveChangesAsync(id, 2);

                var NewOrden = await context.OrdenEmbarque.Where(x => x.Cod == orden.Cod && x.Codtad == id_terminal)
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
        [HttpGet]//TODO: checar utilidad
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
                OrdenCompra newFolio = new();
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

        [HttpPost("check/chofer")]//TODO: checar utilidad
        public async Task<ActionResult> PostConfirmChofer([FromBody] CheckChofer checkChofer)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var chofer = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Codton == checkChofer.Tonel
                && x.Codchf == checkChofer.Chofer && x.CompartmentId == checkChofer.Compartimento && x.Fchcar == checkChofer.FechaCarga
                && x.Bolguidid == null && x.Codtad == id_terminal);
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var orden = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Cod == code && x.Codtad == id_terminal);

                if (orden is null)
                    return NotFound();

                orden.Chofer = null;
                orden.Tonel = null;
                orden.Tad = null;
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
        public async Task<ActionResult> PutPedido([FromBody] OrdenCierre cierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                OrdenEmbarque? orden = cierre.OrdenEmbarque;

                cierre.Producto = null!;
                cierre.Destino = null!;
                cierre.Cliente = null!;
                cierre.ContactoN = null!;
                cierre.OrdenEmbarque = null!;
                cierre.OrdenPedidos = null!;
                cierre.Terminal = null!;

                context.Update(cierre);

                if (orden != null)
                {
                    orden.Producto = null!;
                    orden.Chofer = null!;
                    orden.Destino = null!;
                    orden.Tonel = null!;
                    orden.Tad = null!;
                    orden.OrdenCompra = null!;
                    orden.Estado = null!;
                    orden.Cliente = null!;
                    orden.OrdenCierre = null!;
                    orden.OrdenPedido = null!;

                    orden.Codprd = cierre.CodPrd;
                    orden.Coddes = cierre.CodDes;
                    orden.Fchcar = cierre.FchCar;
                    orden.Codtad = cierre.Id_Tad;
                    orden.Vol = cierre.Volumen;
                    orden.Pre = cierre.Precio;

                    context.Update(orden);
                }

                await context.SaveChangesAsync();

                var newOrden = context.OrdenCierre.Where(x => x.Cod == cierre.Cod && x.Id_Tad == id_terminal)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tad)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Estado)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.Cliente)
                    .IgnoreAutoIncludes()
                    .FirstOrDefault();

                return Ok(newOrden);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost("asignar/unidad")]
        public async Task<ActionResult> AsignarPedido([FromBody] OrdenEmbarque orden)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                orden.Producto = null;
                orden.Chofer = null;
                orden.Destino = null;
                orden.Tonel = null;
                orden.Tad = null;
                orden.OrdenCompra = null;
                orden.Estado = null;
                orden.Cliente = null!;
                orden.OrdenCierre = null!;
                orden.OrdenPedido = null!;

                context.Update(orden);
                await context.SaveChangesAsync();

                var ord = await context.OrdenEmbarque.Where(x => x.Cod == orden.Cod && x.Codtad == id_terminal)
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

        [HttpPut("cierre/update/{cod:int}")]//TODO: checar utilidad
        public async Task<ActionResult> PutPedidoCierre([FromBody] OrdenCierre orden, [FromRoute] int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var o = await context.OrdenEmbarque.FirstOrDefaultAsync(x => x.Cod == cod && x.Codtad == id_terminal);
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
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenEmbarque> ordens = new();

                ordens = await context.OrdenEmbarque
                    .Where(x => x.Fchcar >= fechas.DateInicio && x.Fchcar <= fechas.DateFin && x.Codtad == id_terminal)
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

                return Ok(ordens.DistinctBy(x => x.Cod));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("cierre/{folio?}")] //TODO: checar utilidad
        public async Task<ActionResult> PostPedidoVolumen([FromBody] OrdenEmbarque orden, [FromRoute] string? folio)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (string.IsNullOrEmpty(folio))
                {
                    return await Post(orden);
                    //return Ok(orden);
                }
                else
                {
                    var cierres = context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(folio) && x.Id_Tad == id_terminal).ToList();
                    if (cierres is null)
                        return BadRequest("No existe el cierre.");

                    var pedidos = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(folio) && x.OrdenEmbarque != null
                    && x.OrdenEmbarque.Codtad == id_terminal).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Orden).ToList();

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
                    orden.Codtad = id_terminal;

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

        [HttpPost("verificar/carga/{ID_Cierre:int}")]//TODO: checar utilidad
        public async Task<ActionResult> Verificar_Volumen_Creacion_Orden([FromBody] OrdenCierre orden, [FromRoute] int ID_Cierre)
        {
            try
            {
                //return Ok(true);
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (orden == null)
                    return BadRequest();

                OrdenCierre? cierre = new();

                if (ID_Cierre != 0)
                    cierre = context.OrdenCierre.FirstOrDefault(x => x.Cod == ID_Cierre && x.Id_Tad == id_terminal);
                else
                    cierre = context.OrdenCierre.FirstOrDefault(x => x.Folio == orden.Folio_Perteneciente && x.CodPrd == orden.CodPrd && x.Id_Tad == id_terminal);

                if (cierre is null)
                    return BadRequest("No existe el cierre.");

                if (context.OrdenPedido.Any(x => !string.IsNullOrEmpty(cierre.Folio) && x.Folio == cierre.Folio))
                {
                    var VolumenDisponible = cierre.Volumen;

                    var listConsumido = context.OrdenPedido.Where(x => x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(cierre.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == cierre.CodPrd
                            && x.OrdenEmbarque.Codest == 22
                            && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Bolguidid != null)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel).ToList();

                    var VolumenCongelado = listConsumido.Sum(item => item.OrdenEmbarque!.Compartment == 1 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 2 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom2!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 3 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom3!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 4 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom4!.ToString())
                                            : item.OrdenEmbarque!.Vol);

                    var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(cierre.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == cierre.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                    var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(cierre.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == cierre.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                    var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(cierre.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == cierre.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                    var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado + VolumenSolicitado);

                    if (VolumenTotalDisponible < orden.Volumen)
                        return Ok(false);
                }

                return Ok(true);
            }
            catch (ArgumentNullException)
            {
                return BadRequest("La orden o el codigo de cierre no pueden ir vacios o con valores nulos");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("orden/{folio?}")]//TODO: checar utilidad
        public async Task<ActionResult> GetOrdens([FromRoute] int? folio)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenEmbarque> ordenes = new();
                //List<OrdenEmbarque> ordenes_adicionales = new List<OrdenEmbarque>();

                if (folio != null && folio != 0)
                    ordenes = await context.OrdenEmbarque.Where(x => x.Folio == folio && x.Codtad == id_terminal)
                        .Include(x => x.Producto)
                        .Include(x => x.Estado)
                        .Include(x => x.Destino)
                        .Include(x => x.Tonel)
                        .Include(x => x.Chofer)
                        .Include(x => x.OrdenCierre)
                        .IgnoreAutoIncludes()
                        .ToListAsync();

                foreach (var item in ordenes)
                {
                    List<Orden> Ordenes_Synthesis = context.Orden.Where(x => x.Ref == item.FolioSyn)
                        .Include(x => x.Destino)
                        .Include(x => x.Producto)
                        .Include(x => x.Estado)
                        .IgnoreAutoIncludes()
                        .ToList();

                    if (Ordenes_Synthesis is not null)
                        item.Ordenes_Synthesis.AddRange(Ordenes_Synthesis);
                }

                //foreach (var item in ordenes)
                //{
                //    if (context.Orden.Count(x => item != null && item.Orden != null && x.Ref == item.FolioSyn
                //            && x.Codest != 14 && item.Codest != 14) > 1)
                //    {
                //        var Ordenes_Adicionales = context.Orden.Where(x => item != null && item.Orden != null && x.Ref == item.FolioSyn
                //        && x.Cod != item.Orden.Cod).Include(x => x.Destino).ThenInclude(x => x.Cliente).Include(x => x.Producto).Include(x => x.Tonel).Include(x => x.Estado).IgnoreAutoIncludes().ToList();

                //        foreach (var oa in Ordenes_Adicionales)
                //        {
                //            OrdenEmbarque ordenEmbarque = new();

                //            if (item != null && item.Orden != null)
                //            {
                //                ordenEmbarque.Folio = item.Folio;
                //                ordenEmbarque.Pre = item.Pre;
                //                ordenEmbarque.Destino = new() { Den = oa?.Destino?.Den };
                //                ordenEmbarque.Cliente = new() { Den = oa?.Cliente?.Den };
                //                ordenEmbarque.Producto = new() { Den = oa?.Producto?.Den };
                //                ordenEmbarque.Tonel = new()
                //                {
                //                    Tracto = oa?.Tonel?.Tracto,
                //                    Placa = oa?.Tonel?.Placa
                //                };
                //                ordenEmbarque.Orden = new()
                //                {
                //                    Cod = oa.Cod,
                //                    BatchId = oa?.BatchId,
                //                    Fchcar = oa?.Fchcar,
                //                    Vol = oa.Vol,
                //                    Vol2 = oa.Vol2,
                //                    Liniteid = oa.Liniteid,
                //                };
                //                ordenEmbarque.Orden.Estado = new() { den = oa.Estado?.den };

                //                if (!ordenes_adicionales.Contains(ordenEmbarque))
                //                    ordenes_adicionales.Add(ordenEmbarque);
                //            }
                //        }
                //    }
                //}

                //ordenes.AddRange(ordenes_adicionales);

                return Ok(ordenes.OrderByDescending(x => x.Fchcar));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("cancel/{cod:int}")]//TODO: checar utilidad
        public async Task<ActionResult> CancelPedido([FromRoute] int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                OrdenEmbarque? pedido = context.OrdenEmbarque.FirstOrDefault(x => x.Cod == cod && x.Codtad == id_terminal);

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

        [HttpDelete("cancel/orden/{cod:int}")]// TODO: checar utilidad
        public async Task<ActionResult> Cancelar_Orden_Y_Ordenes_Synthesis([FromRoute] int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                OrdenEmbarque? pedido = context.OrdenEmbarque.FirstOrDefault(x => x.Cod == cod && x.Codtad == id_terminal);

                if (pedido is null)
                    return NotFound();

                OrdenCierre? cierre = context.OrdenCierre.FirstOrDefault(x => x.CodPed == pedido.Cod);

                pedido.Codest = 14;

                if (cierre is not null)
                    cierre.Estatus = false;

                context.Update(pedido);

                if (cierre is not null)
                    context.Update(cierre);

                List<Orden>? ordenes = context.Orden.Where(x => x.Folio == pedido.Folio).ToList();

                foreach (var item in ordenes)
                {
                    item.Codest = 14;
                    context.Update(item);
                }

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

        [HttpDelete("cancel/orden/synthesis/{cod:int}")]//TODO: checar utilidad
        public async Task<ActionResult> Cancelar_Ordenes_Synthesis([FromRoute] int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                Orden? orden = context.Orden.FirstOrDefault(x => x.Cod == cod && x.Id_Tad == id_terminal);

                if (orden is not null)
                {
                    orden.Codest = 14;
                    context.Update(orden);
                }

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


        [HttpPost("recrear")]
        public async Task<ActionResult> RecrearOrden([FromBody] List<OrdenEmbarque> ordenes)
        {
            try
            {
                List<OrdenCierre> cierres = new();
                List<OrdenEmbarque> ordenEmbarques = new();
                OrdenCierre? ordenCierre = new();
                OrdenEmbarque ordenEmbarque = new();
                OrdenPedido ordenPedido = new();

                Cliente? Cliente = new();
                Grupo? Grupo = new();

                var consecutivo = context.Consecutivo.First(x => x.Nombre == "Orden");
                if (consecutivo is null)
                {
                    Consecutivo Nuevo_Consecutivo = new() { Numeracion = 1, Nombre = "Orden" };
                    context.Add(Nuevo_Consecutivo);
                    await context.SaveChangesAsync();
                    consecutivo = Nuevo_Consecutivo;
                }
                else
                {
                    consecutivo.Numeracion++;
                    context.Update(consecutivo);
                    await context.SaveChangesAsync();
                }

                var guidfolio = $"RE{DateTime.Now:yy}-{consecutivo.Numeracion:000000}";

                foreach (var item in ordenes)
                {
                    var ordercopy = item.ShallowCopy();

                    var destino = context.Destino.FirstOrDefault(x => x.Cod == ordercopy.Coddes);
                    if (destino is null)
                        return BadRequest("No se encontro el destino");

                    var cliente = context.Cliente.FirstOrDefault(x => x.Cod == destino.Codcte);
                    if (cliente is null)
                        return BadRequest("No se encontro el cliente");

                    ordercopy.Cod = 0;
                    ordercopy.Codest = 3;
                    ordercopy.Fchcar = DateTime.Today;
                    ordercopy.Fchpet = DateTime.Now;
                    ordercopy.Bolguidid = null;
                    ordercopy.Folio = null;
                    ordercopy.CodordCom = null;
                    ordercopy.FchOrd = DateTime.Now;
                    ordenCierre = ordercopy.OrdenCierre;

                    ordercopy.Chofer = null!;
                    ordercopy.Destino = null!;
                    ordercopy.Producto = null!;
                    ordercopy.Cliente = null!;
                    ordercopy.OrdenCierre = null!;
                    ordercopy.OrdenCompra = null!;
                    ordercopy.OrdenPedido = null!;
                    ordercopy.Estado = null!;
                    ordercopy.Transportista = null!;
                    ordercopy.Tonel = null!;
                    ordercopy.Tad = null!;

                    context.Add(ordercopy);
                    await context.SaveChangesAsync();

                    if (ordenCierre != null)
                    {
                        var ordencierrecopy = ordenCierre.ShallowCopy();
                        ordencierrecopy.OrdenPedidos = null!;
                        ordencierrecopy.Folio = guidfolio;
                        ordencierrecopy.CodPed = ordercopy.Cod;
                        ordencierrecopy.Cod = 0;
                        ordencierrecopy.FchCar = DateTime.Today;
                        ordencierrecopy.FchCierre = DateTime.Today;

                        ordencierrecopy.Destino = null!;
                        ordencierrecopy.Cliente = null!;
                        ordencierrecopy.Producto = null!;
                        ordencierrecopy.OrdenEmbarque = null!;
                        ordencierrecopy.Grupo = null!;

                        context.Add(ordencierrecopy);
                        await context.SaveChangesAsync();

                        if (context.OrdenPedido.Any(x => x.CodPed == item.Cod))
                        {
                            var op = context.OrdenPedido.FirstOrDefault(x => x.CodPed == item.Cod);

                            ordenPedido = new OrdenPedido()
                            {
                                Folio = op.Folio,
                                CodPed = ordercopy.Cod,
                                CodCierre = ordencierrecopy.Cod,
                                Folio_Cierre_Copia = guidfolio,
                                Pedido_Original = item.Cod
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            ordenPedido = new OrdenPedido()
                            {
                                Folio = ordenCierre?.Folio,
                                CodPed = ordercopy.Cod,
                                CodCierre = ordencierrecopy.Cod,
                                Pedido_Original = item?.Cod,
                                Folio_Cierre_Copia = guidfolio
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                    }

                    if (ordenCierre is null)
                    {
                        var cierre = new OrdenCierre()
                        {
                            Folio = guidfolio,
                            CodPed = ordercopy.Cod,
                            FchCar = DateTime.Today,
                            FchCierre = DateTime.Today,
                            fchPrecio = DateTime.Now,
                            FchVencimiento = DateTime.Today.AddDays(5),
                            FchLlegada = DateTime.Today.AddDays(1),
                            Precio = ordercopy.Pre ?? 0,
                            CodDes = ordercopy.Coddes,
                            CodGru = cliente?.Codgru,
                            CodCte = cliente?.Cod,
                            CodPrd = ordercopy.Codprd,
                            Id_Tad = ordercopy.Codtad,
                            Volumen = int.Parse($"{ordercopy.Vol}"),
                            Moneda = ordercopy.Moneda,
                            Equibalencia = ordercopy.Equibalencia
                        };

                        context.Add(cierre);
                        await context.SaveChangesAsync();

                        if (context.OrdenPedido.Any(x => x.CodPed == item.Cod))
                        {
                            var op = context.OrdenPedido.FirstOrDefault(x => x.CodPed == item.Cod);

                            ordenPedido = new OrdenPedido()
                            {
                                Folio = op.Folio,
                                CodPed = ordercopy.Cod,
                                CodCierre = cierre.Cod,
                                Pedido_Original = item?.Cod,
                                Folio_Cierre_Copia = guidfolio
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            ordenPedido = new OrdenPedido()
                            {
                                Folio = ordenCierre?.Folio,
                                CodPed = ordercopy.Cod,
                                CodCierre = cierre.Cod,
                                Pedido_Original = item?.Cod,
                                Folio_Cierre_Copia = guidfolio
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                    }
                }

                return Ok(new CodDenDTO() { Den = guidfolio });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpGet("folios")]//TODO: checar utilidad
        public ActionResult GetFoliosFromPedidos([FromQuery] OrdenCierre ordenCierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioDetalleDTO> Folios = new();
                if (ordenCierre is null)
                    return BadRequest("Datos vacios");

                var foliosquery = context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.CodPed == 0 && x.Estatus == true && x.Activa == true
                    && !x.Folio.StartsWith("RE") && !x.Folio.StartsWith("OP") && x.FchVencimiento > DateTime.Today && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto).Include(x => x.Cliente).Include(x => x.Destino).AsQueryable();

                if (ordenCierre.CodGru != 0 && ordenCierre.CodGru != null)
                    foliosquery = foliosquery.Where(x => x.CodGru == ordenCierre.CodGru);
                if (ordenCierre.CodCte != 0 && ordenCierre.CodCte != null)
                    foliosquery = foliosquery.Where(x => x.CodCte == ordenCierre.CodCte || x.Folio.StartsWith("G"));
                if (ordenCierre.CodDes != 0 && ordenCierre.CodDes != null)
                    foliosquery = foliosquery.Where(x => x.CodDes == ordenCierre.CodDes || x.Folio.StartsWith("G"));

                if (foliosquery is not null)
                {
                    var folioDetalle = foliosquery
                        .Select(x => new ListadoFolioDetalle()
                        {
                            Folio = x.Folio,
                            Producto = x.Producto.Den ?? string.Empty,
                            Cliente = x.Cliente.Den ?? string.Empty,
                            Destino = x.Destino.Den ?? string.Empty,
                            Comentarios = x.Observaciones,
                            Fecha_Cierre = x.FchCierre
                        }).OrderBy(x => x.Fecha_Cierre);
                    return Ok(folioDetalle);
                }
                else
                    return Ok(new List<FolioDetalleDTO>());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("create/all")]
        public async Task<ActionResult> GetFolioToOrden([FromBody] OrdenCierre ordenCierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (!string.IsNullOrEmpty(ordenCierre.Folio_Perteneciente))
                {
                    var cierre = context.OrdenCierre.Where(x => x.Folio == ordenCierre.Folio_Perteneciente && x.Id_Tad == id_terminal).ToList();
                    if (cierre is not null)
                    {
                        if (cierre.Where(x => x.CodPrd == ordenCierre.CodPrd).Count() == 0)
                        {
                            return BadRequest("El producto seleccionado no se encuentra en el cierre");
                        }
                    }
                }

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                if (ordenCierre is null)
                    return BadRequest("No se encontro ninguna orden");

                string folio = string.Empty;

                //if (!string.IsNullOrEmpty(ordenCierre.Folio_Perteneciente))
                folio = context.OrdenCierre.FirstOrDefault(x => x.CodDes == ordenCierre.CodDes && x.CodCte == ordenCierre.CodCte && x.CodPrd == ordenCierre.CodPrd
                && x.CodPed != 0 && x.FchCierre == DateTime.Today && x.Estatus == true && x.Id_Tad == id_terminal)?.Folio ?? string.Empty;

                var user = await context.Usuario.FirstOrDefaultAsync(x => x.Usu == HttpContext.User.FindFirstValue(ClaimTypes.Name));
                if (user == null)
                    return NotFound();

                if (string.IsNullOrEmpty(folio))
                {
                    var consecutivo = context.Consecutivo.First(x => x.Nombre == "Orden");
                    if (consecutivo is null)
                    {
                        Consecutivo Nuevo_Consecutivo = new() { Numeracion = 1, Nombre = "Orden" };
                        context.Add(Nuevo_Consecutivo);
                        await context.SaveChangesAsync();
                        consecutivo = Nuevo_Consecutivo;
                    }
                    else
                    {
                        consecutivo.Numeracion++;
                        context.Update(consecutivo);
                        await context.SaveChangesAsync();
                    }

                    context.Update(consecutivo);
                    await context.SaveChangesAsync();

                    var cliente = context.Cliente.FirstOrDefault(x => x.Cod == ordenCierre.CodCte);

                    if (cliente is null)
                        return BadRequest("No se encontro el cliente");

                    if (!string.IsNullOrEmpty(ordenCierre.Folio_Perteneciente))
                        ordenCierre.Folio = $"O{DateTime.Now:yy}-{consecutivo.Numeracion:000000}{(cliente is not null && !string.IsNullOrEmpty(cliente.CodCte) ? $"-{cliente.CodCte}" : "-DFT")}";
                    else
                        ordenCierre.Folio = $"OP{DateTime.Now:yy}-{consecutivo.Numeracion:000000}{(cliente is not null && !string.IsNullOrEmpty(cliente.CodCte) ? $"-{cliente.CodCte}" : "-DFT")}";

                }
                else
                {
                    ordenCierre.Folio = folio;
                }

                var bin = context.OrdenEmbarque.Select(x => x.Bin).OrderBy(x => x).LastOrDefault();

                var bincount = context.OrdenEmbarque.Count(x => x.Bin == bin);

                var count = context.OrdenCierre.Count(x => x.Folio == folio && x.CodDes == ordenCierre.CodDes && x.CodCte == ordenCierre.CodCte
                && x.CodPrd == ordenCierre.CodPrd);

                OrdenEmbarque ordenEmbarque = new()
                {
                    Codest = 9,
                    Codtad = ordenCierre.Id_Tad,
                    Codprd = ordenCierre.CodPrd,
                    Pre = ordenCierre.Precio,
                    Vol = ordenCierre.Volumen,
                    Coddes = ordenCierre.CodDes,
                    Fchpet = DateTime.Now,
                    Fchcar = ordenCierre.FchCar,
                    Bin = count == 0 || bincount >= 2 ? ++bin : count % 2 == 0 ? ++bin : bin,
                    Codusu = user?.Cod,
                    Moneda = ordenCierre.Moneda,
                    Equibalencia = ordenCierre.Equibalencia
                };

                context.Add(ordenEmbarque);
                await context.SaveChangesAsync();

                ordenCierre.Producto = null!;
                ordenCierre.Destino = null!;
                ordenCierre.Grupo = null!;
                ordenCierre.OrdenEmbarque = null!;
                ordenCierre.OrdenPedidos = null!;
                ordenCierre.Cliente = null!;
                ordenCierre.Terminal = null!;
                ordenCierre.CodPed = ordenEmbarque.Cod;
                ordenCierre.FchVencimiento = ordenCierre.FchCierre?.AddDays(5);

                context.Add(ordenCierre);

                await context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(ordenCierre.Folio_Perteneciente))
                {
                    var cierre = context.OrdenCierre.FirstOrDefault(x => x.Folio == ordenCierre.Folio_Perteneciente);

                    if (cierre is not null)
                    {
                        ordenCierre.TipoPago = cierre.TipoPago ?? string.Empty;
                        context.Update(ordenCierre);

                        OrdenPedido ordenPedido = new()
                        {
                            CodPed = ordenEmbarque.Cod,
                            CodCierre = cierre?.Cod ?? 0,
                            Folio = ordenCierre.Folio_Perteneciente,
                        };

                        context.Add(ordenPedido);
                        await context.SaveChangesAsync();
                    }
                }

                var newOrden = context.OrdenCierre.Where(x => x.Cod == ordenCierre.Cod && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Cliente)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tad)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Estado)
                    .FirstOrDefault();

                return Ok(newOrden);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("folios/activo/vigente")]
        public ActionResult GetFoliosActivoVigente([FromQuery] Folio_Activo_Vigente parametros)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                Porcentaje porcentaje = new();
                var por = context.Porcentaje.FirstOrDefault(x => x.Accion == "cierre");
                if (por != null)
                    porcentaje = por;

                IEnumerable<Folio_Activo_Vigente> folios = new List<Folio_Activo_Vigente>();
                List<OrdenCierre> ordenCierres = new();
                var orden_cierres_query = context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.FchVencimiento >= DateTime.Today
                && x.CodPed == 0 && !x.Folio.StartsWith("RE") && x.Activa == true && x.Estatus == true && x.Id_Tad == id_terminal)
                    .Include(x => x.Destino).Include(x => x.Cliente).Include(x => x.Producto).Include(x => x.Grupo).OrderBy(x => x.FchCierre).IgnoreAutoIncludes().AsQueryable();

                if (orden_cierres_query is not null)
                {
                    if (!string.IsNullOrEmpty(parametros.Grupo_Filtrado))
                        orden_cierres_query = orden_cierres_query.Where(x => x.Grupo != null && !string.IsNullOrEmpty(x.Grupo.Den)
                        && x.Grupo.Den.ToLower().Contains(parametros.Grupo_Filtrado.ToLower()));
                    if (!string.IsNullOrEmpty(parametros.Cliente_Filtrado))
                        orden_cierres_query = orden_cierres_query.Where(x => x.Cliente != null && !string.IsNullOrEmpty(x.Cliente.Den)
                        && x.Cliente.Den.ToLower().Contains(parametros.Cliente_Filtrado.ToLower()));
                    if (!string.IsNullOrEmpty(parametros.Destino_Filtrado))
                        orden_cierres_query = orden_cierres_query.Where(x => x.Destino != null && !string.IsNullOrEmpty(x.Destino.Den)
                        && x.Destino.Den.ToLower().Contains(parametros.Destino_Filtrado.ToLower()));
                    if (!string.IsNullOrEmpty(parametros.Producto_Filtrado))
                        orden_cierres_query = orden_cierres_query.Where(x => x.Producto != null && !string.IsNullOrEmpty(x.Producto.Den)
                        && x.Producto.Den.ToLower().Contains(parametros.Producto_Filtrado.ToLower()));

                    ordenCierres = orden_cierres_query.ToList();
                }

                foreach (var item in ordenCierres)
                {
                    if (!string.IsNullOrEmpty(item.Folio) && item.CodPrd != null)
                    {
                        ordenCierres.First(x => x.Cod == item.Cod).VolumenDisponible = ObtenerVolumenDisponibleDeProducto(item.Folio, item.CodPrd);
                        if (item.VolumenDisponible is not null)
                            foreach (var item1 in item.VolumenDisponible.Productos)
                            {
                                if (item1.PromedioCarga >= (item1.Disponible * (porcentaje.Porcen / 100)))
                                {
                                    item.Activa = false;
                                }
                            }
                    }
                }

                folios = ordenCierres.Where(x => x.Activa == true).Select(x => new Folio_Activo_Vigente()
                {
                    Folio = x.Folio ?? string.Empty,
                    Grupo = x.Grupo,
                    Cliente = x.Cliente,
                    Destino = x.Destino,
                    Producto = x.Producto,
                    Fecha_Vigencia = x.FchVencimiento ?? DateTime.MinValue,
                    ID_Cierre = x.Cod,
                    ID_Producto = x.CodPrd,
                    Fecha_Cierre = x.FchCierre ?? DateTime.MinValue,
                    VolumenDisponibleDTO = x.VolumenDisponible,
                    Comentarios = x.Observaciones
                });

                //foreach (var item in folios)
                //{
                //    var o = context.OrdenCierre.FirstOrDefault(x => x.Cod == item.ID_Cierre);
                //    if (o != null)
                //        if (o.CodGru != null && folios.FirstOrDefault(x => x.ID_Cierre == item.ID_Cierre) is not null)
                //            folios.First(x => x.ID_Cierre == item.ID_Cierre).Grupo = context.Grupo.FirstOrDefault(x => x.Cod == o.CodGru);
                //}

                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("dividir")]
        public async Task<ActionResult> DividirOrdenes([FromBody] OrdenEmbarque ordenEmbarque)
        {
            try
            {
                if (ordenEmbarque is null)
                    return BadRequest("No se encontro ninguna orden");

                if (ordenEmbarque.Ordenes_A_Crear == 0)
                    return BadRequest("No se aceptan valores iguales o menores a 0");

                if (ordenEmbarque.Ordenes_A_Crear > 4)
                    return BadRequest("No se aceptan valores mayores a 4");

                var destino = context.Destino.FirstOrDefault(x => x.Cod == ordenEmbarque.Coddes);
                if (destino is null)
                    return BadRequest("No se encontro el destino");

                var cliente = context.Cliente.FirstOrDefault(x => x.Cod == destino.Codcte);
                if (cliente is null)
                    return BadRequest("No se encontro el cliente");

                var guid = Guid.NewGuid().ToString().Split("-");
                var guidfolio = $"O-{guid[4]}";

                var Volumen_A_Dividir = Math.Round((ordenEmbarque.Vol / ordenEmbarque.Ordenes_A_Crear) ?? 10000, 2);
                var ordenCierre = ordenEmbarque.OrdenCierre;

                OrdenPedido ordenPedido = new();

                for (int i = 0; i < ordenEmbarque?.Ordenes_A_Crear; i++)
                {
                    var orden = ordenEmbarque.ShallowCopy();
                    orden.Cod = 0;
                    orden.Vol = Volumen_A_Dividir;
                    orden.Chofer = null!;
                    orden.Destino = null!;
                    orden.Producto = null!;
                    orden.Cliente = null!;
                    orden.OrdenCierre = null!;
                    orden.OrdenCompra = null!;
                    orden.OrdenPedido = null!;
                    orden.Estado = null!;
                    orden.Transportista = null!;
                    orden.Tonel = null!;
                    orden.Tad = null!;

                    context.Add(orden);
                    await context.SaveChangesAsync();

                    if (ordenCierre != null)
                    {
                        var ordencierrecopy = ordenCierre.ShallowCopy();
                        ordencierrecopy.OrdenPedidos = null!;
                        ordencierrecopy.CodPed = orden.Cod;
                        ordencierrecopy.Cod = 0;
                        ordencierrecopy.Volumen = Convert.ToInt32(Volumen_A_Dividir);

                        ordencierrecopy.Destino = null!;
                        ordencierrecopy.Cliente = null!;
                        ordencierrecopy.Producto = null!;
                        ordencierrecopy.OrdenEmbarque = null!;
                        ordencierrecopy.Grupo = null!;
                        ordencierrecopy.Terminal = null!;

                        context.Add(ordencierrecopy);
                        await context.SaveChangesAsync();

                        if (context.OrdenPedido.Any(x => x.CodPed == ordenEmbarque.Cod))
                        {
                            var op = context.OrdenPedido.FirstOrDefault(x => x.CodPed == ordenEmbarque.Cod);
                            if (op is not null)
                            {
                                ordenPedido = new OrdenPedido()
                                {
                                    Folio = op.Folio,
                                    CodPed = orden.Cod,
                                    CodCierre = ordencierrecopy.Cod,
                                    Pedido_Original = ordenEmbarque.Cod,
                                    Folio_Cierre_Copia = ordenCierre.Folio
                                };

                                context.Add(ordenPedido);
                                await context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            ordenPedido = new OrdenPedido()
                            {
                                Folio = ordenCierre?.Folio,
                                CodPed = orden.Cod,
                                CodCierre = ordencierrecopy.Cod,
                                Pedido_Original = ordenEmbarque?.Cod,
                                Folio_Cierre_Copia = ordenCierre?.Folio
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                    }

                    if (ordenCierre is null)
                    {
                        var cierre = new OrdenCierre()
                        {
                            Folio = guidfolio,
                            CodPed = orden.Cod,
                            FchCar = DateTime.Today,
                            FchCierre = DateTime.Today,
                            fchPrecio = DateTime.Now,
                            FchVencimiento = DateTime.Today.AddDays(5),
                            FchLlegada = DateTime.Today.AddDays(1),
                            Precio = orden.Pre ?? 0,
                            CodDes = orden.Coddes,
                            CodGru = cliente?.Codgru,
                            CodCte = cliente?.Cod,
                            CodPrd = orden.Codprd,
                            Id_Tad = orden.Codtad,
                            Volumen = Convert.ToInt32(orden.Vol),
                            Moneda = orden.Moneda,
                            Equibalencia = orden.Equibalencia
                        };

                        context.Add(cierre);
                        await context.SaveChangesAsync();

                        if (context.OrdenPedido.Any(x => ordenEmbarque != null && x.CodPed == ordenEmbarque.Cod))
                        {
                            var op = context.OrdenPedido.FirstOrDefault(x => ordenEmbarque != null && x.CodPed == ordenEmbarque.Cod);

                            ordenPedido = new OrdenPedido()
                            {
                                Folio = op.Folio,
                                CodPed = orden.Cod,
                                CodCierre = cierre.Cod,
                                Pedido_Original = ordenEmbarque?.Cod,
                                Folio_Cierre_Copia = guidfolio
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            ordenPedido = new OrdenPedido()
                            {
                                Folio = ordenCierre?.Folio,
                                CodPed = orden.Cod,
                                CodCierre = cierre.Cod,
                                Pedido_Original = ordenEmbarque?.Cod,
                                Folio_Cierre_Copia = guidfolio
                            };

                            context.Add(ordenPedido);
                            await context.SaveChangesAsync();
                        }
                    }

                }

                ordenEmbarque.Chofer = null!;
                ordenEmbarque.Destino = null!;
                ordenEmbarque.Producto = null!;
                ordenEmbarque.Cliente = null!;
                ordenEmbarque.OrdenCierre = null!;
                ordenEmbarque.OrdenCompra = null!;
                ordenEmbarque.OrdenPedido = null!;
                ordenEmbarque.Estado = null!;
                ordenEmbarque.Transportista = null!;
                ordenEmbarque.Tonel = null!;
                ordenEmbarque.Tad = null!;

                ordenEmbarque.Codest = 14;
                context.Update(ordenEmbarque);

                await context.SaveChangesAsync();

                return Ok();
            }
            catch (DivideByZeroException)
            {
                return BadRequest("No se pueden divir valores entre 0");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("ordenes/despachadas/{bol:int}")]
        public ActionResult Obtener_Ordenes_Synhtesis_Por_BOL([FromRoute] int bol)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<Orden> ordenes = context.Orden.Where(x => x.BatchId == bol && x.Id_Tad == id_terminal)
                    .Include(x => x.OrdenEmbarque)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tonel)
                    .Include(x => x.Estado)
                    .Include(x => x.Redireccionamiento)
                    .ThenInclude(x => x.Destino)
                    .Include(x => x.Redireccionamiento)
                    .ThenInclude(x => x.Cliente)
                    .IgnoreAutoIncludes()
                    .ToList();

                ordenes.ForEach(x =>
                {
                    if (x.OrdenEmbarque is not null)
                        x.OrdenEmbarque.Pre = Obtener_Precio_Del_Dia_De_Orden_Synthesis(x.Cod).Precio;
                });

                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        private VolumenDisponibleDTO ObtenerVolumenDisponibleDeProducto(string Folio, byte? ID_Producto)
        {
            VolumenDisponibleDTO volumen = new();

            var cierres = context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio == Folio && x.CodPrd == ID_Producto && x.Estatus == true).Include(x => x.Producto).ToList() ?? new List<OrdenCierre>();
            foreach (var item in cierres)
            {
                var VolumenDisponible = item.Volumen;

                var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
                    && x.OrdenEmbarque.Codest == 22
                    && x.OrdenEmbarque.Folio != null
                    && x.OrdenEmbarque.Bolguidid != null
                && x.OrdenEmbarque.Orden == null)
                .Include(x => x.OrdenEmbarque)
                .ThenInclude(x => x.Tonel)
                .Include(x => x.OrdenEmbarque)
                .ThenInclude(x => x.Orden)
                .ThenInclude(x => x.Tonel).ToList();

                var VolumenCongelado = listConsumido.Sum(item => item.OrdenEmbarque!.Compartment == 1 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom!.ToString())
                                : item.OrdenEmbarque!.Compartment == 2 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom2!.ToString())
                                : item.OrdenEmbarque!.Compartment == 3 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom3!.ToString())
                                : item.OrdenEmbarque!.Compartment == 4 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom4!.ToString())
                                : item.OrdenEmbarque!.Vol);

                var countCongelado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Codprd == item.CodPrd
                && x.OrdenEmbarque.Codest == 22
                && x.OrdenEmbarque.Folio != null
            && x.OrdenEmbarque.Orden == null)
                .Include(x => x.OrdenEmbarque)
                .ThenInclude(x => x.Orden)
                .Count();

                var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                    && x.OrdenEmbarque.Orden.Codest != 14
                    && x.OrdenEmbarque.Codest != 14
                && x.OrdenEmbarque.Codprd == item.CodPrd
                && x.OrdenEmbarque.Orden.BatchId != null)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Orden)
                    .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                var countConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                    && x.OrdenEmbarque.Orden.Codest != 14
                    && x.OrdenEmbarque.Codest != 14
                    && x.OrdenEmbarque.Codprd == item.CodPrd
                    && x.OrdenEmbarque.Orden.BatchId != null)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Orden)
                    .Count();

                var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                    && x.OrdenEmbarque.Codprd == item.CodPrd
                    && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                    && x.OrdenEmbarque.FchOrd != null)
                    .Include(x => x.OrdenEmbarque)
                        .Sum(x => x.OrdenEmbarque!.Vol);

                var CountVolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                    && x.OrdenEmbarque.Codprd == item.CodPrd
                    && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                    && x.OrdenEmbarque.FchOrd != null)
                    .Include(x => x.OrdenEmbarque).Count();

                var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                && x.OrdenEmbarque.Codprd == item.CodPrd
                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                && x.OrdenEmbarque.FchOrd == null)
                    .Include(x => x.OrdenEmbarque)
                    .Sum(x => x.OrdenEmbarque!.Vol);

                var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);
                double? PromedioCargas = 0;
                var sumVolumen = VolumenConsumido + VolumenCongelado + VolumenProgramado;
                var sumCount = countCongelado + countConsumido + CountVolumenProgramado;

                if (sumVolumen != 0 && sumCount != 0)
                    PromedioCargas = sumVolumen / sumCount;

                ProductoVolumen productoVolumen = new();

                productoVolumen.Nombre += item.Producto?.Den;
                productoVolumen.Disponible += VolumenTotalDisponible;
                productoVolumen.Congelado += VolumenCongelado;
                productoVolumen.Consumido += VolumenConsumido;
                productoVolumen.Total += VolumenDisponible;
                productoVolumen.PromedioCarga += PromedioCargas;
                productoVolumen.Solicitud += VolumenSolicitado;
                productoVolumen.Programado += VolumenProgramado;

                volumen?.Productos?.Add(productoVolumen);


            }

            return volumen;
        }


        private PrecioBolDTO Obtener_Precio_Del_Dia_De_Orden_Synthesis(long? Id)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    throw new ArgumentException("Sin terminal");

                var orden = context.Orden.Where(x => x.Cod == Id && x.Id_Tad == id_terminal)
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
            catch (Exception)
            {
                return new PrecioBolDTO();
            }
        }
    }
}