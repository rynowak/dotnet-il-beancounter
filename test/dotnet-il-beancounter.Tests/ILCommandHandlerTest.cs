using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ILBeanCounter
{
    public class ILCommandHandlerTest
    {
        public ILCommandHandlerTest()
        {
            Directory = new DirectoryInfo(Path.GetDirectoryName(typeof(ILCommandHandler).Assembly.Location));
        }

        public DirectoryInfo Directory { get; }

        [Theory]
        [InlineData(Grouping.Assembly)]
        [InlineData(Grouping.Namespace)]
        [InlineData(Grouping.Type)]
        [InlineData(Grouping.Method)]
        public async Task SanityCheck(Grouping grouping)
        {
            await ILCommandHandler.ExecuteAsync(Directory, grouping, filter: null);
        }
    }
}