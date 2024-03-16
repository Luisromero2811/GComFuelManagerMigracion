using System;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace GComFuelManager.Shared.DTOs
{
    public class UserTokenDTO
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }
        [JsonIgnore] public List<Claim> Claims { get; set; } = new();
    }
}

