using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TerraFirma
{
    /// <summary>
    /// Represents a chess move
    /// </summary>
    public sealed class Move
    {
        /// <summary>
        /// The TerraFirma.Piece move piece
        /// </summary>
        public int MovingPiece;

        /// <summary>
        /// The TerraFirma.MoveTypes move type
        /// </summary>
        public int MoveType;

        /// <summary>
        /// The TerraFirma.Piece captured piece
        /// </summary>
        public int CapturedPiece;

        /// <summary>
        /// The source square
        /// </summary>
        public int SourceSquare;

        /// <summary>
        /// The destination square
        /// </summary>
        public int DestinationSquare;

        /// <summary>
        /// The side of the player
        /// </summary>
        public int PlayerSide { get { return MovingPiece & 0x1; } }

        /// <summary>
        /// retrieves an algebraic (not PGN, file-rank-file-rank)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            char sourceCol = (char)((SourceSquare & 7) + (int)'a');
            char destCol = (char)((DestinationSquare & 7) + (int)'a');

            return sourceCol.ToString() + (SourceSquare / 8 + 1).ToString() + destCol + (DestinationSquare / 8 + 1).ToString();
        }

        [XmlIgnore]
        internal string SourceSquareString
        {
            get
            {
                return (char)((SourceSquare & 7) + (int)'a') + ((SourceSquare >> 3) + 1).ToString(); ;
            }
        }

        [XmlIgnore]
        internal char SourceRank
        {
            get
            {
                return (char)((SourceSquare >> 3) + 1 + (int)'0') ;
            }
        }

        [XmlIgnore]
        internal char SourceFile
        {
            get
            {
                return (char)((SourceSquare & 7) + (int)'a');
            }
        }

        [XmlIgnore]
        internal string DestinationSquareString
        {
            get
            {
                return (char)((DestinationSquare & 7) + (int)'a') + ((DestinationSquare >> 3) + 1).ToString();
            }
        }

        [XmlIgnore]
        internal char DestinationRank
        {
            get
            {
                return (char)((DestinationSquare >> 3) + 1 + (int)'0');
            }
        }

        [XmlIgnore]
        internal char DestinationFile
        {
            get
            {
                return (char)((DestinationSquare & 7) + (int)'a'); ;
            }
        }

        /// <summary>
        /// Checks whether the current move is the same as _other
        /// </summary>
        /// <param name="_other">The other move</param>
        /// <returns>True if the moves are equal, false otherwise</returns>
        internal bool EqualMove(Move _other)
        {
            if (this.SourceSquare == _other.SourceSquare && this.DestinationSquare == _other.DestinationSquare &&
                this.CapturedPiece == _other.CapturedPiece && this.MoveType == _other.MoveType &&
                this.MovingPiece == _other.MovingPiece && this.PlayerSide == _other.PlayerSide)
                return true;

            return false;
        }
    }

    /// <summary>
    /// Used to compare moves (move ordering)
    /// </summary>
    internal sealed class MoveComparar : IComparer<Move>
    {
        //instead of calculating how close is the move to the center, why won't we just store it
        // (promotion will be benefited because of MoveType preference

        internal static MoveComparar Comparer;

        #region IComparer<Move> Members

        static MoveComparar()
        {
            Comparer = new MoveComparar();
        }

        //gives priority for moves ending near the center.
        private static int[] m_DestinationPriority = new int[64]
        {   
           0,  0,  0,  0,  0,  0,  0,  0, 
           2,  2, 2,   2,  2,  2,  2,  2, 
          -4, -2,  3,  6,  6,  3, -2, -4, 
          -4, -2,  4, 25, 25,  4, -2, -4, 
           0,  0,  6, 16, 16,  6,  0,  0, 
           0,  1,  6, 16, 16,  6,  1,  0, 
           0,  0,  6, 16, 16,  6,  0,  0, 
           0,  0,  0,  0,  0,  0,  0,  0  
        };

        /// <summary>
        /// Compares two Moves. It's accurance is crucical for the speed of the chess engine.
        /// bad move ordering and we might get a minimax. great move ordering and the alpha beta will work best.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(Move x, Move y)
        {
            //first we check if the moves are equal
            if (x == y)
                return 0;

            //if not the same TerraFirma.MoveTypes , return the difference
            if (x.MoveType != y.MoveType)
                return y.MoveType - x.MoveType;
            else
                //if both moves are capture
                if (x.MoveType == MoveTypes.Capture)
                {
                    //give priority to better pieces captured
                    if (x.CapturedPiece != y.CapturedPiece)
                        return y.CapturedPiece - x.CapturedPiece;
                    else
                        //the less important the capturer the better.
                        return x.CapturedPiece - y.CapturedPiece;
                }

            //if the same move type but not a capture, give priority according to vector.
            if (x.MovingPiece == Side.White)
            {
                return m_DestinationPriority[y.DestinationSquare] - m_DestinationPriority[x.DestinationSquare];
            }
            else
                return m_DestinationPriority[63 - y.DestinationSquare] - m_DestinationPriority[63 - x.DestinationSquare];


        }

        #endregion
    }

}