using System;
using System.Collections.Generic;

using System.Text;

namespace TerraFirma
{
    /// <summary>
    /// Used to create zobrist random numbers used in the board hash creation
    /// </summary>
    internal static class BoardComparar
    {
        internal static int[][] HashRandoms = new int[64][];
        internal static int[][] HashCollisions = new int[64][];
        internal static int[] CastleStatus = new int[17];
        internal static int[] EnPassantStatus = new int[10];
        internal static int[] CurrentPlayer = new int[2];


        static BoardComparar()
        {
            Random rand = new Random();

            //initialize hash randod keys
            for (int i = 0; i < 64; i++)
            {
                HashRandoms[i] = new int[12];
                HashCollisions[i] = new int[12];
                for (int k = 0; k < 12; k++)
                {
                    HashRandoms[i][k] = rand.Next();
                    HashCollisions[i][k] = rand.Next();
                }
            }

            for (int i = 1; i < 17; i++)
            {
                CastleStatus[i] = rand.Next();
            }
            for(int i=0;i<9;i++)
                EnPassantStatus[i] = rand.Next();

            CurrentPlayer[0] = rand.Next();
            CurrentPlayer[1] = rand.Next();
        }
    }
}
