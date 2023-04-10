﻿using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Controllers.Cierres
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public ClientesController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            try
            {
                var clientes = context.Cliente.AsEnumerable().Select(x => new CodDenDTO { Cod = x.Cod, Den = x.Den! }).OrderBy(x => x.Den);
                return Ok(clientes);
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
                var clientes = context.Cliente.AsEnumerable().OrderBy(x => x.Den);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("grupo/{cod:int}")]
        public async Task<ActionResult> Get(int cod)
        {
            try
            {
                var clientes = context.Cliente.Where(x=>x.Codgru == cod).AsEnumerable().OrderBy(x => x.Den);
                return Ok(clientes);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("asignar/{cod:int}")]
        public async Task<ActionResult> PostAsignar([FromBody]CodDenDTO codden, [FromRoute] int cod)
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
    }
}
