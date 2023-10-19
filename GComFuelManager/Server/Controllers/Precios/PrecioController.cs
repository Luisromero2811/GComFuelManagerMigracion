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
using OfficeOpenXml;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
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

        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Convert(IFormFile file)
        {
            try
            {
                if (file == null)
                    return BadRequest("No se pudo leer el archivo enviado.");

                using var stream = new MemoryStream();
                file.CopyTo(stream);

                List<PreciosDTO> precios = new List<PreciosDTO>();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelPackage package = new ExcelPackage();

                package.Load(stream);

                if (package.Workbook.Worksheets.Count > 0)
                {
                    using (ExcelWorksheet worksheet = package.Workbook.Worksheets.First())
                    {
                        for (int r = 2; r < (worksheet.Dimension.End.Row + 1); r++)
                        {
                            PreciosDTO precio = new PreciosDTO();

                            var row = worksheet.Cells[r, 1, r, worksheet.Dimension.End.Column].ToList();

                            if (row.Count == 8)
                            {
                                precio.Producto = row[0].Value?.ToString();
                                precio.Zona = row[1].Value?.ToString();
                                precio.Cliente = row[2].Value?.ToString();
                                precio.Destino = row[3].Value?.ToString();
                                precio.CodSyn = row[4].Value?.ToString();
                                precio.CodTux = row[5].Value?.ToString();
                                precio.Fecha = row[6].Value?.ToString();
                                precio.Precio = Math.Round(double.Parse(row[7].Value?.ToString()), 4);
                                precios.Add(precio);
                            }
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

        [HttpGet]
        public async Task<ActionResult> GetPrecios()
        {
            try
            {
                var precios = await context.Precio
                    .Include(x => x.Zona)
                    .Include(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .ToListAsync();

                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtro")]
        public async Task<ActionResult> GetPreciosFiltro([FromQuery] ParametrosBusquedaPrecios parametros)
        {
            try
            {
                var precios = context.Precio
                    .Include(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Zona)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(parametros.cliente))
                    precios = precios.Where(x => x.Cliente.Den.ToLower().Contains(parametros.cliente.ToLower()));

                if (!string.IsNullOrEmpty(parametros.producto))
                    precios = precios.Where(x => x.Producto.Den.ToLower().Contains(parametros.producto.ToLower()));
                if (!string.IsNullOrEmpty(parametros.destino))
                    precios = precios.Where(x => x.Destino.Den.ToLower().Contains(parametros.destino.ToLower()));
                if (!string.IsNullOrEmpty(parametros.zona))
                    precios = precios.Where(x => x.Zona.Nombre.ToLower().Contains(parametros.zona.ToLower()));

                await HttpContext.InsertarParametrosPaginacion(precios, parametros.tamanopagina, parametros.pagina);

                if (HttpContext.Response.Headers.ContainsKey("pagina"))
                {
                    var pagina = HttpContext.Response.Headers["pagina"];
                    if (pagina != parametros.pagina && !string.IsNullOrEmpty(pagina))
                    {
                        parametros.pagina = int.Parse(pagina);
                    }
                }

                precios = precios.Skip((parametros.pagina - 1) * parametros.tamanopagina).Take(parametros.tamanopagina);

                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("programado/filtro")]
        public async Task<ActionResult> GetPreciosProgramadosFiltro([FromQuery] ParametrosBusquedaPrecios parametros)
        {
            try
            {
                var precios = context.PrecioProgramado
                    .Include(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .Include(x => x.Zona)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(parametros.cliente))
                    precios = precios.Where(x => x.Cliente.Den.ToLower().Contains(parametros.cliente.ToLower()));

                if (!string.IsNullOrEmpty(parametros.producto))
                    precios = precios.Where(x => x.Producto.Den.ToLower().Contains(parametros.producto.ToLower()));
                if (!string.IsNullOrEmpty(parametros.destino))
                    precios = precios.Where(x => x.Destino.Den.ToLower().Contains(parametros.destino.ToLower()));
                if (!string.IsNullOrEmpty(parametros.zona))
                    precios = precios.Where(x => x.Zona.Nombre.ToLower().Contains(parametros.zona.ToLower()));

                await HttpContext.InsertarParametrosPaginacion(precios, parametros.tamanopagina, parametros.pagina);

                if (HttpContext.Response.Headers.ContainsKey("pagina"))
                {
                    var pagina = HttpContext.Response.Headers["pagina"];
                    if (pagina != parametros.pagina && !string.IsNullOrEmpty(pagina))
                    {
                        parametros.pagina = int.Parse(pagina);
                    }
                }

                precios = precios.Skip((parametros.pagina - 1) * parametros.tamanopagina).Take(parametros.tamanopagina);

                //var preciosPre = precios.Select(x => new Precio
                //{
                //    Pre = x.Pre,
                //    Cliente = x.Cliente,
                //    Destino = x.Destino,
                //    Producto = x.Producto,
                //    Zona = x.Zona,
                //    FchDia = x.FchDia,
                //    Activo = x.Activo,
                //    codCte = x.codCte,
                //    cod
                //});

                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Precio precio)
        {
            try
            {
                var acc = 0;
                precio.Producto = null!;
                precio.Zona = null!;
                precio.Destino = null!;
                precio.Cliente = null!;
                precio.FchActualizacion = DateTime.Now;

                if (precio.Cod != null)
                {
                    context.Update(precio);
                    acc = 6;
                }
                else
                {
                    if (context.Precio.Any(x => x.codDes == precio.codDes && x.codCte == precio.codCte && x.codPrd == precio.codPrd && x.codZona == precio.codZona))
                        return BadRequest("El destino ya cuenta con un precio asignado para ese producto.");

                    context.Add(precio);
                    acc = 3;
                }
                var precioH = new PrecioHistorico
                {
                    Cod = null!,
                    pre = precio.Pre,
                    codCte = precio?.codCte,
                    codDes = precio?.codDes,
                    codGru = precio?.codGru,
                    codPrd = precio?.codPrd,
                    codZona = precio?.codZona,
                    FchDia = precio.FchDia,
                    FchActualizacion = precio.FchActualizacion
                };

                context.Add(precioH);
                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();
                await context.SaveChangesAsync(id, acc);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("productos/{folio?}")]
        public async Task<ActionResult> GetPrecios([FromBody] ZonaCliente? zonaCliente, [FromRoute] string? folio)
        {
            try
            {
                List<Precio> precios = new List<Precio>();
                List<PrecioProgramado> preciosPro = new List<PrecioProgramado>();
                var LimiteDate = DateTime.Today.AddHours(16);

                if (!string.IsNullOrEmpty(folio))
                {
                    var ordenes = await context.OrdenCierre.Where(x => x.Folio == folio)
                            .Include(x => x.Cliente)
                            .ToListAsync();
                    var ordenesUnic = ordenes.DistinctBy(x => x.CodPrd).Select(x => x);

                    foreach (var item in ordenesUnic)
                    {
                        var zona = context.ZonaCliente.FirstOrDefault(x => x.CteCod == item.CodCte);
                        Precio precio = new Precio()
                        {
                            Pre = item.Precio,
                            codCte = item.CodCte,
                            codDes = item.CodDes,
                            codPrd = item.CodPrd,
                            codGru = item.Cliente?.codgru,
                            codZona = zona?.CteCod,
                            Producto = context.Producto.FirstOrDefault(x => x.Cod == item.CodPrd)
                        };
                        precios.Add(precio);
                    }
                    return Ok(precios);
                }

                precios = await context.Precio.Where(x => x.codCte == zonaCliente.CteCod
                    && x.codDes == zonaCliente.DesCod && x.Activo == true)
                    //&& x.codZona == zona.ZonaCod)
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
                    preciosPro = await context.PrecioProgramado.Where(x => x.codCte == zonaCliente.CteCod
                    && x.codDes == zonaCliente.DesCod && x.Activo == true)
                    //&& x.codZona == zona.ZonaCod)
                    .Include(x => x.Producto)
                    .ToListAsync();

                    foreach (var item in preciosPro)
                    {
                        if (item.FchDia > DateTime.Today)
                        {
                            precios.FirstOrDefault(x => x.codDes == item.codDes && x.codCte == item.codCte && x.codPrd == item.codPrd).Pre = item.Pre;
                            precios.FirstOrDefault(x => x.codDes == item.codDes && x.codCte == item.codCte && x.codPrd == item.codPrd).FchDia = item.FchDia;
                            var pre = precios.FirstOrDefault(x => x.codDes == item.codDes && x.codCte == item.codCte && x.codPrd == item.codPrd);
                            if (pre is null)
                            {
                                precios.Add(new Precio()
                                {
                                    Pre = item.Pre,
                                    codCte = item.codCte,
                                    codDes = item.codDes,
                                    codPrd = item.codPrd,
                                    codGru = item.Cliente?.codgru,
                                    Producto = context.Producto.FirstOrDefault(x => x.Cod == item.codPrd)
                                });
                            }
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

        [HttpPost("confirm/upload")]
        public async Task<ActionResult> PostPrecioDTO([FromBody] List<PreciosDTO> precios)
        {
            try
            {
                List<PrecioProgramado> prec = new List<PrecioProgramado>();
                foreach (var item in precios)
                {
                    var codcte = string.IsNullOrEmpty(item.Cliente) ? string.Empty : item.Cliente;
                    var cliente = context.Cliente.FirstOrDefault(x => x.Den!.Replace("\"", "").Equals(codcte));
                    if (cliente is null)
                        return BadRequest($"No se encontro el cliente {item.Cliente}");

                    var codprd = string.Empty;
                    var arrprd = item.Producto.Split(" ");
                    if (arrprd.Count() > 1)
                        codprd = arrprd[0];
                    else
                        codprd = item.Producto;

                    var producto = context.Producto.FirstOrDefault(x => x.Den!.Contains(codprd));
                    if (producto is null)
                        return BadRequest($"No se encontro el producto {item.Producto}");

                    var codzona = string.IsNullOrEmpty(item.Zona) ? "Sin Zona" : item.Zona;
                    var zona = context.Zona.FirstOrDefault(x => x.Nombre.Equals(codzona));

                    var coddes = string.IsNullOrEmpty(item.CodSyn) ? string.Empty : item.CodSyn;
                    var destino = context.Destino.FirstOrDefault(x => x.Codsyn == coddes);
                    if (destino is null)
                        return BadRequest($"No se encontro el destino {item.Destino} synthesis:{item.CodSyn} tuxpan {item.CodTux}");

                    if (item.Precio == null || item.Precio == 0)
                        return BadRequest($"El destino {destino.Den} no tiene un precio con valor");

                    if (DateTime.Parse(item.Fecha) > DateTime.Today)
                    {
                        var precio = new PrecioProgramado
                        {
                            codCte = cliente.Cod,
                            codDes = destino.Cod,
                            codGru = cliente.codgru,
                            codPrd = producto.Cod,
                            codZona = zona.Cod,
                            FchDia = DateTime.Parse(item.Fecha),
                            FchActualizacion = DateTime.Now,
                            Pre = item.Precio
                        };

                        var p = context.PrecioProgramado.FirstOrDefault(x => x.codGru == precio.codGru
                        //&& x.codZona == precio.codZona
                        && x.codCte == precio.codCte
                        && x.codPrd == precio.codPrd
                        && x.codDes == precio.codDes);

                        if (p is not null)
                        {
                            p.Pre = precio.Pre;
                            p.FchDia = precio.FchDia;
                            p.FchActualizacion = DateTime.Now;
                            context.Update(p);
                        }
                        else
                            context.Add(precio);
                        //prec.Add(precio);
                    }
                    else if (DateTime.Parse(item.Fecha) == DateTime.Today)
                    {
                        var precio = new Precio
                        {
                            codCte = cliente.Cod,
                            codDes = destino.Cod,
                            codGru = cliente.codgru,
                            codPrd = producto.Cod,
                            codZona = zona.Cod,
                            FchDia = DateTime.Parse(item.Fecha),
                            FchActualizacion = DateTime.Now,
                            Pre = item.Precio
                        };

                        var p = context.Precio.FirstOrDefault(x => x.codGru == precio.codGru
                        //&& x.codZona == precio.codZona
                        && x.codCte == precio.codCte
                        && x.codPrd == precio.codPrd
                        && x.codDes == precio.codDes
                        && x.FchDia == precio.FchDia);

                        if (p is not null)
                        {
                            p.Pre = precio.Pre;
                            p.FchActualizacion = DateTime.Now;

                            context.Update(p);
                        }
                        else
                            context.Add(precio);

                        var precioH = new PrecioHistorico
                        {
                            Cod = null!,
                            pre = precio.Pre,
                            codCte = precio.codCte == null ? 0 : precio.codCte,
                            codDes = precio.codDes == null ? 0 : precio.codDes!,
                            codGru = precio.codGru == null ? (short)0 : (short)precio.codGru!,
                            codPrd = precio.codPrd == null ? 0 : precio.codPrd,
                            codZona = precio.codZona == null ? 0 : precio.codZona,
                            FchDia = precio.FchDia,
                            FchActualizacion = precio.FchActualizacion
                        };

                        context.Add(precioH);
                    }
                }

                //if (prec.Count > 0)
                //{
                //    context.PrecioProgramado.ExecuteDelete();
                //    context.AddRange(prec);
                //}

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 8);

                List<Destino> destinos = new List<Destino>();
                List<PreciosDTO> destinosSinPre = new List<PreciosDTO>();
                destinos = context.Destino.ToList();
                foreach (var item in destinos)
                    if (!context.PrecioProgramado.Any(x => x.codDes == item.Cod))
                    {
                        PreciosDTO dTO = new PreciosDTO()
                        {
                            Destino = item.Den,
                            Cliente = item.Cliente?.Den,
                            CodSyn = item.Codsyn,
                            CodTux = item.CodGamo.ToString()
                        };
                        destinosSinPre.Add(dTO);
                    }

                return Ok(destinosSinPre);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost("historial")]
        public async Task<ActionResult> GetDateHistorialPrecio([FromBody] FechasF fechas)
        {
            try
            {
                List<PrecioHistorico> precios = new List<PrecioHistorico>();

                precios = await context.PreciosHistorico
                    .Where(x => x.FchDia >= fechas.DateInicio && x.FchDia <= fechas.DateFin)
                    .Include(x => x.Destino)
                    .Include(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Zona)
                    .OrderBy(x => x.FchDia)
                    .ToListAsync();
                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("programado")]
        public async Task<ActionResult> GetDateHistorialPrecioProgramado()
        {
            try
            {
                var fchInicio = DateTime.Today.AddHours(1).AddMinutes(50);
                var fchHoy = DateTime.Now;
                var fchFin = DateTime.Today.AddHours(2).AddMinutes(20);
                List<Precio> preciosDia = new List<Precio>();
                List<PrecioHistorico> precioHistoricos = new List<PrecioHistorico>();
                //if (fchInicio <= fchHoy && fchFin >= fchHoy)
                //{
                List<PrecioProgramado> precios = new List<PrecioProgramado>();
                precios = context.PrecioProgramado.Where(x => x.FchDia == DateTime.Today).ToList();

                if (precios.Count > 0)
                {
                    foreach (var item in precios)
                    {
                        var precio = context.Precio.FirstOrDefault(x => x.codCte == item.codCte && x.codDes == item.codDes && x.codPrd == item.codPrd && x.Activo == true);
                        if (precio is null)
                        {
                            var precioN = new Precio
                            {
                                codCte = item.codCte,
                                codDes = item.codDes,
                                codGru = item?.codGru,
                                codPrd = item.codPrd,
                                codZona = item?.codZona,
                                FchDia = item.FchDia,
                                FchActualizacion = DateTime.Now,
                                Pre = item.Pre
                            };
                            preciosDia.Add(precioN);
                            //context.Add(precioN);
                        }
                        else
                        {
                            precio.Pre = item.Pre;
                            precio.FchDia = item.FchDia;
                            precio.FchActualizacion = DateTime.Now;
                            context.Update(precio);
                        }

                        var precioH = new PrecioHistorico
                        {
                            Cod = null!,
                            pre = item.Pre,
                            codCte = item.codCte,
                            codDes = item.codDes,
                            codGru = item?.codGru,
                            codPrd = item.codPrd,
                            codZona = item.codZona,
                            FchDia = item.FchDia,
                            FchActualizacion = item.FchActualizacion
                        };
                        precioHistoricos.Add(precioH);
                        //context.Add(precioH);
                    }
                    await context.AddRangeAsync(preciosDia);
                    await context.AddRangeAsync(precioHistoricos);

                    await context.SaveChangesAsync();

                    context.RemoveRange(precios);
                    await context.SaveChangesAsync();
                }
                return Ok(preciosDia);
                //}

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("programados/lista")]
        public async Task<ActionResult> GetPreciosProgramados()
        {
            try
            {
                var precios = await context.PrecioProgramado
                    .Where(x => x.FchDia > DateTime.Today)
                    .Include(x => x.Zona)
                    .Include(x => x.Cliente)
                    .Include(x => x.Producto)
                    .Include(x => x.Destino)
                    .ToListAsync();

                return Ok(precios);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("programado")]
        public async Task<ActionResult> PostProgramado([FromBody] Precio precio)
        {
            try
            {
                precio.Producto = null!;
                precio.Zona = null!;
                precio.Destino = null!;
                precio.Cliente = null!;
                precio.FchActualizacion = DateTime.Now;

                var precioPro = new PrecioProgramado
                {
                    Cod = precio.Cod,
                    codCte = precio.codCte,
                    codDes = precio.codDes,
                    codGru = precio.codGru,
                    codPrd = precio.codPrd,
                    codZona = precio.codZona,
                    FchDia = precio.FchDia,
                    FchActualizacion = DateTime.Now,
                    Pre = precio.Pre,
                    Activo = precio.Activo
                };

                if (precio.Cod != null)
                    context.Update(precioPro);
                else
                {
                    if (context.Precio.Any(x => x.codDes == precio.codDes && x.codCte == precio.codCte && x.codPrd == precio.codPrd && x.FchDia == precio.FchDia && x.codZona == precio.codZona))
                        return BadRequest("El destino ya cuenta con un precio asignado para ese producto.");

                    context.Add(precioPro);
                }

                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}