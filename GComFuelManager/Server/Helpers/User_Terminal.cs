using GComFuelManager.Server.Identity;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;

namespace GComFuelManager.Server.Helpers
{
    public class User_Terminal
    {
        public User_Terminal()
        {
        }

        public short Obtener_Terminal(ApplicationDbContext dbContext, HttpContext httpContext)
        {
            string Id = string.Empty;

            var authheader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (authheader is not null && authheader.StartsWith("Bearer"))
                Id = authheader["Bearer ".Length..];

            var handler = new JwtSecurityTokenHandler();

            if (handler.CanReadToken(Id))
            {
                var token = handler.ReadJwtToken(Id);
                var nombre_terminal = token.Claims.FirstOrDefault(x => x.Type == "Terminal")?.Value;
                if (nombre_terminal is not null)
                {
                    var terminal = dbContext.Tad.FirstOrDefault(x => !string.IsNullOrEmpty(x.Den) && x.Den.Equals(nombre_terminal));
                    if (terminal is not null)
                        return terminal.Cod;
                }
            }

            return 0;
        }
    }
}
