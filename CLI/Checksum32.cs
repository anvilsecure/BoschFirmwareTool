using System;
using System.IO;

namespace BoschFirmwareTool
{
    static class Checksum32
    {
        /// <summary>
        /// Implements a simple Checksum-32 algorithm.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static uint Checksum(Stream stream)
        {
            uint checksum = 0;
            int read = 0;
            Span<byte> buf = stackalloc byte[1024];
            
            while ((read = stream.Read(buf)) > 0)
            {
                foreach (var b in buf[..read])
                {
                    checksum += b;
                }
            }

            return checksum;
        }
    }
}
