# Bosch Firmware Tool
A tool for extracting firmware files intended for Bosch's line of security and IoT cameras, such as the Flexidome line.

## Usage
```
BoschFirmwareTool/CLI> dotnet run -- -h # or ./boschfwtool -h if using dotnet publish-ed version

boschfwtool:
  A tool for parsing and extracting Bosch camera firmware files.

Usage:
  boschfwtool [options] <inputFile>

Arguments:
  <inputFile>    The firmware file to operate on

Options:
  -o, --output <output>    Output directory. Defaults to the current directory. [default:
                           C:\Users\moudi\Documents\GitHub\BoschFirmwareTool\CLI]
  --version                Show version information
  -?, -h, --help           Show help and usage information
```

### Example
```
BoschFirmwareTool/CLI> dotnet run -- -o output ./CPP6_H.264_6.32.0111.fw
Extracting: arm.app1.gz.T_ffff, length: 2CA118
Extracting: aacenc.dll.gz.T_ffff, length: 2F441
Extracting: ambarella.dll.gz.T_ffff, length: 1C966E
Extracting: imagepipe.dll.gz.T_ecff, length: 222927
... snip ...
```

## Development
Download the .NET Core SDK at https://dotnet.microsoft.com/download.

## Capabilities
* Can extract both obfuscated (<= 6.50) and encrypted firmware files.
* Extraction:
    * Parses file contents for both nested (e.g. targeting multiple camera types) and single header files.
    * Parses RomFS contents. RomFS files are the same binary format as files in a nested firmware, but used as immutable file storage on device, mainly for web server assets.
