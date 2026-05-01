using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entidades
{
    public static class HttpContextExtensions
    {
        public async static Task
            InsertarParametrosPaginacionEnCabecera<T>(this HttpContext httpContext, IQueryable<T> queryable)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            double cantidad = await queryable.CountAsync();
            httpContext.Response.Headers.Append("cantidad-total-registros", cantidad.ToString());

        }
    }
}
