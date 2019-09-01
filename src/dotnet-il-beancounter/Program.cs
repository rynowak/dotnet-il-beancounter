using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILBeanCounter
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var command = new RootCommand()
            {
                new Option(new[] { "-i", "--input", }, "input directory")
                {
                    Argument = new Argument<DirectoryInfo>(() => new DirectoryInfo(Directory.GetCurrentDirectory())),
                },

                new Option(new[] { "-g", "--group-by", }, "group results by: (assembly, namespace, type, method)")
                {
                    Argument = new Argument<Grouping>(() =>  Grouping.Type),
                },
            };
            command.Description = "counts IL from all managed assemblies";
            command.Handler = CommandHandler.Create<DirectoryInfo, Grouping>(ExecuteAsync);

            return await command.InvokeAsync(args);
        }

        public static Task<int> ExecuteAsync(DirectoryInfo directory, Grouping grouping)
        {
            var assemblies = 
                directory.EnumerateFiles("*.dll", SearchOption.AllDirectories)
                .Concat(directory.EnumerateFiles("*.exe", SearchOption.AllDirectories));

            foreach (var assembly in assemblies)
            {
                
            }

            return Task.FromResult(0);
        }
    }
}
