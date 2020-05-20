using System;
using System.Collections.Generic;
using System.Text;

namespace boschfwtool
{
    static class Checksum32
    {
        /// <summary>
        /// Implements a simple Checksum-32 algorithm.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static uint Checksum(ReadOnlySpan<byte> source)
        {
            uint checksum = 0;
            foreach (var b in source)
            {
                checksum += b;
            }

            return checksum;
        }
    }
}
