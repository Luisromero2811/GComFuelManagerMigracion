﻿using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static GComFuelManager.Server.Controllers.Precios.PrecioController;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Administrador")]
    public class VendedorController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserId verifyUser;

        public VendedorController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserId verifyUser)
        {
            this.context = context;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
        }

        [HttpGet("anos/reporte")]
        public ActionResult Obtener_Años_Disponbles()
        {
            try
            {
                var Fechas_Diponibles = context.Orden.Where(x => x.Fch != null).Select(x => x.Fch.Value.Year);
                var años_ordenados = Fechas_Diponibles.Order();
                var años = Fechas_Diponibles.Distinct();
                return Ok(años);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public ActionResult Obtener_Vendedores([FromQuery] Vendedor vendedor)
        {
            try
            {
                var vendedores = context.Vendedores.Include(x => x.Originadores).IgnoreAutoIncludes().OrderBy(x => x.Nombre).AsQueryable();

                if (!string.IsNullOrEmpty(vendedor.Nombre))
                    vendedores = vendedores.Where(x => x.Nombre.ToLower().Contains(vendedor.Nombre.ToLower())).OrderBy(x => x.Nombre);

                if (!string.IsNullOrEmpty(vendedor.Nombre_Originador) && vendedor.Originadores is not null)
                    vendedores = vendedores.Where(x => x.Originadores.Any(x => x.Nombre.ToLower().Contains(vendedor.Nombre_Originador.ToLower())));

                return Ok(vendedores);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtrar")]
        public ActionResult Obtener_Vendedores_Filtrados([FromQuery] Vendedor vendedor)
        {
            try
            {
                var vendedores = context.Vendedores.Where(x => x.Activo).OrderBy(x => x.Nombre).IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(vendedor.Nombre))
                    vendedores = vendedores.Where(x => x.Nombre.ToLower().Contains(vendedor.Nombre.ToLower())).OrderBy(x => x.Nombre);

                return Ok(vendedores);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("clientes")]
        public ActionResult Obtener_Clientes_De_Vendedores_Filtrados([FromQuery] Cliente cliente)
        {
            try
            {
                var vendedores = context.Cliente.IgnoreAutoIncludes().Where(x => x.Activo && x.Id_Vendedor == cliente.Id_Vendedor).Include(x => x.Originador).OrderBy(x => x.Den).AsQueryable();

                if (!string.IsNullOrEmpty(cliente.Den) || !string.IsNullOrWhiteSpace(cliente.Den))
                    vendedores = vendedores.Where(x => (!string.IsNullOrEmpty(x.Den) || !string.IsNullOrWhiteSpace(x.Den)) && x.Den.ToLower().Contains(cliente.Den.ToLower())).OrderBy(x => x.Den);

                return Ok(vendedores);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Guardar_Vendedores([FromBody] Vendedor vendedor)
        {
            try
            {
                if (vendedor is null)
                    return NotFound();

                if (string.IsNullOrEmpty(vendedor.Nombre) || string.IsNullOrWhiteSpace(vendedor.Nombre))
                    return BadRequest("Nombre de vendedor no valido");

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                if (vendedor.Id != 0)
                {
                    //vendedor.Vendedor_Originador = null!;
                    vendedor.Originadores = null!;

                    context.Update(vendedor);
                    await context.SaveChangesAsync(id, 38);
                }
                else
                {
                    context.Add(vendedor);
                    await context.SaveChangesAsync(id, 37);

                    if (vendedor.Id_Originador != 0)
                    {
                        Vendedor_Originador vendedor_Originador = new()
                        {
                            VendedorId = vendedor.Id,
                            OriginadorId = vendedor.Id_Originador
                        };

                        context.Add(vendedor_Originador);
                        await context.SaveChangesAsync(id, 41);
                    }

                    if (!context.Metas_Vendedor.Any(x => x.VendedorId == vendedor.Id && x.Mes.Year == DateTime.Today.Year))
                    {
                        for (int i = 1; i <= 12; i++)
                        {
                            Metas_Vendedor metas_Vendedor = new()
                            {
                                VendedorId = vendedor.Id,
                                Mes = new DateTime(DateTime.Today.Year, i, 1)
                            };

                            if (!context.Metas_Vendedor.Any(x => x.Mes.Month == metas_Vendedor.Mes.Month && x.VendedorId == vendedor.Id && x.Mes.Year == metas_Vendedor.Mes.Year))
                            {
                                context.Add(metas_Vendedor);
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("relacionar/cliente")]
        public async Task<ActionResult> Guardar_Relacion_Cliente_Vendedor([FromBody] List<Cliente> clientes, [FromQuery] Vendedor vendedor)
        {
            try
            {
                if (clientes is null)
                    return NotFound();

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                foreach (var cliente in clientes)
                {
                    var cliente_buscado = context.Cliente.FirstOrDefault(x => x.Cod == cliente.Cod);
                    if (cliente_buscado is not null)
                    {
                        cliente_buscado.Id_Vendedor = vendedor.Id;
                        cliente_buscado.Id_Originador = vendedor.Id_Originador;
                        context.Update(cliente_buscado);
                        await context.SaveChangesAsync(id, 38);
                    }
                }
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("borrar/relacion/cliente")]
        public async Task<ActionResult> Guardar_Relacion_Vendeor_Originador([FromQuery] Cliente cliente)
        {
            try
            {
                if (cliente is null)
                    return NotFound();

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                var cliente_encontrado = context.Cliente.Find(cliente.Cod);

                if (cliente_encontrado is not null)
                {
                    if (cliente_encontrado.Id_Vendedor == cliente.Id_Vendedor)
                    {
                        cliente_encontrado.Id_Vendedor = null!;
                        cliente_encontrado.Id_Originador = null!;
                        context.Update(cliente_encontrado);
                        await context.SaveChangesAsync(id, 42);
                        return Ok();
                    }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("relacionar/originador")]
        public async Task<ActionResult> Guardar_Relacion_Vendeor_Originador([FromQuery] Vendedor_Originador vendedor_Originador)
        {
            try
            {
                if (vendedor_Originador is null)
                    return NotFound();

                if (vendedor_Originador.OriginadorId == 0)
                    return BadRequest("Originador no valido");

                if (vendedor_Originador.VendedorId == 0)
                    return BadRequest("Vendedor no valido");

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                if (vendedor_Originador.Borrar)
                    context.Remove(vendedor_Originador);
                else
                    context.Add(vendedor_Originador);


                await context.SaveChangesAsync(id, 41);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("reporte")]
        public ActionResult Obtener_Venta_De_Meses_Por_Vendedor([FromQuery] Vendedor vendedor)
        {
            try
            {
                var vendedores = context.Vendedores.IgnoreAutoIncludes().Where(x => x.Activo).Include(x => x.Vendedor_Originador).IgnoreAutoIncludes().Include(x => x.Clientes).IgnoreAutoIncludes()
                    .OrderBy(x => x.Nombre).AsQueryable();

                if (!string.IsNullOrEmpty(vendedor.Nombre))
                    vendedores = vendedores.Where(x => x.Nombre.ToLower().Contains(vendedor.Nombre.ToLower())).OrderBy(x => x.Nombre);

                if (vendedor.Id != 0)
                    vendedores = vendedores.Where(x => x.Id == vendedor.Id).OrderBy(x => x.Nombre);

                if (vendedor.Id_Originador != 0)
                    vendedores = vendedores.Where(x => x.Vendedor_Originador != null && x.Vendedor_Originador.Any(x => x.OriginadorId == vendedor.Id_Originador)).OrderBy(x => x.Nombre);

                List<Vendedor> Vendedores_Validos = vendedores.ToList();

                var meses = CultureInfo.CurrentCulture.Calendar.GetMonthsInYear(vendedor.Fecha_Registro);

                foreach (var vendedor_valido in Vendedores_Validos)
                {
                    for (int mes = 1; mes <= meses; mes++)
                    {
                        Mes_Venta mes_Venta = new()
                        {
                            Nro_Mes = mes,
                            Nombre_Mes = new DateTime(DateTime.Today.Year, mes, 1).ToString("MMM")
                        };

                        if (vendedor_valido.Clientes is not null)
                        {

                            List<Cliente> clientes_validos = new();

                            if (vendedor.Id_Originador != 0)
                                clientes_validos = vendedor_valido.Clientes.Where(x => x.Id_Originador == vendedor.Id_Originador).ToList();
                            else
                                clientes_validos = vendedor_valido.Clientes;

                            foreach (var cliente in clientes_validos)
                            {
                                List<Orden> Ordenes = context.Orden.IgnoreAutoIncludes().Where(x => x.Destino != null && x.Destino.Codcte == cliente.Cod
                                && x.Fchcar != null && x.Fchcar.Value.Month == mes && x.Fchcar.Value.Year == vendedor.Fecha_Registro && x.Codest != 14)
                                .Include(x => x.Producto)
                                .Include(x => x.Destino)
                                .Include(x => x.OrdenEmbarque)
                                .ToList();

                                List<Orden> ordenes_a_sumar = new();

                                foreach (var orden in Ordenes)
                                    if (!ordenes_a_sumar.Any(x => x.Ref == orden.Ref && x.Bolguiid != orden.Bolguiid))
                                        ordenes_a_sumar.Add(orden);

                                //var ordenes_dintinguidas = Ordenes.DistinctBy(x => x.Liniteid);
                                var ordenes_dintinguidas = ordenes_a_sumar;

                                foreach (var orden in ordenes_dintinguidas)
                                {
                                    //Mes_Venta_Producto mes_Venta_Producto = new()
                                    //{
                                    //    Producto = orden.Obtener_Nombre_Producto,
                                    //    Litros_Vendidos = orden.Obtener_Volumen,
                                    //    Venta = orden.Obtener_Precio_Orden_Embarque * orden.Obtener_Volumen
                                    //};

                                    //mes_Venta.Litros_Vendidos += mes_Venta_Producto.Litros_Vendidos;
                                    //mes_Venta.Venta += mes_Venta_Producto.Venta;

                                    mes_Venta.Litros_Vendidos += orden.Obtener_Volumen;
                                    mes_Venta.Venta += orden.Obtener_Precio_Orden_Embarque * orden.Obtener_Volumen;

                                    //mes_Venta.Mes_Venta_Productos.Add(mes_Venta_Producto);
                                }
                            }
                        }
                        vendedor_valido.Venta_Por_Meses.Add(mes_Venta);
                    }
                }

                return Ok(Vendedores_Validos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("reporte/originador")]
        public ActionResult Obtener_Venta_De_Meses_Por_Originador([FromQuery] Vendedor originador)
        {
            try
            {
                var originadores = context.Originadores.IgnoreAutoIncludes().Where(x => x.Activo).Include(x => x.Clientes).OrderBy(x => x.Nombre).AsQueryable();

                if (!string.IsNullOrEmpty(originador.Nombre))
                    originadores = originadores.Where(x => x.Nombre.ToLower().Contains(originador.Nombre.ToLower())).OrderBy(x => x.Nombre);

                if (originador.Id_Originador != 0)
                    originadores = originadores.Where(x => x.Id == originador.Id_Originador).OrderBy(x => x.Nombre);

                var meses = CultureInfo.CurrentCulture.Calendar.GetMonthsInYear(originador.Fecha_Registro);

                var originadores_validos = originadores.ToList();

                foreach (var item in originadores_validos)
                {
                    for (int mes = 1; mes <= meses; mes++)
                    {
                        Mes_Venta mes_Venta = new()
                        {
                            Nro_Mes = mes,
                            Nombre_Mes = new DateTime(DateTime.Today.Year, mes, 1).ToString("MMM")
                        };

                        foreach (var cliente in item.Clientes)
                        {

                            List<Orden> Ordenes = context.Orden.IgnoreAutoIncludes().Where(x => x.Destino != null && x.Destino.Codcte == cliente.Cod
                                    && x.Fchcar != null && x.Fchcar.Value.Month == mes && x.Fchcar.Value.Year == originador.Fecha_Registro && x.Codest != 14)
                                    .Include(x => x.Producto)
                                    .Include(x => x.Destino)
                                    .Include(x => x.OrdenEmbarque)
                                    .ToList();

                            List<Orden> ordenes_a_sumar = new();

                            foreach (var orden in Ordenes)
                                if (!ordenes_a_sumar.Any(x => x.Ref == orden.Ref && x.Bolguiid != orden.Bolguiid))
                                    ordenes_a_sumar.Add(orden);

                            //var ordenes_dintinguidas = Ordenes.DistinctBy(x => x.Liniteid);
                            var ordenes_dintinguidas = ordenes_a_sumar;

                            foreach (var orden in ordenes_dintinguidas)
                            {
                                //Mes_Venta_Producto mes_Venta_Producto = new()
                                //{
                                //    Producto = orden.Obtener_Nombre_Producto,
                                //    Litros_Vendidos = orden.Obtener_Volumen,
                                //    Venta = orden.Obtener_Precio_Orden_Embarque * orden.Obtener_Volumen
                                //};

                                //mes_Venta.Litros_Vendidos += mes_Venta_Producto.Litros_Vendidos;
                                //mes_Venta.Venta += mes_Venta_Producto.Venta;

                                mes_Venta.Litros_Vendidos += orden.Obtener_Volumen;
                                mes_Venta.Venta += (orden.Obtener_Precio_Orden_Embarque * orden.Obtener_Volumen);

                                //mes_Venta.Mes_Venta_Productos.Add(mes_Venta_Producto);
                            }

                        }
                        item.Venta_Por_Meses.Add(mes_Venta);
                    }
                }


                return Ok(originadores_validos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}