using Microsoft.AspNetCore.Builder;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks; 
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace TodoApi.Middleware
{
    public class OwinExample
    {
        private readonly AppFunc _next;

        public OwinExample(AppFunc next)
        {
            Log.Debug("OwinExample instancing");
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
        
	        Log.Information("Begin custom OWIN middleware handling");
            LogRequestHeaders(environment);
            await _next.Invoke(environment);
            Log.Information("End custom OWIN middleware handling");
        }
 
        void LogRequestHeaders(IDictionary<string, object> environment)
        {
            // OWIN Environment Keys: http://owin.org/spec/spec/owin-1.0.0.html
            var headersKey = "owin.RequestHeaders";
            var headers = (IDictionary<string, string[]>) environment["owin.RequestHeaders"];
            
            Log.Information($"{headersKey}: {{HeaderNames}}", string.Join(", ", headers.Keys));
        }    
    }

    // @Configure: app.UseOwinExample()
    public static class OwinExampleExtensions
    {
        public static IApplicationBuilder UseOwinExample(this IApplicationBuilder builder) =>
            builder.UseOwin(pipeline =>
                pipeline(next => new OwinExample(next).Invoke));
    }
}