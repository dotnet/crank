// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Microsoft.Crank.Jobs.HttpClientClient
{
    internal class ScriptConsole
    {
        public bool HasErrors { get; private set; }

        public void Log(params object[] args)
        {
            Console.WriteLine(String.Join(" ", args.Select(x => x.ToString())));
        }

        public void Info(params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(String.Join(" ", args.Select(x => x.ToString())));
            Console.ResetColor();
        }

        public void Warn(params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(String.Join(" ", args.Select(x => x.ToString())));
            Console.ResetColor();
        }

        public void Error(params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(String.Join(" ", args.Select(x => x.ToString())));
            Console.ResetColor();

            HasErrors = true;
        }
    }
}
