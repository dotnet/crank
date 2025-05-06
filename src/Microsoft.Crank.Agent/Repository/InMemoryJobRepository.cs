// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crank.Models;

namespace Repository
{
    public class InMemoryJobRepository(IEnumerable<string> allowedDomains = null) : IJobRepository
    {
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<int, Job> _items = new ConcurrentDictionary<int, Job>();
        private int _nextId = 1;

        public string[] AllowedDomains { get; } = allowedDomains?.ToArray() ?? [];

        public Job Add(Job item)
        {
            if (item.Id != 0)
            {
                throw new ArgumentException("item.Id must be 0.");
            }

            lock (_lock)
            {
                var id = _nextId;
                _nextId++;
                item.Id = id;
                _items[id] = item;
                return item;
            }
        }

        public Job Find(int id)
        {
            _items.TryGetValue(id, out var job);

            return job;
        }

        public IEnumerable<Job> GetAll()
        {
            return _items.Values;
        }

        public Job Remove(int id)
        {
            _items.TryRemove(id, out var job);

            return job;
        }

        public void Update(Job item)
        {
            var oldItem = Find(item.Id);

            if (!object.ReferenceEquals(item, oldItem))
            {
                _items[item.Id] = item;
            }
        }
    }
}
