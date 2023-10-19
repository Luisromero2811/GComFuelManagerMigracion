using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GComFuelManager.Server.Controllers.Contactos
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ContactoController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<IdentityUsuario> UserManager;
        private readonly VerifyUserId verifyUser;

        public ContactoController(ApplicationDbContext context, UserManager<IdentityUsuario> UserManager, VerifyUserId verifyUser)
        {
            this.context = context;
            this.UserManager = UserManager;
            this.verifyUser = verifyUser;
        }

        [HttpGet("cliente/{cod:int}")]
        public async Task<ActionResult> GetByCliente([FromRoute] int cod)
        {
            try
            {
                var contactos = await context.Contacto.Where(x => x.CodCte == cod).Include(x => x.AccionCorreos).ThenInclude(x => x.Accion).ToListAsync();

                return Ok(contactos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{cod:int}")]
        public ActionResult GetContacto([FromRoute] int cod)
        {
            try
            {
                if (cod == 0)
                    return NotFound();

                var contactos = context.Contacto.Where(x => x.CodCte == cod && x.Estado == true).OrderBy(x => x.Nombre).AsEnumerable();

                return Ok(contactos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Contacto contacto)
        {
            try
            {
                if (contacto == null)

                    return BadRequest();

                context.Add(contacto);
                await context.SaveChangesAsync();

                if (contacto.CodCte != 0)
                {
                    foreach (var item in contacto.Accions)
                    {
                        AccionCorreo accionCorreo = new AccionCorreo();

                        accionCorreo.CodAccion = item.Cod;
                        accionCorreo.CodContacto = contacto.Cod;
                        context.Add(accionCorreo);
                    }
                }

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 24);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPost("internos")]
        public async Task<ActionResult> PostInterno([FromBody] Contacto contacto)
        {
            try
            {
                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                if (contacto.Cod == 0)
                {
                    if (contacto == null)

                        return BadRequest();

                    else

                        context.Add(contacto);
                    await context.SaveChangesAsync(id, 24);

                    foreach (var item in contacto.Accions)
                    {
                        AccionCorreo accionCorreo = new AccionCorreo();

                        accionCorreo.CodAccion = item.Cod;
                        accionCorreo.CodContacto = contacto.Cod;
                        context.Add(accionCorreo);
                    }
                    await context.SaveChangesAsync();
                }
                else
                {
                    if (contacto == null)
                        return BadRequest();

                    var acciones = context.AccionCorreo.Where(x => x.CodContacto == contacto.Cod).ToList();

                    if (acciones.Count > 0)
                        context.RemoveRange(acciones);

                    await context.SaveChangesAsync();
                    if (contacto.Cod != 0)
                    {
                        foreach (var item in contacto.Accions)
                        {
                            AccionCorreo accionCorreo = new AccionCorreo();

                            accionCorreo.CodAccion = item.Cod;
                            accionCorreo.CodContacto = contacto.Cod;
                            context.Add(accionCorreo);
                        }
                    }
                    await context.SaveChangesAsync(id, 25);

                    contacto.AccionCorreos = null!;

                    context.Update(contacto);
                    await context.SaveChangesAsync(id, 25);

                }
                await context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpPut]
        public async Task<ActionResult> Put([FromBody] Contacto contacto)
        {
            try
            {
                if (contacto == null)
                    return BadRequest();

                var acciones = context.AccionCorreo.Where(x => x.CodContacto == contacto.Cod).ToList();

                if (acciones.Count > 0)
                    context.RemoveRange(acciones);

                await context.SaveChangesAsync();
                if (contacto.CodCte != 0)
                {
                    foreach (var item in contacto.Accions)
                    {
                        AccionCorreo accionCorreo = new AccionCorreo();

                        accionCorreo.CodAccion = item.Cod;
                        accionCorreo.CodContacto = contacto.Cod;
                        context.Add(accionCorreo);
                    }
                }
                await context.SaveChangesAsync();

                contacto.AccionCorreos = null!;

                context.Update(contacto);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 25);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("{cod:int}")]
        public async Task<ActionResult> ChangeStatus([FromRoute] int cod, [FromBody] bool status)
        {
            try
            {
                if (cod == 0)
                    return BadRequest();

                var contacto = context.Contacto.Where(x => x.Cod == cod).FirstOrDefault();

                if (contacto == null)
                    return NotFound();

                contacto.Estado = status;

                context.Update(contacto);

                var id = await verifyUser.GetId(HttpContext, UserManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync(id, 25);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("comprador")]
        public ActionResult GetContactoComprador()
        {
            try
            {
                var user = context.Usuario.FirstOrDefault(x => x.Usu == HttpContext.User.FindFirstValue(ClaimTypes.Name));
                if (user == null)
                    return BadRequest();

                var clientes = context.Contacto.Where(x => x.CodCte == user!.CodCte).AsEnumerable().OrderBy(x => x.Nombre);

                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
