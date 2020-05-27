using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace BoschFirmwareTool.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCmd = new RootCommand("A tool for parsing and extracting Bosch camera firmware files.")
            {
                new Argument<FileInfo>("inputFile", "The firmware file to operate on").ExistingOnly(),
                new Option<DirectoryInfo>(new [] { "--output", "-o" },
                    () => { return new DirectoryInfo(Directory.GetCurrentDirectory()); },
                     "Output directory. Defaults to the current directory.")
            };

            rootCmd.Handler = CommandHandler.Create<FileInfo, DirectoryInfo>((inputFile, output) =>
            {
                try
                {
                    using var file = File.OpenRead(inputFile.FullName);
                    using var firmwareFile = new BoschFirmware(file);

                    var directory = Directory.CreateDirectory(output.FullName);
                    foreach (var f in firmwareFile.Files)
                    {
                        var fpath = Path.Combine(directory.FullName, f.Header.Filename);
                        Console.WriteLine($"Writing: {fpath}");
                        using var newFile = File.Create(fpath);
                        newFile.Write(f.Contents);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Operation failed: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            });

            return await rootCmd.InvokeAsync(args);
        }
    }
}
