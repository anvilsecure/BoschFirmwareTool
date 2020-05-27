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
                new Argument<FileInfo>("inputFile", "The firmware file to operate on").ExistingOnly()
            };

            rootCmd.Handler = CommandHandler.Create<FileInfo>((inputFile) =>
            {
                using var file = File.OpenRead(inputFile.FullName);
                using var firmwareFile = new BoschFirmware(file);

                foreach (var h in firmwareFile.Headers)
                {
                    Console.WriteLine(h.ToString());
                }
            });

            return await rootCmd.InvokeAsync(args);
        }
    }
}
