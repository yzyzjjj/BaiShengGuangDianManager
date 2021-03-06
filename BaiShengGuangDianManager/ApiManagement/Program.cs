using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
#if !DEBUG
using Microsoft.Extensions.Configuration;
#endif

namespace ApiManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //foreach (var a in UrlMappings.Urls)
            //{
            //    Console.WriteLine($"public static string {a.Key} = \"{a.Key}\";");
            //    //Console.WriteLine($"{{ UrlMappings.{a.Key},\"{a.Value}\"}},");
            //}
            CreateWebHostBuilder(args).Build().Run();
        }

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>();
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
#if DEBUG
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
#else
            var configuration = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return WebHost.CreateDefaultBuilder(args).UseConfiguration(configuration)
                .UseStartup<Startup>();
#endif
        }
    }
}
