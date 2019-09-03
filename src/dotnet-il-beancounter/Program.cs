using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace ILBeanCounter
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var command = new RootCommand()
            {
                new Option(new[] { "-i", "--input", }, "input directory")
                {
                    Argument = new Argument<DirectoryInfo>("directory", new DirectoryInfo(Directory.GetCurrentDirectory())),
                },

                new Option(new[] { "-g", "--group-by", }, "group results by: (assembly, namespace, type, method)")
                {
                    Argument = new Argument<Grouping>("grouping", Grouping.Assembly),
                },
            };

            command.Description = "counts IL from all managed assemblies";
            command.Handler = CommandHandler.Create<DirectoryInfo, Grouping>((input, groupBy) =>
            {
                return ExecuteAsync(input, groupBy);
            });

            return await command.InvokeAsync(args);
        }

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

                Console.WriteLine($"Total size is: {pe.GetEntireImage().Length} bytes");

                var sum = pe.PEHeaders.PEHeader.SizeOfHeaders;
                Console.WriteLine($"PE Header is {pe.PEHeaders.PEHeader.SizeOfHeaders} bytes");

                // We don't attempt to exhaustively catalog all of the things PE format supports, just
                // the stuff that's likely to contribute to a .NET assembly's size in significant ways.
                sum += pe.PEHeaders.PEHeader.CertificateTableDirectory.Size;
                if (pe.PEHeaders.PEHeader.CertificateTableDirectory.Size > 0)
                {
                    Console.WriteLine($"Certificate Table is {pe.PEHeaders.PEHeader.CertificateTableDirectory.Size} bytes");
                }

                foreach (var section in pe.PEHeaders.SectionHeaders)
                {
                    sum += section.SizeOfRawData;
                    Console.WriteLine($"Section {section.Name} is {section.SizeOfRawData} bytes");
                }

                if (pe.GetEntireImage().Length - sum > 0)
                {
                    Console.WriteLine($"Unaccounted size: {pe.GetEntireImage().Length - sum} bytes");
                }
                Console.WriteLine();

                if (!pe.HasMetadata)
                {
                    Console.WriteLine($"{assembly} has no metadata -- skipping");
                    Console.WriteLine();
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine("Analyzing .text section and COR header (used by .NET)");

                sum = 0;
                sum += pe.PEHeaders.CorHeader.MetadataDirectory.Size;
                Console.WriteLine($"Metadata is {pe.PEHeaders.CorHeader.MetadataDirectory.Size} bytes");

                sum += pe.PEHeaders.CorHeader.StrongNameSignatureDirectory.Size;
                Console.WriteLine($"Strong-Name Signature is {pe.PEHeaders.CorHeader.StrongNameSignatureDirectory.Size} bytes");
                
                sum += pe.PEHeaders.CorHeader.ResourcesDirectory.Size;
                Console.WriteLine($"Resources is {pe.PEHeaders.CorHeader.ResourcesDirectory.Size} bytes");

                var metadata = pe.GetMetadataReader();
                var methods = MethodILReader.ReadMethods(pe);
                var ilSize = methods.Sum(m => m.TotalSizeInBytes);
                sum += ilSize;
                Console.WriteLine($"IL is {ilSize} bytes");

                if (pe.GetSectionData(".text").Length - sum > 0)
                {
                    Console.WriteLine($"Unaccounted size: {pe.GetSectionData(".text").Length - sum} bytes");
                }
                Console.WriteLine();

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

        private static IEnumerable<IGrouping<string, MethodILEntry>> GroupByNamespace(PEReader pe, ImmutableArray<MethodILEntry> methods)
        {
            var metadata = pe.GetMetadataReader();
            return methods.GroupBy(m => metadata.GetString(metadata.GetTypeDefinition(m.Method.GetDeclaringType()).Namespace));
        }
    }
}
