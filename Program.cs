using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace MinPlan
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                 .UseStartup<Startup>();
#if DEBUG
            builder.UseKestrel(options =>
            {
                options.ConfigureEndpointDefaults(config => config.UseHttps());
                options.ListenLocalhost(9999);
            });
#endif
            return builder;
        }
    }
}
