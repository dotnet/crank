// Copyright (c) .NEServerJob Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Benchmarks.Models;

namespace Repository
{
    public interface IJobRepository
    {
        ServerJob Add(ServerJob item);
        IEnumerable<ServerJob> GetAll();
        ServerJob Find(int id);
        ServerJob Remove(int id);
        void Update(ServerJob item);
    }
}
