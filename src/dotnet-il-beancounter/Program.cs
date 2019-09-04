using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace ILBeanCounter
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var pe = new Command("pe", "analyze PE characteristics")
            {
                new Option(new[] { "-i", "--input", }, "input directory")
                {
                    Argument = new Argument<DirectoryInfo>("directory", new DirectoryInfo(Directory.GetCurrentDirectory())),
                },
            };

            pe.Description = "shows aggregate information about PE files and sections";
            pe.Handler = CommandHandler.Create<DirectoryInfo>((input) =>
            {
                return PECommandHandler.ExecuteAsync(input);
            });

            var il = new Command("il", "analyze IL characteristics")
            {
                new Option(new[] { "-i", "--input", }, "input directory")
                {
                    Argument = new Argument<DirectoryInfo>("directory", new DirectoryInfo(Directory.GetCurrentDirectory())),
                },

                new Option(new[] { "-g", "--group-by", }, "group results by: (assembly, namespace, type, method)")
                {
                    Argument = new Argument<Grouping>("grouping", Grouping.Assembly),
                },

                new Option(new[] { "-f", "--filter", }, "filter results by prefix")
                {
                    Argument = new Argument<string>("prefix"),
                }
            };

            il.Description = "shows detailed information about the size of IL in assemblies";
            il.Handler = CommandHandler.Create<DirectoryInfo, Grouping, string>((input, groupBy, filter) =>
            {
                return ILCommandHandler.ExecuteAsync(input, groupBy, filter);
            });

            var command = new RootCommand();
            command.AddCommand(il);
            command.AddCommand(pe);

            command.Description = "tools for exploring IL assemblies";

            return await command.InvokeAsync(args);
        }
    }
}
