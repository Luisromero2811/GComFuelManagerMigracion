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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class EstacionController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly User_Terminal _terminal;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly VerifyUserId verifyUser;

        public EstacionController(ApplicationDbContext context, User_Terminal _Terminal, UserManager<IdentityUsuario> userManager, VerifyUserId verifyUser)
        {
            this.context = context;
            this._terminal = _Terminal;
            this.userManager = userManager;
            this.verifyUser = verifyUser;
        }

        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] Folio_Activo_Vigente filtro_)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var estaciones_filtradas = context.Destino.IgnoreAutoIncludes().Where(x => x.Terminales.Any(x => x.Cod == id_terminal)).Include(x => x.Terminales).IgnoreAutoIncludes().AsQueryable();

                if (filtro_.ID_Cliente != 0)
                    estaciones_filtradas = estaciones_filtradas.Where(x => x.Codcte == filtro_.ID_Cliente);

                if (!string.IsNullOrEmpty(filtro_.Destino_Filtrado))
                    estaciones_filtradas = estaciones_filtradas.Where(x => !string.IsNullOrEmpty(x.Den) && x.Den.ToLower().Contains(filtro_.Destino_Filtrado.ToLower()));

                var estaciones = estaciones_filtradas.OrderBy(x => x.Den);

                return Ok(estaciones);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("{cliente:int}")]
        public async Task<ActionResult> Get([FromRoute] int cliente)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var estaciones = await context.Destino.IgnoreAutoIncludes()
                    .Where(x => x.Codcte == cliente && x.Activo == true && x.Terminales.Any(x => x.Cod == id_terminal))
                    .Include(x => x.Terminales).IgnoreAutoIncludes()
                    .Select(x => new CodDenDTO { Cod = x.Cod, Den = x.Den! })
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(estaciones);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("filtro/{cliente:int}")]
        public async Task<ActionResult> GetCliente(int cliente)
        {
            try
            {
                var estaciones = await context.Destino
                    .Where(x => x.Codcte == cliente && x.Activo == true)
                    .Include(x => x.Terminales)
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(estaciones);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("{cliente:int}/all")]
        public async Task<ActionResult> GetAll([FromRoute] int cliente)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var estaciones = await context.Destino.IgnoreAutoIncludes()
                    .Where(x => x.Codcte == cliente && x.Activo == true && x.Terminales.Any(x => x.Cod == id_terminal))
                    .Include(x => x.Terminales).IgnoreAutoIncludes()
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(estaciones);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpPost()]
        public async Task<ActionResult> EditDestino([FromBody] Destino destino)
        {
            try
            {
                if (destino is null)
                    return BadRequest();

                context.Update(destino);
                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpGet("estaciones")]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var estaciones = await context.Destino
                    .Where(x => x.Activo == true)
                    .Select(x => new CodDenDTO { Cod = x.Cod, Den = x.Den! })
                    .OrderBy(x => x.Den)
                    .ToListAsync();
                return Ok(estaciones);
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult> PostDestino([FromBody] Destino destino)
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                if (destino is null)
                {
                    return NotFound();
                }
                //Si el destino viene en ceros del front lo agregamos como nuevo sino actualizamos
                if (destino.Cod == 0)
                {
                    destino.Id_Tad = id_terminal;
                    //Agregamos cliente
                    context.Add(destino);
                }
                else
                {
                    context.Update(destino);
                }
                await context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("asignar/{cod:int}")]
        public async Task<ActionResult> PostAsignar([FromBody] Destino codden, [FromRoute] int cod)
        {
            try
            {
                var destino = await context.Destino.FirstOrDefaultAsync(x => x.Cod == codden.Cod);

                if (destino == null)
                {
                    return NotFound();
                }

                destino.Codcte = cod;
                context.Update(destino);
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
                //Si el cliente es nulo, retornamos un badrequest
                if (clienteTadDTO is null)
                    return BadRequest();
                foreach (var terminal in clienteTadDTO.Tads)
                {
                    foreach (var destino in clienteTadDTO.Destinos)
                    {
                        if (!context.Destino_Tad.Any(x => x.Id_Terminal == terminal.Cod && x.Id_Destino == destino.Cod))
                        {
                            Destino_Tad destino_Tad = new()
                            {
                                Id_Destino = destino.Cod,
                                Id_Terminal = terminal.Cod
                            };
                            context.Add(destino_Tad);
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
        public async Task<ActionResult> Borrar_Relacion([FromBody] Destino_Tad clienteterminal)
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
