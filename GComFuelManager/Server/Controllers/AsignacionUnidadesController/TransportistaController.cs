using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using GComFuelManager.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GComFuelManager.Server.Controllers.AsignacionUnidadesController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class TransportistaController : ControllerBase 
	{
        private readonly ApplicationDbContext context;

        public TransportistaController(ApplicationDbContext context)
		{
            this.context = context;
        }
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                var transportistas = await context.Transportista.Where(x => x.activo == true && x.gru != null)
                    //.Select(x => new CodDenDTO { Cod = Convert.ToInt32(x.busentid), Den = x.den!})
                    .OrderBy(x => x.den)
                    .ToListAsync();
                return Ok(transportistas);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}

