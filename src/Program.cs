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
            if (args.Length > 0 && args[0] == "--child")
            {
                return ChildProgram.Main(args.Skip(1).ToArray());
            }
            else
            {
                return ParentProgram.Main(args);
            }
        }
    }

}
