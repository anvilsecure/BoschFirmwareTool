using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace BoschFirmwareTool.CLI
{
    class Program
    {
        static void ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            Console.WriteLine($"Extracting: {e.FileName}, length: {e.FileSize:X}");
        }

        static async Task<int> Main(string[] args)
        {
            var rootCmd = new RootCommand("A tool for parsing and extracting Bosch camera firmware files.")
            {
                new Argument<FileInfo>("inputFile", "The firmware file to operate on").ExistingOnly(),
                new Option<DirectoryInfo>(new [] { "--output", "-o" },
                    () => { return new DirectoryInfo(Directory.GetCurrentDirectory()); },
                     "Output directory. Defaults to the current directory.")
            };

            rootCmd.Handler = CommandHandler.Create((FileInfo inputFile, DirectoryInfo output) =>
            {
                try
                {
                    using var firmware = BoschFirmware.FromFile(inputFile.FullName);
                    firmware.ExtractProgress += ExtractProgress;
                    firmware.ExtractAll(output.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Operation failed: {ex.Message}");
                }
            });

            return await rootCmd.InvokeAsync(args);
        }
    }
}
