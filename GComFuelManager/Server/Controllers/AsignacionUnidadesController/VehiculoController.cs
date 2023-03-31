﻿using System;
using Microsoft.AspNetCore.Mvc;
using GComFuelManager.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Controllers.AsignacionUnidadesController
{
    [Route("api/[controller]")]
    [ApiController]
	public class VehiculoController : ControllerBase
	{
        private readonly ApplicationDbContext context;

        public VehiculoController(ApplicationDbContext context)
		{
            this.context = context;
        }
        [HttpGet("{transportista:int}")]
        public async Task<ActionResult> Get(int transportista)
        {
            try
            {
                var vehiculos = await context.Tonel
                    .Where(x => Convert.ToInt32(x.Carid) == transportista && x.Activo == true)
                    //.Select(x => new CodDenDTO { Cod = x.Cod, Den =  $"{x.Tracto} {x.Placatracto} {x.Placa} {x.Capcom!} {x.Capcom2!} {x.Capcom3!} {x.Capcom4!} {x.Codsyn!}" })
                    .OrderBy(x => x.Placa)
                    .ToListAsync();
                return Ok(vehiculos);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}

//+ x.Tracto + x.Placatracto
//+ x.Placa + x.Capcom + " " + x.Capcom2 + " " + x.Capcom3 + " " + x.Capcom4 + " " + x.Codsyn