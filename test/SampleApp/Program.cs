using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("merry xmas");
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
            // Configure Web API for self-host. 
            //var config = new HttpConfiguration();
            app.Run(context =>
            {
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Hello, world 11.");
            });
        }
    }
}
