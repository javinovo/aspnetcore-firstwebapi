using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ConsoleApplication
{
    public class Program
    {
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
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
	
	public class Startup
	{		
		public void ConfigureServices(IServiceCollection services)
		{                        
	        Log.Information("Configuring services");

			// Add framework services.
			services.AddMvc();
			services.AddLogging();

			// Add our repository type
			services.AddSingleton<ITodoRepository, TodoRepository>();
		}
		
		public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
		{              
            Log.Information("Configuring");

            // https://github.com/serilog/serilog-extensions-logging
            loggerFactory.AddSerilog(); // Add Serilog to the logging pipeline

            // Middleware: order matters => A > B > C > B > A

            app.Use(AspNetCoreMiddleware); // A

            app.UseOwin(builder => // B       
                builder(next => 
                    async env =>
                    {
                        Log.Information("Begin custom OWIN middleware handling");
                        OwinMiddleware(env);
                        await next.Invoke(env);
                        Log.Information("End custom OWIN middleware handling");
                    }));

            /*
            app.Run(context => // Terminates the pipeline (no next): C won't be executed, but B > A will
                context.Response.WriteAsync("Hello from ASP.NET Core!")); // Same for any request
            */

	        app.UseMvcWithDefaultRoute(); // C
		}

        async Task AspNetCoreMiddleware(HttpContext ctx, Func<Task> next)
        {
            Log.Information("Begin custom ASP.NET Core middleware handling");
            await next();
            Log.Information("End custom ASP.NET Core middleware handling");
        }

        // OWIN's AppFunc returns Task, however we don't need async here 
        void OwinMiddleware(IDictionary<string, object> environment)
        {
            // OWIN Environment Keys: http://owin.org/spec/spec/owin-1.0.0.html
            var headersKey = "owin.RequestHeaders";
            var headers = (IDictionary<string, string[]>) environment["owin.RequestHeaders"];
            
            Log.Information($"{headersKey}: {{HeaderNames}}", string.Join(", ", headers.Keys));
        }
	}

    public class TodoItem
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public bool IsComplete { get; set; }
    }

    public interface ITodoRepository
    {
        void Add(TodoItem item);
        IEnumerable<TodoItem> GetAll();
        TodoItem Find(string key);
        TodoItem Remove(string key);
        void Update(TodoItem item);
    }
	
    public class TodoRepository : ITodoRepository
    {
        private static ConcurrentDictionary<string, TodoItem> _todos = 
              new ConcurrentDictionary<string, TodoItem>();

        public TodoRepository()
        {
            Add(new TodoItem { Name = "Item1" });
        }

        public IEnumerable<TodoItem> GetAll()
        {
            return _todos.Values;
        }

        public void Add(TodoItem item)
        {
            item.Key = Guid.NewGuid().ToString();
            _todos[item.Key] = item;
        }

        public TodoItem Find(string key)
        {
            TodoItem item;
            _todos.TryGetValue(key, out item);
            return item;
        }

        public TodoItem Remove(string key)
        {
            TodoItem item;
            _todos.TryGetValue(key, out item);
            _todos.TryRemove(key, out item);
            return item;
        }

        public void Update(TodoItem item)
        {
            _todos[item.Key] = item;
        }
    }	
	
	[Route("api/[controller]")]
    public class TodoController : Controller
    {
        public TodoController(ITodoRepository todoItems)
        {
            TodoItems = todoItems;
        }
        public ITodoRepository TodoItems { get; set; }
		
		public IEnumerable<TodoItem> GetAll()
		{
			return TodoItems.GetAll();
		}

		[HttpGet("{id}", Name = "GetTodo")]
		public IActionResult GetById(string id)
		{
			var item = TodoItems.Find(id);
			if (item == null)
			{
				return NotFound();
			}
			return new ObjectResult(item);
		}
		
		[HttpPost]
		public IActionResult Create([FromBody] TodoItem item)
		{
			if (item == null)
			{
				return BadRequest();
			}
			TodoItems.Add(item);
			return CreatedAtRoute("GetTodo", new { controller = "Todo", id = item.Key }, item);
		}		
		
		[HttpPut("{id}")]
		public IActionResult Update(string id, [FromBody] TodoItem item)
		{
			if (item == null || item.Key != id)
			{
				return BadRequest();
			}

			var todo = TodoItems.Find(id);
			if (todo == null)
			{
				return NotFound();
			}

			TodoItems.Update(item);
			return new NoContentResult();
		}		

		[HttpDelete("{id}")]
		public void Delete(string id)
		{
			TodoItems.Remove(id);
		}
    }
}
