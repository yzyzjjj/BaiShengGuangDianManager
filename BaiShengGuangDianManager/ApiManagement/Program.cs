using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
