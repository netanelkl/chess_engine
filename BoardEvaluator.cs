using System;
using System.Collections.Generic;
using System.Text;

namespace TerraFirma
{
    internal static class BoardEvaluator
    {
        //consts
        internal static int m_EvaluationCounter = 0;
        //private static int m_ControlledSquareMinAttacks;
        internal static int[] PieceValues;


        internal static int[] m_PawnBonusesInitial = new int[64]
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

        internal static ulong m_DarkMask;
        internal static ulong m_LightMask;
        internal static ulong m_Edges;

        static BoardEvaluator()
        {
            PieceValues = new int[12];
            PieceValues[Piece.WhitePawn] = 100;
            PieceValues[Piece.BlackPawn] = -100;
            PieceValues[Piece.WhiteKnight] = 300;
            PieceValues[Piece.BlackKnight] = -300;
            PieceValues[Piece.WhiteBishop] = 300;
            PieceValues[Piece.BlackBishop] = -300;
            PieceValues[Piece.WhiteRook] = 500;
            PieceValues[Piece.BlackRook] = -500;
            PieceValues[Piece.WhiteQueen] = 900;
            PieceValues[Piece.BlackQueen] = -900;
            PieceValues[Piece.WhiteKing] = int.MaxValue;
            PieceValues[Piece.BlackKing] = -int.MaxValue;

            m_DarkMask = 0xaa55aa55aa55aa55;
            m_LightMask = 0x55aa55aa55aa55aa;
            m_Edges = MovesGenerator.rankMask[0] | MovesGenerator.rankMask[7] | MovesGenerator.fileMask[0] | MovesGenerator.fileMask[7];
        }

        internal static int Evaluate(Board _board, int _playerToEval)
        {
            int score = MaterialBalance(_board) + CastleStatus(_board);
            if (_board.Turn < 15)
                score += InitialDevelopment(_board);
            else
            {
                score += OpenRooks(_board) + PawnStructure(_board);

                //check for endgame
                if (_board.Turn > 20 && Global.BitCount(_board.BitBoards[Piece.AllBlacks + 1 - _playerToEval]) < 10)
                {
                    score += BishopsInEndGame(_board);
                }
                else
                {
                    //middle game scoring
                }
            }

            if (_playerToEval == Side.Black)
                return -score;
            else
                return score;

        }
        internal static int EvaluateQuickie(Board _board, int _playerToEval)
        {
            if (_playerToEval == Side.Black)
                return -MaterialBalance(_board);

            return MaterialBalance(_board);
        }

        private static int MaterialBalance(Board _board)
        {
            return _board.MaterialValue;
        }


        private static int GoodBadBishops(Board board)
        {
            int score = 0;

            ulong allPawns = board.BitBoards[Piece.WhitePawn] | board.BitBoards[Piece.BlackPawn];

            //get all pawns
            int pawnLightSquares = Global.BitCount(board.BitBoards[allPawns] & m_LightMask);
            int pawnDarkSquares = Global.BitCount(board.BitBoards[allPawns] & m_DarkMask);

            if ((m_LightMask & board.BitBoards[Piece.WhiteBishop]) != 0)
                score = pawnLightSquares;

            if ((m_DarkMask & board.BitBoards[Piece.WhiteBishop]) != 0)
                score -= pawnDarkSquares;

            if ((m_LightMask & board.BitBoards[Piece.BlackBishop]) != 0)
                score += pawnLightSquares;

            if ((m_DarkMask & board.BitBoards[Piece.BlackBishop]) != 0)
                score += pawnDarkSquares;

            return score;
        }

        private static int KnightsInEdges(Board board)
        {
            int score = 0;

            score -= 5 * Global.BitCount(board.BitBoards[Piece.WhiteKnight] & m_Edges);
            score += 5 * Global.BitCount(board.BitBoards[Piece.BlackKnight] & m_Edges);

            return score;
        }

        private static int OpenRooks(Board board)
        {
            int score = 0;
            foreach (int whiteRook in Position.FindPositions(board.BitBoards[Piece.WhiteRook]))
            {
                score += Global.BitCount(MovesGenerator.fileMask[whiteRook % 8] & board.BitBoards[Piece.EmptySquare]);
            }
            foreach (int blackRook in Position.FindPositions(board.BitBoards[Piece.BlackRook]))
            {
                score -= Global.BitCount(MovesGenerator.fileMask[blackRook % 8] & board.BitBoards[Piece.EmptySquare]);
            }

            return score;
        }

        private static int BishopsInEndGame(Board board)
        {
            int score = 0;

            score += 7 * Global.BitCount(board.BitBoards[Piece.WhiteBishop]);
            score -= 7 * Global.BitCount(board.BitBoards[Piece.BlackBishop]);

            return score;
        }


