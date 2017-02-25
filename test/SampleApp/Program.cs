using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //throw new Exception("Hi");
           
            using (WebApp.Start<Startup>("http://localhost:8087"))
            {
                Console.WriteLine("Web Server is running..");
                Console.WriteLine("Press any key to quit.");
                Console.ReadLine();
            }
        }
    }

    class Startup{
        public void Configuration(IAppBuilder app)
        {
            var text = File.ReadAllText("local.txt");
            Console.WriteLine(text);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.Write("WHY");
            Console.ResetColor();

            // Configure Web API for self-host. 
            //var config = new HttpConfiguration();
            app.Run(context =>
            {
                context.Response.ContentType = "text/plain";
                Console.WriteLine(text);
                return context.Response.WriteAsync(text);
            });
        }
    }
}
