using System;
using System.Collections.Generic;

using System.Text;

namespace TerraFirma
{

    /// <summary>
    /// Represents a chess game board. It is based on the Bitboard representation system.
    /// </summary>
    internal sealed class Board
    {
        //number of total bitboards
        private static int TOTAL_BitBoards = 15;

        //used if null move pruning is active
        //NOTE: null move pruning is in testing and might not make it to the final release

        public bool NullMoveOk { get; set; }
        //an array of bitboards.
        internal ulong[] BitBoards;

        //the status of the game
        internal TerminalType IsTerminal;

        //pre enPassant column option - saves information on whether we can 
        //perform an enPassant at the moment.
        internal int EnPassantOptionColumn;

        //used for 3-moves repetition
        internal Move[] LastMove;
        internal int RepetitionCounter;

        //the current player
        internal int CurrentPlayer;

        //material values for each side (we do not calculate it at evaluation but rathar at ApplyMove
        //this is much faster
        internal int MaterialValue;

        //castle options for each side
        internal CastleOption[] Castles;

        //the current Turn
        internal int Turn;

        /// <summary>
        /// Examines the board's position to detect whethet '_side' is in check
        /// </summary>
        /// <param name="_side">The side in question</param>
        /// <returns>True if _side is in check, false otherwise</returns>
        internal bool IsInCheck(int _side)
        {
            //compute attackers
            int attackedPiece = Piece.BlackKing + _side;
            return MovesGenerator.EnemyAttacksOn(this, Position.LSB(BitBoards[attackedPiece]), _side) != 0;
        }

        /// <summary>
        /// Clones the board. Most important in the search method.
        /// Instead of unmaking a move (thus we have the need to save all history data) ,
        /// we duplicate the board and work on the new one. After testing it was more efficient then undoing a move.
        /// </summary>
        /// <param name="_orig">The board to clone</param>
        /// <returns>A cloned board</returns>
        internal static Board Clone(Board _orig)
        {
            Board board = new Board();

            //copy all data from original board to cloned
            board.CurrentPlayer = _orig.CurrentPlayer;
            board.Turn = _orig.Turn;
            board.EnPassantOptionColumn = _orig.EnPassantOptionColumn;

            //material values and castle options for each side
            board.MaterialValue = _orig.MaterialValue;
            board.Castles = new CastleOption[2];
            board.Castles[0] = _orig.Castles[0];
            board.Castles[1] = _orig.Castles[1];
            board.LastMove = new Move[2];
            board.LastMove[0] = _orig.LastMove[0];
            board.LastMove[1] = _orig.LastMove[1];
            //board.LastMove[2] = _orig.LastMove[2];
            //board.LastMove[3] = _orig.LastMove[3];

            board.NullMoveOk = true;
            for (int i = 0; i < TOTAL_BitBoards; i++)
            {
                board.BitBoards[i] = _orig.BitBoards[i];
            }
            board.RepetitionCounter = _orig.RepetitionCounter;
            return board;
        }

        internal void ApplyNullMove()
        {
            this.CurrentPlayer = 1 - this.CurrentPlayer;
            NullMoveOk = false;

        }

