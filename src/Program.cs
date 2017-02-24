using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rerunner
{
    public class Program
    {
        private static object singleRunningAppLock = new object();

        static int Main(string[] args)
        {
            if (args.Length == 0) {
                PrintInstructions();
                return -1;
            }

            if(args[0] == "--single")
            {
                //single mode simply allows us to load an exe and run it in our app domain without
                //locking the original files. It does no rerunning, just a single run
                var exitCode = StartInAppDomain(args[1], args.Skip(2).ToArray()).Result;
                return exitCode;
            }

         
            var exeFileInfo = new FileInfo(args[0]);
            if (!exeFileInfo.Exists)
            {
                Console.WriteLine($"executable '{args[0]}' does not exist");
                return -1;
            }

            Process process = null;
            var keepRerunning = true;

            SetConsoleCtrlHandler(new HandlerRoutine(ctrlType=>
            {
                //if a kill signal has been send we will stop things
                StopProcess(process);
                keepRerunning = false;
                return true;
            }), true);

            WatchDirectory(exeFileInfo.DirectoryName, new DelayExecution(() =>
            {
                //files have changed, stop our process so a new one can be created
                StopProcess(process);
            }, 1000).Start);


            do
            {
                //keep starting processes
                process = StartInProcess(exeFileInfo.FullName, args.Skip(1).ToArray());
                process.WaitForExit(-1);
                if (keepRerunning)
                    WriteRestartingMessage();
            } while (keepRerunning);

            return process.ExitCode;

        }

       

        private static Process StartInProcess(string exe, string[] args)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "Rerunner.exe",
                    Arguments = "\"" + string.Join("\" \"", new[] { "--single", exe }.Concat(args)) + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                }
            };
            proc.Start();

            StreamPipe pout = new StreamPipe(proc.StandardOutput.BaseStream, Console.OpenStandardOutput());
            StreamPipe perr = new StreamPipe(proc.StandardError.BaseStream, Console.OpenStandardError());
            StreamPipe pin = new StreamPipe(Console.OpenStandardInput(), proc.StandardInput.BaseStream);

            Task.Run(async () =>
            {
                pin.Connect();
                pout.Connect();
                perr.Connect();
                while (!proc.WaitForExit(0))
                    await Task.Delay(100);
                pin.Disconnect();
                pout.Disconnect();
                perr.Disconnect();
            });
            

            return proc;
        }

        private static async Task<int> StartInAppDomain(string exe, string[] args)
        {
            var exeFileInfo = new FileInfo(exe);
            if (!exeFileInfo.Exists) {
                Console.WriteLine($"exectable '{exe}' doesnt exist. Waiting for it to materialize");
                return -1;
            }
            
            var setup = new AppDomainSetup();
            setup.ShadowCopyFiles = bool.TrueString;
            setup.ApplicationBase = exeFileInfo.DirectoryName;

            if (new FileInfo(exe + ".config").Exists)
                setup.ConfigurationFile = exe + ".config";

            var newDomain = AppDomain.CreateDomain("Service", null, setup);
            var result = await Task.Run<int>(() => newDomain.ExecuteAssembly(exe, args));
            AppDomain.Unload(newDomain);
            return result;
        }

        private static void StopProcess(Process process)
        {
            if (process != null && !process.HasExited)
                process.Kill();
        }

        static void WatchDirectory(string directory, Action action)
        {
            var watcher = new FileSystemWatcher();
            watcher.Path = directory;
            watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size;
            var handler = new FileSystemEventHandler((s, a) => action());
            watcher.Changed += handler;
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += new RenamedEventHandler((s,a) => action());
            watcher.EnableRaisingEvents = true;
        }

        static void PrintInstructions()
        {
            Console.WriteLine("Usage: rerunner.exe [path-to-executable] [...args-to-pass-to-executable]");
        }

        private static void WriteRestartingMessage()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("--- RESTARTING ---");
            Console.ResetColor();
            Console.WriteLine();
        }

        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion

    }

}
