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

        /// <summary>
        /// Convert the BCD byte array to it's integer representation
        /// </summary>
        /// <param name="value">The byte array to convert</param>
        /// <returns>The integer representation of the given BCD byte array (same as BCDToInteger(true)</returns>
        public static int BCDToInteger(this byte[] value)
        {
            return value.BCDToInteger(true);
        }

        /// <summary>
        /// Convert the BCD byte array to it's integer representation
        /// </summary>
        /// <param name="value">The byte array to convert</param>
        /// <param name="msb">if the frame is msg first</param>
        /// <returns>The integer representation of the given BCD byte array</returns>
        public static int BCDToInteger(this byte[] value, bool msb)
        {
            var cpt = value.Length;
            var result = 0;

            while (--cpt >= 0)
            {
                var b = value[cpt];
                var high = b >> 4;
                if (high > 0x09)
                    throw new Exception("NOT BCD !");
                var low = b & 0x0F;
                if (low > 0x09)
                    throw new Exception("NOT BCD !");

                result += msb ?
                    high * (int)Math.Pow(10, (cpt * 2) + 1) + low * (int)Math.Pow(10, cpt * 2) :
                    high * (int)Math.Pow(10, ((value.Length - (cpt + 1)) * 2) + 1) + low * (int)Math.Pow(10, (value.Length - (cpt + 1)) * 2);
            }

            return result;
        }

        public static bool IsMyKindOfEnum(this System.Linq.ParallelMergeOptions truc)
        {
            switch(truc)
            {
                case ParallelMergeOptions.AutoBuffered:
                case ParallelMergeOptions.FullyBuffered:
                    return true;
                default:
                    return false;
            }
        }

        public static void truc<T>(this T machin)
        {
            Console.WriteLine(machin.ToString());
        }
    }
}
