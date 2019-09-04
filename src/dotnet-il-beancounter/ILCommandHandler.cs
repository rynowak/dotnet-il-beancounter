using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace ILBeanCounter
{
    public static class ILCommandHandler
    {
        public static Task<int> ExecuteAsync(DirectoryInfo directory, Grouping grouping, string filter)
        {
            var assemblies =
                directory.EnumerateFiles("*.dll", SearchOption.AllDirectories)
                .Concat(directory.EnumerateFiles("*.exe", SearchOption.AllDirectories));

            var methods = new List<MethodILEntry>();
            foreach (var assembly in assemblies)
            {
                PEReader pe;
                using (var stream = assembly.OpenRead())
                {
                    pe = new PEReader(stream, PEStreamOptions.PrefetchEntireImage);
                }

                if (!pe.HasMetadata)
                {
                    continue;
                }

                var metadata = pe.GetMetadataReader();
                methods.AddRange(MethodILReader.ReadMethods(pe));
            }

            IEnumerable<IGrouping<string, MethodILEntry>> groups;
            switch (grouping)
            {
                case Grouping.Assembly:
                    {
                        groups = methods.GroupBy(m => m.AssemblyName);
                        break;
                    }

                case Grouping.Namespace:
                    {
                        groups = methods.GroupBy(m => m.NamespaceName);
                        break;
                    }

                case Grouping.Type:
                    {
                        groups = methods.GroupBy(m => m.FullyQualifiedTypeName);
                        break;
                    }

                case Grouping.Method:
                    {
                        groups = methods.GroupBy(m => $"{m.FullyQualifiedTypeName}.{m.MethodName}");
                        break;
                    }

                default:
                    {
                        throw new Exception($"Unknown grouping type: {grouping}.");
                    }
            }

            if (filter != null)
            {
                groups = groups.Where(g => g.Key.StartsWith(filter));
            }

            foreach (var group in groups)
            {
                Console.WriteLine($"{group.Key}: {group.Sum(m => m.TotalSizeInBytes)} bytes");
            }
            
            return Task.FromResult(0);
        }
    }
}