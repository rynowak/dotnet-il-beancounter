using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ILBeanCounter
{
    public class PECommandHandlerTest
    {
        public PECommandHandlerTest()
        {
            Directory = new DirectoryInfo(Path.GetDirectoryName(typeof(PECommandHandler).Assembly.Location));
        }

        public DirectoryInfo Directory { get; }

        [Fact]
        public async Task SanityCheck()
        {
            await PECommandHandler.ExecuteAsync(Directory);
        }
    }
}