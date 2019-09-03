using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace ILBeanCounter
{
    public static class ILCommandHandler
    {
        public static Task<int> ExecuteAsync(DirectoryInfo directory, Grouping grouping)
        {
            var assemblies = 
                directory.EnumerateFiles("*.dll", SearchOption.AllDirectories)
                .Concat(directory.EnumerateFiles("*.exe", SearchOption.AllDirectories));

            foreach (var assembly in assemblies)
            {
                Console.WriteLine($"Processing: {assembly}");

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
                var methods = MethodILReader.ReadMethods(pe);

                switch (grouping)
                {
                    case Grouping.Assembly:
                    {
                        // Do nothing, we already output the IL size at this level in the summary.
                        break;
                    }

                    case Grouping.Namespace:
                    {
                        Console.WriteLine("IL sizes grouped by namespace");
                        var methodsByNamespace = methods.GroupBy(m => m.NamespaceName);
                        foreach (var @namespace in methodsByNamespace)
                        {
                            Console.WriteLine($"{@namespace.Key}: {@namespace.Sum(m => m.TotalSizeInBytes)} bytes");
                        }
                        break;
                    }

                    case Grouping.Type:
                    {
                        Console.WriteLine("IL sizes grouped by type");
                        var methodsByType = methods.GroupBy(m => m.DeclaringTopLevelType);
                        foreach (var type in methodsByType)
                        {
                            var @namespace = metadata.GetString(type.Key.Namespace);
                            Console.WriteLine($"{@namespace}{metadata.GetString(type.Key.Name)}: {@type.Sum(m => m.TotalSizeInBytes)} bytes");
                        }
                        break;
                    }

                    case Grouping.Method:
                    {
                        Console.WriteLine("IL sizes grouped by method");
                        foreach (var method in methods)
                        {
                            Console.WriteLine($"{@method.NamespaceName}{method.DeclaringTypeName}: {method.TotalSizeInBytes} bytes");
                        }
                        break;
                    }
                }

                Console.WriteLine();
            }

            return Task.FromResult(0);
        }
    }
}