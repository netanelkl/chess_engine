using System.Collections.Generic;
using System;
namespace TerraFirma
{
    /// <summary>
    /// Provides tools of operation on positions and bitboards
    /// </summary>
    internal static class Position
    {
        /// <summary>
        /// Converts a position to a bitboard
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal static ulong ToBitBoard(this int position)
        {
            return (ulong)(1L << position);
        }

        internal static ulong ToBitBoard(int row, int col)
        {
            return (ulong)(1L << (row * 8 + col));
        }

        private static ulong debruijn64 = 0x07EDD5E59A4E28C2;

        private static int[] index64 = new int[64] {
           63,  0, 58,  1, 59, 47, 53,  2,
           60, 39, 48, 27, 54, 33, 42,  3,
           61, 51, 37, 40, 49, 18, 28, 20,
           55, 30, 34, 11, 43, 14, 22,  4,
           62, 57, 46, 52, 38, 26, 32, 41,
           50, 36, 17, 19, 29, 10, 13, 21,
           56, 45, 25, 31, 35, 16,  9, 12,
           44, 24, 15,  8, 23,  7,  6,  5
            };

        public static int LSB(ulong bitBoard)
        {
            return index64[((bitBoard ^ (bitBoard & (bitBoard - 1))) * debruijn64) >> 58];
        }

        public static int MSB(ulong bitBoard)
        {
            int msb;
            do
            {
                ulong lsb = bitBoard ^ (bitBoard & (bitBoard - 1));
                msb = index64[(lsb * debruijn64) >> 58];
                bitBoard = bitBoard & ~lsb;
            } while ((bitBoard >> msb) != 0);

            return msb;
        }

        public static int[] FindPositions(ulong bitBoard)
        {
            if (bitBoard == 0)
                return new int[0];

            int[] positions = new int[Global.BitCount(bitBoard)];
            int count = 0;
            int posInt;
            while (bitBoard != 0)
            {
                posInt = index64[((bitBoard ^ (bitBoard & (bitBoard - 1))) * debruijn64) >> 58];
                positions[count] = posInt;
                bitBoard &= (bitBoard - 1);
                count++;
            }

            return positions;
        }
    }
}