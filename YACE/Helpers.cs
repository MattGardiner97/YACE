using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace YACE
{
    public static class Helpers
    {
        public static byte ReadNibble(ushort Value, int NibbleIndex)
        {
            switch (NibbleIndex)
            {
                case 0:
                    return (byte)(Value & 0xF);
                case 1:
                    return (byte)((Value & 0xF0) >>4);
                case 2:
                    return (byte)((Value & 0xF00) >> 8);
                case 3:
                    return (byte)((Value & 0xF000) >> 12);
                default:
                    throw new ArgumentException("NibbleIndex cannot be greater than 3");
            }
        }

    }
}
