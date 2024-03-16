using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.GamoModels;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace GComFuelManager.Server.Controllers.AsignacionUnidadesController
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DestinoController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly VerifyUserToken verifyUser;
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly User_Terminal _terminal;

        public DestinoController(ApplicationDbContext context, VerifyUserToken verifyUser, UserManager<IdentityUsuario> userManager, User_Terminal _Terminal)
        {
            this.context = context;
            this.verifyUser = verifyUser;
            this.userManager = userManager;
            this._terminal = _Terminal;
        }

        //[HttpGet]//TODO: checar utilidad
        //public ActionResult<List<Destino>> Get()
        //{
        //    try
        //    {
        //        var destinos = context.Destino
        //            .ToList();
        //        return Ok(destinos);
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(e.Message);
        //    }
        //}

        [HttpGet("comprador")]
        public ActionResult GetClientComprador()
        {
            try
            {
                var id_terminal = _terminal.Obtener_Terminal(context, HttpContext);
                if (id_terminal == 0)
                    return BadRequest();

                var userId = verifyUser.GetName(HttpContext);

                if (string.IsNullOrEmpty(userId))
                    return BadRequest();

                var user = context.Usuario.FirstOrDefault(x => x.Usu == userId);
                if (user == null)
                    return BadRequest();

                var clientes = context.Destino.IgnoreAutoIncludes().Where(x => x.Codcte == user!.CodCte && x.Terminales.Any(x => x.Cod == id_terminal))
                    .Include(x => x.Terminales).IgnoreAutoIncludes().OrderBy(x => x.Den).ToList();

                return Ok(clientes);
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

                var destino = context.Destino.Where(x => x.Cod == cod).FirstOrDefault();
                if (destino == null)
                {
                    return NotFound();
                }
                destino.Activo = status;

                context.Update(destino);
                var acc = destino.Activo ? 26 : 27;
                var id = await verifyUser.GetId(HttpContext, userManager);
                if (string.IsNullOrEmpty(id))
                    return BadRequest();

                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("gamo")]
        public async Task<ActionResult> GetGamo()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] code = Encoding.ASCII.GetBytes("apisimsa@ubiquite.mx:UA3VbQrbENWF62d");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(code));

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    Task<HttpResponseMessage> get = client.GetAsync("https://energas.ubiquite.mx/api/energasB2b/destinos");

                    var response = JsonConvert.DeserializeObject<DestinoGamoList>(await get.Result.Content.ReadAsStringAsync());

                    foreach (var item in response.Destinos)
                    {
                        if (item.IdDestinoTuxpan != 0)
                        {
                            var destino = context.Destino.Where(x => x.Den.ToLower().Contains(item.Nombre.ToLower()) &&
                                x.Cliente.Den.ToLower().Contains(item.RazonSocial.ToLower()))
                                .Include(x => x.Cliente)
                                .FirstOrDefault();
                            if (destino != null)
                            {
                                destino.CodGamo = item.IdDestinoTuxpan;

                                context.Update(destino);
                            }
                        }
                    }

                    await context.SaveChangesAsync();

                    return Ok(response);
                }
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }
    }
}

