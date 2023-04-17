﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.ServiceModel;

namespace GComFuelManager.Server.Controllers.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public ServicesController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [Route("transportistas")]
        [HttpGet]
        public async Task<ActionResult> GetTransportistas()
        {
            try
            {
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}