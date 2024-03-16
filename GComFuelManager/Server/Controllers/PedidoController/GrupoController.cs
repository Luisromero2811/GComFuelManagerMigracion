using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Administrador Sistema, Revision Precios ,Administrador de Usuarios ,Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Comprador, Programador, Ejecutivo de Cuenta Operativo, Lectura de Cierre de Orden, Cierre Pedidos, Consulta Precios, Cliente Lectura, Contador, Precios, Revision Precios")]
    public class GrupoController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly VerifyUserId verifyUser;
        private readonly UserManager<IdentityUsuario> userManager;

        public GrupoController(ApplicationDbContext context, VerifyUserId verifyUser, UserManager<IdentityUsuario> userManager)
        {
            this.context = context;
            this.verifyUser = verifyUser;
            this.userManager = userManager;
        }

        [HttpGet]
        public ActionResult Get([FromQuery] Folio_Activo_Vigente filtro_)
        {
            try
            {
                var grupos_filtrados = context.Grupo.AsQueryable();

                if (!string.IsNullOrEmpty(filtro_.Grupo_Filtrado))
                    grupos_filtrados = grupos_filtrados.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(filtro_.Grupo_Filtrado.ToLower()));

                var grupos = grupos_filtrados.Select(x => new CodDenDTO() { Cod = x.Cod, Den = x.Den }).OrderBy(x => x.Den);

                return Ok(grupos);
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
                var grupos = context.Grupo
                    .OrderBy(x => x.Den)
                    .AsEnumerable();
                return Ok(grupos);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("allactives")]
        public async Task<ActionResult> GetAllActives()
        {
            try
            {
                var grupos = await context.Grupo
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(grupos);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtraractivos")]
        public ActionResult Obtener_Grupos_Activos([FromQuery] Grupo grupo)
        {
            try
            {
                var grupos = context.Grupo.IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(grupo.Den))
                    grupos = grupos.Where(x => x.Den!.ToLower().Contains(grupo.Den.ToLower()));

                return Ok(grupos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Grupo grupo)
        {
            try
            {
                grupo.Fch = DateTime.Now;
                if (grupo.Cod == 0)
                    context.Add(grupo);
                else
                    context.Update(grupo);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 28);
                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpPost("cliente")]
        public async Task<ActionResult> AsignCliente([FromBody] Cliente cliente)
        {
            try
            {
                if (cliente == null)
                {
                    return NotFound();
                }

                context.Update(cliente);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtrar")]
        public async Task<ActionResult> Filtrar_Grupo([FromQuery] CodDenDTO parametros)
        {
            try
            {
                var clientes = context.Grupo.AsQueryable();

                if (!string.IsNullOrEmpty(parametros.Den))
                {
                    clientes = clientes.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(parametros.Den.ToLower()));
                }

                await HttpContext.InsertarParametrosPaginacion(clientes, parametros.tamanopagina, parametros.pagina);

                if (HttpContext.Response.Headers.ContainsKey("pagina"))
                {
                    var pagina = HttpContext.Response.Headers["pagina"];
                    if (pagina != parametros.pagina && !string.IsNullOrEmpty(pagina))
                    {
                        parametros.pagina = int.Parse(pagina);
                    }
                }

                clientes = clientes.Skip((parametros.pagina - 1) * parametros.tamanopagina).Take(parametros.tamanopagina);

                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