        /// <summary>
        /// Applies a move on the board. A very important method.
        /// It was optimised to be very fast.
        /// </summary>
        /// <param name="_move">The move to apply</param>
        internal void ApplyMove(Move _move)
        {
            EnPassantOptionColumn = -2;
            int side = _move.PlayerSide;
            int directionSign, startingPos;
            if (side == Side.White)
            {
                directionSign = 1;
                startingPos = 0;
            }
            else
            {
                directionSign = -1;
                startingPos = 7;
            }
            int eigthRank = startingPos << 3;

            //if king did not catle before, check for castling limitations
            if ((int)Castles[side] < 4)
            {
                //if king moves, he can't castle in future
                if (_move.MovingPiece - side == Piece.BlackKing)
                {
                    Castles[side] = CastleOption.NoOption;
                }
            }
            if ((int)Castles[Side.White] < 4)
            {
                if (_move.SourceSquare == 0 || _move.DestinationSquare == 0)
                {
                    Castles[Side.White] &= CastleOption.HasKingSide;
                }
                if (_move.SourceSquare == 7 || _move.DestinationSquare == 7)
                {
                    Castles[Side.White] &= CastleOption.HasQueenSide;
                }

            }

            if ((int)Castles[Side.Black] < 4)
            {
                if (_move.SourceSquare == 56 || _move.DestinationSquare == 56)
                {
                    Castles[Side.Black] &= CastleOption.HasKingSide;
                }
                if (_move.SourceSquare == 63 || _move.DestinationSquare == 63)
                {
                    Castles[Side.Black] &= CastleOption.HasQueenSide;
                }
            }

            /*if (Turn > 2 && LastMove[2 + side].EqualMove(_move))
            {
                RepetitionCounter++;
                if (RepetitionCounter == 3)
                    IsTerminal = TerminalType.Stalemate;
            }
            else
            {
                RepetitionCounter = 1;
            }

            LastMove[2 + side] = LastMove[side];*/
            LastMove[side] = _move;

            switch (_move.MoveType)
            {
                case MoveTypes.Normal:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, _move.MovingPiece);
                    break;
                case MoveTypes.DoublePawn:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, _move.MovingPiece);
                    EnPassantOptionColumn = _move.SourceSquare & 7;
                    break;
                case MoveTypes.Capture:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    RemovePiece(_move.DestinationSquare, _move.CapturedPiece);
                    AddPiece(_move.DestinationSquare, _move.MovingPiece);
                    MaterialValue -= BoardEvaluator.PieceValues[_move.CapturedPiece];
                    break;
                case (MoveTypes.Capture | MoveTypes.PromotionKnight):
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    RemovePiece(_move.DestinationSquare, _move.CapturedPiece);
                    AddPiece(_move.DestinationSquare, Piece.BlackKnight + side);
                    MaterialValue += BoardEvaluator.PieceValues[Piece.BlackKnight + side] - BoardEvaluator.PieceValues[_move.CapturedPiece] - BoardEvaluator.PieceValues[Piece.BlackPawn + side];
                    break;
                case (MoveTypes.Capture | MoveTypes.PromotionQueen):
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    RemovePiece(_move.DestinationSquare, _move.CapturedPiece);
                    AddPiece(_move.DestinationSquare, Piece.BlackQueen + side);
                    MaterialValue += BoardEvaluator.PieceValues[Piece.BlackQueen + side] - BoardEvaluator.PieceValues[_move.CapturedPiece] - BoardEvaluator.PieceValues[Piece.BlackPawn + side];
                    break;
                case MoveTypes.PromotionKnight:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, Piece.BlackKnight + side);
                    MaterialValue += BoardEvaluator.PieceValues[Piece.BlackKnight + side] - BoardEvaluator.PieceValues[Piece.BlackPawn + side];
                    break;
                case MoveTypes.PromotionQueen:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, Piece.BlackQueen + side);
                    MaterialValue += BoardEvaluator.PieceValues[Piece.BlackQueen + side] - BoardEvaluator.PieceValues[Piece.BlackPawn + side];
                    break;
                case MoveTypes.CastleKingSide:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, _move.MovingPiece);
                    Castles[side] = CastleOption.CastledKingSide;
                    RemovePiece(eigthRank + 7, _move.CapturedPiece);
                    AddPiece(eigthRank + 5, _move.CapturedPiece);
                    break;
                case MoveTypes.CastleQueenSide:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, _move.MovingPiece);
                    Castles[side] = CastleOption.CastledQueenSide;
                    RemovePiece(eigthRank, _move.CapturedPiece);
                    AddPiece(eigthRank + 3, _move.CapturedPiece);
                    break;
                case MoveTypes.EnPassant:
                    RemovePiece(_move.SourceSquare, _move.MovingPiece);
                    AddPiece(_move.DestinationSquare, _move.MovingPiece);
                    RemovePiece(_move.DestinationSquare - (directionSign << 3), _move.CapturedPiece);
                    MaterialValue -= BoardEvaluator.PieceValues[_move.CapturedPiece];
                    break;
            }

            if (side == Side.Black)
                Turn++;

            CurrentPlayer = 1 - CurrentPlayer;

            return;
        }

        /// <summary>
        /// Removes a piece from it's bitboard and update general bitboards
        /// </summary>
        /// <param name="_square">The square of the piece</param>
        /// <param name="_piece">The TerraFirma.Piece type of piece</param>
        private void RemovePiece(int _square, int _piece)
        {
            int side = _piece & 0x1;
            BitBoards[_piece] &= ~_square.ToBitBoard();
            // Remove the piece itself from the whole colored board
            BitBoards[Piece.AllBlacks + side] &= ~_square.ToBitBoard();
            BitBoards[14] |= _square.ToBitBoard();
        }

        /// <summary>
        /// Adds a piece to it's bitboard and update general bitboards
        /// </summary>
        /// <param name="_square">The square of the piece</param>
        /// <param name="_piece">The TerraFirma.Piece type of piece</param>
        private void AddPiece(int _square, int _piece)
        {
            int side = _piece & 0x1;
            BitBoards[_piece] |= _square.ToBitBoard();
            // Remove the piece itself from the whole colored board
            BitBoards[Piece.AllBlacks + side] |= _square.ToBitBoard();
            BitBoards[14] &= ~_square.ToBitBoard();
        }

        internal Board()
        {
            BitBoards = new ulong[TOTAL_BitBoards];
        }

        /// <summary>
        /// Resets the board to the starting position
        /// </summary>
        internal void StartingPosition()
        {
            EnPassantOptionColumn = -2;
            CurrentPlayer = Side.White;

            for (int i = 0; i < 8; i++)
            {
                //Pawns
                BitBoards[Piece.WhitePawn] |= Position.ToBitBoard(1, i);
                BitBoards[Piece.BlackPawn] |= Position.ToBitBoard(6, i);

                //all white/ all black
                BitBoards[Piece.AllWhites] |= Position.ToBitBoard(1, i) | Position.ToBitBoard(0, i);
                BitBoards[Piece.AllBlacks] |= Position.ToBitBoard(6, i) | Position.ToBitBoard(7, i);

                for (int j = 2; j < 6; j++)
                {
                    BitBoards[Piece.EmptySquare] |= Position.ToBitBoard(j, i);
                }
            }

            //Knights
            BitBoards[Piece.WhiteKnight] |= Position.ToBitBoard(0, 1) | Position.ToBitBoard(0, 6);
            BitBoards[Piece.BlackKnight] |= Position.ToBitBoard(7, 1) | Position.ToBitBoard(7, 6);

            //Bishop
            BitBoards[Piece.WhiteBishop] |= Position.ToBitBoard(0, 2) | Position.ToBitBoard(0, 5);
            BitBoards[Piece.BlackBishop] |= Position.ToBitBoard(7, 2) | Position.ToBitBoard(7, 5);

            //Rook
            BitBoards[Piece.WhiteRook] |= Position.ToBitBoard(0, 0) | Position.ToBitBoard(0, 7);
            BitBoards[Piece.BlackRook] |= Position.ToBitBoard(7, 0) | Position.ToBitBoard(7, 7);

            //Queen
            BitBoards[Piece.WhiteQueen] |= Position.ToBitBoard(0, 3);
            BitBoards[Piece.BlackQueen] |= Position.ToBitBoard(7, 3);

            //King
            BitBoards[Piece.WhiteKing] |= Position.ToBitBoard(0, 4);
            BitBoards[Piece.BlackKing] |= Position.ToBitBoard(7, 4);

            Castles = new CastleOption[2];
            LastMove = new Move[2];

            MaterialValue = 0;
            Turn = 1;
            Castles[Side.Black] = Castles[Side.White] = CastleOption.HasKingSide | CastleOption.HasQueenSide;

        }

        /// <summary>
        /// Finds a piece in a position square with a specified side
        /// </summary>
        /// <param name="dest">The position of the square</param>
        /// <param name="sideOfPiece">The side of the square</param>
        /// <returns>The piece found. If not found, TerraFirma.Piece.EmptySquare is returned</returns>
        internal int FindPiece(int dest, int sideOfPiece)
        {
            ulong destBitBoard = dest.ToBitBoard();

            //if side is black look in all black squares
            if (sideOfPiece == Side.Black)
            {
                if ((BitBoards[Piece.AllBlacks] & destBitBoard) != 0)
                {
                    for (int i = 0; i <= 10; i = i + 2)
                        if ((BitBoards[i] & destBitBoard) != 0)
                            return i;
                }
            }
            else
                if ((BitBoards[Piece.AllWhites] & destBitBoard) != 0)
                {
                    for (int i = 1; i <= 11; i = i + 2)
                        if ((BitBoards[i] & destBitBoard) != 0)
                            return i;
                }

            return Piece.EmptySquare;
        }

        /// <summary>
        /// Finds a piece in a position square.
        /// </summary>
        /// <param name="dest">The position of the square</param>
        /// <returns>The piece found. If not found, TerraFirma.Piece.EmptySquare is returned</returns>
        internal int FindPiece(int dest)
        {
            ulong destBitBoard = dest.ToBitBoard();
            if ((BitBoards[Piece.AllBlacks] & destBitBoard) != 0)
            {
                for (int i = 0; i <= 10; i = i + 2)
                    if ((BitBoards[i] & destBitBoard) != 0)
                        return i;
            }
            else
                if ((BitBoards[Piece.AllWhites] & destBitBoard) != 0)
                {
                    for (int i = 1; i <= 11; i = i + 2)
                        if ((BitBoards[i] & destBitBoard) != 0)
                            return i;
                }

            return Piece.EmptySquare;
        }

        /// <summary>
        /// Prints the board to the console because we are on console, we use the int value of each piece
        /// instead of the graphic figure.
        /// </summary>
        internal void Print()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 7; i >= 0; i--)
            {
                for (int j = 0; j < 8; j++)
                {
                    string piece = FindPiece(8 * i + j).ToString();
                    sb.Append(piece);
                    for (int k = 0; k < 3 - piece.Length; k++)
                        sb.Append(' ');
                }
                sb.Append('\n');
            }
            sb.Append('\n');
            Console.Write(sb);
        }

        /// <summary>
        /// Used by transposition tables. this is basically a zobrist key.
        /// </summary>
        /// <returns></returns>
        internal new int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < 12; i++)
            {
                int[] positions = Position.FindPositions(BitBoards[i]);
                foreach (int position in positions)
                {
                    hash ^= BoardComparar.HashRandoms[position][i];
                }
            }
            hash ^= BoardComparar.EnPassantStatus[EnPassantOptionColumn + 2];
            hash ^= BoardComparar.CurrentPlayer[CurrentPlayer];

            return hash;
        }

        /// <summary>
        /// used to detect collisions between two hash keys.
        /// </summary>
        /// <returns></returns>
        internal int HashCollisionsKey()
        {
            int hash = 0;
            for (int i = 0; i < 12; i++)
            {
                int[] positions = Position.FindPositions(BitBoards[i]);
                foreach (int position in positions)
                {
                    hash ^= BoardComparar.HashCollisions[position][i];
                }
            }

            return hash;
        }
    }
}
