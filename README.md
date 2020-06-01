# Bosch Firmware Tool
A tool for extracting firmware files intended for Bosch's line of security and IoT cameras, such as the Flexidome line.

## Capabilities
* Currently supports obfuscated firmware revisions. Largely pre-6.5.
* Extraction:
    * Detects nested / stacked headers
    * Parses file contents for both nested and single header files
    * Can optionally parse RomFS contents. RomFS files are the same binary format as files in a nested firmware.
    * Can optionally decompress RomFS contents upon extraction. Many files are usually GZipped.

## Upcoming Features
* Supports for encrypted firmware. Versions roughly post-6.5 encrypt data segments and have an additional 0x100 byte blob in the file headers.

## Firmware Format

### Root header
Each file has a 0x400 byte header with metadata about the file (size, targets, checksum, etc.). All fields are in big-endian.
```
[0x000]: Magic - 0x10122003
[0x004]: Target - Likely device type. 0x10 indicates nested headers, however.
[0x008]: Variant - Device variant.
[0x00C]: Version - Firmware version
[0x010]: Length - length of data following this header, minus 0x400 bytes header.
[0x014]: Base - Unknown
[0x018]: Checksum - A Checksum-32 over the data following this header, up to Length bytes.
[0x01C]: Type - Unknown
[0x020]: NegativeList - 0x20 bytes - Unknown, possibly devices blacklisted from applying this list. Further reverse engineering needed.
[0x044]: Length - Always same as first length field, unknown purpose.
[0x048]: Unknown
[0x04C]: 0x100 bytes - Unknown, possibly RSA signature. Nested headers generally do not have this data.
```

### File header
Data following the root file header can be one of two formats:
* A single file, with no additional headers.
* A nested header, with a section of files. This format has multiple files concatenated together in the form `[file header][data]`, with a final "null" terminating header
* File headers follow the format (fields are big-endian):
```
[0x00]: Magic - 0xDEADAFFE
[0x04]: Offset to next header
[0x08]: 0x20 bytes null terminated string - Filename
[0x28]: File length
[0x2C-0x40]: Blank / `\x00` bytes
```
* File length can be shorter than the offset to the next header, as files are padded to the nearest 0x10 bytes.
* A set of files will be terminated with a file header with all zero fields, except magic.

### Obfuscation
Firmware files before approximately version 6.5 will have obfuscated data segments. This obfuscation is a simple XOR over the data with the byte `\x42`, or 'B'.

### Encryption
Current files have encrypted segments. Algorithm is currently unknown.