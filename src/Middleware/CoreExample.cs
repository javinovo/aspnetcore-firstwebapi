using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Threading.Tasks;

namespace TodoApi.Middleware
{
    public class CoreExample
    {
        private readonly RequestDelegate _next;

        public CoreExample(RequestDelegate next)
        {                        
	        Log.Debug("CoreExample instancing");
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Log.Information("Begin custom ASP.NET Core middleware handling");
            await _next.Invoke(context);
            Log.Information("End custom ASP.NET Core middleware handling");
        }
    }


    // @Configure: app.UseCoreExample()
    public static class CoreExampleExtensions
    {
        public static IApplicationBuilder UseCoreExample(this IApplicationBuilder builder) =>
            builder.UseMiddleware<CoreExample>();
    }
}

