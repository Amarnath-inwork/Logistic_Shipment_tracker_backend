using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Logistic_Shipment_tracker.Middleware
{
    public class CookieToHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieToHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only add Authorization header if it doesn't already exist
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                var token = context.Request.Cookies["auth_token"];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Request.Headers["Authorization"] = "Bearer " + token;
                }
            }

            await _next(context);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class CookieToHeaderMiddlewareExtensions
    {
        public static IApplicationBuilder UseCookieToHeader(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CookieToHeaderMiddleware>();
        }
    }
}
