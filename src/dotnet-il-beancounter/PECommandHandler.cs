using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace ILBeanCounter
{
    public static class PECommandHandler
    {
        public static Task<int> ExecuteAsync(DirectoryInfo directory)
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

                Console.WriteLine();
            }

            return Task.FromResult(0);
        }
    }
}