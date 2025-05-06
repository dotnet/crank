// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Crank.Models;

namespace Repository
{
    public interface IJobRepository
    {
        string[] AllowedDomains { get; }
        Job Add(Job item);
        IEnumerable<Job> GetAll();
        Job Find(int id);
        Job Remove(int id);
        void Update(Job item);
    }
}
