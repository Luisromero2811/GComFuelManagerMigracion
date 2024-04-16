﻿using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GComFuelManager.Server.Controllers.Auth
{
    [ApiController]
    [Route("api/cuentas")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUsuario> userManager;
        private readonly SignInManager<IdentityUsuario> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;

        public AuthController(UserManager<IdentityUsuario> userManager,
            SignInManager<IdentityUsuario> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
            this.context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserTokenDTO>> Login([FromBody] UsuarioInfo info)
        {
            try
            {

                var usuario = await context.Usuario.FirstOrDefaultAsync(x => x.Usu == info.UserName);
                if (usuario == null)
                    return BadRequest("El usuario no tiene acceso al sistema");

                if (usuario!.Activo == true)
                {

                    var user_asp = await userManager.FindByNameAsync(info.UserName);
                    if (user_asp == null)
                        return BadRequest("El usuario no tiene acceso al sistema");

                    var terminal = context.Tad.FirstOrDefault(x => !string.IsNullOrEmpty(x.Den) && x.Den.Equals(info.Terminal));
                    if (terminal is null)
                    {
                        var user = await userManager.FindByNameAsync(info.UserName);
                        if (user is not null)
                        {
                            if (await userManager.IsInRoleAsync(user, "Obtencion de Ordenes") || await userManager.IsInRoleAsync(user, "Consulta Precios"))
                            {
                                terminal = new() { Cod = 0 };
                                info.Terminal = "Interno";
                            }
                            else
                                return BadRequest("No tiene acceso a esta terminal");
                        }
                        else
                            return BadRequest("No tiene acceso a esta terminal");
                    }

                    if (context.Usuario_Tad.Any(x => x.Id_Usuario == user_asp.Id && x.Id_Terminal == terminal!.Cod))
                    {
                        var resultado = await signInManager.PasswordSignInAsync(info.UserName, info.Password, isPersistent: false, lockoutOnFailure: false);
                        if (resultado.Succeeded)
                        {
                            var token = await BuildToken(info);

                            return Ok(token);
                        }
                        else
                            return BadRequest("Nombre de usuario y/o contraseña no validos");
                    }
                    else
                        return BadRequest("No tiene acceso a esta terminal");
                }
                else
                    return BadRequest("El usuario no tiene acceso al sistema");
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error interno del servidor al iniciar sesion.");
            }
        }

        [HttpGet("renovarToken")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserTokenDTO>> Renovar()
        {
            //Construimos un userInfo para poder utilizar el method de BuildToken
            var userInfo = new UsuarioInfo()
            {
                UserName = HttpContext.User.Identity!.Name!
            };

            return await BuildToken(userInfo);
        }

        private async Task<UserTokenDTO> BuildToken(UsuarioInfo info)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, info.UserName),
                new Claim(JwtRegisteredClaimNames.UniqueName, info.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var usuario = await userManager.FindByNameAsync(info.UserName);
            if (usuario != null)
            {
                var roles = await userManager.GetRolesAsync(usuario);

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwtkey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //var expiration = DateTime.Now.AddHours(1);
            var expiration = DateTime.Now.AddMinutes(30);
            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expiration,
                signingCredentials: creds);
            return new UserTokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration,
            };
        }

        [HttpGet("check/user"), Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> GetClient()
        {
            try
            {
                var usuario = await userManager.FindByNameAsync(HttpContext.User.FindFirstValue(ClaimTypes.Name)!);
                //Si el usuario no existe
                if (usuario == null)
                    return NotFound();

                var user = new UsuarioInfo();

                var isClient = await userManager.IsInRoleAsync(usuario, "Correo de Clientes");

                user.IsClient = isClient;

                if (isClient)
                    user.CodCte = context.Usuario.Find(usuario.UserCod)!.CodCte;

                return Ok(user);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("check/client"), Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> CheckClient()
        {
            try
            {
                var usuario = await userManager.FindByNameAsync(HttpContext.User.FindFirstValue(ClaimTypes.Name)!);
                //Si el usuario no existe
                if (usuario == null)
                    return NotFound();

                var user = new UsuarioInfo();

                var isClient = await userManager.IsInRoleAsync(usuario, "Comprador");

                user.IsClient = isClient;

                if (isClient)
                {
                    var u = context.Usuario.Find(usuario.UserCod);
                    user.CodCte = u.CodCte;
                    user.CodGru = u.CodGru;
                }
                return Ok(user);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}
