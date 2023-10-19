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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Administrador Sistema, Administrador de Usuarios ,Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Comprador, Programador, Ejecutivo de Cuenta Operativo, Lectura de Cierre de Orden, Cierre Pedidos, Consulta Precios, Cliente Lectura, Contador, Precios")]
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
        public async Task<ActionResult> Get()
        {
            try
            {
                var grupos = await context.Grupo
                    .Select(x => new CodDenDTO { Cod = x.Cod, Den = x.Den! })
                    .OrderBy(x => x.Den)
                    .ToListAsync();
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

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Grupo grupo)
        {
            try
            {
                grupo.Fch = DateTime.Now;
                context.Add(grupo);

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id,28);
                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpPost("cliente")]
        public async Task<ActionResult> AsignCliente([FromBody]Cliente cliente)
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
    }
}
