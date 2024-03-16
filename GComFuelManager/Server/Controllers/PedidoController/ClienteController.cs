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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin, Administrador Sistema, Administrador de Usuarios ,Direccion, Gerencia, Ejecutivo de Cuenta Comercial, Comprador, Programador, Ejecutivo de Cuenta Operativo, Lectura de Cierre de Orden, Cierre Pedidos, Consulta Precios, Cliente Lectura, Contador, Precios, Revision Precios")]
    public class ClienteController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly User_Terminal _terminal;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserId verifyUser;

        public ClienteController(ApplicationDbContext context, User_Terminal _Terminal, UserManager<IdentityUsuario> userManager, VerifyUserId verifyUser)
        {
            this.context = context;
            this._terminal = _Terminal;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
        }

        [HttpGet("{grupo:int}")]
        public async Task<ActionResult> Get(int grupo)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var grupos = await context.Cliente.IgnoreAutoIncludes()
                    .Where(x => x.Codgru == grupo && x.Activo == true && x.Terminales.Any(x => x.Cod == id_terminal))
                    .Include(x => x.Terminales).IgnoreAutoIncludes()
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


        [HttpGet("Grupo/{grupo:int}")]
        public async Task<ActionResult> GetCliente(int grupo)
        {
            try
            {
                var grupos = await context.Cliente
                    .Include(x => x.Terminales)
                    .Where(x => x.Codgru == grupo && x.Activo == true)
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
        public ActionResult Obtener_Clientes_Activos([FromQuery] Cliente cliente)
        {
            try
            {
                var clientes = context.Cliente.Where(x => x.Activo).IgnoreAutoIncludes().AsQueryable();

                if (!string.IsNullOrEmpty(cliente.Den))
                    clientes = clientes.Where(x => x.Den!.ToLower().Contains(cliente.Den.ToLower()));

                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult> PostCliente([FromBody] Cliente cliente)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (cliente is null)
                    return BadRequest();
                //Si el cliente viene en ceros del front lo agregamos como nuevo sino actualizamos
                if (cliente.Cod == 0)
                {
                    cliente.Codgru = cliente.grupo!.Cod;
                    cliente.Id_Tad = id_terminal;
                    //Agregamos cliente
                    context.Add(cliente);
                }
                else
                {
                    context.Update(cliente);
                }
                await context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("relacion")]
        public async Task<ActionResult> PostClienteTerminal([FromBody] ClienteTadDTO clienteTadDTO)
        {
            try
            {
                if (clienteTadDTO is null)
                    return NotFound();

                foreach (var terminal in clienteTadDTO.Tads)
                {
                    foreach (var cliente in clienteTadDTO.Clientes)
                    {
                        if (!context.Cliente_Tad.Any(x => x.Id_Terminal == terminal.Cod && x.Id_Cliente == cliente.Cod))
                        {
                            Cliente_Tad clientetad = new()
                            {
                                Id_Cliente = cliente.Cod,
                                Id_Terminal = terminal.Cod
                            };
                            context.Add(clientetad);
                        }
                    }
                }
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("borrar/relacion")]
        public async Task<ActionResult> Borrar_Relacion([FromBody] Cliente_Tad clienteterminal)
        {
            try
            {
                if (clienteterminal is null)
                    return NotFound();

                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                context.Remove(clienteterminal);
                await context.SaveChangesAsync();

                return Ok(clienteterminal);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
