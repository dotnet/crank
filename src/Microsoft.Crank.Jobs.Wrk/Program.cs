using System;
using System.Diagnostics;

namespace Microsoft.Crank.Jobs.Wrk
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("wrk Client");
            Console.WriteLine("args: " + String.Join(' ', args));

            var wrkFilename = "./wrk";
            Process.Start("chmod", "+x " + wrkFilename);

            var process = Process.Start(wrkFilename, String.Join(' ', args));
            
            process.WaitForExit();
        }
    }
}
