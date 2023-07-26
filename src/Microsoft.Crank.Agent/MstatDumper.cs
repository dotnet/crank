// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Crank.Agent
{
    internal static class MstatDumper
    {
        internal static DumperResults GetInfo(string path)
        {
            var mstats = Directory.EnumerateFiles(path, "*.mstat", SearchOption.AllDirectories);

            if (mstats == null || !mstats.Any())
            {
                return null;
            }

            var fileName = mstats.First();

            Log.Info($"Begin read mstat file [{fileName}]");

            try
            {
                using var asm = AssemblyDefinition.ReadAssembly(fileName);
                var globalType = (TypeDefinition)asm.MainModule.LookupToken(0x02000001);

                int versionMajor = asm.Name.Version.Major;

                var types = globalType.Methods.First(x => x.Name == "Types");
                var typeStats = GetTypes(versionMajor, types).ToList();
                var typeSize = typeStats.Sum(x => x.Size);
                var typesByModules = typeStats.GroupBy(x => x.Type.Scope).Select(x => new { x.Key.Name, Sum = x.Sum(x => x.Size) }).ToList();
                var typeResult = new List<DumperResultItem>();
                foreach (var m in typesByModules.OrderByDescending(x => x.Sum))
                {
                    typeResult.Add(new DumperResultItem
                    {
                        Name = m.Name.Replace("`", "\\`"),
                        Size = m.Sum
                    });
                }

                var methods = globalType.Methods.First(x => x.Name == "Methods");
                var methodStats = GetMethods(versionMajor, methods).ToList();
                var methodSize = methodStats.Sum(x => x.Size + x.GcInfoSize + x.EhInfoSize);
                var methodsByModules = methodStats.GroupBy(x => x.Method.DeclaringType.Scope).Select(x => new { x.Key.Name, Sum = x.Sum(x => x.Size + x.GcInfoSize + x.EhInfoSize) }).ToList();
                var methodResult = new List<DumperResultItem>();
                foreach (var m in methodsByModules.OrderByDescending(x => x.Sum))
                {
                    methodResult.Add(new DumperResultItem
                    {
                        Name = m.Name.Replace("`", "\\`"),
                        Size = m.Sum
                    });
                }

                string FindNamespace(TypeReference type)
                {
                    var current = type;
                    while (true)
                    {
                        if (!string.IsNullOrEmpty(current.Namespace))
                        {
                            return current.Namespace;
                        }

                        if (current.DeclaringType == null)
                        {
                            return current.Name;
                        }

                        current = current.DeclaringType;
                    }
                }

                var methodsByNamespace = methodStats.Select(x => new TypeStats { Type = x.Method.DeclaringType, Size = x.Size + x.GcInfoSize + x.EhInfoSize }).Concat(typeStats).GroupBy(x => FindNamespace(x.Type)).Select(x => new { x.Key, Sum = x.Sum(x => x.Size) }).ToList();
                var namespaceResult = new List<DumperResultItem>();
                foreach (var m in methodsByNamespace.OrderByDescending(x => x.Sum))
                {
                    namespaceResult.Add(new DumperResultItem
                    {
                        Name = m.Key.Replace("`", "\\`"),
                        Size = m.Sum
                    });
                }

                var blobs = globalType.Methods.First(x => x.Name == "Blobs");
                var blobStats = GetBlobs(blobs).ToList();
                var blobSize = blobStats.Sum(x => x.Size);
                var blobResult = new List<DumperResultItem>();
                foreach (var m in blobStats.OrderByDescending(x => x.Size))
                {
                    blobResult.Add(new DumperResultItem
                    {
                        Name = m.Name.Replace("`", "\\`"),
                        Size = m.Size
                    });
                }

                return new DumperResults
                {
                    TypeTotalSize = typeSize,
                    TypeStats = typeResult,
                    MethodTotalSize = methodSize,
                    MethodStats = methodResult,
                    BlobTotalSize = blobSize,
                    BlobStats = blobResult,
                    NamespaceStats = namespaceResult,
                };
            }
            catch (Exception e)
            {
                Log.Error(e, $"Read mstat file [{fileName}] failed");
                return null;
            }
        }

        private static IEnumerable<TypeStats> GetTypes(int formatVersion, MethodDefinition types)
        {
            int entrySize = formatVersion == 1 ? 2 : 3;

            types.Body.SimplifyMacros();
            var il = types.Body.Instructions;
            for (int i = 0; i + entrySize < il.Count; i += entrySize)
            {
                var type = (TypeReference)il[i + 0].Operand;
                var size = (int)il[i + 1].Operand;
                yield return new TypeStats
                {
                    Type = type,
                    Size = size
                };
            }
        }

        private static IEnumerable<MethodStats> GetMethods(int formatVersion, MethodDefinition methods)
        {
            int entrySize = formatVersion == 1 ? 4 : 5;

            methods.Body.SimplifyMacros();
            var il = methods.Body.Instructions;
            for (int i = 0; i + entrySize < il.Count; i += entrySize)
            {
                var method = (MethodReference)il[i + 0].Operand;
                var size = (int)il[i + 1].Operand;
                var gcInfoSize = (int)il[i + 2].Operand;
                var ehInfoSize = (int)il[i + 3].Operand;
                yield return new MethodStats
                {
                    Method = method,
                    Size = size,
                    GcInfoSize = gcInfoSize,
                    EhInfoSize = ehInfoSize
                };
            }
        }

        private static IEnumerable<BlobStats> GetBlobs(MethodDefinition blobs)
        {
            blobs.Body.SimplifyMacros();
            var il = blobs.Body.Instructions;
            for (int i = 0; i + 2 < il.Count; i += 2)
            {
                var name = (string)il[i + 0].Operand;
                var size = (int)il[i + 1].Operand;
                yield return new BlobStats
                {
                    Name = name,
                    Size = size
                };
            }
        }
    }

    public class DumperResults
    { 
        public int TypeTotalSize { get; set; }
        public List<DumperResultItem> TypeStats { get; set; }
        public int MethodTotalSize { get; set; }
        public List<DumperResultItem> MethodStats { get; set; }
        public int BlobTotalSize { get; set; }
        public List<DumperResultItem> BlobStats { get; set; }
        public List<DumperResultItem> NamespaceStats { get; set; }
    }

    public class DumperResultItem
    {
        public string Name { get; set; }
        public int Size { get; set; }
    }

    public class TypeStats
    {
        public string MethodName { get; set; }
        public TypeReference Type { get; set; }
        public int Size { get; set; }
    }

    public class MethodStats
    {
        public MethodReference Method { get; set; }
        public int Size { get; set; }
        public int GcInfoSize { get; set; }
        public int EhInfoSize { get; set; }
    }

    public class BlobStats
    {
        public string Name { get; set; }
        public int Size { get; set; }
    }
}
