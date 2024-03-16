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

namespace GComFuelManager.Server.Controllers.Cierres
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Consulta Precio Orden, Administrador Sistema, Revision Precios ,Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Comprador, Programador, Ejecutivo de Cuenta Operativo, Lectura de Cierre de Orden, Cierre Pedidos, Consulta Precios, Cliente Lectura")]
    public class CierreController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly VerifyUserId verifyUser;
        private readonly User_Terminal _terminal;

        public CierreController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserId verifyUser, User_Terminal _Terminal)
        {
            this.context = context;
            UserManager = userManager;
            this.verifyUser = verifyUser;
            _terminal = _Terminal;
        }

        public UserManager<IdentityUsuario> UserManager { get; }

        private async Task SaveErrors(Exception e, string accion)
        {
            context.Add(new Errors()
            {
                Error = JsonConvert.SerializeObject(new Error()
                {
                    Inner = JsonConvert.SerializeObject(e.InnerException),
                    Message = JsonConvert.SerializeObject(e.Message),
                    StackTrace = JsonConvert.SerializeObject(e.StackTrace)
                }),
                Accion = accion
            });
            await context.SaveChangesAsync();
        }

        //[HttpGet]
        //public async Task<ActionResult> Get()
        //{
        //    try
        //    {
        //        var ordenes = context.OrdenCierre.AsEnumerable();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        [HttpGet("{folio}")]
        public async Task<ActionResult> GetByFolio([FromRoute] string folio)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var ordenes = await context.OrdenCierre.Where(x => x.Folio == folio && x.Estatus == true && x.Id_Tad == id_terminal)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x!.Destino)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x!.Producto)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tad)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tonel)
                    .ToListAsync();
                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("get/{folio}")]
        public async Task<ActionResult> GetCierreByFolio([FromRoute] string folio)
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

                var cierres = await context.OrdenCierre.Where(x => x.Folio == folio && x.Id_Tad == id_terminal)
                    .Include(x => x.Destino)
                    .Include(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Grupo)
                    .Include(x => x.OrdenPedidos)
                    .ThenInclude(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Estado)
                    .ToListAsync();

                cierres.ForEach(x =>
                {
                    x.GetTieneVolumenDisponible(porcentaje);
                    x.SetCantidades();
                    x.SetCantidades();
                    x.GetCantidadSugerida();
                    x.Ordenes_Relacionadas = x.OrdenPedidos.Count;
                });

                return Ok(cierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{folio}/detalle")]
        public async Task<ActionResult> GetDetailByFolio([FromRoute] string folio)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> cierresVolumen = new();
                List<OrdenCierre> Ordenes_Adicionales_Modificadas = new();
                var ordenes = context.OrdenCierre.IgnoreAutoIncludes().Where(x => x.Folio == folio && x.Estatus == true && x.Id_Tad == id_terminal).ToList();

                if (ordenes.Count > 0)
                {

                    var pedidos = context.OrdenPedido.Where(x => x.Folio == folio && x.CodPed != null && x.OrdenEmbarque != null && x.OrdenCierre != null && x.OrdenCierre.Id_Tad == id_terminal)
                        .Include(x => x.OrdenEmbarque).IgnoreAutoIncludes()
                        .Include(x => x.OrdenCierre).IgnoreAutoIncludes()
                        .ToList();

                    foreach (var item1 in pedidos)
                    {
                        var pedido = context.OrdenCierre.IgnoreAutoIncludes().Where(x => x.CodPed == item1.CodPed && x.Id_Tad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .Include(x => x.Cliente)
                            .Include(x => x.Producto)
                            .Include(x => x.Destino)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tad)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Estado)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .ThenInclude(x => x.Estado)
                            .IgnoreAutoIncludes()
                            .DefaultIfEmpty()
                            .FirstOrDefault();

                        if (pedido != null)
                        {
                            cierresVolumen.Add(pedido);

                            if (context.Orden.Count(x => pedido.OrdenEmbarque != null && pedido.OrdenEmbarque.Orden != null && x.Ref == pedido.OrdenEmbarque.FolioSyn
                            && x.Codest != 14 && pedido.OrdenEmbarque.Codest != 14 && x.Id_Tad == id_terminal) > 1)
                            {
                                var Ordenes_Adicionales = context.Orden.Where(x => pedido.OrdenEmbarque != null && pedido.OrdenEmbarque.Orden != null && x.Ref == pedido.OrdenEmbarque.FolioSyn
                                && x.Cod != pedido.OrdenEmbarque.Orden.Cod && x.Id_Tad == id_terminal).ToList();

                                foreach (var oa in Ordenes_Adicionales)
                                {
                                    OrdenCierre ordenCierre = new();

                                    if (pedido?.OrdenEmbarque != null && pedido?.OrdenEmbarque.Orden != null)
                                    {
                                        ordenCierre.Folio = pedido.Folio;
                                        ordenCierre.FchCierre = pedido.FchCierre;
                                        ordenCierre.FchLlegada = pedido.FchLlegada;
                                        ordenCierre.Observaciones = pedido.Observaciones;
                                        ordenCierre.Precio = pedido.Precio;
                                        ordenCierre.Destino = new() { Den = pedido?.Destino?.Den };
                                        ordenCierre.Cliente = new() { Den = pedido?.Cliente?.Den };
                                        ordenCierre.Producto = new() { Den = pedido?.Producto?.Den };
                                        ordenCierre.OrdenEmbarque = new() { Folio = pedido?.OrdenEmbarque?.Folio };
                                        ordenCierre.OrdenEmbarque.Tonel = new()
                                        {
                                            Tracto = pedido?.OrdenEmbarque?.Tonel?.Tracto,
                                            Placa = pedido?.OrdenEmbarque?.Tonel?.Placa
                                        };
                                        ordenCierre.OrdenEmbarque.Orden = new()
                                        {
                                            BatchId = pedido?.OrdenEmbarque?.Orden?.BatchId,
                                            Fchcar = pedido?.OrdenEmbarque?.Orden?.Fchcar,
                                            Vol = oa.Vol,
                                            Vol2 = oa.Vol2,
                                            Liniteid = oa.Liniteid,
                                        };
                                        ordenCierre.OrdenEmbarque.Orden.Estado = new() { den = pedido?.OrdenEmbarque?.Orden?.Estado?.den };
                                        cierresVolumen.Add(ordenCierre);
                                    }
                                }
                            }
                        }
                    }

                    return Ok(cierresVolumen);
                }

                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{folio}/detalles")]
        public async Task<ActionResult> GetDetaislByFolios([FromRoute] string folio)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> cierresVolumen = new();
                var ordenes = context.OrdenCierre.Where(x => x.Folio == folio && x.Estatus == true && x.Id_Tad == id_terminal).ToList();

                if (ordenes.Count > 0)
                {

                    var pedidos = context.OrdenPedido.IgnoreAutoIncludes().Where(x => x.Folio == folio && x.CodPed != null && x.OrdenEmbarque != null && x.OrdenCierre != null && x.OrdenCierre.Id_Tad == id_terminal)
                        .Include(x => x.OrdenCierre).IgnoreAutoIncludes()
                        .Include(x => x.OrdenEmbarque).IgnoreAutoIncludes()
                        .ToList();

                    foreach (var item1 in pedidos)
                    {
                        var pedido = context.OrdenCierre.Where(x => x.CodPed == item1.CodPed)
                            .Include(x => x.OrdenEmbarque)
                            .Include(x => x.Cliente)
                            .Include(x => x.Producto)
                            .Include(x => x.Destino)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tad)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Estado)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .ThenInclude(x => x.Estado)
                            .DefaultIfEmpty()
                            .FirstOrDefault();

                        if (pedido != null)
                            cierresVolumen.Add(pedido);
                    }

                    return Ok(cierresVolumen);
                }

                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        public async Task<ActionResult> Post([FromBody] OrdenCierre orden)
        {
            try
            {
                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (orden is null)
                    return BadRequest("No se aceptan ordenes vacios");

                Cliente? Cliente = new();
                Grupo? Grupo = new();

                var consecutivo = context.Consecutivo.First(x => x.Nombre == "Folio");
                if (consecutivo is null)
                {
                    Consecutivo Nuevo_Consecutivo = new() { Numeracion = 1, Nombre = "Folio" };
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

                if (!orden.isGroup)
                {
                    Cliente = context.Cliente.FirstOrDefault(x => x.Cod == orden.CodCte);

                    orden.TipoVenta = Cliente?.Tipven;

                    if (!string.IsNullOrEmpty(Cliente?.Tipven))
                    {
                        orden.ModeloVenta = Cliente?.MdVenta;
                        orden.TipoVenta = Cliente?.Tipven;
                    }
                    else
                    {
                        orden.ModeloVenta = string.Empty;
                        orden.TipoVenta = string.Empty;
                    }
                }
                else
                {
                    Grupo = context.Grupo.FirstOrDefault(x => x.Cod == orden.CodGru);

                    orden.TipoVenta = Grupo?.Tipven;

                    if (!string.IsNullOrEmpty(Grupo?.Tipven))
                    {
                        orden.ModeloVenta = Grupo?.MdVenta;
                        orden.TipoVenta = Grupo?.Tipven;
                    }
                    else
                    {
                        orden.ModeloVenta = string.Empty;
                        orden.TipoVenta = string.Empty;
                    }
                }

                if (!orden.isGroup)
                    orden.Folio = $"P{DateTime.Now:yy}-{consecutivo.Numeracion:000000}{(Cliente is not null && !string.IsNullOrEmpty(Cliente.CodCte) ? $"-{Cliente.CodCte}" : "-DFT")}";
                else
                    orden.Folio = $"G{DateTime.Now:yy}-{consecutivo.Numeracion:000000}{(Grupo is not null && !string.IsNullOrEmpty(Grupo.CodGru) ? $"-{Grupo.CodGru}" : "-DFT")}";

                orden.OrdenEmbarque = null!;
                orden.Cliente = null!;
                orden.Producto = null!;
                orden.Destino = null!;

                orden.Id_Tad = id_terminal;
                orden.FchVencimiento = orden.FchCierre?.AddDays(5);
                context.Add(orden);

                await context.SaveChangesAsync(id, 1);

                var NewOrden = await context.OrdenCierre.Where(x => x.Cod == orden.Cod)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.Terminal)
                    .Include(x => x.Grupo)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Estado)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.OrdenPedidos)
                    .ThenInclude(x => x.OrdenEmbarque)
                    .FirstOrDefaultAsync();

                if (orden.PrecioOverDate)
                {
                    CierrePrecioDespuesFecha cierreprecio = new()
                    {
                        CodCie = orden.Cod,
                        CodCte = orden.CodCte,
                        CodPrd = orden.CodPrd,
                        Precio = orden.Precio
                    };
                    context.Add(cierreprecio);
                    await context.SaveChangesAsync();
                }

                if (NewOrden is not null)
                    NewOrden.SetVolumen();

                return Ok(NewOrden);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("update")]
        public async Task<ActionResult> PutOrden([FromBody] OrdenCierre orden)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                orden.Producto = null;
                orden.Destino = null;
                orden.Cliente = null;
                orden.ContactoN = null!;
                orden.OrdenEmbarque = null!;

                context.Update(orden);
                await context.SaveChangesAsync();

                var newOrden = context.OrdenCierre.Where(x => x.Cod == orden.Cod && x.Id_Tad == id_terminal)
                    .Include(x => x.OrdenEmbarque)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.Cliente)
                    .FirstOrDefault();

                return Ok(orden);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{cod:int}/cancel")]
        public async Task<ActionResult> CancelCierre([FromRoute] int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var orden = await context.OrdenCierre.FirstOrDefaultAsync(x => x.Cod == cod && x.Id_Tad == id_terminal);

                if (orden == null)
                    return NotFound();

                orden.Estatus = false;

                context.Update(orden);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("list")] //TODO: verificar uso
        public async Task<ActionResult> PostList(List<int> list)
        {
            try
            {
                List<OrdenCierre> ordenes = new();
                OrdenCierre? pedido = new();

                foreach (var item in list)
                {
                    pedido = await context.OrdenCierre
                    .Where(x => x.Cod == item)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.Cliente)
                    .Include(x => x.ContactoN)
                    .FirstOrDefaultAsync();
                    ordenes.Add(pedido!);
                }
                return Ok(ordenes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost("filtroMensualGrupo")]
        public async Task<ActionResult> FiltroMensualGrupo([FromBody] CierreFiltroDTO filtroDTO)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> cierresGroup = new();
                cierresGroup = await context.OrdenCierre.IgnoreAutoIncludes().Where(x => !string.IsNullOrEmpty(x.Folio) && x.CodGru == filtroDTO.codGru && x.Folio.StartsWith("G") && x.Estatus == true && x.Id_Tad == id_terminal)
                     .Include(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Destino)
                        .Include(x => x.ContactoN)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tonel)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Estado)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tad)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ToListAsync();
                return Ok(cierresGroup);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("filtrar")]
        public async Task<ActionResult> PostFilter([FromBody] CierreFiltroDTO filtroDTO)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> cierres = new();

                if (!filtroDTO.forFolio)
                {
                    cierres = await context.OrdenCierre.Where(x => x.CodCte == filtroDTO.codCte
                    && x.FchCierre >= filtroDTO.FchInicio && x.FchCierre <= filtroDTO.FchFin && x.Estatus == true && x.Id_Tad == id_terminal)
                        .Include(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Destino)
                        .Include(x => x.ContactoN)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tonel)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Estado)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tad)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ToListAsync();

                }
                else
                {
                    //Filtrado mediante el folio del cierre
                    if (!string.IsNullOrEmpty(filtroDTO.Folio))
                    {
                        cierres = await context.OrdenCierre.Where(x => x.Folio == filtroDTO.Folio && x.Estatus == true && x.Id_Tad == id_terminal)
                            .Include(x => x.Cliente)
                            .Include(x => x.Producto)
                            .Include(x => x.Destino)
                            .Include(x => x.ContactoN)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Estado)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tad)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .Include(x => x.OrdenPedidos)
                            .ThenInclude(x => x.OrdenEmbarque)
                            .ToListAsync();
                    }
                    else
                        return BadRequest("Debe escribir un folio valido.");
                }

                return Ok(cierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("Folio/{Referencia}")]
        public ActionResult GetPrecioByFolio([FromRoute] string referencia)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<PrecioBolDTO> precios = new();

                var ordenes = context.Orden.IgnoreAutoIncludes().Where(x => x.Ref == referencia && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Terminal)
                    .ToList();

                if (ordenes is null || ordenes.Count == 0)
                    return Ok(new List<PrecioBolDTO>() { new PrecioBolDTO() });

                foreach (var item in ordenes)
                {
                    PrecioBolDTO precio = new();

                    OrdenEmbarque? orden = new();
                    orden = context.OrdenEmbarque.IgnoreAutoIncludes().Where(x => x.FolioSyn == item.Ref && x.Codtad == id_terminal).Include(x => x.Producto).Include(x => x.Tad).Include(x => x.Destino).ThenInclude(x => x.Cliente).FirstOrDefault();

                    precio.Fecha_De_Carga = item.Fchcar;

                    precio.Referencia = item.Ref;
                    precio.Folio = item.Folio;
                    if (orden is not null)
                    {
                        if (orden.Producto is not null)
                            precio.Producto_Original = orden.Producto.Den;

                        if (orden.Destino is not null)
                        {
                            precio.Destino_Original = orden.Destino.Den;
                            if (orden.Destino.Cliente is not null)
                                if (!string.IsNullOrEmpty(orden.Destino.Cliente.Den))
                                    precio.Cliente_Original = orden.Destino.Cliente.Den;

                        }
                    }

                    precio.BOL = item.BatchId;
                    precio.Volumen_Cargado = item.Vol;
                    precio.Unidad_Negocio = item.Terminal.Den;
                    if (orden is not null)
                    {
                        if (orden.Destino is not null)
                            precio.Destino_Original = orden.Destino.Den ?? "";

                        if (orden.Producto is not null)
                            precio.Producto_Original = orden.Producto.Den ?? "";
                    }

                    var precioVig = context.Precio.Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.Id_Tad == id_terminal)
                        .OrderByDescending(x => x.FchDia)
                        .FirstOrDefault();

                    var precioPro = context.PrecioProgramado.Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.Id_Tad == id_terminal)
                        .OrderByDescending(x => x.FchDia)
                        .FirstOrDefault();

                    var precioHis = context.PreciosHistorico.Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd
                        && item.Fchcar != null && x.FchDia <= item.Fchcar.Value.Date && x.Id_Tad == id_terminal)
                        .OrderByDescending(x => x.FchDia)
                        .FirstOrDefault();

                    if (precioHis is not null)
                    {
                        precio.Precio = precioHis.pre;
                        precio.Fecha_De_Precio = precioHis.FchDia;
                        precio.Precio_Encontrado = true;
                        precio.Precio_Encontrado_En = "Historial";
                        //precio.Moneda = Enum.GetName(typeof(Moneda), precioHis.Moneda ?? Moneda.NONE) ?? string.Empty;
                        //precio.Tipo_De_Cambio = precioHis.Equibalencia ?? 1;
                    }

                    if (item != null && precioVig is not null && item.Fchcar is not null && item.Fchcar.Value.Date == DateTime.Today)
                    {
                        precio.Precio = precioVig.Pre;
                        precio.Fecha_De_Precio = precioVig.FchDia;
                        precio.Precio_Encontrado = true;
                        precio.Precio_Encontrado_En = "Vigente";
                        //precio.Moneda = Enum.GetName(typeof(Moneda), precioVig.Moneda ?? Moneda.NONE) ?? string.Empty;
                        //precio.Tipo_De_Cambio = precioVig.Equibalencia ?? 1;
                    }

                    if (item != null && precioPro is not null && item.Fchcar is not null && item.Fchcar.Value.Date == DateTime.Today && context.PrecioProgramado.Any())
                    {
                        precio.Precio = precioPro.Pre;
                        precio.Fecha_De_Precio = precioPro.FchDia;
                        precio.Precio_Encontrado = true;
                        precio.Precio_Encontrado_En = "Programado";
                        //precio.Moneda = Enum.GetName(typeof(Moneda), precioPro.Moneda ?? Moneda.NONE) ?? string.Empty;
                        //precio.Tipo_De_Cambio = precioPro.Equibalencia ?? 1;
                    }

                    if (orden != null && context.OrdenPedido.Any(x => x.CodPed == orden.Cod))
                    {
                        var ordenepedido = context.OrdenPedido.Where(x => x.CodPed == orden.Cod && !string.IsNullOrEmpty(x.Folio)).FirstOrDefault();

                        if (ordenepedido is not null)
                        {
                            var cierre = context.OrdenCierre.Where(x => x.Folio == ordenepedido.Folio
                             && x.CodPrd == orden.Codprd).FirstOrDefault();

                            if (cierre is not null)
                            {
                                precio.Precio = cierre.Precio;
                                precio.Fecha_De_Precio = cierre.fchPrecio;
                                precio.Es_Cierre = true;
                                precio.Precio_Encontrado = true;
                                precio.Precio_Encontrado_En = "Cierre";
                                precio.Folio_Cierre = cierre.Folio ?? "Orden Individual";
                                //precio.Moneda = Enum.GetName(typeof(Moneda), cierre.Moneda ?? Moneda.NONE) ?? string.Empty;
                                //precio.Tipo_De_Cambio = cierre.Equibalencia ?? 1;
                            }
                        }
                    }
                    if (item is not null && precioHis is null && precioPro is null && precioVig is null && !precio.Es_Cierre)
                    {
                        precio.Precio = item.OrdenEmbarque!.Pre;

                        if (item.OrdenCierre is not null)
                            precio.Fecha_De_Precio = item.OrdenCierre.fchPrecio;

                        precio.Es_Precio_De_Creacion = true;
                        precio.Precio_Encontrado_En = "Creacion";
                    }
                    precios.Add(precio);
                }

                return Ok(precios);

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Selección de grupo-cliente para obtener precio
        [HttpPost("fechasGroup")]
        public ActionResult GetPrecioByGroup([FromBody] CierreDiarioDTO fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<PrecioBolDTO> precios = new();
                List<Orden> ordenes_unificadas = new();

                //var ordenes = context.OrdenCierre.Where(x => x.CodGru == fechas.codGru && x.CodCte == fechas.codCte && x.FchCierre >= fechas.FchInicio && x.FchCierre <= fechas.FchFin && x.Folio.StartsWith("OP"));

                var ordenes = context.Orden.Where(x => x.Fchcar != null && x.Fchcar.Value.Date >= fechas.FchInicio.Date && x.Fchcar.Value.Date <= fechas.FchFin.Date && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .IgnoreAutoIncludes()
                    .ToList();

                if (ordenes is null || ordenes.Count == 0)
                    return Ok(new List<PrecioBolDTO>() { new PrecioBolDTO() });

                foreach (var item in ordenes)
                {
                    if (ordenes.Count(x => x.Ref == item.Ref && x.Bolguiid == item.Bolguiid && x.BatchId == item.BatchId && x.CompartmentId == item.CompartmentId) > 1)
                    {
                        if (!ordenes_unificadas.Any(x => x.Ref == item.Ref && x.Bolguiid == item.Bolguiid && x.BatchId == item.BatchId && x.CompartmentId == item.CompartmentId))

                            if (context.Orden.Count(x => x.Ref == item.Ref && x.Bolguiid == item.Bolguiid && x.BatchId == item.BatchId && x.CompartmentId == item.CompartmentId && x.Id_Tad == id_terminal) > 1)
                            {
                                var ordenesa_a_unificar = context.Orden.Where(x => x.Ref == item.Ref && x.Bolguiid == item.Bolguiid && x.BatchId == item.BatchId && x.CompartmentId == item.CompartmentId && x.Cod != item.Cod
                                && x.Id_Tad == id_terminal)
                                    .IgnoreAutoIncludes()
                                    .ToList();

                                foreach (var ou in ordenesa_a_unificar)
                                {
                                    item.Vol += ou.Vol;
                                }

                                ordenes_unificadas.Add(item);
                            }
                    }
                    else
                        ordenes_unificadas.Add(item);
                }

                foreach (var item in ordenes_unificadas)
                {
                    PrecioBolDTO precio = new();

                    OrdenEmbarque? orden = new();
                    orden = context.OrdenEmbarque.IgnoreAutoIncludes().Where(x => x.FolioSyn == item.Ref && x.Codtad == id_terminal)
                        .Include(x => x.Producto).Include(x => x.Destino).ThenInclude(x => x.Cliente).Include(x => x.OrdenCierre).IgnoreAutoIncludes().FirstOrDefault();

                    precio.Fecha_De_Carga = item.Fchcar;

                    precio.Referencia = item.Ref;
                    precio.BOL = item.BatchId;
                    precio.Volumen_Cargado = item.Vol;

                    if (orden is not null)
                    {
                        if (orden.Producto is not null)
                            precio.Producto_Original = orden.Producto.Den;

                        if (orden.Destino is not null)
                        {
                            precio.Destino_Original = orden.Destino.Den;
                            if (orden.Destino.Cliente is not null)
                                if (!string.IsNullOrEmpty(orden.Destino.Cliente.Den))
                                    precio.Cliente_Original = orden.Destino.Cliente.Den;

                        }
                    }

                    if (item is not null)
                    {
                        if (item.Destino is not null)
                        {
                            precio.Destino_Original = item.Destino.Den;
                            if (item.Destino.Cliente is not null)
                                if (!string.IsNullOrEmpty(item.Destino.Cliente.Den))
                                    precio.Cliente_Original = item.Destino.Cliente.Den;

                        }

                        if (item.Producto is not null)
                            precio.Producto_Original = item.Producto.Den ?? "";
                    }

                    var precioVig = context.Precio.Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.Id_Tad == id_terminal).OrderByDescending(x => x.FchActualizacion).FirstOrDefault();

                    var precioPro = context.PrecioProgramado.Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.Id_Tad == id_terminal).OrderByDescending(x => x.FchActualizacion).FirstOrDefault();

                    var precioHis = context.PreciosHistorico.Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd
                        && item.Fchcar != null && x.FchDia <= item.Fchcar.Value.Date && x.Id_Tad == id_terminal)
                        .OrderByDescending(x => x.FchDia)
                        .FirstOrDefault();

                    if (precioHis is not null)
                    {
                        precio.Precio = precioHis.pre;
                        precio.Fecha_De_Precio = precioHis.FchDia;
                        precio.Precio_Encontrado = true;
                        precio.Precio_Encontrado_En = "Historial";
                        precio.Moneda = precioHis?.Moneda?.Nombre ?? "MXN";
                        precio.Tipo_De_Cambio = precioHis?.Equibalencia ?? 1;
                    }

                    if (item != null && precioVig is not null && orden is null)
                    {
                        if (precioVig.FchDia == DateTime.Today)
                        {
                            precio.Precio = precioVig.Pre;
                            precio.Fecha_De_Precio = precioVig.FchDia;
                            precio.Precio_Encontrado = true;
                            precio.Precio_Encontrado_En = "Vigente";
                            precio.Moneda = precioHis?.Moneda?.Nombre ?? "MXN";
                            precio.Tipo_De_Cambio = precioHis?.Equibalencia ?? 1;
                        }
                    }

                    if (item != null && precioPro is not null && context.PrecioProgramado.Any())
                    {
                        if (precioPro.FchDia == DateTime.Today)
                        {
                            precio.Precio = precioPro.Pre;
                            precio.Fecha_De_Precio = precioPro.FchDia;
                            precio.Precio_Encontrado = true;
                            precio.Precio_Encontrado_En = "Programado";
                            precio.Moneda = precioHis?.Moneda?.Nombre ?? "MXN";
                            precio.Tipo_De_Cambio = precioHis?.Equibalencia ?? 1;
                        }
                    }

                    if (orden != null && context.OrdenPedido.Any(x => x.CodPed == orden.Cod))
                    {
                        var ordenepedido = context.OrdenPedido.Where(x => x.CodPed == orden.Cod && !string.IsNullOrEmpty(x.Folio)).FirstOrDefault();

                        if (ordenepedido is not null)
                        {
                            var cierre = context.OrdenCierre.Where(x => x.Folio == ordenepedido.Folio
                             && x.CodPrd == orden.Codprd).FirstOrDefault();

                            if (cierre is not null)
                            {
                                precio.Precio = cierre.Precio;
                                precio.Fecha_De_Precio = cierre.fchPrecio;
                                precio.Es_Cierre = true;
                                precio.Precio_Encontrado = true;
                                precio.Precio_Encontrado_En = "Cierre";
                                precio.Moneda = precioHis?.Moneda?.Nombre ?? "MXN";
                                precio.Tipo_De_Cambio = precioHis?.Equibalencia ?? 1;
                            }
                        }
                    }

                    if (orden is not null && precioHis is null && precioPro is null && precioVig is null && !precio.Es_Cierre)
                    {
                        precio.Precio = orden.Pre;

                        if (orden.OrdenCierre is not null)
                            precio.Fecha_De_Precio = orden.OrdenCierre.fchPrecio;

                        precio.Es_Precio_De_Creacion = true;
                        precio.Precio_Encontrado_En = "Creacion";
                    }

                    precios.Add(precio);
                }

                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("filtrar/activo")]
        public async Task<ActionResult> PostFilterActivos([FromBody] CierreFiltroDTO filtroDTO)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> cierres = new();

                if (!filtroDTO.forFolio)
                {
                    cierres = await context.OrdenCierre.Where(x => x.CodCte == filtroDTO.codCte
                    && x.FchCierre >= filtroDTO.FchInicio && x.FchCierre <= filtroDTO.FchFin && x.Estatus == true && x.Activa == true && x.Id_Tad == id_terminal)
                        .Include(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Destino)
                        .Include(x => x.ContactoN)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tonel)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Estado)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tad)
                        .ToListAsync();

                }
                else
                {
                    if (!string.IsNullOrEmpty(filtroDTO.Folio))
                    {
                        cierres = await context.OrdenCierre.Where(x => x.Folio == filtroDTO.Folio && x.Estatus == true && x.Activa == true && x.Id_Tad == id_terminal)
                            .Include(x => x.Cliente)
                            .Include(x => x.Producto)
                            .Include(x => x.Destino)
                            .Include(x => x.ContactoN)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Estado)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tad)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .ToListAsync();
                    }
                    else
                        return BadRequest("Debe escribir un folio valido.");
                }

                return Ok(cierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("years")] //TODO: verificar uso
        public async Task<ActionResult> GetYears()
        {
            try
            {
                var years = context.OrdenCierre.Select(x => x.FchCierre!.Value.Year).Distinct().ToList();
                return Ok(years);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("asignacion/filtrar")]
        public async Task<ActionResult> GetDate([FromBody] FechasF fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var ordens = await context.OrdenCierre
                    .Where(x => x.OrdenEmbarque!.Fchcar >= fechas.DateInicio && x.OrdenEmbarque!.Fchcar <= fechas.DateFin && x.OrdenEmbarque!.Codest == 3 && x.OrdenEmbarque!.FchOrd != null
                    && x.OrdenEmbarque!.Bolguidid == null && x.Id_Tad == id_terminal)
                    .Include(x => x.OrdenEmbarque!.Chofer)
                    .Include(x => x.OrdenEmbarque!.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.OrdenEmbarque!.Estado)
                    .Include(x => x.OrdenEmbarque!.OrdenCompra)
                    .Include(x => x.OrdenEmbarque!.Tad)
                    .Include(x => x.OrdenEmbarque!.Producto)
                    .Include(x => x.OrdenEmbarque!.Tonel)
                    .ThenInclude(x => x.Transportista)
                    .OrderBy(x => x.OrdenEmbarque!.Fchpet)
                    .Take(1000)
                    .ToListAsync();

                return Ok(ordens);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("vencimiento")]
        public async Task<ActionResult> PostVencimieto([FromBody] CierreFiltroDTO fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> ordens = new();

                ordens = context.OrdenCierre.Where(x => x.Folio == fechas.Folio && x.Id_Tad == id_terminal).ToList();

                if (ordens is null)
                    return BadRequest();

                ordens.ForEach(x =>
                {
                    x.FchVencimiento = fechas.FchFin;
                    if (x.FchVencimiento >= DateTime.Today)
                        x.Activa = true;
                });

                context.UpdateRange(ordens);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 5);

                var ordenes = await context.OrdenCierre.Where(x => x.Folio == fechas.Folio && x.Estatus == true && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x!.Destino)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x!.Producto)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tad)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tonel)
                    .ToListAsync();

                return Ok(ordens);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("volumen")] //TODO: checar utilidad
        public async Task<ActionResult<VolumenDisponibleDTO>> GetListadoVolumen([FromBody] CierreFiltroDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                VolumenDisponibleDTO volumen = new();

                if (!filtro.forFolio)
                {
                    var cierres = await context.OrdenCierre.Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && x.Estatus == true && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal)
                        .Include(x => x.Producto).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Tonel).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Orden).ToListAsync();

                    if (cierres is null)
                        return new VolumenDisponibleDTO();

                    cierres = cierres.DistinctBy(x => x.Cod).ToList();

                    foreach (var item in cierres.DistinctBy(x => x.Cod))
                    {
                        if (context.OrdenPedido.Any(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio)))
                        {

                            var VolumenDisponible = item.Volumen;

                            var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Codest == 22
                            && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Bolguidid != null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel).ToList();

                            var VolumenCongelado = listConsumido.Sum(item => item.OrdenEmbarque!.Compartment == 1 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 2 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom2!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 3 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom3!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 4 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom4!.ToString())
                                            : item.OrdenEmbarque!.Vol);

                            var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null
                            && x.OrdenEmbarque.Orden.Id_Tad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                            var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == item.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null
                                && x.OrdenEmbarque.Codtad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();

                            productoVolumen.Nombre = item.Producto?.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            if (volumen.Productos.Any(x => x.Nombre.Equals(item.Producto.Den)))
                            {

                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Disponible += productoVolumen.Disponible;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Congelado += productoVolumen.Congelado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Consumido += productoVolumen.Consumido;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Total += productoVolumen.Total;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Programado += productoVolumen.Programado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Solicitud += productoVolumen.Solicitud;
                            }
                            else
                                volumen.Productos.Add(productoVolumen);
                        }
                        else
                        {

                            var VolumenDisponible = item.Volumen;

                            var VolumenCongelado = item.OrdenEmbarque?.Folio is not null && item.OrdenEmbarque?.Orden is null ? item?.OrdenEmbarque?.Compartment == 1
                                && item.OrdenEmbarque?.Codest == 22
                                && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom?.ToString())
                                            : item?.OrdenEmbarque?.Compartment == 2 && item.OrdenEmbarque?.Tonel is not null
                                            && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom2?.ToString())
                                            : item?.OrdenEmbarque?.Compartment == 3 && item.OrdenEmbarque?.Tonel is not null
                                            && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom3?.ToString())
                                            : item?.OrdenEmbarque?.Compartment == 4 && item.OrdenEmbarque?.Tonel is not null
                                            && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom4?.ToString())
                                            : item?.OrdenEmbarque?.Vol : 0;

                            var VolumenConsumido = item.OrdenEmbarque?.Folio is not null && item.OrdenEmbarque?.Orden?.BatchId is not null
                            && item.OrdenEmbarque?.Orden?.Codest != 14 && item.OrdenEmbarque?.Codest != 14
                            ? item.OrdenEmbarque?.Orden?.Vol : 0;

                            var VolumenProgramado = item?.OrdenEmbarque?.Codest == 3 && item.OrdenEmbarque.FchOrd != null &&
                                item.OrdenEmbarque.Folio == null && item.OrdenEmbarque.Bolguidid == null ? item.OrdenEmbarque.Vol : 0;

                            var VolumenSolicitado = item?.OrdenEmbarque?.Codest == 9 && item.OrdenEmbarque.FchOrd == null &&
                                item.OrdenEmbarque.Folio == null && item.OrdenEmbarque.Bolguidid == null ? item.OrdenEmbarque.Vol : 0;

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();
                            productoVolumen.Nombre = item.Producto!.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            if (volumen.Productos.Any(x => x.Nombre.Equals(item.Producto.Den)))
                            {
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Disponible += productoVolumen.Disponible;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Congelado += productoVolumen.Congelado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Consumido += productoVolumen.Consumido;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Total += productoVolumen.Total;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Programado += productoVolumen.Programado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Solicitud += productoVolumen.Solicitud;
                            }
                            else
                                volumen.Productos.Add(productoVolumen);

                        }
                    }
                    return Ok(volumen);
                }
                else
                {
                    if (context.OrdenPedido.Any(x => !string.IsNullOrEmpty(x.Folio) && x.Folio == filtro.Folio))
                    {
                        var cierres = await context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio == filtro.Folio).Include(x => x.Producto).ToListAsync();
                        if (cierres is null)
                            return BadRequest("No existe el cierre.");

                        cierres = cierres.DistinctBy(x => x.Cod).ToList();

                        foreach (var item in cierres.DistinctBy(x => x.Cod))
                        {
                            var VolumenDisponible = item.Volumen;

                            var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
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

                            var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                            var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == item.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();

                            productoVolumen.Nombre = item.Producto?.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            volumen.Productos.Add(productoVolumen);
                        }
                        return Ok(volumen);

                    }
                    else
                    {
                        var ordenes = context.OrdenCierre.Where(x => x.Folio == filtro.Folio)
                            .Include(x => x.Producto)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x!.Orden)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x!.Tonel).ToList();

                        foreach (var item in ordenes.DistinctBy(x => x.Producto?.Den))
                        {
                            var VolumenDisponible = ordenes.Where(x => x.CodPrd == item.CodPrd && x.Estatus is true).Sum(x => x.Volumen);

                            var VolumenCongelado = ordenes.Where(x => x.CodPrd == item.CodPrd
                            && x.Estatus is true && item.OrdenEmbarque?.Codest == 22
                            && x?.OrdenEmbarque?.Folio is not null
                            && x?.OrdenEmbarque?.Orden is null).Sum(item =>
                            item?.OrdenEmbarque?.Compartment == 1 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom?.ToString() ?? "0")
                                            : item?.OrdenEmbarque?.Compartment == 2 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom2?.ToString() ?? "0")
                                            : item?.OrdenEmbarque?.Compartment == 3 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom3?.ToString() ?? "0")
                                            : item?.OrdenEmbarque?.Compartment == 4 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom4?.ToString() ?? "0")
                                            : item?.OrdenEmbarque?.Vol);

                            var VolumenConsumido = ordenes.Where(x => x.CodPrd == item.CodPrd
                            && x.OrdenEmbarque?.Orden?.Codest != 14 && item.OrdenEmbarque?.Codest != 14
                            && x.Estatus is true
                            && x?.OrdenEmbarque?.Folio is not null
                            && x?.OrdenEmbarque?.Orden?.BatchId is not null).Sum(x => x.OrdenEmbarque?.Orden?.Vol);

                            var VolumenProgramado = ordenes.Where(x => x.OrdenEmbarque?.Folio is null
                            && x.OrdenEmbarque?.Codprd == item.CodPrd
                            && x.OrdenEmbarque?.Bolguidid is null && x.OrdenEmbarque?.Codest == 3
                            && x.OrdenEmbarque.FchOrd is not null)
                                .Sum(x => x.OrdenEmbarque?.Vol);

                            var VolumenSolicitado = ordenes.Where(x => x.OrdenEmbarque?.Folio is null
                            && x.OrdenEmbarque?.Codprd == item.CodPrd
                            && x.OrdenEmbarque?.Bolguidid is null && x.OrdenEmbarque?.Codest == 9
                            && x.OrdenEmbarque.FchOrd is null)
                                .Sum(x => x.OrdenEmbarque?.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();
                            productoVolumen.Nombre = item.Producto?.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            volumen.Productos.Add(productoVolumen);
                        }
                        return Ok(volumen);
                    }
                }
                return Ok(volumen);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
                throw;
            }
        }

        [HttpPost("volumen/mes")] //TDODO: checar utilidad
        public async Task<ActionResult<VolumenDisponibleDTO>> GetListadoVolumenMes([FromBody] CierreFiltroDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                VolumenDisponibleDTO volumen = new();

                var cierres = await context.OrdenCierre.IgnoreAutoIncludes().Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                && x.Estatus == true && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Tonel).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Orden).ToListAsync();

                if (cierres is null)
                    return new VolumenDisponibleDTO();

                foreach (var item in cierres.DistinctBy(x => x.Cod))
                {
                    var VolumenDisponible = item.CodPed == 0 ? item.Volumen : 0;

                    var VolumenCongelado = item?.OrdenEmbarque?.Folio is not null && item.OrdenEmbarque?.Orden is null
                        && item.OrdenEmbarque?.Codest == 22 ?
                                      item?.OrdenEmbarque?.Compartment == 1 && item.OrdenEmbarque?.Tonel is not null
                                    && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom?.ToString() ?? "0")
                                    : item?.OrdenEmbarque?.Compartment == 2 && item.OrdenEmbarque?.Tonel is not null
                                    && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom2?.ToString() ?? "0")
                                    : item?.OrdenEmbarque?.Compartment == 3 && item.OrdenEmbarque?.Tonel is not null
                                    && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom3?.ToString() ?? "0")
                                    : item?.OrdenEmbarque?.Compartment == 4 && item.OrdenEmbarque?.Tonel is not null
                                    && item.OrdenEmbarque?.Codest == 22 ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom4?.ToString() ?? "0")
                                    : item?.OrdenEmbarque?.Vol : 0;

                    var VolumenConsumido = item?.OrdenEmbarque?.Folio is not null && item.OrdenEmbarque?.Orden?.BatchId is not null
                    && item.OrdenEmbarque?.Codest != 14
                    && item.OrdenEmbarque?.Orden?.Codest != 14
                    ? item.OrdenEmbarque?.Orden?.Vol : 0;

                    var VolumenProgramado = item?.OrdenEmbarque?.Codest == 3 && item.OrdenEmbarque.FchOrd != null &&
                        item.OrdenEmbarque.Folio == null && item.OrdenEmbarque.Bolguidid == null ? item.OrdenEmbarque.Vol : 0;

                    var VolumenSolicitado = item?.OrdenEmbarque?.Codest == 9 && item.OrdenEmbarque.FchOrd == null &&
                        item.OrdenEmbarque.Folio == null && item.OrdenEmbarque.Bolguidid == null ? item.OrdenEmbarque.Vol : 0;

                    var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                    ProductoVolumen productoVolumen = new();
                    productoVolumen.Nombre = item?.Producto!.Den;
                    productoVolumen.Disponible = VolumenTotalDisponible;
                    productoVolumen.Congelado = VolumenCongelado;
                    productoVolumen.Consumido = VolumenConsumido;
                    productoVolumen.Total = VolumenDisponible;
                    productoVolumen.Solicitud = VolumenSolicitado;
                    productoVolumen.Programado = VolumenProgramado;

                    if (volumen.Productos.Any(x => !string.IsNullOrEmpty(x.Nombre) && item.Producto != null && x.Nombre.Equals(item.Producto.Den)))
                    {
                        volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Disponible += productoVolumen.Disponible;
                        volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Congelado += productoVolumen.Congelado;
                        volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Consumido += productoVolumen.Consumido;
                        volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Total += productoVolumen.Total;
                        volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Programado += productoVolumen.Programado;
                        volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Solicitud += productoVolumen.Solicitud;
                    }
                    else
                        volumen.Productos.Add(productoVolumen);
                }
                return Ok(volumen);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
                throw;
            }
        }

        [HttpPost("volumen/activo")]
        public async Task<ActionResult<VolumenDisponibleDTO>> GetListadoVolumenActiva([FromBody] CierreFiltroDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                VolumenDisponibleDTO volumen = new();

                if (!filtro.forFolio)
                {
                    var cierres = await context.OrdenCierre.Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && x.Estatus == true && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal)
                        .Include(x => x.Producto).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Tonel).Include(x => x.OrdenEmbarque).ThenInclude(x => x.Orden).ToListAsync();

                    if (cierres is null)
                        return new VolumenDisponibleDTO();

                    foreach (var item in cierres.DistinctBy(x => x.Cod))
                    {
                        if (context.OrdenPedido.Any(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio)))
                        {

                            var VolumenDisponible = item.Volumen;

                            var listConsumido = context.OrdenPedido.Where(x => x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Codest == 22
                            && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Bolguidid != null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel).ToList();

                            var VolumenCongelado = listConsumido.Sum(item => item.OrdenEmbarque!.Compartment == 1 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 2 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom2!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 3 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom3!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 4 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom4!.ToString())
                                            : item.OrdenEmbarque!.Vol);

                            var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null
                            && x.OrdenEmbarque.Orden.Id_Tad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                            var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == item.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null
                                && x.OrdenEmbarque.Codtad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();

                            productoVolumen.Nombre = item.Producto?.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            if (volumen.Productos.Any(x => !string.IsNullOrEmpty(x.Nombre) && item.Producto != null && x.Nombre.Equals(item.Producto.Den)))
                            {

                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Disponible += productoVolumen.Disponible;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Congelado += productoVolumen.Congelado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Consumido += productoVolumen.Consumido;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Total += productoVolumen.Total;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Programado += productoVolumen.Programado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Solicitud += productoVolumen.Solicitud;
                            }
                            else
                                volumen.Productos.Add(productoVolumen);
                        }
                        else
                        {

                            var VolumenDisponible = item.Volumen;

                            var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Codest == 22
                            && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Bolguidid != null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel).ToList();

                            var VolumenCongelado = listConsumido.Sum(item => item.OrdenEmbarque!.Compartment == 1 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 2 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom2!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 3 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom3!.ToString())
                                            : item.OrdenEmbarque!.Compartment == 4 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom4!.ToString())
                                            : item.OrdenEmbarque!.Vol);

                            var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null
                            && x.OrdenEmbarque.Orden.Id_Tad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                            var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == item.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null
                                && x.OrdenEmbarque.Codtad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();
                            productoVolumen.Nombre = item.Producto!.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            if (volumen.Productos.Any(x => !string.IsNullOrEmpty(x.Nombre) && x.Nombre.Equals(item.Producto.Den)))
                            {
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Disponible += productoVolumen.Disponible;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Congelado += productoVolumen.Congelado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Consumido += productoVolumen.Consumido;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Total += productoVolumen.Total;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Programado += productoVolumen.Programado;
                                volumen.Productos.FirstOrDefault(x => x.Nombre!.Equals(item.Producto!.Den))!.Solicitud += productoVolumen.Solicitud;
                            }
                            else
                                volumen.Productos.Add(productoVolumen);

                        }
                    }
                    return Ok(volumen);
                }
                else
                {
                    if (context.OrdenPedido.Any(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(filtro.Folio)))
                    {
                        var cierres = await context.OrdenCierre.Where(x => x.Folio!.Equals(filtro.Folio) && x.Activa == true).Include(x => x.Producto).ToListAsync();
                        if (cierres is null)
                            return BadRequest("No existe el cierre.");

                        foreach (var item in cierres)
                        {
                            var VolumenDisponible = item.Volumen;

                            var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
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

                            var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                            var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == item.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();

                            productoVolumen.Nombre = item.Producto?.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            volumen.Productos.Add(productoVolumen);
                        }
                        return Ok(volumen);

                    }
                    else
                    {
                        var ordenes = context.OrdenCierre.Where(x => x.Folio == filtro.Folio)
                            .Include(x => x.Producto)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x!.Orden)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x!.Tonel).ToList();

                        ordenes = ordenes.DistinctBy(x => x.Cod).ToList();

                        foreach (var item in ordenes.DistinctBy(x => x.Producto?.Den))
                        {
                            var VolumenDisponible = ordenes.Where(x => x.CodPrd == item.CodPrd && x.Estatus is true).Sum(x => x.Volumen);

                            var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
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

                            var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                                && x.OrdenEmbarque.Orden.Codest != 14
                                && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null)
                                .Include(x => x.OrdenEmbarque)
                                .ThenInclude(x => x.Orden)
                                .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                            var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                                && x.OrdenEmbarque.Codprd == item.CodPrd
                                && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                                && x.OrdenEmbarque.FchOrd != null)
                                .Include(x => x.OrdenEmbarque)
                                    .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                            && x.OrdenEmbarque.FchOrd == null)
                                .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                            var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);

                            ProductoVolumen productoVolumen = new();
                            productoVolumen.Nombre = item.Producto?.Den;
                            productoVolumen.Disponible = VolumenTotalDisponible;
                            productoVolumen.Congelado = VolumenCongelado;
                            productoVolumen.Consumido = VolumenConsumido;
                            productoVolumen.Total = VolumenDisponible;
                            productoVolumen.Solicitud = VolumenSolicitado;
                            productoVolumen.Programado = VolumenProgramado;

                            volumen.Productos.Add(productoVolumen);
                        }
                        return Ok(volumen);
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
                throw;
            }
        }

        [HttpPost("activo")] //TODO: checar utilidad
        public async Task<ActionResult> CheckCierreActivo([FromBody] OrdenCierre orden)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var producto = context.Producto.Find(orden.CodPrd);
                var ordens = await context.OrdenCierre.Where(x => x.CodCte == orden.CodCte && x.Activa == true && x.Estatus == true && x.Id_Tad == id_terminal).OrderByDescending(x => x.FchCierre).Take(100)
                    .Include(x => x.Producto)
                    .ToListAsync();

                foreach (var item in ordens)
                {
                    if (item.Activa is true && producto?.Cod == item.CodPrd && item.CodPed == 0)
                    {
                        return BadRequest($"El producto {item.Producto?.Den} aun cuenta con un cierre activo. Folio: {item.Folio}");
                    }

                    if (item.Activa is true && producto?.Cod == item.CodPrd)
                    {
                        return BadRequest($"El producto {item.Producto?.Den} aun cuenta con ordenes disponibles. Folio: {item.Folio}");
                    }
                }

                return Ok(true);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("cerrar/pedido/{folio}")]
        public async Task<ActionResult> ClosePedido([FromRoute] string folio)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (string.IsNullOrEmpty(folio))
                    return BadRequest("Folio vacio o no valido");

                var ordens = context.OrdenCierre.Where(x => x.Folio.Equals(folio) && x.Id_Tad == id_terminal).ToList();

                foreach (var item in ordens)
                {
                    await CloseOrden(item.Cod);
                }

                return Ok(true);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("cerrar/orden/{cod:int}")]
        public async Task<ActionResult> CloseOrden([FromRoute] int cod)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (cod == 0)
                    return BadRequest("Orden no valida");

                var orden = context.OrdenCierre.FirstOrDefault(x => x.Cod == cod && x.Id_Tad == id_terminal);

                if (orden is null)
                    return BadRequest("No se encontro la orden");

                orden.Activa = false;
                orden.Estatus = false;

                context.Update(orden);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 16);

                return Ok(true);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("folios")]
        public async Task<ActionResult> GetFoliosValidos()
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<string?> folios = new();

                folios = context.OrdenCierre.Where(x => x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                && !string.IsNullOrEmpty(x.Folio) && x.Activa == true && x.Id_Tad == id_terminal)
                    .Select(x => x.Folio)
                    .Distinct()
                    .ToList();

                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("folios/detalle")] // TODO: checar utilidad
        public async Task<ActionResult> GetFoliosValidosActivos([FromBody] CierreFiltroDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioDetalleDTO> folios = new();
                //Filtro de órdenes mediante la obtención del folio
                if (!filtro.forFolio)
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre).Where(
                x => x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                && !string.IsNullOrEmpty(x.Folio)
                && x.Activa == true
                && x.CodPed == 0
                && x.Estatus == true
                && x.Id_Tad == id_terminal ||
                x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                && !string.IsNullOrEmpty(x.Folio)
                && x.Activa == true
                && x.Folio.StartsWith("OP")
                && x.Estatus == true
                && x.Id_Tad == id_terminal)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.Grupo)
                    .Include(x => x.OrdenEmbarque)
                    .Select(x => new FolioDetalleDTO()
                    {
                        Folio = x.Folio,
                        Grupo = x.Grupo,
                        Cliente = x.Cliente,
                        Destino = x.Destino,
                        Producto = x.Producto,
                        Precio = x.Precio,
                        ordenEmbarque = x.OrdenEmbarque,
                        FchCierre = x.FchCierre,
                        Comentarios = x.Observaciones
                    })
                    .OrderByDescending(x => x.FchCierre)
                    .ToListAsync();
                }
                //Filtro de órdenes obteniendo el cliente con su grupo empresarial
                else
                {
                    if (filtro.codCte != null && filtro.codGru != null)
                        folios = await context.OrdenCierre.OrderBy(x => x.FchCierre).Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.Activa == true && x.CodPed == 0 && x.Estatus == true && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte
                    && x.Id_Tad == id_terminal ||
                      //x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                      x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio)
                    && x.Activa == true
                    && x.Folio.StartsWith("OP")
                    && x.Estatus == true && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte
                    && x.Id_Tad == id_terminal)
                        .Include(x => x.Cliente)
                        .Include(x => x.Destino)
                        .Include(x => x.Producto)
                         .Include(x => x.Grupo)
                         .Include(x => x.OrdenEmbarque)
                        .Select(x => new FolioDetalleDTO()
                        {
                            Folio = x.Folio,
                            Grupo = x.Grupo,
                            Cliente = x.Cliente,
                            Destino = x.Destino,
                            Producto = x.Producto,
                            ordenEmbarque = x.OrdenEmbarque,
                            Precio = x.Precio,
                            FchCierre = x.FchCierre,
                            Comentarios = x.Observaciones
                        })
                    .OrderByDescending(x => x.FchCierre)
                        .ToListAsync();

                    //Filtro de órdenes solamente obteniendo el grupo
                    else if (filtro.codGru != null)
                    {
                        folios = await context.OrdenCierre.OrderByDescending(x => x.FchCierre).Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.CodPed == 0 && x.Estatus == true && x.CodGru == filtro.codGru && x.Id_Tad == id_terminal ||
                    //  x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                    x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio)
                    //&& x.Activa == true
                    && x.Folio.StartsWith("OP")
                    && x.Estatus == true && x.CodGru == filtro.codGru && x.Id_Tad == id_terminal)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                     .Include(x => x.Grupo)
                     .Include(x => x.OrdenEmbarque)
                    .Select(x => new FolioDetalleDTO()
                    {
                        Folio = x.Folio,
                        Grupo = x.Grupo,
                        Cliente = x.Cliente,
                        Destino = x.Destino,
                        Producto = x.Producto,
                        Precio = x.Precio,
                        ordenEmbarque = x.OrdenEmbarque,
                        FchCierre = x.FchCierre,
                        Comentarios = x.Observaciones
                    })
                    .OrderByDescending(x => x.FchCierre)
                    .ToListAsync();
                    }
                    //Filtro de órdenes por rango de fechas
                    //  x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                    else
                    {
                        folios = await context.OrdenCierre.OrderBy(x => x.FchCierre).Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.Activa == true && x.CodPed == 0 && x.Estatus == true && x.Id_Tad == id_terminal
                    ||
                    x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio)
                    && x.Activa == true
                    && x.Folio.StartsWith("OP")
                    && x.Estatus == true
                    && x.Id_Tad == id_terminal)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                     .Include(x => x.Grupo)
                     .Include(x => x.OrdenEmbarque)
                    .Select(x => new FolioDetalleDTO()
                    {
                        Folio = x.Folio,
                        Grupo = x.Grupo,
                        Cliente = x.Cliente,
                        Destino = x.Destino,
                        Producto = x.Producto,
                        Precio = x.Precio,
                        FchCierre = x.FchCierre,
                        ordenEmbarque = x.OrdenEmbarque,
                        Comentarios = x.Observaciones
                    })
                    .OrderByDescending(x => x.FchCierre)
                    .ToListAsync();
                    }

                }
                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("folios/detalle/status")] // TODO: checar utilidad
        public async Task<ActionResult> GetFoliosValidosActivosStatus([FromBody] CierreFiltroDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioDetalleDTO> folios = new();
                //Cuando se sabe el folio de la orden
                if (!filtro.forFolio)
                    folios = await context.OrdenCierre.Where(
                x => x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                && !string.IsNullOrEmpty(x.Folio)
                && x.Activa == true
                && x.CodPed == 0
                && x.Estatus == true && x.Id_Tad == id_terminal ||
                x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                && !string.IsNullOrEmpty(x.Folio)
                && x.Activa == true
                && x.Folio.StartsWith("OP")
                && x.Estatus == true && x.Id_Tad == id_terminal)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                     .Include(x => x.Grupo)
                     .Include(x => x.OrdenEmbarque)
                    .Select(x => new FolioDetalleDTO()
                    {
                        Folio = x.Folio,
                        Grupo = x.Grupo,
                        Cliente = x.Cliente,
                        Destino = x.Destino,
                        Producto = x.Producto,
                        Precio = x.Precio,
                        FchCierre = x.FchCierre,
                        ordenEmbarque = x.OrdenEmbarque,
                        Comentarios = x.Observaciones
                    })
                    .OrderByDescending(x => x.FchCierre)
                    .ToListAsync();
                else
                {
                    //Cuando se filtra por grupo empresarial y cliente
                    if (filtro.codCte != null && filtro.codGru != null)
                        folios = await context.OrdenCierre.Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.CodPed == 0 && x.Estatus == true && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal ||
                      //x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                      x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio)
                    && x.Folio.StartsWith("OP")
                    && x.Estatus == true && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal)
                        .Include(x => x.Cliente)
                         .Include(x => x.Grupo)
                        .Include(x => x.Destino)
                        .Include(x => x.Producto)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ThenInclude(x => x.Estado)
                        .Select(x => new FolioDetalleDTO()
                        {
                            Folio = x.Folio,
                            Grupo = x.Grupo,
                            Cliente = x.Cliente,
                            Destino = x.Destino,
                            Producto = x.Producto,
                            Precio = x.Precio,
                            ordenEmbarque = x.OrdenEmbarque,
                            FchCierre = x.FchCierre,
                            Comentarios = x.Observaciones,
                            Estado = x.OrdenEmbarque.Estado.den,
                            Activa = x.Activa
                        })
                    .OrderByDescending(x => x.FchCierre)
                        .ToListAsync();


                    //Cuando se filtra solo por su grupo  && x.Estatus == true  
                    else if (filtro.codGru != null)
                        folios = await context.OrdenCierre.Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.CodPed == 0 && x.CodGru == filtro.codGru && x.Estatus == true && x.Id_Tad == id_terminal ||
                      //x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                      x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio)
                   && x.Folio.StartsWith("OP")
                   && x.Estatus == true
                    && x.CodGru == filtro.codGru && x.Id_Tad == id_terminal)
                    .Include(x => x.Cliente)
                     .Include(x => x.Grupo)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Orden)
                    .ThenInclude(x => x.Estado)
                    .Select(x => new FolioDetalleDTO()
                    {
                        Folio = x.Folio,
                        Grupo = x.Grupo,
                        Cliente = x.Cliente,
                        Destino = x.Destino,
                        Producto = x.Producto,
                        ordenEmbarque = x.OrdenEmbarque,
                        FchCierre = x.FchCierre,
                        Precio = x.Precio,
                        Comentarios = x.Observaciones,
                        Estado = x.OrdenEmbarque.Estado.den,
                        Activa = x.Activa
                    })
                    .OrderByDescending(x => x.FchCierre)
                    .ToListAsync();
                    else
                        //Solamente las fechas
                        folios = await context.OrdenCierre.Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.CodPed == 0 && x.Estatus == true && x.Id_Tad == id_terminal ||
                    //x.FchCierre >= DateTime.Today.AddDays(-10) && x.FchCierre <= DateTime.Today.AddDays(1)
                    x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin
                    && !string.IsNullOrEmpty(x.Folio)
                    && x.Folio.StartsWith("OP")
                    && x.Estatus == true && x.Id_Tad == id_terminal)
                    .Include(x => x.Cliente)
                     .Include(x => x.Grupo)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Orden)
                    .ThenInclude(x => x.Estado)
                    .Select(x => new FolioDetalleDTO()
                    {
                        Folio = x.Folio,
                        Grupo = x.Grupo,
                        Cliente = x.Cliente,
                        Destino = x.Destino,
                        Producto = x.Producto,
                        Precio = x.Precio,
                        ordenEmbarque = x.OrdenEmbarque,
                        FchCierre = x.FchCierre,
                        Comentarios = x.Observaciones,
                        Estado = x.OrdenEmbarque.Estado.den,
                        Activa = x.Activa
                    })
                    .OrderByDescending(x => x.FchCierre)
                    .ToListAsync();
                }
                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Filtro para obtener los cierres grupales y de pedido
        [HttpPost("cierrescompletos")] // TODO: checar utilidad
        public async Task<ActionResult> GetCierresGP([FromBody] CierreDiarioDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioCierreDTO> folios = new();
                folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                      .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("P") && x.Id_Tad == id_terminal
                   || x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("G") && x.Id_Tad == id_terminal)
                      .Include(x => x.Cliente)
                      .Include(x => x.Destino)
                      .Include(x => x.Terminal)
                      .Include(x => x.Producto)
                      .Include(x => x.Grupo)
                      .Include(x => x.OrdenEmbarque)
                      .ThenInclude(x => x.Orden)
                      .ThenInclude(x => x.Estado)
                      .Select(x => new FolioCierreDTO()
                      {
                          Folio = x.Folio,
                          cliente = x.Cliente,
                          destino = x.Destino,
                          Producto = x.Producto,
                          FchCierre = x.FchCierre,
                          FchCierre_Vencimiento = x.FchVencimiento,
                          Grupo = x.Grupo,
                          Estado = x.OrdenEmbarque.Estado.den,
                          Activa = x.Activa,
                          Estatus = x.Estatus,
                          Precio = x.Precio,
                          Volumen = x.Volumen,
                          Observaciones = x.Observaciones,
                          Tipo_Venta = x.TipoPago,
                          Terminales = x.Terminal.Den
                      })
                      .OrderByDescending(x => x.FchCierre)
                      .ToListAsync();
                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Filtro para cierres por medio de fechas, grupo y cliente
        [HttpPost("cierrecompleto")] // TODO: checar utilidad
        public async Task<ActionResult> GetCierreGP([FromBody] CierreDiarioDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioCierreDTO> folios = new();
                //Cuando se filtra por grupo empresarial y cliente
                if (filtro.codCte != null && filtro.codGru != null)
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                     .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("P") && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal
                  || x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("G") && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal)
                     .Include(x => x.Cliente)
                     .Include(x => x.Destino)
                     .Include(x => x.Producto)
                     .Include(x => x.Grupo)
                     .Include(x => x.OrdenEmbarque)
                     .ThenInclude(x => x.Orden)
                     .ThenInclude(x => x.Estado)
                     .Select(x => new FolioCierreDTO()
                     {
                         Folio = x.Folio,
                         cliente = x.Cliente,
                         destino = x.Destino,
                         Producto = x.Producto,
                         FchCierre = x.FchCierre,
                         Grupo = x.Grupo,
                         Estado = x.OrdenEmbarque.Estado.den,
                         Activa = x.Activa,
                         Precio = x.Precio,
                         Volumen = x.Volumen,
                         Observaciones = x.Observaciones
                     })
                     .OrderByDescending(x => x.FchCierre)
                     .ToListAsync();

                }
                else if (filtro.codGru != null)
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                   .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("P") && x.CodGru == filtro.codGru && x.Id_Tad == id_terminal
                || x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("G") && x.CodGru == filtro.codGru && x.Id_Tad == id_terminal)
                   .Include(x => x.Cliente)
                   .Include(x => x.Destino)
                   .Include(x => x.Producto)
                   .Include(x => x.Grupo)
                   .Include(x => x.OrdenEmbarque)
                   .ThenInclude(x => x.Orden)
                   .ThenInclude(x => x.Estado)
                   .Select(x => new FolioCierreDTO()
                   {
                       Folio = x.Folio,
                       cliente = x.Cliente,
                       destino = x.Destino,
                       Producto = x.Producto,
                       FchCierre = x.FchCierre,
                       Grupo = x.Grupo,
                       Estado = x.OrdenEmbarque.Estado.den,
                       Activa = x.Activa,
                       Precio = x.Precio,
                       Volumen = x.Volumen,
                       Observaciones = x.Observaciones
                   })
                   .OrderByDescending(x => x.FchCierre)
                   .ToListAsync();
                }
                else
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                  .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("P") && x.Id_Tad == id_terminal
               || x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("G") && x.Id_Tad == id_terminal)
                  .Include(x => x.Cliente)
                  .Include(x => x.Destino)
                  .Include(x => x.Producto)
                  .Include(x => x.Grupo)
                  .Include(x => x.OrdenEmbarque)
                  .ThenInclude(x => x.Orden)
                  .ThenInclude(x => x.Estado)
                  .Select(x => new FolioCierreDTO()
                  {
                      Folio = x.Folio,
                      cliente = x.Cliente,
                      destino = x.Destino,
                      Producto = x.Producto,
                      FchCierre = x.FchCierre,
                      Grupo = x.Grupo,
                      Estado = x.OrdenEmbarque.Estado.den,
                      Activa = x.Activa,
                      Precio = x.Precio,
                      Volumen = x.Volumen,
                      Observaciones = x.Observaciones
                  })
                  .OrderByDescending(x => x.FchCierre)
                  .ToListAsync();
                }
                return Ok(folios);

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Filtro para cierre de pedidos
        [HttpPost("cierrefolio/detalle")] //TODO: checar utilidad
        public async Task<ActionResult> GetFoliosCierres([FromBody] CierreDiarioDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioCierreDTO> folios = new();
                if (filtro.codCte != null && filtro.codGru != null)
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                        .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("P")
                        && x.CodGru == filtro.codGru && x.CodCte == filtro.codCte && x.Id_Tad == id_terminal)
                        .Include(x => x.Cliente)
                        .Include(x => x.Destino)
                        .Include(x => x.Terminal)
                        .Include(x => x.Producto)
                        .Include(x => x.Grupo)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ThenInclude(x => x.Estado)
                        .Select(x => new FolioCierreDTO()
                        {
                            Folio = x.Folio,
                            cliente = x.Cliente,
                            destino = x.Destino,
                            Producto = x.Producto,
                            FchCierre = x.FchCierre,
                            FchCierre_Vencimiento = x.FchVencimiento,
                            Grupo = x.Grupo,
                            Estado = x.OrdenEmbarque.Estado.den,
                            Activa = x.Activa,
                            Estatus = x.Estatus,
                            Precio = x.Precio,
                            Volumen = x.Volumen,
                            Observaciones = x.Observaciones,
                            Terminales = x.Terminal.Den
                        })
                        .OrderByDescending(x => x.FchCierre)
                        .ToListAsync();
                }
                else
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                       .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("P") && x.Id_Tad == id_terminal)
                       .Include(x => x.Cliente)
                       .Include(x => x.Destino)
                       .Include(x => x.Terminal)
                       .Include(x => x.Producto)
                       .Include(x => x.Grupo)
                       .Include(x => x.OrdenEmbarque)
                       .ThenInclude(x => x.Orden)
                       .ThenInclude(x => x.Estado)
                       .Select(x => new FolioCierreDTO()
                       {
                           Folio = x.Folio,
                           cliente = x.Cliente,
                           destino = x.Destino,
                           Producto = x.Producto,
                           FchCierre = x.FchCierre,
                           FchCierre_Vencimiento = x.FchVencimiento,
                           Grupo = x.Grupo,
                           Estado = x.OrdenEmbarque.Estado.den,
                           Activa = x.Activa,
                           Precio = x.Precio,
                           Volumen = x.Volumen,
                           Observaciones = x.Observaciones,
                           Terminales = x.Terminal.Den
                       })
                       .OrderByDescending(x => x.FchCierre)
                       .ToListAsync();
                }
                return Ok(folios);

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Filtro para cierres grupales
        [HttpPost("cierregrupo/detalle")] //TODO: checar utilidad
        public async Task<ActionResult> GetCierresGrupales([FromBody] CierreDiarioDTO filtro)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<FolioCierreDTO> folios = new();
                if (filtro.codGru != null)
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                        .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("G")
                          && x.CodGru == filtro.codGru && x.Id_Tad == id_terminal)
                        .Include(x => x.Cliente)
                        .Include(x => x.Destino)
                        .Include(x => x.Producto)
                        .Include(x => x.Grupo)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ThenInclude(x => x.Estado)
                        .Select(x => new FolioCierreDTO()
                        {
                            Folio = x.Folio,
                            cliente = x.Cliente,
                            destino = x.Destino,
                            Producto = x.Producto,
                            FchCierre = x.FchCierre,
                            Grupo = x.Grupo,
                            Estado = x.OrdenEmbarque.Estado.den,
                            Activa = x.Activa,
                            Precio = x.Precio,
                            Volumen = x.Volumen,
                            Observaciones = x.Observaciones,
                            Terminales = x.Terminal.Den
                        })
                        .OrderByDescending(x => x.FchCierre)
                        .ToListAsync();
                }
                else
                {
                    folios = await context.OrdenCierre.OrderBy(x => x.FchCierre)
                       .Where(x => x.FchCierre >= filtro.FchInicio && x.FchCierre <= filtro.FchFin && x.Folio.StartsWith("G") && x.Id_Tad == id_terminal)
                       .Include(x => x.Cliente)
                       .Include(x => x.Destino)
                       .Include(x => x.Producto)
                        .Include(x => x.Grupo)
                       .Include(x => x.OrdenEmbarque)
                       .ThenInclude(x => x.Orden)
                       .ThenInclude(x => x.Estado)
                       .Select(x => new FolioCierreDTO()
                       {
                           Folio = x.Folio,
                           cliente = x.Cliente,
                           destino = x.Destino,
                           Producto = x.Producto,
                           FchCierre = x.FchCierre,
                           Grupo = x.Grupo,
                           Estado = x.OrdenEmbarque.Estado.den,
                           Activa = x.Activa,
                           Precio = x.Precio,
                           Volumen = x.Volumen,
                           Observaciones = x.Observaciones,
                           Terminales = x.Terminal.Den
                       })
                       .OrderByDescending(x => x.FchCierre)
                       .ToListAsync();
                }
                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("folios/{cliente}")]
        public async Task<ActionResult> GetFoliosValidosFiltro([FromBody] CierreFiltroDTO filtroDTO, [FromRoute] int cliente)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<string?> folios = new();
                if (cliente != 0)
                    folios = context.OrdenCierre.Where(x => x.FchCierre >= filtroDTO.FchInicio && x.FchCierre <= filtroDTO.FchFin
                    && !string.IsNullOrEmpty(x.Folio) && x.Activa == true && x.CodCte == cliente && x.Id_Tad == id_terminal)
                        .Select(x => x.Folio)
                        .Distinct()
                        .ToList();
                else
                    folios = context.OrdenCierre.Where(x => x.FchCierre >= filtroDTO.FchInicio && x.FchCierre <= filtroDTO.FchFin
                && !string.IsNullOrEmpty(x.Folio) && x.Activa == true && x.Id_Tad == id_terminal)
                    .Select(x => x.Folio)
                    .Distinct()
                    .ToList();

                return Ok(folios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("disponibles")] //TODO: checar unificacion de metodo de volumen
        public async Task<ActionResult> GetCierresDisponiles([FromBody] CierreFiltroDTO filtroDTO)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (filtroDTO is null)
                    return BadRequest();

                List<OrdenCierre> cierres = new();

                var cierresDis = await context.OrdenCierre.IgnoreAutoIncludes().Where(x => x.FchCierre >= filtroDTO.FchInicio && x.FchCierre <= filtroDTO.FchFin
                && x.Activa == true && x.CodPed == 0 && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Cliente)
                    .Include(x => x.Grupo)
                    .ToListAsync();

                if (cierresDis is null)
                    return Ok(cierres);

                Porcentaje porcentaje = new();
                var por = context.Porcentaje.FirstOrDefault(x => x.Accion == "cierre");
                if (por != null)
                    porcentaje = por;

                foreach (var item in cierresDis)
                {
                    if (context.OrdenPedido.Any(x => x.Folio.Equals(item.Folio)))
                    {

                        var VolumenDisponible = item.Volumen;

                        var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Codest == 22
                            && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Bolguidid != null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel).ToList();

                        var VolumenCongelado = listConsumido.Sum(item => item.OrdenEmbarque!.Compartment == 1 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom!.ToString())
                                        : item.OrdenEmbarque!.Compartment == 2 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom2!.ToString())
                                        : item.OrdenEmbarque!.Compartment == 3 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom3!.ToString())
                                        : item.OrdenEmbarque!.Compartment == 4 && item.OrdenEmbarque.Tonel != null ? double.Parse(item!.OrdenEmbarque!.Tonel!.Capcom4!.ToString())
                                        : item.OrdenEmbarque!.Vol);

                        var countCongelado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Codprd == item.CodPrd
                        && x.OrdenEmbarque.Codest == 22
                        && x.OrdenEmbarque.Folio != null
                        && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .Count();

                        var VolumenConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Orden.Codest != 14
                            && x.OrdenEmbarque.Codest != 14
                        && x.OrdenEmbarque.Codprd == item.CodPrd
                        && x.OrdenEmbarque.Orden.BatchId != null
                        && x.OrdenEmbarque.Orden.Id_Tad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .Sum(x => x.OrdenEmbarque!.Orden!.Vol);

                        var countConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Orden != null && x.OrdenEmbarque.Folio != null
                            && x.OrdenEmbarque.Orden.Codest != 14
                            && x.OrdenEmbarque.Codest != 14
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Orden.BatchId != null
                            && x.OrdenEmbarque.Orden.Id_Tad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .Count();

                        var VolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                            && x.OrdenEmbarque.FchOrd != null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                                .Sum(x => x.OrdenEmbarque!.Vol);

                        var CountVolumenProgramado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                            && x.OrdenEmbarque.Codprd == item.CodPrd
                            && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 3
                            && x.OrdenEmbarque.FchOrd != null
                            && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque).Count();

                        var VolumenSolicitado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Folio == null
                        && x.OrdenEmbarque.Codprd == item.CodPrd
                        && x.OrdenEmbarque.Bolguidid == null && x.OrdenEmbarque.Codest == 9
                        && x.OrdenEmbarque.FchOrd == null
                        && x.OrdenEmbarque.Codtad == id_terminal)
                            .Include(x => x.OrdenEmbarque)
                            .Sum(x => x.OrdenEmbarque!.Vol);

                        var VolumenTotalDisponible = VolumenDisponible - (VolumenConsumido + VolumenCongelado + VolumenProgramado);
                        double? PromedioCargas = 0;
                        var sumVolumen = VolumenConsumido + VolumenCongelado + VolumenProgramado;
                        var sumCount = countCongelado + countConsumido + CountVolumenProgramado;

                        if (sumVolumen != 0 && sumCount != 0)
                            PromedioCargas = sumVolumen / sumCount;

                        ProductoVolumen productoVolumen = new();

                        productoVolumen.Nombre = item.Producto?.Den;
                        productoVolumen.Disponible = VolumenTotalDisponible;
                        productoVolumen.Congelado = VolumenCongelado;
                        productoVolumen.Consumido = VolumenConsumido;
                        productoVolumen.Total = VolumenDisponible;
                        productoVolumen.PromedioCarga = PromedioCargas;
                        productoVolumen.Solicitud = VolumenSolicitado;
                        productoVolumen.Programado = VolumenProgramado;

                        item.VolumenDisponible?.Productos?.Add(productoVolumen);

                    }
                    //if (item.VolumenDisponible?.Productos?.FirstOrDefault()?.PromedioCarga >=
                    //(item.VolumenDisponible?.Productos?.FirstOrDefault()?.Disponible * (porcentaje.Porcen/100)))
                    cierres.Add(item);
                }

                return Ok(cierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("cerrar/auto")] //No limitar la terminal
        public async Task<ActionResult> CerrarCierresPorPorcentaje([FromBody] CierreFiltroDTO filtroDTO)
        {
            try
            {
                if (filtroDTO is null)
                    return BadRequest();

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                List<OrdenCierre> cierres = new();

                var cierresDis = await context.OrdenCierre.Where(x => x.FchCierre >= filtroDTO.FchInicio && x.FchCierre <= filtroDTO.FchFin
                && x.Activa == true && x.CodPed == 0)
                    .Include(x => x.Producto)
                    .ToListAsync();

                if (cierresDis is null)
                    return Ok(cierres);

                Porcentaje porcentaje = new();
                var por = context.Porcentaje.FirstOrDefault(x => x.Accion == "cierre");
                if (por != null)
                    porcentaje = por;

                foreach (var item in cierresDis)
                {
                    if (context.OrdenPedido.Any(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio)))
                    {

                        var VolumenDisponible = item.Volumen;

                        var listConsumido = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Tonel != null && x.OrdenEmbarque.Codprd == item.CodPrd
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

                        var countCongelado = context.OrdenPedido.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(item.Folio) && x.OrdenEmbarque != null && x.OrdenEmbarque.Codprd == item.CodPrd
                        && x.OrdenEmbarque.Codest == 22
                        && x.OrdenEmbarque.Folio != null)
                            .Include(x => x.OrdenEmbarque)
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

                        productoVolumen.Nombre = item.Producto?.Den;
                        productoVolumen.Disponible = VolumenTotalDisponible;
                        productoVolumen.Congelado = VolumenCongelado;
                        productoVolumen.Consumido = VolumenConsumido;
                        productoVolumen.Total = VolumenDisponible;
                        productoVolumen.PromedioCarga = PromedioCargas;
                        productoVolumen.Solicitud = VolumenSolicitado;
                        productoVolumen.Programado = VolumenProgramado;
                        productoVolumen.ID_Producto = item.CodPrd;
                        item.VolumenDisponible?.Productos?.Add(productoVolumen);

                    }

                    if (item.VolumenDisponible?.Productos?.FirstOrDefault(x => x.ID_Producto == item.CodPrd)?.PromedioCarga >=
                    (item.VolumenDisponible?.Productos?.FirstOrDefault(x => x.ID_Producto == item.CodPrd)?.Disponible * (porcentaje.Porcen / 100)))
                    {
                        item.Activa = false;
                        cierres.Add(item);
                    }
                }
                context.UpdateRange(cierres);

                await context.SaveChangesAsync(id, 16);

                return Ok(cierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("filtrar/pendientes")]
        public async Task<ActionResult> GetCierresPendentes([FromBody] FechasF fechas)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenCierre> cierres = new();
                cierres = await context.OrdenCierre.Where(x => x.FchCierre >= fechas.DateInicio && x.FchCierre <= fechas.DateFin && x.Confirmada == false
                && x.Activa == true && x.Estatus == true && x.Id_Tad == id_terminal && x.CodPed == 0)
                    .Include(x => x.Cliente)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.Grupo)
                    .OrderBy(x => x.FchCierre)
                    .Take(1000)
                    .ToListAsync();

                return Ok(cierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
                throw;
            }
        }

        [HttpPost("confirm")]
        public async Task<ActionResult> ConfirmCierresPendentes([FromBody] List<OrdenCierre> cierres)
        {
            try
            {

                cierres.ForEach(x =>
                {
                    x.Cliente = null!;
                    x.Destino = null!;
                    x.OrdenEmbarque = null!;
                    x.Grupo = null!;
                    x.Producto = null!;
                    x.VolumenDisponible = null!;
                    x.Confirmada = true;
                    x.Terminal = null!;
                });

                context.UpdateRange(cierres);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 32);

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
                throw;
            }
        }

        [HttpGet("caducidad/verify")]//TODO: verificar utilidad
        public async Task<ActionResult> VerificarCaducidad([FromQuery] CierreFiltroDTO dTO)
        {
            try
            {
                if (string.IsNullOrEmpty(dTO.Folio))
                    return BadRequest("No se encontro un folio");

                var activo = false;
                var fchActiva = string.Empty;

                var cierre = context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio.Equals(dTO.Folio)).ToList();

                foreach (var item in cierre)
                    if (item?.FchVencimiento >= DateTime.Today)
                        activo = true;
                    else
                        fchActiva = item?.FchVen;

                if (cierre is not null)
                {
                    if (activo)
                        return Ok(activo);
                    else
                        return BadRequest($"Este pedido ya no se encuentra vigente.Fecha de vecimiento: {fchActiva}");
                }
                else
                    return BadRequest($"No se encontro algun cierre perteneciente al folio {dTO.Folio}");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("caducidad/cerrar")]//No limitar terminal
        public async Task<ActionResult> CerrarCierreAutoCaducidad()
        {
            try
            {
                List<OrdenCierre> cierres = new();

                cierres = context.OrdenCierre.Where(x => !string.IsNullOrEmpty(x.Folio) && x.FchVencimiento < DateTime.Today && x.Activa == true && x.Folio.StartsWith("P")
                || !string.IsNullOrEmpty(x.Folio) && x.FchVencimiento < DateTime.Today && x.Activa == true && x.Folio.StartsWith("G"))
                    .OrderByDescending(x => x.FchVencimiento)
                    .ToList();

                foreach (var item in cierres)
                {
                    item.Activa = false;
                    context.Update(item);
                }

                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("caducidad/abrir")] // No limitar terminal
        public async Task<ActionResult> AbrirCierreAutoCaducidad()
        {
            try
            {
                List<OrdenCierre> cierres = new();

                cierres = context.OrdenCierre.Where(x => x.FchVencimiento > DateTime.Today && x.Activa == false).ToList();

                foreach (var item in cierres)
                {
                    item.Activa = true;
                    context.Update(item);
                }

                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("autocreate/orden")]
        public async Task<ActionResult> AutocrearOrdenes([FromBody] OrdenCierre cierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                List<OrdenEmbarque> embarques = new();

                if (cierre is null)
                    return BadRequest("No se envio cierre para crear ordenes");

                var newCierre = context.OrdenCierre.IgnoreAutoIncludes().Where(x => x.Cod == cierre.Cod && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Cliente)
                    .Include(x => x.OrdenPedidos)
                    .ThenInclude(x => x.OrdenEmbarque)
                    .FirstOrDefault();

                if (newCierre is null)
                    return BadRequest();

                newCierre.SetVolumen();

                if ((cierre.Volumen_Por_Unidad * cierre.Cantidad_Confirmada) > newCierre.GetVolumenDisponible())
                    return BadRequest($"No tiene suficiente volumen disponible. Disponible: {newCierre.GetVolumenDisponible()}. Solicitado: {cierre.Volumen_Por_Unidad * cierre.Cantidad_Confirmada}");

                //if ((cierre.Volumen_Por_Unidad * cierre.Cantidad_Confirmada) > newCierre.GetVolumenDisponible())
                //    return BadRequest($"No tiene suficiente volumen disponible. Disponible: {cierre.GetVolumenDisponible()}. Solicitado: {cierre.Volumen_Por_Unidad * cierre.Cantidad_Confirmada}");

                Cliente? Cliente = new();
                Grupo? Grupo = new();
                string folio = string.Empty;

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

                if (!cierre.isGroup)
                {
                    Cliente = context.Cliente.FirstOrDefault(x => x.Cod == cierre.CodCte);

                    cierre.TipoVenta = Cliente?.Tipven;

                    if (!string.IsNullOrEmpty(Cliente?.Tipven))
                    {
                        cierre.ModeloVenta = Cliente?.MdVenta;
                        cierre.TipoVenta = Cliente?.Tipven;
                    }
                    else
                    {
                        cierre.ModeloVenta = string.Empty;
                        cierre.TipoVenta = string.Empty;
                    }
                }
                else
                {
                    Grupo = context.Grupo.FirstOrDefault(x => x.Cod == cierre.CodGru);

                    cierre.TipoVenta = Grupo?.Tipven;

                    if (!string.IsNullOrEmpty(Grupo?.Tipven))
                    {
                        cierre.ModeloVenta = Grupo?.MdVenta;
                        cierre.TipoVenta = Grupo?.Tipven;
                    }
                    else
                    {
                        cierre.ModeloVenta = string.Empty;
                        cierre.TipoVenta = string.Empty;
                    }
                }

                if (!cierre.isGroup)
                    folio = $"O{DateTime.Now:yy}-{consecutivo.Numeracion:000000}{(Cliente is not null && !string.IsNullOrEmpty(Cliente.CodCte) ? $"-{Cliente.CodCte}" : "-DFT")}";
                else
                    folio = $"O{DateTime.Now:yy}-{consecutivo.Numeracion:000000}{(Grupo is not null && !string.IsNullOrEmpty(Grupo.CodGru) ? $"-{Grupo.CodGru}" : "-DFT")}";

                for (int i = 0; i < cierre.Cantidad_Confirmada; i++)
                {
                    //var litros = cierre.Volumen_Seleccionado >= 42000 ? cierre.Volumen_Seleccionado / 2 : 20000;

                    var bin = await context.OrdenEmbarque.Select(x => x.Bin).OrderBy(x => x).LastOrDefaultAsync();

                    var embarque = new OrdenEmbarque()
                    {
                        Codprd = cierre.CodPrd,
                        Coddes = cierre.CodDes,
                        Codtad = cierre.Id_Tad,
                        Pre = cierre.Precio,
                        Fchpet = DateTime.Now,
                        Codest = 9,
                        Vol = cierre.Volumen_Por_Unidad,
                        Fchcar = cierre.FchCar,
                        Bin = i == 0 ? ++bin : i % 2 == 0 ? ++bin : bin
                    };

                    context.Add(embarque);
                    await context.SaveChangesAsync();

                    var ordenpedido = new OrdenPedido()
                    {
                        CodPed = embarque.Cod,
                        CodCierre = newCierre.Cod,
                        Folio = newCierre.Folio,
                        OrdenEmbarque = null,
                        OrdenCierre = null
                    };

                    context.Add(ordenpedido);
                    await context.SaveChangesAsync();

                    var ordencierre = cierre.ShallowCopy();
                    ordencierre.Cod = 0;
                    ordencierre.Vendedor = string.Empty;
                    ordencierre.Observaciones = string.Empty;
                    ordencierre.CodPed = embarque.Cod;
                    ordencierre.Folio = folio;
                    ordencierre.Activa = true;
                    ordencierre.Estatus = true;
                    ordencierre.Cliente = null;
                    ordencierre.Destino = null;
                    ordencierre.OrdenEmbarque = null;
                    ordencierre.OrdenPedidos = null;
                    ordencierre.Producto = null;
                    ordencierre.Grupo = null;
                    ordencierre.Terminal = null;
                    ordencierre.Volumen = (int)embarque.Vol;

                    context.Add(ordencierre);
                    await context.SaveChangesAsync();
                    //embarques.Add(embarque);
                }

                //context.AddRange(embarques);
                //await context.SaveChangesAsync();
                newCierre = context.OrdenCierre.Where(x => x.Cod == cierre.Cod && x.Id_Tad == id_terminal)
                    .Include(x => x.Grupo)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Cliente)
                    .IgnoreAutoIncludes()
                    .FirstOrDefault();

                if (newCierre is null)
                    return BadRequest();

                newCierre.Volumen_Seleccionado = cierre.Volumen_Seleccionado;

                newCierre.SetCantidades();
                newCierre.SetVolumen();
                newCierre.GetCantidadSugerida();

                newCierre.Ordenes_Relacionadas = newCierre.OrdenPedidos.Count;

                newCierre.OrdenPedidos = new List<OrdenPedido>();

                return Ok(newCierre);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("detalle")]
        public ActionResult Obtener_Detalle_Cierre([FromQuery] OrdenCierre ordenCierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                Crear_Orden_Template_DTO crear_Orden_Template_DTO = new();

                OrdenCierre? orden = context.OrdenCierre
                    .IgnoreAutoIncludes()
                    .Include(x => x.Moneda)
                    .IgnoreAutoIncludes()
                    .FirstOrDefault(x => !string.IsNullOrEmpty(x.Folio) && x.Cod == ordenCierre.Cod && x.Folio == ordenCierre.Folio && x.Id_Tad == id_terminal);

                if (orden is null)
                    return BadRequest("No se encontro el cierre");

                if (!string.IsNullOrEmpty(ordenCierre.Folio))
                    crear_Orden_Template_DTO.Puede_Seleccionar_Cliete_Destino = ordenCierre.Folio.StartsWith("G");

                crear_Orden_Template_DTO.OrdenCierre = orden;

                crear_Orden_Template_DTO.ID_Grupo = orden.CodGru;

                crear_Orden_Template_DTO.ID_Cliente = orden.CodCte;

                crear_Orden_Template_DTO.ID_Destino = orden.CodDes;

                crear_Orden_Template_DTO.ID_Producto = orden.CodPrd;

                if (crear_Orden_Template_DTO.ID_Grupo is not null)
                    crear_Orden_Template_DTO.Grupos = context.Grupo.Where(x => x.Cod == crear_Orden_Template_DTO.ID_Grupo).ToList();
                else
                    crear_Orden_Template_DTO.Grupos = context.Grupo.ToList();

                if (crear_Orden_Template_DTO.ID_Cliente is not null)
                    crear_Orden_Template_DTO.Clientes = context.Cliente.Where(x => x.Cod == crear_Orden_Template_DTO.ID_Cliente).ToList();
                else if (crear_Orden_Template_DTO.ID_Cliente is null && crear_Orden_Template_DTO.ID_Grupo is not null)
                    crear_Orden_Template_DTO.Clientes = context.Cliente.Where(x => x.Codgru == crear_Orden_Template_DTO.ID_Grupo).ToList();

                if (crear_Orden_Template_DTO.ID_Destino is not null)
                    crear_Orden_Template_DTO.Destinos = context.Destino.Where(x => x.Cod == crear_Orden_Template_DTO.ID_Destino).ToList();

                if (crear_Orden_Template_DTO.ID_Producto is not null)
                    crear_Orden_Template_DTO.Producto = context.Producto.First(x => x.Cod == crear_Orden_Template_DTO.ID_Producto);

                Precio precio = new()
                {
                    Pre = orden.Precio,
                    Producto = crear_Orden_Template_DTO.Producto,
                    Moneda = orden.Moneda ?? new Moneda(),
                    ID_Moneda = orden.ID_Moneda,
                    Equibalencia = orden.Equibalencia ?? 1
                };

                crear_Orden_Template_DTO.Precio = precio;

                var pedidos = context.OrdenPedido.Where(x => x.Folio == orden.Folio && x.CodPed != null && x.OrdenEmbarque != null).ToList();

                foreach (var item in pedidos)
                {
                    var pedido = context.OrdenCierre.Where(x => x.CodPed == item.CodPed && x.CodPrd == orden.CodPrd)
                        .Include(x => x.OrdenEmbarque)
                        .Include(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Destino)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tad)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tonel)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Estado)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ThenInclude(x => x.Estado)
                        .IgnoreAutoIncludes()
                        .DefaultIfEmpty()
                        .FirstOrDefault();

                    if (pedido != null)
                        crear_Orden_Template_DTO.OrdenCierres.Add(pedido);
                }

                return Ok(crear_Orden_Template_DTO);
            }
            catch (ArgumentNullException)
            {
                return BadRequest("Parametros vacios");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("detalle/general")]
        public ActionResult Obtener_Detalle_Cierre_General([FromQuery] OrdenCierre ordenCierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                Crear_Orden_Template_DTO crear_Orden_Template_DTO = new();

                List<OrdenCierre> ordenes = context.OrdenCierre.IgnoreAutoIncludes().Where(x => !string.IsNullOrEmpty(x.Folio) && x.Folio == ordenCierre.Folio && x.Id_Tad == id_terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Moneda)
                    .IgnoreAutoIncludes().ToList();

                if (ordenes is null)
                    return BadRequest("No se encontro el cierre");

                if (!string.IsNullOrEmpty(ordenCierre.Folio))
                {
                    crear_Orden_Template_DTO.Puede_Seleccionar_Cliete_Destino = ordenCierre.Folio.StartsWith("G") || ordenCierre.Folio.StartsWith("RE"); ;
                    crear_Orden_Template_DTO.Es_Orden_Copiada = ordenCierre.Folio.StartsWith("RE");
                }

                foreach (var item in ordenes)
                {
                    if (crear_Orden_Template_DTO.ID_Grupo is not null && !crear_Orden_Template_DTO.Es_Orden_Copiada)
                    {
                        if (!crear_Orden_Template_DTO.Grupos.Any(x => x.Cod == item.CodGru))
                            crear_Orden_Template_DTO.Grupos.AddRange(context.Grupo.Where(x => x.Cod == item.CodGru).ToList());
                    }
                    else
                        crear_Orden_Template_DTO.Grupos = context.Grupo.ToList();

                    if (crear_Orden_Template_DTO.ID_Cliente is not null && !crear_Orden_Template_DTO.Es_Orden_Copiada)
                        if (!crear_Orden_Template_DTO.Clientes.Any(x => x.Cod == item.CodCte))
                            crear_Orden_Template_DTO.Clientes.AddRange(context.Cliente.Where(x => x.Cod == item.CodCte).ToList());

                    if (crear_Orden_Template_DTO.ID_Destino is not null && !crear_Orden_Template_DTO.Es_Orden_Copiada)
                        if (!crear_Orden_Template_DTO.Destinos.Any(x => x.Cod == item.CodDes))
                            crear_Orden_Template_DTO.Destinos.AddRange(context.Destino.Where(x => x.Cod == item.CodDes).ToList());

                    if (crear_Orden_Template_DTO.ID_Producto is not null && !crear_Orden_Template_DTO.Es_Orden_Copiada)
                        if (!crear_Orden_Template_DTO.Productos.Any(x => x.Cod == item.CodPrd))
                            crear_Orden_Template_DTO.Productos.AddRange(context.Producto.Where(x => x.Cod == item.CodPrd).ToList());

                    Precio precio = new()
                    {
                        Pre = item.Precio,
                        Producto = item.Producto,
                        Moneda = item.Moneda,
                        ID_Moneda = item.ID_Moneda,
                        Equibalencia = item.Equibalencia ?? 1
                    };

                    crear_Orden_Template_DTO.Precios.Add(precio);
                }
                if (!crear_Orden_Template_DTO.Es_Orden_Copiada)
                {
                    var pedidos = context.OrdenPedido.IgnoreAutoIncludes().Where(x => x.Folio == ordenCierre.Folio && x.CodPed != null && x.OrdenEmbarque != null
                    && x.OrdenCierre != null && x.OrdenCierre.Id_Tad == id_terminal)
                        .Include(x => x.OrdenEmbarque).IgnoreAutoIncludes()
                        .Include(x => x.OrdenCierre).IgnoreAutoIncludes().ToList();
                    foreach (var item in pedidos)
                    {
                        var pedido = context.OrdenCierre.Where(x => x.CodPed == item.CodPed)
                            .Include(x => x.OrdenEmbarque)
                            .Include(x => x.Cliente)
                            .Include(x => x.Producto)
                            .Include(x => x.Destino)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tad)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Estado)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .ThenInclude(x => x.Estado)
                            .IgnoreAutoIncludes()
                            .DefaultIfEmpty()
                            .FirstOrDefault();

                        if (pedido != null)
                            crear_Orden_Template_DTO.OrdenCierres.Add(pedido);
                    }
                }
                else
                {
                    var pedidos = context.OrdenPedido.IgnoreAutoIncludes().Where(x => x.Folio_Cierre_Copia == ordenCierre.Folio && x.CodPed != null && x.OrdenEmbarque != null
                    && x.OrdenCierre != null && x.OrdenCierre.Id_Tad == id_terminal)
                        .Include(x => x.OrdenEmbarque).IgnoreAutoIncludes()
                        .Include(x => x.OrdenCierre).IgnoreAutoIncludes().ToList();
                    foreach (var item in pedidos)
                    {
                        var pedido = context.OrdenCierre.Where(x => x.CodPed == item.CodPed)
                            .Include(x => x.OrdenEmbarque)
                            .Include(x => x.Cliente)
                            .Include(x => x.Producto)
                            .Include(x => x.Destino)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tad)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Tonel)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Estado)
                            .Include(x => x.OrdenEmbarque)
                            .ThenInclude(x => x.Orden)
                            .ThenInclude(x => x.Estado)
                            .IgnoreAutoIncludes()
                            .DefaultIfEmpty()
                            .FirstOrDefault();

                        if (pedido != null)
                            crear_Orden_Template_DTO.OrdenCierres.Add(pedido);
                    }
                }

                return Ok(crear_Orden_Template_DTO);
            }
            catch (ArgumentNullException)
            {
                return BadRequest("Parametros vacios");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("detalle/copia")]
        public ActionResult Obtener_Detalle_Cierre_Copiado([FromQuery] OrdenCierre ordenCierre)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (string.IsNullOrEmpty(ordenCierre.Folio) || string.IsNullOrWhiteSpace(ordenCierre.Folio))
                    return BadRequest("No se admiten valores vacios");

                Crear_Orden_Template_DTO crear_Orden_Template_DTO = new();

                var pedidos = context.OrdenPedido.Where(x => x.Folio_Cierre_Copia == ordenCierre.Folio && x.CodPed != null && x.OrdenEmbarque != null
                && x.OrdenCierre != null && x.OrdenCierre.Id_Tad == id_terminal)
                    .Include(x => x.OrdenCierre).IgnoreAutoIncludes()
                    .Include(x => x.OrdenEmbarque).IgnoreAutoIncludes().ToList();

                foreach (var item in pedidos)
                {
                    var pedido = context.OrdenCierre.Where(x => x.CodPed == item.CodPed)
                        .Include(x => x.OrdenEmbarque)
                        .Include(x => x.Cliente)
                        .Include(x => x.Producto)
                        .Include(x => x.Destino)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tad)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Tonel)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Estado)
                        .Include(x => x.OrdenEmbarque)
                        .ThenInclude(x => x.Orden)
                        .ThenInclude(x => x.Estado)
                        .IgnoreAutoIncludes()
                        .DefaultIfEmpty()
                        .FirstOrDefault();

                    if (pedido != null)
                        crear_Orden_Template_DTO.OrdenCierres.Add(pedido);
                }

                return Ok(crear_Orden_Template_DTO);
            }
            catch (ArgumentNullException)
            {
                return BadRequest("Parametros vacios");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("cancelarordencliente/{cod}")]
        public async Task<ActionResult> Cancelar_Ordenembarque([FromRoute] int cod)
        {
            try
            {
                var ordenembarque = context.OrdenEmbarque.FirstOrDefault(x => x.Cod == cod);
                if (ordenembarque is null)
                    return NotFound();

                var orden = context.OrdenCierre.FirstOrDefault(x => x.CodPed == ordenembarque.Cod);
                if (orden == null)
                    return NotFound();

                orden.Estatus = false;
                ordenembarque.Codest = 14;

                context.Update(orden);
                context.Update(ordenembarque);

                await context.SaveChangesAsync();


                return Ok(ordenembarque);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Confirmar pedido
        [HttpPost("confirmarpedido")]
        public async Task<ActionResult<OrdenCierre>> PostConfirm(List<OrdenCierre> orden)
        {
            try
            {
                if (orden is null)
                    return BadRequest("No se encontro el pedido");
                orden.ForEach(x =>
                {
                    x.Cliente = null!;
                    x.Destino = null!;
                    x.OrdenEmbarque = null!;
                    x.Grupo = null!;
                    x.Producto = null!;
                    x.VolumenDisponible = null!;
                    x.Confirmada = true;
                });

                context.UpdateRange(orden);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 15);
                return Ok(orden);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpDelete("cancelar/{cod:int}")]
        public async Task<ActionResult> CancelOrden([FromRoute] int cod)
        {
            try
            {
                if (cod == 0)
                    return BadRequest("Orden no valida");

                var orden = context.OrdenCierre.Find(cod);

                if (orden is null)
                    return BadRequest("No se encontro la orden");

                orden.Activa = false;
                orden.Estatus = false;

                context.Update(orden);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 16);

                return Ok(true);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpGet("cancelar/{ID_Orden}")]
        public async Task<ActionResult> Cancelar_Orden([FromRoute] int ID_Orden)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var orden = context.OrdenCierre.IgnoreAutoIncludes().FirstOrDefault(x => x.Cod == ID_Orden && x.Id_Tad == id_terminal);
                if (orden == null)
                    return NotFound();

                var ordenembarque = context.OrdenEmbarque.IgnoreAutoIncludes().FirstOrDefault(x => x.Cod == orden.CodPed && x.Codtad == id_terminal);
                if (ordenembarque is null)
                    return NotFound();

                orden.Estatus = false;
                ordenembarque.Codest = 14;

                context.Update(orden);
                context.Update(ordenembarque);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 33);

                var newOrden = context.OrdenCierre
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Estado)
                    .Include(x => x.Destino)
                    .Include(x => x.Producto)
                    .Include(x => x.OrdenEmbarque)
                    .ThenInclude(x => x.Tad)
                    .FirstOrDefault(x => x.Cod == orden.Cod && x.Id_Tad == id_terminal);

                return Ok(newOrden);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
