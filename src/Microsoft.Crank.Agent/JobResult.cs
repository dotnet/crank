// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crank.Models;

namespace Microsoft.Crank.Agent
{
    public class JobResult
    {
        public JobResult(Job job, IUrlHelper urlHelper)
        {
            Id = job.Id;
            RunId = job.RunId;
            State = job.State.ToString();
            DetailsUrl = urlHelper.ActionLink("GetById", "Jobs", new { Id });
            BuildLogsUrl = urlHelper.ActionLink("BuildLog", "Jobs", new { Id });
            OutputLogsUrl = urlHelper.ActionLink("Output", "Jobs", new { Id });
        }

        public int Id { get; set; }
        public string RunId { get; set; }
        public string State { get; set;}
        public string DetailsUrl { get; set; }
        public string BuildLogsUrl { get; set; }
        public string OutputLogsUrl { get; set; }
    }
}
