// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Crank.Models;
using Microsoft.Extensions.DependencyInjection;
using Repository;

namespace Microsoft.Crank.Agent
{
    public class JobsApis
    {
        private static Job GetJob(HttpContext context)
        {
            var id = Convert.ToInt32(context.Request.RouteValues["id"]);
            var jobRepository = context.RequestServices.GetRequiredService<IJobRepository>();
            var job = jobRepository.Find(id);

            return job;
        }

        public static Task GetState(HttpContext context)
        {
            var job = GetJob(context);

            if (job == null)
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            context.Response.StatusCode = 200;
            return context.Response.WriteAsync(job.State.ToString());
        }

        public static Task GetTouch(HttpContext context)
        {
            var job = GetJob(context);

            if (job == null)
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            // Mark when the job was last read to notify that the driver is still connected
            job.LastDriverCommunicationUtc = DateTime.UtcNow;
            context.Response.StatusCode = 200;

            return Task.CompletedTask;
        }        
    }
}
