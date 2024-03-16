using GComFuelManager.Client.Helpers;
using GComFuelManager.Client.Repositorios;
using GComFuelManager.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace GComFuelManager.Client.Auth
{
    public class ProveedorAutenticacionJWT : AuthenticationStateProvider, ILoginService
    {
        private readonly IJSRuntime js;
        private readonly HttpClient client;
        private readonly NavigationManager navigation;
        private readonly IRepositorio repositorio;

        public ProveedorAutenticacionJWT(IJSRuntime js, HttpClient client, NavigationManager navigation, IRepositorio repositorio)
        {
            this.js = js;
            this.client = client;
            this.navigation = navigation;
            this.repositorio = repositorio;
        }

        public static readonly string TOKENKEY = "TOKENKEY";
        public static readonly string EXPIRATIONTOKENKEY = "EXPIRATIONTOKENKEY";

        private AuthenticationState anonimo =>
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await js.GetItemLocalStorage(TOKENKEY);

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(token))
            {
                navigation.NavigateTo("/login");
                return anonimo;
            }

            var tiempoExpiracionObject = await js.GetItemLocalStorage(EXPIRATIONTOKENKEY);
            DateTime tiempoExpiracion;

            if (tiempoExpiracionObject is null)
            {
                await Limpiar();
                navigation.NavigateTo("/login");
                return anonimo;
            }
            if (DateTime.TryParse(tiempoExpiracionObject.ToString(), out tiempoExpiracion))
            {
                if (TokenExpirado(tiempoExpiracion))
                {
                    await Limpiar();
                    navigation.NavigateTo("/login");
                    return anonimo;
                }
                if (DebeRenovarToken(tiempoExpiracion))
                {
                    token = await RenovarToken(token.ToString()!);
                }
            }

            return ConstruirAuthenticationState(token.ToString()!);
        }

        private bool TokenExpirado(DateTime tiempoExpiracion)
        {
            return tiempoExpiracion <= DateTime.Now;
        }
        //Condición para que el sistema verifique si debería de renovar su JWT
        private bool DebeRenovarToken(DateTime tiempoExpiracion)
        {
            return tiempoExpiracion.Subtract(DateTime.Now) < TimeSpan.FromMinutes(30);
        }
        //Acción para renovar el Token 
        public async Task ManejarRenovacionToken()
        {
            var tiempoExpiracionObject = await js.GetItemLocalStorage(EXPIRATIONTOKENKEY);
            DateTime tiempoExpiracion;

            await CheckLoginApp();

            if (DateTime.TryParse(tiempoExpiracionObject, out tiempoExpiracion))
            {
                if (TokenExpirado(tiempoExpiracion))
                {
                    await Logoute();
                }
                if (DebeRenovarToken(tiempoExpiracion))
                {
                    var token = await js.GetItemLocalStorage(TOKENKEY);
                    var nuevoToken = await RenovarToken(token.ToString()!);
                    var authState = ConstruirAuthenticationState(nuevoToken);
                    NotifyAuthenticationStateChanged(Task.FromResult(authState));
                }
            }

        }

        private async Task<string> RenovarToken(string token)
        {
            //Console.WriteLine("Renovando Token");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Dictionary<string, string> query = new();

            query["t"] = token;
            var url = Constructor_De_URL_Parametros.Generar_URL(query);

            var nuevoTokenResponse = await repositorio.Get<UserTokenDTO>($"api/cuentas/renovarToken?{url}");
            var nuevoToken = nuevoTokenResponse.Response!;

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(token))
            {
                await Logoute();
                return "";
            }

            await js.SetItemLocalStorage(TOKENKEY, nuevoToken.Token);
            await js.SetItemLocalStorage(EXPIRATIONTOKENKEY, nuevoToken.Expiration.ToString());

            return nuevoToken.Token;
        }

        private AuthenticationState ConstruirAuthenticationState(string token)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            var claims = ParsearClaimsJwt(token);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }

        private IEnumerable<Claim> ParsearClaimsJwt(string token)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenDesearizado = jwtSecurityTokenHandler.ReadJwtToken(token);
            return tokenDesearizado.Claims;
        }

        public async Task Login(UserTokenDTO token)
        {
            await js.SetItemLocalStorage(TOKENKEY, token.Token);
            await js.SetItemLocalStorage(EXPIRATIONTOKENKEY, token.Expiration.ToString());
            var authentication = ConstruirAuthenticationState(token.Token);
            NotifyAuthenticationStateChanged(Task.FromResult(authentication));
        }

        public async Task Logoute()
        {
            await Limpiar();
            NotifyAuthenticationStateChanged(Task.FromResult(anonimo));
            navigation.NavigateTo("/login");
        }
        public async Task Limpiar()
        {
            await js.RemoveItemLocalStorage(TOKENKEY);
            await js.RemoveItemLocalStorage(EXPIRATIONTOKENKEY);
            client.DefaultRequestHeaders.Authorization = null!;
        }

        public async Task CheckLoginApp()
        {
            var token = await js.GetItemLocalStorage(TOKENKEY);
            var tiempoExpiracionObject = await js.GetItemLocalStorage(EXPIRATIONTOKENKEY);

            if (string.IsNullOrEmpty(token) || string.IsNullOrWhiteSpace(tiempoExpiracionObject))
                await Logoute();
        }
    }
}