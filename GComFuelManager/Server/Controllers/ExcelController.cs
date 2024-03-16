using Azure;
using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System.Dynamic;
using System.Globalization;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ExcelController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly VerifyUserId verifyUser;

        public ExcelController(ApplicationDbContext context, UserManager<IdentityUsuario> userManager, VerifyUserId verifyUser)
        {
            this.context = context;
            this.verifyUser = verifyUser;
        }

        [HttpPost("reporte/volumen/cierre")]
        public ActionResult Obtener_Reporte_Volumen_Cierres([FromBody] Folio_Activo_Vigente datos)
        {
            try
            {
                if (datos is null)
                    return BadRequest("No se admiten parametros vacios");

                List<FolioCierreDTO> DTO = new List<FolioCierreDTO>();
                if (datos.OrdenCierres is not null)
                {
                    foreach (var item in datos.OrdenCierres)
                    {
                        DTO.Add(new FolioCierreDTO()
                        {
                            Grupo = item.Grupo,
                            cliente = item.Cliente,
                            destino = item.Destino,
                            Producto = item.Producto,
                            FchCierre = item.FchCierre,
                            FchCierre_Vencimiento = item.FchVencimiento,
                            Volumen_Cosumido = item.Volumen_Cosumido,
                            Volumen = item.Volumen,
                            Volumen_Disponible = item.Volumen_Disponible,
                            Volumen_Programado = item.Volumen_Programado,
                            Volumen_Restante = item.Volumen - item.Volumen_Cosumido,
                            Volumen_Espera_Carga = item.Volumen_Espera_Carga,
                            Folio = item.Folio,
                            Observaciones = item.Observaciones,
                            Precio = item.Precio,
                            Tipo_Venta = item.TipoPago,
                            Activa = item.Activa,
                            Estatus = item.Estatus,
                            Terminales = item.Terminal.Den
                        });
                    }
                }

                return Ok(DTO);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("reporte/detalle/cierre")]
        public ActionResult Obtener_Reporte_Detalle_Cierre([FromBody] List<OrdenCierre> datos)
        {
            try
            {
                if (datos is null)
                    return BadRequest("No se admiten valores vacios");

                List<DetalleCierreDTO> detalleCierres = new List<DetalleCierreDTO>();

                foreach (var item in datos)
                {
                    detalleCierres.Add(new DetalleCierreDTO
                    {
                        OC = item.Folio,
                        FolioReferencia = item?.OrdenEmbarque?.Folio.ToString(),
                        Precio = item.Precio.ToString(),
                        Volumen = item?.OrdenEmbarque?.Orden is not null ? item?.OrdenEmbarque?.Orden?.Vol
                                    : item?.OrdenEmbarque?.Compartment == 1 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom?.ToString())
                                    : item?.OrdenEmbarque?.Compartment == 2 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom2?.ToString())
                                    : item?.OrdenEmbarque?.Compartment == 3 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom3?.ToString())
                                    : item?.OrdenEmbarque?.Compartment == 4 && item.OrdenEmbarque?.Tonel is not null ? double.Parse(item?.OrdenEmbarque?.Tonel?.Capcom4?.ToString())
                                    : item?.OrdenEmbarque?.Vol,
                        Observaciones = item?.Observaciones,
                        FchCierre = item?.FchCierre?.ToString("dd/MM/yyyy"),
                        Destino = item?.Destino?.Den,
                        Producto = item?.Producto?.Den,
                        BOL = item?.OrdenEmbarque?.Orden?.BatchId.ToString(),
                        Unidad = item?.OrdenEmbarque?.Tonel?.Veh,
                        Estatus = item?.OrdenEmbarque?.Orden != null ? item.OrdenEmbarque?.Orden?.Estado?.den : item?.OrdenEmbarque?.Estado?.den,
                        FchLlegada = item?.FechaLlegada
                    });
                }

                return Ok(detalleCierres);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("reporte/ordenes/pedimento")]
        public ActionResult Obtener_Reporte_Ordenes_Pedimento([FromBody] List<OrdenEmbarque> datos)
        {
            try
            {
                if (datos is null)
                    return BadRequest("No se aceptan valores vacios");

                List<Excel_Ordenes_Pedimento> Datos_Excel = new();

                foreach (var item in datos)
                {
                    Datos_Excel.Add(new Excel_Ordenes_Pedimento()
                    {
                        Referencia = item.FolioSyn ?? string.Empty,
                        BOL = item.Orden?.BatchId,
                        Precio = item.Pre ?? 0,
                        Costo = item.Costo,
                        Volumen_Cargado = item.Obtener_Volumen_De_Orden(),
                        Estado = item.Obtener_Estado_De_Orden,
                        Fecha_Carga = item?.Orden?.Fchcar?.ToString("d"),
                        Cliente = item?.Obtener_Cliente_De_Orden,
                        Destino = item?.Obtener_Destino_De_Orden,
                        Producto = item?.Obtener_Producto_De_Orden,
                        Fecha_Programa = item?.Fchcar?.ToString("d"),
                        Utilidad = item?.Obtener_Utilidad_Coste(),
                        Utilidad_Sobre_Volumen = item?.Obtener_Utilidad_Sobre_Volumen()
                    });
                }

                return Ok(Datos_Excel);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost("reporte/redireccion")]
        public ActionResult Obtener_Reporte_Redireccion([FromBody] List<Redireccionamiento> redirecciones)
        {
            try
            {
                if (redirecciones is null)
                    return BadRequest();

                List<Redireccion_Excel> datos = new();

                foreach (var item in redirecciones)
                {
                    datos.Add(new()
                    {
                        BOL = item.Bol_Orden,
                        Cliente_Original = item.Nombre_Cliente_Origibal,
                        Cliente_Red = item.Nombre_Cliente,
                        Destino_Original = item.Nombre_Destino_Original,
                        Destino_Red = item.Nombre_Destino,
                        Fecha_Redireccion = item.Fecha_Red.ToShortDateString(),
                        Motivo = item.Motivo_Red,
                        Producto = item?.Orden?.Producto?.Nombre_Producto ?? string.Empty,
                        Precio_redireccion = item?.Precio_Red,
                        Precio = item?.Orden?.Obtener_Precio_Original
                    });
                }

                return Ok(datos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("reporte/ventas/desempeno")]
        public ActionResult Obtener_Reporte_Desempeño_De_Ventas([FromBody] List<Vendedor> vendedores)
        {
            try
            {
                Reporte_Completo_Vendedor_Desempeño reporte_completo = new();
                List<int> numero_meses = new();

                List<ExpandoObject> Meses_Dinamicos = new();

                //dynamic totales_litros = new ExpandoObject();
                //dynamic totales_ventas = new ExpandoObject();

                //totales_litros.Vendedor = "Total";
                //totales_ventas.Vendedor = "Total";

                foreach (var vendedor in vendedores)
                {
                    dynamic litros = new ExpandoObject();
                    dynamic venta = new ExpandoObject();

                    foreach (var mes in vendedor.Venta_Por_Meses)
                    {

                        litros.Vendedor = vendedor.Nombre;
                        venta.Vendedor = vendedor.Nombre;

                        switch (mes.Nro_Mes)
                        {
                            case 1:
                                litros.Ene = mes.Litros_Vendidos;
                                venta.Ene = mes.Venta;
                                break;
                            case 2:
                                litros.Feb = mes.Litros_Vendidos;
                                venta.Feb = mes.Venta;
                                break;
                            case 3:
                                litros.Mar = mes.Litros_Vendidos;
                                venta.Mar = mes.Venta;
                                break;
                            case 4:
                                litros.Abr = mes.Litros_Vendidos;
                                venta.Abr = mes.Venta;
                                break;
                            case 5:
                                litros.May = mes.Litros_Vendidos;
                                venta.May = mes.Venta;
                                break;
                            case 6:
                                litros.Jun = mes.Litros_Vendidos;
                                venta.Jun = mes.Venta;
                                break;
                            case 7:
                                litros.Jul = mes.Litros_Vendidos;
                                venta.Jul = mes.Venta;
                                break;
                            case 8:
                                litros.Ago = mes.Litros_Vendidos;
                                venta.Ago = mes.Venta;
                                break;
                            case 9:
                                litros.Sep = mes.Litros_Vendidos;
                                venta.Sep = mes.Venta;
                                break;
                            case 10:
                                litros.Oct = mes.Litros_Vendidos;
                                venta.Oct = mes.Venta;
                                break;
                            case 11:
                                litros.Nov = mes.Litros_Vendidos;
                                venta.Nov = mes.Venta;
                                break;
                            case 12:
                                litros.Dic = mes.Litros_Vendidos;
                                venta.Dic = mes.Venta;
                                break;

                            default: break;
                        }
                    }

                    reporte_completo.Litros.Add(litros);
                    reporte_completo.Venta.Add(venta);
                }

                foreach (var item in reporte_completo.Litros)
                {
                    var Litros_Convertidos = new Dictionary<string, object>();
                    foreach (var entry in item)
                    {

                        if (entry.Value is not null)
                        {
                            if (double.TryParse(entry.Value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double convertRow))
                            {
                                Litros_Convertidos[entry.Key] = Math.Round(convertRow, 2);
                            }
                            else
                            {
                                Litros_Convertidos[entry.Key] = entry.Value;
                            }
                        }
                    }
                    reporte_completo.Diccionario_Litros.Add(Litros_Convertidos);
                }

                foreach (var item in reporte_completo.Venta)
                {
                    var Ventas_Convertidas = new Dictionary<string, object>();
                    foreach (var entry in item)
                    {
                        if (entry.Value is not null)
                        {
                            if (double.TryParse(entry.Value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double convertRow))
                            {
                                Ventas_Convertidas[entry.Key] = Math.Round(convertRow, 4);
                            }
                            else
                            {
                                Ventas_Convertidas[entry.Key] = entry.Value;
                            }
                        }
                    }
                    reporte_completo.Diccionario_Ventas.Add(Ventas_Convertidas);
                }

                ExcelPackage.LicenseContext = LicenseContext.Commercial;
                var excel = new ExcelPackage();
                var ws_l = excel.Workbook.Worksheets.Add("Litros");
                var ws_v = excel.Workbook.Worksheets.Add("Ventas");

                var table_l = ws_l.Cells["A1"].LoadFromDictionaries(reporte_completo.Diccionario_Litros, c =>
                {
                    c.PrintHeaders = true;
                    c.TableStyle = TableStyles.Medium2;
                });

                var table_v = ws_v.Cells["A1"].LoadFromDictionaries(reporte_completo.Diccionario_Ventas, c =>
                {
                    c.PrintHeaders = true;
                    c.TableStyle = TableStyles.Medium2;
                });



                if (reporte_completo.Diccionario_Litros.Count > 1)
                {
                    ws_l.Cells[$"A{ws_l.Dimension.End.Row + 1}"].Value = "Total";

                    if (ws_l.Dimension.End.Column > 1)
                    {
                        for (int col = 2; col <= ws_l.Dimension.End.Column; col++)
                            ws_l.Cells[ws_l.Dimension.End.Row, col].Formula = $"SUM(Table1[{ws_l.Cells[1, col].Value}])";

                        ws_l.Calculate();
                    }
                }

                if (reporte_completo.Diccionario_Ventas.Count > 1)
                {
                    ws_v.Cells[$"A{ws_v.Dimension.End.Row + 1}"].Value = "Total";

                    if (ws_v.Dimension.End.Column > 1)
                    {
                        for (int col = 2; col <= ws_v.Dimension.End.Column; col++)
                            ws_v.Cells[ws_v.Dimension.End.Row, col].Formula = $"SUM(Table2[{ws_v.Cells[1, col].Value}])";

                        ws_v.Calculate();
                    }
                }

                ws_l.Cells[1, 1, ws_l.Dimension.End.Row, ws_l.Dimension.End.Column].Style.Numberformat.Format = "#,##0.00";
                ws_v.Cells[1, 1, ws_v.Dimension.End.Row, ws_v.Dimension.End.Column].Style.Numberformat.Format = "$#,####0.0000";

                ws_l.Cells[1, 1, ws_l.Dimension.End.Row, ws_l.Dimension.End.Column].AutoFitColumns();
                ws_v.Cells[1, 1, ws_v.Dimension.End.Row, ws_v.Dimension.End.Column].AutoFitColumns();



                return Ok(excel.GetAsByteArray());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
