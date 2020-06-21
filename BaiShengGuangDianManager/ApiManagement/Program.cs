using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ModelBase.Base.UrlMappings;
using ServiceStack;

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
            var configuration = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            return WebHost.CreateDefaultBuilder(args).UseConfiguration(configuration)
                .UseStartup<Startup>();
        }
    }
}
