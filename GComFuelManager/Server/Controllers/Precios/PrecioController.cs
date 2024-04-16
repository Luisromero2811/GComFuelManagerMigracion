﻿
using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GComFuelManager.Server.Controllers.Precios
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PrecioController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserToken verifyUser;

        public PrecioController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserToken verifyUser)
        {
            this.context = context;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
        }

        //[HttpGet]
        //public async Task<ActionResult> GetPrecios()
        //{
        //    try
        //    {
        //        var precios = await context.Precio
        //            .Include(x => x.Zona)
        //            .Include(x => x.Cliente)
        //            .Include(x => x.Producto)
        //            .Include(x => x.Destino)
        //            .ToListAsync();

        //        return Ok(precios);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        [HttpPost("productos/{folio?}")]
        public async Task<ActionResult> GetPrecios([FromBody] ZonaCliente? zonaCliente, [FromRoute] string? folio)
        {
            try
            {
                List<Precio> precios = new();
                List<PrecioProgramado> preciosPro = new();
                var LimiteDate = DateTime.Today.AddHours(16);

                var user = await userManager.FindByNameAsync(HttpContext.User.FindFirstValue(ClaimTypes.Name)!);
                if (user == null)
                    return NotFound();

                var userSis = context.Usuario.FirstOrDefault(x => x.Usu == user.UserName);
                if (userSis == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(folio))
                {
                    var ordenes = await context.OrdenCierre.Where(x => x.Folio == folio)
                            .Include(x => x.Cliente)
                            .ToListAsync();
                    var ordenesUnic = ordenes.DistinctBy(x => x.CodPrd).Select(x => x);

                    foreach (var item in ordenesUnic)
                    {
                        var zona = context.ZonaCliente.FirstOrDefault(x => x.CteCod == item.CodCte);
                        Precio precio = new()
                        {
                            Pre = item.Precio,
                            CodCte = item.CodCte,
                            CodDes = item.CodDes,
                            CodPrd = item.CodPrd,
                            CodGru = item.Cliente?.codgru,
                            CodZona = zona?.CteCod,
                            Producto = context.Producto.FirstOrDefault(x => x.Cod == item.CodPrd)
                        };
                        precios.Add(precio);
                    }
                    return Ok(precios);
                }

                precios = await context.Precio.Where(x => x.CodCte == userSis.CodCte && x.CodDes == zonaCliente.DesCod && x.Activo == true)
                    //&& x.CodZona == zona.ZonaCod)
                    .Include(x => x.Producto)
                    .ToListAsync();

                precios.ForEach(x =>
                {
                    if (x.FchDia < DateTime.Today
                    && DateTime.Today.DayOfWeek != DayOfWeek.Saturday
                    && DateTime.Today.DayOfWeek != DayOfWeek.Sunday
                    && DateTime.Today.DayOfWeek != DayOfWeek.Monday)
                    {
                        var porcentaje = context.Porcentaje.FirstOrDefault(x => x.Accion == "cliente");
                        var aumento = (porcentaje.Porcen / 100) + 1;
                        x.Pre = x.FchDia < DateTime.Today ? Math.Round((x.Pre * aumento), 4) : Math.Round(x.Pre, 4);
                    }
                });

                if (DateTime.Now > LimiteDate &&
                    DateTime.Today.DayOfWeek != DayOfWeek.Saturday &&
                    DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                {
                    preciosPro = await context.PrecioProgramado.Where(x => x.CodCte == userSis.CodCte
                    && x.CodDes == zonaCliente.DesCod && x.Activo == true)
                    //&& x.CodZona == zona.ZonaCod)
                    .Include(x => x.Producto)
                    .ToListAsync();

                    foreach (var item in preciosPro)
                    {
                        precios.FirstOrDefault(x => x.CodDes == item.CodDes && x.CodCte == item.CodCte && x.CodPrd == item.CodPrd && x.FchDia < item.FchDia).Pre = item.Pre;
                        precios.FirstOrDefault(x => x.CodDes == item.CodDes && x.CodCte == item.CodCte && x.CodPrd == item.CodPrd && x.FchDia < item.FchDia).FchDia = item.FchDia;
                        var pre = precios.FirstOrDefault(x => x.CodDes == item.CodDes && x.CodCte == item.CodCte && x.CodPrd == item.CodPrd);
                        if (pre is null)
                        {
                            precios.Add(new Precio()
                            {
                                Pre = item.Pre,
                                CodCte = item.CodCte,
                                CodDes = item.CodDes,
                                CodPrd = item.CodPrd,
                                CodGru = item.Cliente?.codgru,
                                Producto = context.Producto.FirstOrDefault(x => x.Cod == item.CodPrd)
                            });
                        }
                    }
                }

                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{Orden_Compra}/{Id_Terminal}")]
        public ActionResult GetPrecioByEner([FromRoute] int Orden_Compra, [FromRoute] Int16 Id_Terminal)
        {
            try
            {
                List<PrecioBol> precios = new();

                var ordenes = context.OrdenEmbarque.IgnoreAutoIncludes().Where(x => x.Folio == Orden_Compra && x.Codtad == Id_Terminal)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .ThenInclude(x => x.Cliente)
                    .Include(x => x.Tad)
                    .ToList();

                if (ordenes is null)
                    return Ok(new List<PrecioBol>() { new PrecioBol() });

                foreach (var item in ordenes)
                {
                    PrecioBol precio = new();

                    Orden? orden = new();
                    orden = context.Orden.IgnoreAutoIncludes().Where(x => x.Ref == item.FolioSyn).Include(x => x.Producto).Include(x => x.Destino).ThenInclude(x => x.Cliente).Include(x => x.Terminal).FirstOrDefault();

                    precio.Fecha_De_Carga = orden?.Fchcar ?? item.Fchcar;

                    precio.Referencia = orden?.Ref ?? item.FolioSyn;

                    if (orden is not null)
                    {
                        if (orden.Producto is not null)
                            precio.Producto_Synthesis = orden.Producto.Den ?? string.Empty;

                        if (orden.Destino is not null)
                            precio.Destino_Synthesis = orden.Destino.Den ?? string.Empty;

                        if (orden.Terminal is not null)
                            if (!string.IsNullOrEmpty(orden.Terminal.Den))
                            {
                                precio.Terminal_Final = orden.Terminal.Den;
                                if (!string.IsNullOrEmpty(orden.Terminal.Codigo))
                                    precio.Codigo_Terminal_Final = orden.Terminal.Codigo;
                            }

                        precio.BOL = orden.BatchId ?? 0;
                        precio.Volumen_Cargado = orden.Vol;
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
                            if (!string.IsNullOrEmpty(item.Producto.Den))
                                precio.Producto_Original = item.Producto.Den;

                        if (item.Tad is not null)
                        {
                            if (!string.IsNullOrEmpty(item.Tad.Den))
                                precio.Terminal_Original = item.Tad.Den;
                            
                            if (!string.IsNullOrEmpty(item.Tad.Codigo))
                                precio.Codigo_Terminal_Original = item.Tad.Codigo;
                        }
                    }

                    var precioVig = context.Precio.IgnoreAutoIncludes()
                        .Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.Id_Tad == item.Codtad)
                        .OrderByDescending(x => x.FchActualizacion).FirstOrDefault();

                    if (orden is not null)
                        precioVig = context.Precio.IgnoreAutoIncludes()
                        .Where(x => x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd && x.Id_Tad == orden.Id_Tad)
                        .OrderByDescending(x => x.FchActualizacion).FirstOrDefault();

                    var precioPro = context.PrecioProgramado.IgnoreAutoIncludes()
                        .Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.Id_Tad == item.Codtad)
                        .OrderByDescending(x => x.FchActualizacion).FirstOrDefault();

                    if (orden is not null)
                        precioPro = context.PrecioProgramado.IgnoreAutoIncludes()
                        .Where(x => x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd && x.Id_Tad == orden.Id_Tad)
                        .OrderByDescending(x => x.FchActualizacion).FirstOrDefault();

                    var precioHis = context.PreciosHistorico.IgnoreAutoIncludes()
                        .Where(x => item != null && x.CodDes == item.Coddes && x.CodPrd == item.Codprd && x.FchDia <= DateTime.Today && x.Id_Tad == item.Codtad)
                        .OrderByDescending(x => x.FchActualizacion)
                        .FirstOrDefault();

                    if (orden is not null)
                        precioHis = context.PreciosHistorico.IgnoreAutoIncludes()
                        .Where(x => x.CodDes == orden.Coddes && x.CodPrd == orden.Codprd && orden.Fchcar != null && x.FchDia <= orden.Fchcar.Value.Date && x.Id_Tad == orden.Id_Tad)
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

                    if (item != null && precioVig is not null && orden is null || orden is not null && precioVig is not null)
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

                    if (item != null && precioPro is not null && context.PrecioProgramado.Any() || orden is not null && precioPro is not null && context.PrecioProgramado.Any())
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

                    if (item != null && context.OrdenPedido.Any(x => x.CodPed == item.Cod && x.Pedido_Original == 0 && string.IsNullOrEmpty(x.Folio_Cierre_Copia)))
                    {
                        var ordenepedido = context.OrdenPedido.Where(x => x.CodPed == item.Cod && !string.IsNullOrEmpty(x.Folio) && x.Pedido_Original == 0 && string.IsNullOrEmpty(x.Folio_Cierre_Copia)).FirstOrDefault();

                        if (ordenepedido is not null)
                        {
                            var cierre = context.OrdenCierre.Where(x => x.Folio == ordenepedido.Folio
                             && x.CodPrd == item.Codprd).FirstOrDefault();

                            if (cierre is not null)
                            {
                                precio.Precio = cierre.Precio;
                                precio.Fecha_De_Precio = cierre.fchPrecio;
                                precio.Es_Cierre = true;
                                precio.Precio_Encontrado = true;
                                precio.Precio_Encontrado_En = "Cierre";
                                precio.Moneda = precioHis?.Moneda?.Nombre ?? "MXN";
                                precio.Tipo_De_Cambio = precioHis?.Equibalencia ?? 1;
                                precio.Folio_Cierre = cierre.Folio ?? string.Empty;
                            }
                        }
                    }

                    if (item is not null && precioHis is null && precioPro is null && precioVig is null && !precio.Es_Cierre)
                    {
                        precio.Precio = item.Pre;

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

        //[HttpGet("{BOL}")]
        //public ActionResult GetPrecioByBol([FromRoute] int BOL)
        //{
        //    try
        //    {
        //        PrecioBol precios = new PrecioBol();

        //        var ordenes = context.Orden.Where(x => x.BatchId == BOL)
        //            .Include(x => x.Producto)
        //            .Include(x => x.Destino)
        //            .FirstOrDefault();

        //        if (ordenes is null)
        //            return Ok(new PrecioBol());

        //        PrecioBol precio = new PrecioBol();

        //        OrdenEmbarque? orden = new OrdenEmbarque();
        //        orden = context.OrdenEmbarque.Where(x => x.FolioSyn == ordenes.Ref).Include(x => x.Producto).Include(x => x.Destino).ThenInclude(x => x.Cliente).Include(x => x.OrdenCierre).FirstOrDefault();

        //        precio.Fecha_De_Carga = ordenes.Fchcar;

        //        precio.Referencia = ordenes.Ref;

        //        if (orden is not null)
        //        {
        //            if (orden.Producto is not null)
        //                precio.Producto_Original = orden.Producto.Den;

        //            if (orden.Destino is not null)
        //            {
        //                precio.Destino_Original = orden.Destino.Den;
        //                if (orden.Destino.Cliente is not null)
        //                    if (!string.IsNullOrEmpty(orden.Destino.Cliente.Den))
        //                        precio.Cliente_Original = orden.Destino.Cliente.Den;

        //            }
        //        }

        //        precio.BOL = ordenes.BatchId;
        //        precio.Volumen_Cargado = ordenes.Vol;
        //        if (orden is not null)
        //        {
        //            if (orden.Destino is not null)
        //                precio.Destino_Original = orden.Destino.Den ?? "";

        //            if (orden.Producto is not null)
        //                precio.Producto_Original = orden.Producto.Den ?? "";
        //        }

        //        var precioVig = context.Precio.Where(x => ordenes != null && x.CodDes == ordenes.CodDes && x.CodPrd == ordenes.CodPrd)
        //            .OrderByDescending(x => x.FchDia)
        //            .FirstOrDefault();

        //        var precioPro = context.PrecioProgramado.Where(x => ordenes != null && x.CodDes == ordenes.CodDes && x.CodPrd == ordenes.CodPrd)
        //            .OrderByDescending(x => x.FchDia)
        //            .FirstOrDefault();

        //        var precioHis = context.PreciosHistorico.Where(x => ordenes != null && x.CodDes == ordenes.CodDes && x.CodPrd == ordenes.CodPrd
        //            && ordenes.Fchcar != null && x.FchDia <= ordenes.Fchcar.Value.Date)
        //            .OrderByDescending(x => x.FchDia)
        //            .FirstOrDefault();

        //        if (precioHis is not null)
        //        {
        //            precio.Precio = precioHis.pre;
        //            precio.Fecha_De_Precio = precioHis.FchDia;
        //            precio.Precio_Encontrado = true;
        //            precio.Precio_Encontrado_En = "Historial";
        //            precio.Moneda = precioHis?.Moneda?.Nombre;
        //            precio.Tipo_De_Cambio = precioHis?.Equibalencia ?? 1;
        //        }

        //        if (ordenes != null && precioVig is not null && ordenes.Fchcar is not null && ordenes.Fchcar.Value.Date == DateTime.Today)
        //        {
        //            precio.Precio = precioVig.Pre;
        //            precio.Fecha_De_Precio = precioVig.FchDia;
        //            precio.Precio_Encontrado = true;
        //            precio.Precio_Encontrado_En = "Vigente";
        //            precio.Moneda = precioVig?.Moneda?.Nombre;
        //            precio.Tipo_De_Cambio = precioVig?.Equibalencia ?? 1;
        //        }

        //        if (ordenes != null && precioPro is not null && ordenes.Fchcar is not null && ordenes.Fchcar.Value.Date == DateTime.Today && context.PrecioProgramado.Any())
        //        {
        //            precio.Precio = precioPro.Pre;
        //            precio.Fecha_De_Precio = precioPro.FchDia;
        //            precio.Precio_Encontrado = true;
        //            precio.Precio_Encontrado_En = "Programado";
        //            precio.Moneda = precioPro?.Moneda?.Nombre;
        //            precio.Tipo_De_Cambio = precioPro?.Equibalencia ?? 1;
        //        }

        //        if (orden != null && context.OrdenPedido.Any(x => x.CodPed == orden.Cod))
        //        {
        //            var ordenepedido = context.OrdenPedido.Where(x => x.CodPed == orden.Cod && !string.IsNullOrEmpty(x.Folio)).FirstOrDefault();

        //            if (ordenepedido is not null)
        //            {
        //                var cierre = context.OrdenCierre.Where(x => x.Folio == ordenepedido.Folio
        //                 && x.CodPrd == orden.CodPrd).FirstOrDefault();

        //                if (cierre is not null)
        //                {
        //                    precio.Precio = cierre.Precio;
        //                    precio.Fecha_De_Precio = cierre.fchPrecio;
        //                    precio.Es_Cierre = true;
        //                    precio.Precio_Encontrado = true;
        //                    precio.Precio_Encontrado_En = "Cierre";
        //                    precio.Moneda = cierre?.Moneda?.Nombre;
        //                    precio.Tipo_De_Cambio = cierre?.Equibalencia ?? 1;
        //                }
        //            }
        //        }

        //        if (orden is not null && precioHis is null && precioPro is null && precioVig is null && !precio.Es_Cierre)
        //        {
        //            precio.Precio = orden.Pre;

        //            if (orden.OrdenCierre is not null)
        //                precio.Fecha_De_Precio = orden.OrdenCierre.fchPrecio;

        //            precio.Es_Precio_De_Creacion = true;
        //            precio.Precio_Encontrado_En = "Creacion";
        //        }

        //        return Ok(precios);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        public class PrecioBol
        {
            public double? Precio { get; set; } = 0;
            public string? Referencia { get; set; } = string.Empty;
            public int? BOL { get; set; } = 0;
            public DateTime? Fecha_De_Carga { get; set; } = DateTime.MinValue;
            public DateTime? Fecha_De_Precio { get; set; } = DateTime.MinValue;
            public string Destino_Synthesis { get; set; } = string.Empty;
            public string? Destino_Original { get; set; } = string.Empty;
            public string Producto_Synthesis { get; set; } = string.Empty;
            public string? Producto_Original { get; set; } = string.Empty;
            public bool Es_Cierre { get; set; } = false;
            public bool Es_Precio_De_Creacion { get; set; } = false;
            public bool Precio_Encontrado { get; set; } = false;
            public string Precio_Encontrado_En { get; set; } = string.Empty;
            public double Tipo_De_Cambio { get; set; } = 1;
            public string? Moneda { get; set; } = "MXN";
            public string? Cliente_Original { get; set; } = string.Empty;
            public double? Volumen_Cargado { get; set; } = 0;
            public string Folio_Cierre { get; set; } = string.Empty;
            public string Terminal_Original { get; set; } = string.Empty;
            public string Codigo_Terminal_Original { get; set; } = string.Empty;
            public string Terminal_Final { get; set; } = string.Empty;
            public string Codigo_Terminal_Final { get; set; } = string.Empty;
        }
    }
}