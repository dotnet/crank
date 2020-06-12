// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Crank.Agent.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [HttpGet("info")]
        public IActionResult Info()
        {
            return Json(new
            {
                hw = Startup.Hardware.ToString(),
                env = Startup.HardwareVersion.ToString(),
                os = Startup.OperatingSystem.ToString(),
                arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                proc = Environment.ProcessorCount
            });
        }
    }
}
