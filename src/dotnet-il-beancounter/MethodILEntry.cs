using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace ILBeanCounter
{
    public class MethodILEntry
    {
        public MethodILEntry(
            MetadataReader metadata,
            MethodDefinition method,
            int headerSizeInBytes,
            int ilSizeInBytes,
            int ehSizeInBytes,
            ImmutableArray<byte> il)
        {
            Metadata = metadata;
            Method = method;
            HeaderSizeInBytes = headerSizeInBytes;
            ILSizeInBytes = ilSizeInBytes;
            EHSizeInBytes = ehSizeInBytes;
            IL = il;
        }

        public MetadataReader Metadata { get; }

        public MethodDefinition Method { get; }

        public TypeDefinition DeclaringType => Metadata.GetTypeDefinition(Method.GetDeclaringType());

        public string DeclaringTypeName
        {
            get
            {
                var type = new Nullable<TypeDefinition>(DeclaringType);

                var text = new StringBuilder();
                while (type != null)
                {
                    if (text.Length > 0)
                    {
                        text.Insert(0, "+");
                    }
                    text.Insert(0, Metadata.GetString(type.Value.Name));
                    type = type.Value.IsNested ? new Nullable<TypeDefinition>(Metadata.GetTypeDefinition(type.Value.GetDeclaringType())) : null;
                }

                return text.ToString();
            }
        }

        public TypeDefinition DeclaringTopLevelType
        {
            get
            {
                var type = DeclaringType;

                while (type.IsNested)
                {
                    type = Metadata.GetTypeDefinition(type.GetDeclaringType());
                }

                return type;
            }
        }
        public string NamespaceName => Metadata.GetString(DeclaringTopLevelType.Namespace);


        public int TotalSizeInBytes => HeaderSizeInBytes + ILSizeInBytes + EHSizeInBytes;

        public int HeaderSizeInBytes { get; }

        public int ILSizeInBytes { get; }

        public int EHSizeInBytes { get; }

        public ImmutableArray<byte> IL { get; }
    }
}