// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.Crank.Controller
{
    public class ScriptFile
    {
        public string ReadFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            return File.ReadAllText(filename);
        }

        public void WriteFile(string filename, string data)
        {
            if (String.IsNullOrEmpty(filename))
            {
                return;
            }

            File.WriteAllText(filename, data);
        }

        public bool Exists(string filename)
        {
            return !String.IsNullOrEmpty(filename) && File.Exists(filename);
        }
    }
}
