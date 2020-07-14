// Copyright (c) .NEServerJob Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Benchmarks.Models;

namespace Repository
{
    public class InMemoryJobRepository : IJobRepository
    {
        private readonly object _lock = new object();
        private readonly List<ServerJob> _items = new List<ServerJob>();
        private int _nextId = 1;

        public ServerJob Add(ServerJob item)
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
                _items.Add(item);
                return item;
            }
        }

        public ServerJob Find(int id)
        {
            lock (_lock)
            {
                var items = _items.Where(job => job.Id == id);
                Debug.Assert(items.Count() == 0 || items.Count() == 1);
                return items.FirstOrDefault();
            }
        }

        public IEnumerable<ServerJob> GetAll()
        {
            lock (_lock)
            {
                return _items.ToArray();
            }
        }

        public ServerJob Remove(int id)
        {
            lock (_lock)
            {
                var item = Find(id);
                if (item == null)
                {
                    throw new ArgumentException($"Could not find item with Id '{id}'.");
                }
                else
                {
                    _items.Remove(item);
                    return item;
                }
            }
        }

        public void Update(ServerJob item)
        {
            lock (_lock)
            {
                var oldItem = Find(item.Id);
                _items[_items.IndexOf(oldItem)] = item;
            }
        }
    }
}
