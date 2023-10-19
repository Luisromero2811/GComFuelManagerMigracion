using Microsoft.EntityFrameworkCore;

namespace GComFuelManager.Server.Helpers
{
    public static class HttpContextPaginationExtension
    {
        public async static Task InsertarParametrosPaginacion<T>(this HttpContext context, IQueryable<T> queryable, int cantidad, int pagina)
        {
            if (context is null) { throw new ArgumentNullException(nameof(context)); }
            double conteo = await queryable.CountAsync();
            double totalpaginas = Math.Ceiling(conteo / cantidad);
            context.Response.Headers.Add("conteo", conteo.ToString());
            context.Response.Headers.Add("paginas", totalpaginas.ToString());
            if (pagina > totalpaginas)
                context.Response.Headers.Add("pagina", "1");
            else
                context.Response.Headers.Add("pagina", pagina.ToString());
        }
    }
}
