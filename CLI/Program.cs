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
                var fw = FirmwareFile.FromFile(inputFile.FullName);
                var checksumPassed = fw.Checksum();
                Console.WriteLine($"{fw.FileHeader.Magic:X}, Checksum passed: {checksumPassed}");
            });

            return await rootCmd.InvokeAsync(args);
        }
    }
}
