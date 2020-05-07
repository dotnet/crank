// Copyright (c) .NEServerJob Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Crank.Models;

namespace Repository
{
    public interface IJobRepository
    {
        Job Add(Job item);
        IEnumerable<Job> GetAll();
        Job Find(int id);
        Job Remove(int id);
        void Update(Job item);
    }
}
