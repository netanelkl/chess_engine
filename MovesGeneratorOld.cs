using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessEngine
{
    public static class MovesGenerator
    {
        private static Position[,] BishopMoves;
        private static Position[,][] KnightMoves;
        private static Position[,][] KingMoves;
        private static Position[,][][] RookMoves;


        static MovesGenerator()
        {
            BishopMoves = new Position[8,8];
            RookMoves = new Position[8, 8][][];
            KnightMoves = new Position[8, 8][];
            KingMoves = new Position[8, 8][];

            //create moves lists for all Pieces' positions
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    List<Position> moves = new List<Position>();

                    //create bishop moves
                    //north west
                    for (int k = i - 1, m = j - 1; k >= 0 && m >= 0; k--, m--)
                        BishopMoves[i,j] |= new Position(k, m).ToBitBoard();

                    //north east
                    for (int k = i - 1, m = j + 1; k >= 0 && m <= 7; k--, m++)
                        BishopMoves[i, j] |= new Position(k, m).ToBitBoard();

                    //south east
                    for (int k = i + 1, m = j + 1; k <= 7 && m <= 7; k++, m++)
                        BishopMoves[i, j] |= new Position(k, m).ToBitBoard();
                    BishopMoves[i, j][2] = moves.ToArray();

                    moves.Clear();
                    //south west
                    for (int k = i + 1, m = j - 1; k <= 7 && m >= 0; k++, m--)
                        BishopMoves[i, j] |= new Position(k, m).ToBitBoard();
                    BishopMoves[i, j][3] = moves.ToArray();

                    //create knight moves
                    moves.Clear();
                    if (i == 0 && j == 1)
                    {
                        int x;
                        x = 1;
                    }

                    for (int k = i - 2; k <= i + 2; k++)
                        if (k != i)
                        {
                            int sign = Math.Sign(k - i);
                            int colAdd = (3 - Math.Abs(k - i) % 3);

                            if (sign == -1 && k >= 0)
                            {
                                if (j + colAdd <= 7)
                                    moves.Add(new Position { Row = k, Col = j + colAdd });
                                if (j - colAdd >= 0)
                                    moves.Add(new Position { Row = k, Col = j - colAdd });
                            }
                            if (sign == 1 && k <= 7)
                            {
                                if (j + colAdd <= 7)
                                    moves.Add(new Position { Row = k, Col = j + colAdd });
                                if (j - colAdd >= 0)
                                    moves.Add(new Position { Row = k, Col = j - colAdd });
                            }
                        }
                    KnightMoves[i, j] = moves.ToArray();

                    //create rook moves
                    moves.Clear();
                    RookMoves[i, j] = new Position[4][];

                    //north
                    for (int k = i - 1; k >= 0; k--)
                        moves.Add(new Position { Row = k, Col = j });
                    RookMoves[i, j][0] = moves.ToArray();

                    moves.Clear();
                    //east
                    for (int k = j + 1; k <= 7; k++)
                        moves.Add(new Position { Row = i, Col = k });
                    RookMoves[i, j][1] = moves.ToArray();

                    moves.Clear();
                    //south
                    for (int k = i + 1; k <= 7; k++)
                        moves.Add(new Position { Row = k, Col = j });
                    RookMoves[i, j][2] = moves.ToArray();

                    moves.Clear();
                    //west
                    for (int k = j - 1; k >= 0; k--)
                        moves.Add(new Position { Row = i, Col = k });
                    RookMoves[i, j][3] = moves.ToArray();


                    //create king moves
                    moves.Clear();
                    for (int k = i - 1; k <= i + 1; k++)
                    {
                        for (int m = j - 1; m <= j + 1; m++)
                        {
                            if (k >= 0 && k <= 7 && m >= 0 && m <= 7)
                                moves.Add(new Position { Row = k, Col = m });
                        }
                    }
                    KingMoves[i, j] = moves.ToArray();
                }
            }
        }

        /*Another Rook moves generator (static) by Justin Rogers
         * private static ulong[] GenerateRookMoves() {
ulong[] moves = new ulong[64];
ulong bitMask, bit1, bit2;
    
for(int i = 0; i < 64; i++) {
 bitMask = 0;
        bit1 = ((ulong) 1) << (i&7);
        bit2 = ((ulong) 1) << ((i>>3)<<3);
        for(int j = 0; j < 8; j++) {
            bitMask |= bit1 | bit2;
            bit1 <<= 8;
            bit2 <<= 1;
        }
        
        moves[i] = bitMask;
    }
    
    return moves;
}

         */

        public static List<Move> GetLegalMoves(Board _board, Side _player)
        {
            List<Move> moves = new List<Move>();
            List<Move> newMoves = new List<Move>();
            // Now, compute the moves, one piece type at a time
            //we don't use try catch blocks because it hurts performance and here it's most important
            newMoves = ComputeRayedMoves(_board, Piece.BlackQueen + (int)_player);
            if (newMoves != null)
                moves.AddRange(newMoves);
            else
                return null;

            newMoves = ComputeRayedMoves(_board, Piece.BlackRook + (int)_player);
            if (newMoves != null)
                moves.AddRange(newMoves);
            else
                return null;

            newMoves = ComputeRayedMoves(_board, Piece.BlackBishop + (int)_player);
            if (newMoves != null)
                moves.AddRange(newMoves);
            else
                return null;

            newMoves = ComputePawnMoves(_board, _player);
            if (newMoves != null)
                moves.AddRange(newMoves);
            else
                return null;

            newMoves = ComputeUnRayedMoves(_board, Piece.BlackKing + (int)_player);
            if (newMoves != null)
                moves.AddRange(newMoves);
            else
                return null;

            newMoves = ComputeUnRayedMoves(_board, Piece.BlackKnight + (int)_player);
            if (newMoves != null)
                moves.AddRange(newMoves);
            else
                return null;

            if (moves.Count == 0)
                throw new System.Exception("illegal position");
            else
            {
                return moves;
            }
        }

        private static List<Move> ComputeRayedMoves(Board _board, Piece _piece)
        {
            //we don't use ComputeRayedMoves_Internal directly because queen uses it twice
            //(for rook and knight)

            if (_piece == Piece.BlackBishop || _piece == Piece.WhiteBishop)
                return ComputeRayedMoves_Internal(_board, _piece, BishopMoves);
            else
                if (_piece == Piece.BlackRook || _piece == Piece.WhiteRook)
                    return ComputeRayedMoves_Internal(_board, _piece, RookMoves);
                else
                    if (_piece == Piece.BlackQueen || _piece == Piece.WhiteQueen)
                    {
                        List<Move> moves;
                        moves = ComputeRayedMoves_Internal(_board, _piece, BishopMoves);

                        if (moves == null)
                            return null;
                        List<Move> rookMoves = ComputeRayedMoves_Internal(_board, _piece, RookMoves);
                        if (rookMoves == null)
                            return null;

                        moves.AddRange(rookMoves);
                        return moves;
                    }
                    else
                        throw new Exception("illegal piece");
        }

        private static List<Move> ComputeRayedMoves_Internal(Board _board, Piece piece, Position[,][][] pieceMoves)
        {
            List<Move> moves = new List<Move>();
            // Fetch the bitboard containing positions of these pieces
            ulong pieces;
            ulong friendBoard, oppBoard;

            pieces = _board.BitBoards[(int)piece];

            // Check for pieces of type
            if (pieces == 0)
            {
                return moves;
            }

            friendBoard = _board.BitBoards[(int)Piece.AllBlacks + ((int)piece % 2)];
            oppBoard = _board.BitBoards[(int)Piece.AllBlacks + (((int)piece + 1) % 2)];

            foreach (Position sourceSquare in Position.FindPositions(pieces))
            {
                //loop over all rays (initalized at engine start)
                for (int ray = 0; ray < pieceMoves[sourceSquare.Row, sourceSquare.Col].Length; ray++)
                {
                    for (int k = 0; k < pieceMoves[sourceSquare.Row, sourceSquare.Col][ray].Length; k++)
                    {
                        // Get the destination square
                        Position dest = pieceMoves[sourceSquare.Row, sourceSquare.Col][ray][k];

                        //if dest is friendly, continue
                        if ((friendBoard & dest.ToBitBoard()) != 0)
                            break;

                        // Otherwise, the move is legal, so we must prepare to add it
                        Move move = new Move() { SourceSquare = sourceSquare, DestinationSquare = dest, MovingPiece = piece };

                        // Is the destination occupied by an enemy?  If so, we have a capture
                        if ((oppBoard & dest.ToBitBoard()) != 0)
                        {
                            move.CapturedPiece = _board.FindPiece(dest);

                            if (move.CapturedPiece == Piece.BlackKing)
                            {
                                //move is a checkmate, change board and return null ( so move generation will stop).
                                move.MoveType = MoveType.Checkmate;
                                _board.IsTerminal = (TerminalType)(TerminalType.BlackWon + (int)piece % 2);
                                return null;
                            }
                            else
                                move.MoveType = MoveType.Capture;
                        }
                        // otherwise, it is a simple move
                        else
                        {
                            move.MoveType = MoveType.Normal;

                        }
                        moves.Add(move);
                    }
                }
            }

            return moves;
        }

        private static List<Move> ComputeUnRayedMoves(Board _board, Piece piece)
        {
            List<Move> moves = new List<Move>();
            // Fetch the bitboard containing positions of these pieces
            ulong pieces;
            ulong friendBoard, oppBoard;
            Side side = (Side)((int)piece % 2);

            Position[,][] pieceMoves;

            if (piece == Piece.BlackKnight || piece == Piece.WhiteKnight)
                pieceMoves = KnightMoves;
            else
                if (piece == Piece.BlackKing || piece == Piece.WhiteKing)
                    pieceMoves = KingMoves;
                else
                    throw new Exception("illegal piece");

            pieces = _board.BitBoards[(int)piece];

            // Check for pieces of type
            if (pieces == 0)
            {
                return moves;
            }

            friendBoard = _board.BitBoards[(int)Piece.AllBlacks + ((int)piece % 2)];
            oppBoard = _board.BitBoards[(int)Piece.AllBlacks + (((int)piece + 1) % 2)];

            //lets roll over all pieces and examine possible moves (already stored)
            foreach (Position sourceSquare in Position.FindPositions(pieces))
            {
                for (int k = 0; k < pieceMoves[sourceSquare.Row, sourceSquare.Col].Length; k++)
                {
                    Position dest = pieceMoves[sourceSquare.Row, sourceSquare.Col][k];

                    //if dest is friendly, continue
                    if ((friendBoard & dest.ToBitBoard()) != 0)
                        continue;

                    //add the move
                    Move move = new Move() { SourceSquare = sourceSquare, DestinationSquare = dest, MovingPiece = piece };

                    //check for capture
                    if ((oppBoard & dest.ToBitBoard()) != 0)
                    {
                        move.CapturedPiece = _board.FindPiece(dest);

                        if (move.CapturedPiece == Piece.BlackKing)
                        {
                            //move is a checkmate, change board and return null ( so move generation will stop).
                            move.MoveType = MoveType.Checkmate;
                            _board.IsTerminal = (TerminalType)(TerminalType.BlackWon + (int)piece % 2);
                            return null;
                        }
                        else
                            move.MoveType = MoveType.Capture;
                    }
                    else
                    {
                        move.MoveType = MoveType.Normal;
                    }

                    moves.Add(move);
                }
            }

            return moves;
        }

        private static List<Move> ComputePawnMoves(Board _board, Side _side)
        {
            List<Move> moves = new List<Move>();
            Piece movingPiece = Piece.BlackPawn + (int)_side;
            ulong pawns = _board.BitBoards[(int)movingPiece];
            ulong oppBoard = _board.BitBoards[(int)Piece.AllBlacks + ((int)_side + 1) % 2];
            ulong emptyBoard = _board.BitBoards[(int)Piece.EmptySquare];
            int directionSign = _side == Side.White ? -1 : +1;
            int startingPos = _side == Side.White ? 7 : 0;
            //check which pawns can move one step by moving all pawns one row forward and AND-ing with the empty squares
            ulong canOneStep, canTwoSteps, canCaptureLeft, canCaptureRight;



            if (_side == Side.White)
            {
                canOneStep = ((pawns >> 8) & emptyBoard) << 8;

                //to check two-steps moves ( 7th row) we first clear all other pawn pieces by shifting 6 rows then we shift back to the position
                //of two steps forward. If empty then that pawn can move there.
                canTwoSteps = (((canOneStep >> (8 * 6)) << 8 * 4) & emptyBoard) << 8 * 2;

                //look for capture moves
                canCaptureLeft = ((pawns >> 9) & oppBoard) << 9;
                canCaptureRight = ((pawns >> 7) & oppBoard) << 7;
            }
            else
            {
                canOneStep = ((pawns << 8) & emptyBoard) >> 8;
                canTwoSteps = (((canOneStep << (8 * 6)) >> 8 * 4) & emptyBoard) >> 8 * 2;
                canCaptureLeft = ((pawns << 9) & oppBoard) >> 9;
                canCaptureRight = ((pawns << 7) & oppBoard) >> 7;
            }

            //add normal one-step moves, (including promotion)
            foreach (Position sourceSquare in Position.FindPositions(canOneStep))
            {
                if (sourceSquare.Row == 7 - startingPos + directionSign)
                {
                    moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.PromotionQueen, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign, sourceSquare.Col) });
                    moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.PromotionBishop, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign, sourceSquare.Col) });
                    moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.PromotionKnight, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign, sourceSquare.Col) });
                    moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.PromotionRook, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign, sourceSquare.Col) });
                }
                else
                    moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.Normal, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign, sourceSquare.Col) });
            }

            //add two-step moves
            foreach (Position sourceSquare in Position.FindPositions(canTwoSteps))
                moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.Normal, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign * 2, sourceSquare.Col) });

            //add capture moves
            //to the left
            foreach (Position sourceSquare in Position.FindPositions(canCaptureLeft))
            {
                Position capturedPosition = new Position(sourceSquare.Row + directionSign, sourceSquare.Col + directionSign);

                //might happen when bitwise shifting
                if (capturedPosition.Col == -1 || capturedPosition.Col == 8)
                    continue;

                Piece captured = _board.FindPiece(capturedPosition);

                if (captured == Piece.BlackKing)
                {
                    //move is a checkmate, change board and return null ( so move generation will stop).
                    _board.IsTerminal = (TerminalType)(TerminalType.BlackWon + (int)_side);
                    return null;
                }

                if (sourceSquare.Row == 7 - startingPos + directionSign)
                {
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionQueen, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionBishop, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionKnight, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionRook, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                }
                else
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
            }

            //to the right
            foreach (Position sourceSquare in Position.FindPositions(canCaptureRight))
            {
                Position capturedPosition = new Position(sourceSquare.Row + directionSign, sourceSquare.Col - directionSign);

                //might happen when bitwise shifting
                if (capturedPosition.Col == 8 || capturedPosition.Col == -1)
                    continue;

                Piece captured = _board.FindPiece(capturedPosition);

                if (sourceSquare.Col == 7 - startingPos + directionSign)
                {
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionQueen, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionBishop, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionKnight, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture & MoveType.PromotionRook, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
                }
                else
                    moves.Add(new Move { MovingPiece = movingPiece, CapturedPiece = captured, MoveType = MoveType.Capture, SourceSquare = sourceSquare, DestinationSquare = capturedPosition });
            }

            //find EnPassant
            foreach (Position sourceSquare in Position.FindPositions(pawns))
            {
                //check en-passant
                if (_board.EnPassantOptionColumn == sourceSquare.Col - 1 || _board.EnPassantOptionColumn == sourceSquare.Col + 1)
                {
                    moves.Add(new Move { MovingPiece = movingPiece, MoveType = MoveType.EnPassant, CapturedPiece = movingPiece == Piece.BlackPawn ? Piece.WhitePawn : Piece.BlackPawn, SourceSquare = sourceSquare, DestinationSquare = new Position(sourceSquare.Row + directionSign, _board.EnPassantOptionColumn) });
                }
            }


            return moves;
        }


    }
}
