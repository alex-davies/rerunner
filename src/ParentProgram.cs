using Rerunner.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rerunner
{
    class ParentProgram
    {
        /// <summary>
        /// Parent program will monitor file changes, when
        /// changes are detected it will kill the child nodes and rerun them. This process
        /// remains alive until it is closed by the user. 
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

            Process process = null;
            var keepRerunning = true;
            var singleChildLock = new object();

            SetConsoleCtrlHandler(new HandlerRoutine(ctrlType =>
            {
                //if a kill signal has been send we will stop things
                StopProcess(process);
                keepRerunning = false;
                return true;
            }), true);

            WatchDirectory(new FileInfo(exeFilename).DirectoryName, new DelayExecution(() =>
            {
                lock (singleChildLock)
                {
                    WriteRestartingMessage();
                    StopProcess(process);
                    process = StartChild(exeFilename, exeArgs);
                }
            }, 1000).Start);


            lock (singleChildLock)
            {
                process = StartChild(exeFilename, exeArgs);
            }

            while (keepRerunning)
            {
                Thread.Sleep(100);
            }

            return 0;
        }

        private static void StopProcess(Process process)
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        private static Process StartChild(string exe, string[] args)
        {
            var exeFileInfo = new FileInfo(exe);
            string thisAppExe = Process.GetCurrentProcess().MainModule.FileName;

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = thisAppExe,
                    Arguments = "\"" + string.Join("\" \"", new[] { "--child", exeFileInfo.FullName }.Concat(args)) + "\"",
                    UseShellExecute = false,
                    WorkingDirectory = exeFileInfo.DirectoryName,
                }
            };
            proc.Start();
            return proc;
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
            watcher.Renamed += new RenamedEventHandler((s, a) => action());
            watcher.EnableRaisingEvents = true;
        }

        private static void WriteRestartingMessage()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.Write("--- RESTARTING ---");
            Console.ResetColor();
            Console.WriteLine();
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
