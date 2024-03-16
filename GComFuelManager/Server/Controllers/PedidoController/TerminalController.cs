using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TerminalController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public TerminalController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public ActionResult Get()
        {
            try
            {
                var terminales = context.Tad.Where(x => x.Activo == true).ToList();
                return Ok(terminales.Order());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("login"), AllowAnonymous]
        public ActionResult Obteer_Terminales_De_Login()
        {
            try
            {
                List<string?> tads = new();
                tads = context.Tad.Where(x => x.Activo == true && !string.IsNullOrEmpty(x.Den)).Select(x => x.Den).ToList();
                return Ok(tads.Order());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("activas")]
        public async Task <ActionResult<List<Tad>>> Obtener_Terminales_De_Login()
        {
            try
            {
                return await context.Tad.Where(x => x.Activo == true).ToListAsync();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
