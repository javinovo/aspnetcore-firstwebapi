using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using Serilog.Events;
using TodoApi.Models;
using TodoApi.Middleware;

namespace TodoApi
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Log.Information("Configuring services");

			// Add framework services.
			services.AddMvc();
			services.AddLogging();

			// Add our repository type
			services.AddSingleton<ITodoRepository, TodoRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {              
            Log.Information("Configuring application pipeline");

            // https://github.com/serilog/serilog-extensions-logging
            loggerFactory.AddSerilog(); // Add Serilog to the logging pipeline

            // Middleware: order matters => A > B > C > B > A

            app.UseCoreExample(); // A
            app.UseOwinExample(); // B

            /*
            app.Run(context => // Terminates the pipeline (no next): C won't be executed, but B > A will
                context.Response.WriteAsync("Hello from ASP.NET Core!")); // Same for any request
            */

	        app.UseMvcWithDefaultRoute(); // C
		}

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext() // dynamically add and remove properties from the ambient "execution context"
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile(@"log-{Date}.txt", 
                    fileSizeLimitBytes: 1024, 
                    restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();  

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                //.UseIISIntegration()
                .Build();

            host.Run();          
        }
    }
}
