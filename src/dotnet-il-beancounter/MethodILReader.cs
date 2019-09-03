using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILBeanCounter
{
    // See: http://pzsolt.blogspot.com/2005/12/fat-vs-tiny-method-header.html
    //
    // This is based on a summary of the spec there, and this code is heavily inspired
    // by the structure of the samples.
    public static class MethodILReader
    {
        public static ImmutableArray<MethodILEntry> ReadMethods(PEReader pe)
        {
            var results = ImmutableArray.CreateBuilder<MethodILEntry>();

            var metadata = pe.GetMetadataReader();
            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                results.Add(ReadMethod(pe, metadata, method));
            }

            return results.ToImmutable();
        }

        private static MethodILEntry ReadMethod(PEReader pe, MetadataReader metadata, MethodDefinition method)
        {
            if (method.RelativeVirtualAddress == 0)
            {
                return new MethodILEntry(metadata, method, 0, 0, 0, ImmutableArray<byte>.Empty);
            }

            var section = pe.GetSectionData(method.RelativeVirtualAddress);
            var reader = section.GetReader();

            int headerSize;
            int ilSize;
            int ehSize;
            byte[] il;

            var headerByte1 = reader.ReadByte();
            if ((headerByte1 & 0x3) == 2)
            {
                // Tiny header
                headerSize = 1;
                ilSize = headerByte1 >> 2;
                ehSize = 0;

                il = new byte[ilSize];
                reader.ReadBytes(ilSize, il, bufferOffset: 0);
            }
            else if ((headerByte1 & 0x3) == 3)
            {
                // Fat header
                var hasMoreSections = (headerByte1 & 0x8) == 0x8;

                var headerByte2 = reader.ReadByte();
                headerSize = (headerByte2 >> 4) * 4;

                _ = reader.ReadUInt16(); // max stack
                ilSize = reader.ReadInt32();
                _ = reader.ReadUInt32(); // LocalVarSig

                il = new byte[ilSize];
                reader.ReadBytes(ilSize, il, bufferOffset: 0);

                ehSize = 0;
                while (hasMoreSections)
                {
                    ehSize += ReadEHSection(reader, ref hasMoreSections);
                }
            }
            else
            {
                throw new Exception("Invalid method header.");
            }

            return new MethodILEntry(metadata, method, headerSize, ilSize, ehSize, ImmutableArray.Create(il));
        }

        private static int ReadEHSection(BlobReader reader, ref bool hasMoreSections)
        {
            reader.Align(4);

            var sectionHeader = reader.ReadByte();
            var isEHSection = (sectionHeader & 0x1) == 0x1;
            if (!isEHSection)
            {
                throw new Exception("unknown section type");
            }

            hasMoreSections = (sectionHeader & 0x8) == 0x8;

            int sectionLength;
            if ((sectionHeader & 0x4) == 0x4)
            {
                // Fat header
                sectionLength = reader.ReadByte()
                    + reader.ReadByte() * 0x100
                    + reader.ReadByte() * 0x10000;
            }
            else
            {
                sectionLength = reader.ReadByte();
                reader.ReadBytes(2); // padding, all of the sections have the same size header
            }

            // advance past the section
            reader.ReadBytes(sectionLength - 4);
            return sectionLength;
        }
    }
}