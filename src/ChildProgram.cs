using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rerunner
{
    class ChildProgram
    {
        /// <summary>
        /// Child progrses acts a bootstrapper, loading the given
        /// exe and running it without locking the files. When the given exe ends this process
        /// ends. if the parent process ends this will also kill itself
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: rerunner.exe --child [path-to-executable] [...args-to-pass-to-executable]");
                return -1;
            }
            var exeFilename = args[0];
            var exeArgs = args.Skip(1).ToArray();
            if (!File.Exists(exeFilename))
            {
                Console.WriteLine($"executable '{exeFilename}' does not exist");
                return -1;
            }

            var exitCode = StartInNewAppDomain(exeFilename, exeArgs).Result;
            return exitCode;
        }

        private static async Task<int> StartInNewAppDomain(string exe, string[] args)
        {
            var exeFileInfo = new FileInfo(exe);
            if (!exeFileInfo.Exists)
            {
                Console.WriteLine($"exectable '{exe}' doesnt exist. Waiting for it to materialize");
                return -1;
            }

            var setup = new AppDomainSetup();
            setup.ShadowCopyFiles = bool.TrueString;
            setup.ApplicationBase = exeFileInfo.DirectoryName;

            if (new FileInfo(exe + ".config").Exists)
                setup.ConfigurationFile = exe + ".config";

            var newDomain = AppDomain.CreateDomain("exe", null, setup);
            var result = await Task.Run<int>(() => newDomain.ExecuteAssembly(exe, args));
            AppDomain.Unload(newDomain);
            return result;
        }
    }
}
