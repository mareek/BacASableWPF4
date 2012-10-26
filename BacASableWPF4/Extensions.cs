using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BacASableWPF4
{
    static class Extensions
    {
        public static bool GetBit(this byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }

        public static bool[] GetBitSlice(this byte b, int startBit, int size)
        {
            return Enumerable.Range(startBit, size).Select(i => b.GetBit(i)).ToArray();
        }

        public static byte SetBit(this byte b, int bitNumber, bool bitValue)
        {
            if (bitValue)
            {
                return (byte)(b | (1 << bitNumber));

            }
            return (byte)(b & (0xFF ^ (1 << bitNumber)));
        }

        public static byte SetBitSlice(this byte b, int startBit, bool[] bitValues)
        {
            return Enumerable.Range(0, bitValues.Length).Aggregate((byte)0, (res, i) => res.SetBit(startBit + i, bitValues[i]));
        }
    }
}
