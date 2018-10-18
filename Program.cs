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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                //.UseKestrel(options =>
                //{
                //    options.ConfigureEndpointDefaults(x => { x.UseHttps(); });
                //    options.ListenLocalhost(9999);
                //})
            ;
    }
}