        private static int CastleStatus(Board board)
        {
            int score = 0;
            if (board.Castles[Side.White] == CastleOption.CastledKingSide || board.Castles[Side.White] == CastleOption.CastledQueenSide)
            {
                score += 40;
            }
            else
            {
                if ((board.Castles[Side.White] & CastleOption.HasQueenSide) != 0)
                    score += 5;
                if ((board.Castles[Side.White] & CastleOption.HasKingSide) != 0)
                    score += 5;
            }

            if (board.Castles[Side.Black] == CastleOption.CastledKingSide || board.Castles[Side.Black] == CastleOption.CastledQueenSide)
            {
                score -= 40;
            }
            else
            {
                if ((board.Castles[Side.Black] & CastleOption.HasQueenSide) != 0)
                    score -= 5;
                if ((board.Castles[Side.Black] & CastleOption.HasKingSide) != 0)
                    score -= 5;
            }

            //basically we are saying for the first five turns return the normal score.
            //aterwards start lowering importance
            /*if (board.Turn > 10)
            {
                score = score * 10 / board.Turn;
            }*/
            return score;
        }


        //check pawn progress, bishop and knights progress, queen should remain
        private static int InitialDevelopment(Board board)
        {
            //we want to make sure pawns get rewarded for advancing towards the center
            int score = 0;

            int[] pawns = Position.FindPositions(board.BitBoards[Piece.WhitePawn]);
            for (int i = 0; i < pawns.Length; i++)
            {
                score += m_PawnBonusesInitial[pawns[i]];
            }

            //now for black, notice we flip the board to fit black.
            pawns = Position.FindPositions(board.BitBoards[Piece.BlackPawn]);
            for (int i = 0; i < pawns.Length; i++)
            {
                score -= m_PawnBonusesInitial[63 - pawns[i]];
            }


            //bad score for bishop and knight staying in back rank.
            score -= 5 * Global.BitCount(board.BitBoards[Piece.WhiteKnight] & MovesGenerator.rankMask[0]);
            score += 10 * Global.BitCount(board.BitBoards[Piece.WhiteQueen] & MovesGenerator.rankMask[0]);

            score += 5 * Global.BitCount(board.BitBoards[Piece.BlackKnight] & MovesGenerator.rankMask[7]);
            score -= 10 * Global.BitCount(board.BitBoards[Piece.BlackQueen] & MovesGenerator.rankMask[7]);

            return score;
        }


        private static int PawnStructure(Board board)
        {
            ulong whitePawns = board.BitBoards[Piece.WhitePawn];
            ulong blackPawns = board.BitBoards[Piece.BlackPawn];
            bool flagWhite = false;
            bool flagBlack = false;
            int score = 0;
            //pawn islands and doubled pawns
            for (int file = 0; file < 8; file++)
            {
                int filePawns = Global.BitCount(whitePawns & MovesGenerator.fileMask[file]);
                if (filePawns != 0)
                {
                    if (filePawns > 1)
                        score -= 10;

                    if (flagWhite == true)
                    {
                        flagWhite = false;
                        score -= 5;
                    }
                }
                else
                    flagWhite = true;

                filePawns = Global.BitCount(blackPawns & MovesGenerator.fileMask[file]);

                if (filePawns > 1)
                    score += 10;

                if (filePawns != 0)
                {
                    if (flagBlack == true)
                    {
                        flagBlack = false;
                        score += 5;
                    }
                }
                else
                    flagBlack = true;

            }

            return score;
        }


        /*private double ControlledSquares()
        {
            double score = 0;

            Dictionary<Position, int> positionMoves = new Dictionary<Position, int>();
            Dictionary<Position, int> oppPositionMoves = new Dictionary<Position, int>();

            foreach (Move move in MovesGenerator.GetLegalMoves(m_board, m_playerToEval))
            {
                if (positionMoves.ContainsKey(move.DestinationSquare))
                    positionMoves[move.DestinationSquare]++;
                else
                    positionMoves.Add(move.DestinationSquare, 1);
            }

            foreach (Move move in MovesGenerator.GetLegalMoves(m_board, m_otherPlayer))
            {
                if (positionMoves.ContainsKey(move.DestinationSquare))
                    oppPositionMoves[move.DestinationSquare]++;
                else
                    oppPositionMoves.Add(move.DestinationSquare, 1);
            }

            //narrow down all squares not attacked heavily by players
            foreach (KeyValuePair<Position, int> piKVPair in positionMoves)
            {
                if (oppPositionMoves.ContainsKey(piKVPair.Key))
                {
                    if (oppPositionMoves[piKVPair.Key] + piKVPair.Value >= m_ControlledSquareMinAttacks)
                    {
                        Side inSquare = (Side)((int)m_board.FindPiece(piKVPair.Key) & 0x1);

                        //if we can dominate square, score us. else score opp.
                        if (oppPositionMoves[piKVPair.Key] > piKVPair.Value && )
                            score++;
                        else 
                            score--;

                        if (inSquare == m_playerToEval)
                        {
                            if (oppPositionMoves[piKVPair.Key] > piKVPair.Value)

                            score++;
                        }
                        else
                            score--;

                    }
                    else
                        oppPositionMoves.Remove(piKVPair.Key);
                }
                else
                {
                    oppPositionMoves.Remove(piKVPair.Key);
                }
            }

            return score;
        }*/
    }
}
