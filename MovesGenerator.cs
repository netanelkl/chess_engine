using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraFirma
{
    /// <summary>
    /// Represents the moves generator used for normal move generation
    /// Many tricks were used the speed up move generation because it consumes much of the search time.
    /// </summary>
    internal static class MovesGenerator
    {
        private static readonly ulong[][] BishopMoves;
        private static readonly ulong[] KingMoves;
        private static readonly ulong[] KnightMoves;
        private static readonly ulong[][] RookMoves;

        internal static ulong[] fileMask;
        internal static ulong[] rankMask;


        static MovesGenerator()
        {
            BishopMoves = new ulong[64][];
            RookMoves = new ulong[64][];
            KnightMoves = new ulong[64];
            KingMoves = new ulong[64];

            //first create file and rank masks
            fileMask = new ulong[8];
            rankMask = new ulong[8];
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    fileMask[i] |= Position.ToBitBoard(j, i);
                    rankMask[i] |= Position.ToBitBoard(i, j);
                }


            //create moves lists for all Pieces' positions
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int pos = 8 * i + j;
                    //create bishop moves - can be written more readable using bitwise shifting
                    BishopMoves[pos] = new ulong[4];

                    //north west
                    for (int k = i + 1, m = j - 1; k <= 7 && m >= 0; k++, m--)
                        BishopMoves[pos][0] |= Position.ToBitBoard(k, m);

                    //north east
                    for (int k = i + 1, m = j + 1; k <= 7 && m <= 7; k++, m++)
                        BishopMoves[pos][1] |= Position.ToBitBoard(k, m);

                    //south east
                    for (int k = i - 1, m = j + 1; k >= 0 && m <= 7; k--, m++)
                        BishopMoves[pos][2] |= Position.ToBitBoard(k, m);

                    //south west
                    for (int k = i - 1, m = j - 1; k >= 0 && m >= 0; k--, m--)
                        BishopMoves[pos][3] |= Position.ToBitBoard(k, m);

                    //create knight moves
                    for (int k = i - 2; k <= i + 2; k++)
                        if (k != i)
                        {
                            int sign = Math.Sign(k - i);
                            int colAdd = (3 - Math.Abs(k - i) % 3);

                            if (sign == -1 && k >= 0)
                            {
                                if (j + colAdd <= 7)
                                    KnightMoves[pos] |= Position.ToBitBoard(k, j + colAdd);
                                if (j - colAdd >= 0)
                                    KnightMoves[pos] |= Position.ToBitBoard(k, j - colAdd);
                            }
                            if (sign == 1 && k <= 7)
                            {
                                if (j + colAdd <= 7)
                                    KnightMoves[pos] |= Position.ToBitBoard(k, j + colAdd);
                                if (j - colAdd >= 0)
                                    KnightMoves[pos] |= Position.ToBitBoard(k, j - colAdd);
                            }
                        }

                    //create rook moves
                    RookMoves[pos] = new ulong[4];

                    //north
                    for (int k = i + 1; k <= 7; k++)
                        RookMoves[pos][0] |= Position.ToBitBoard(k, j);

                    //east
                    for (int k = j + 1; k <= 7; k++)
                        RookMoves[pos][1] |= Position.ToBitBoard(i, k);

                    //south
                    for (int k = i - 1; k >= 0; k--)
                        RookMoves[pos][2] |= Position.ToBitBoard(k, j);

                    //west
                    for (int k = j - 1; k >= 0; k--)
                        RookMoves[pos][3] |= Position.ToBitBoard(i, k);

                    //create king moves
                    for (int k = i - 1; k <= i + 1; k++)
                    {
                        for (int m = j - 1; m <= j + 1; m++)
                        {
                            if ((i != k || j != m) & (k >= 0 && k <= 7 && m >= 0 && m <= 7))
                                KingMoves[pos] |= Position.ToBitBoard(k, m);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get all moves for the board (not all are legal for sure, if one of the moves is not legal,
        /// on the next request for moves, null will be returned.
        /// </summary>
        /// <param name="board">The board to generate move to.</param>
        /// <param name="player">The player to generate moves to</param>
        /// <param name="_toSort">A bool indicating whether a sorting is needed. .Net's default List sorting is used.</param>
        /// <returns>A list of all moves (psudo-legal) moves.</returns>
        internal static List<Move> GetMoves(Board board, bool _toSort)
        {
            int player = board.CurrentPlayer;

            // Now, compute the moves, one piece type at a time
            //we don't use try catch blocks because it hurts performance and here it's most important
            //every time a null is returned means that the current board is illegal so the previous move ended the game.

            //compute queen moves
            var queenMoves = ComputeRayedMoves(board, Piece.BlackQueen + player);
            if (queenMoves == null)
                return null;

            //rook moves
            var rookMoves = ComputeRayedMoves(board, Piece.BlackRook + player);
            if (rookMoves == null)
                return null;

            //bishop moves
            var bishopMoves = ComputeRayedMoves(board, Piece.BlackBishop + player);
            if (bishopMoves == null)
                return null;

            //pawn moves
            var pawnMoves = ComputePawnMoves(board, player);
            if (pawnMoves == null)
                return null;

            //king moves
            var kingMoves = ComputeKingMoves(board, player);
            if (kingMoves == null)
                return null;

            //knight moves
            var knightMoves = ComputeUnRayedMoves(board, Piece.BlackKnight + player);
            if (knightMoves == null)
                return null;

            //count the number of moves found
            int counter = queenMoves.Count + rookMoves.Count + bishopMoves.Count + pawnMoves.Count + kingMoves.Count + knightMoves.Count;
            if (counter == 0)
            {
                //if no valid moves, check if we reached endgame
                if (board.IsInCheck(player))
                    board.IsTerminal = TerminalType.BlackWon + 1 - player;
                else
                    board.IsTerminal = TerminalType.Stalemate;
                return null;
            }
            else
            {
                //create a new list with the expected capacity.
                List<Move> movesList = new List<Move>(counter);

                //add all moves found to list
                movesList.AddRange(queenMoves); movesList.AddRange(rookMoves); movesList.AddRange(bishopMoves);
                movesList.AddRange(pawnMoves); movesList.AddRange(kingMoves); movesList.AddRange(knightMoves);

                //sort if needed
                if (_toSort)
                {
                    movesList.Sort(MoveComparar.Comparer);
                }
                return movesList;

            }
        }

        /// <summary>
        /// Get only legal (solid) moves. used by opening book.
        /// </summary>
        /// <param name="board">The board to generate moves for</param>
        /// <returns>A list of all legal moves</returns>
        internal static List<Move> GetLegalMoves(Board board)
        {
            //get all pseudo legal moves and eliminate the ones that aren't really legal
            var moves = GetMoves(board, false);
            List<Move> validMoves = new List<Move>();

            //foreach move , apply the move and check if results in check for the moving side
            foreach (Move move in moves)
            {
                Board clone = Board.Clone(board);
                clone.ApplyMove(move);
                if (!clone.IsInCheck(1 - clone.CurrentPlayer))
                    validMoves.Add(move);
            }

            return validMoves;
        }

        /// <summary>
        /// Computes all enemy attacks on a position
        /// </summary>
        /// <param name="board">The board to compute bitboard for</param>
        /// <param name="position">The position of the attacked piece</param>
        /// <param name="attackedPiece">The TerraFirma.Piece attacked piece</param>
        /// <returns>A bitboard of the attackers of position</returns>
        internal static ulong EnemyAttacksOn(Board board, int position, int attackedSide)
        {
            ulong positionBoard = position.ToBitBoard();
            int oppSide = 1 - attackedSide;
            ulong attacks = ((RayAttackFrom(board, Piece.BlackBishop + oppSide, position) &
                              (board.BitBoards[Piece.BlackBishop + oppSide] | board.BitBoards[Piece.BlackQueen + oppSide])) |
                             (RayAttackFrom(board, Piece.BlackRook + oppSide, position) &
                              (board.BitBoards[Piece.BlackRook + oppSide] | board.BitBoards[Piece.BlackQueen + oppSide])) |
                             (KnightMoves[position] & board.BitBoards[Piece.BlackKnight + oppSide]));

            if (oppSide == Side.Black)
            {
                attacks |= (((((positionBoard & ~fileMask[0]) << 7) & board.BitBoards[Piece.BlackPawn]) >> 7) | ((((positionBoard & ~fileMask[7]) << 9) & board.BitBoards[Piece.BlackPawn]) >> 9));
            }
            else
            {
                attacks |= (((((positionBoard & ~fileMask[7]) >> 7) & board.BitBoards[Piece.WhitePawn]) << 7) | ((((positionBoard & ~fileMask[0]) >> 9) & board.BitBoards[Piece.WhitePawn]) << 9));
            }

            return attacks;
        }

        /// <summary>
        /// Compute all rayed moves , valid for all rayed pieces (Bishop,Rook,Queen)
        /// </summary>
        /// <param name="board">The board to get moves to</param>
        /// <param name="piece">The TerraFirma.Piece to get the moves to</param>
        /// <returns>returns a list of valid rayed moves</returns>
        private static LinkedList<Move> ComputeRayedMoves(Board board, int piece)
        {
            var moves = new LinkedList<Move>();
            // Fetch the bitboard containing positions of these pieces
            ulong pieces = board.BitBoards[piece], oppBoard, oppKing;

            // Check for pieces of type
            if (pieces == 0)
            {
                return moves;
            }

            int side = piece & 0x1;
            oppBoard = board.BitBoards[Piece.AllBlacks + 1 - side];
            oppKing = board.BitBoards[Piece.BlackKing + 1 - side];

            foreach (int sourceSquare in Position.FindPositions(pieces))
            {
                //notice that we adapted 'BishopMoves' and 'RookMoves' such that 
                //the first two rays will use LSB and the other two rays will use MSB
                ulong possibleMoves = RayAttackFrom(board, piece, sourceSquare);

                //remove all moves to friendly destination

                //iterate captures
                ulong captureMoves = possibleMoves & oppBoard;

                //if found attack on king , declare checkmate
                //remember we don't use throw-try-catch due to performance
                if ((captureMoves & oppKing) != 0)
                {
                    board.IsTerminal = (TerminalType.BlackWon + side);
                    return null;
                }

                //capture moves
                foreach (int dest in Position.FindPositions(captureMoves))
                {
                    moves.AddLast(new Move
                                  {
                                      SourceSquare = sourceSquare,
                                      DestinationSquare = dest,
                                      MovingPiece = piece,
                                      MoveType = MoveTypes.Capture,
                                      CapturedPiece = board.FindPiece(dest, 1 - side)
                                  });
                };

                possibleMoves &= ~oppBoard;

                //iterate normal moves
                foreach (int dest in Position.FindPositions(possibleMoves))
                {
                    moves.AddLast(new Move
                                  {
                                      SourceSquare = sourceSquare,
                                      DestinationSquare = dest,
                                      MoveType = MoveTypes.Normal,
                                      MovingPiece = piece
                                  });
                };
            }

            return moves;
        }

        /// <summary>
        /// Computes all rayed attacks by one piece type from one square.
        /// </summary>
        /// <param name="board">The board to calculate rayed attacks to</param>
        /// <param name="attackerType">The TerraFirma.Piece piece type whose in source square</param>
        /// <param name="sourceSquare">The source square of the attacker</param>
        /// <returns></returns>
        private static ulong RayAttackFrom(Board board, int attackerType, int sourceSquare)
        {
            //get the memory bitboards associated with the attackerType.
            ulong[][][] pieceMovesList = null;
            switch (attackerType - (attackerType & 0x1))
            {
                case Piece.BlackRook:
                    pieceMovesList = new ulong[1][][];
                    pieceMovesList[0] = RookMoves;
                    break;
                case Piece.BlackBishop:
                    pieceMovesList = new ulong[1][][];
                    pieceMovesList[0] = BishopMoves;
                    break;
                case Piece.BlackQueen:
                    pieceMovesList = new ulong[2][][];
                    pieceMovesList[0] = RookMoves;
                    pieceMovesList[1] = BishopMoves;
                    break;
            }


			int piece = board.FindPiece(sourceSquare);
            ulong possibleMoves = 0;
            ulong occupiedBoard = ~board.BitBoards[Piece.EmptySquare];

            //iterate over bitboard lists ( we have 2 if we calculate for queen)
            for (int i = 0; i < pieceMovesList.Length; i++)
            {
                var pieceMoves = pieceMovesList[i];


                //north west - bishop, north - rook
                ulong attacks = pieceMoves[sourceSquare][0];
                ulong blockers = attacks & occupiedBoard;

                //if a blocker was found , add it to the possible moves.
                if (blockers != 0)
                {
                    int firstBlocker = Position.LSB(blockers);
                    possibleMoves |= attacks ^ pieceMoves[firstBlocker][0];
                }
                else
                    possibleMoves |= attacks;

                //north east - bishop / east - rook
                attacks = pieceMoves[sourceSquare][1];
                blockers = attacks & occupiedBoard;
                if (blockers != 0)
                {
                    int firstBlocker = Position.LSB(blockers);
                    possibleMoves |= attacks ^ pieceMoves[firstBlocker][1];
                }
                else
                    possibleMoves |= attacks;

                //south east - bishop / south - rook
                attacks = pieceMoves[sourceSquare][2];
                blockers = attacks & occupiedBoard;
                if (blockers != 0)
                {
                    int firstBlocker = Position.MSB(blockers);
                    possibleMoves |= attacks ^ pieceMoves[firstBlocker][2];
                }
                else
                    possibleMoves |= attacks;

                //south west - bishop / west - rook 
                attacks = pieceMoves[sourceSquare][3];
                blockers = attacks & occupiedBoard;
                if (blockers != 0)
                {
                    int firstBlocker = Position.MSB(blockers);
                    possibleMoves |= attacks ^ pieceMoves[firstBlocker][3];
                }
                else
                    possibleMoves |= attacks;

                //PossibleMoves &= oppBoard;
            }
            return (possibleMoves & ~board.BitBoards[Piece.AllBlacks + ((piece) & 0x1)]);
        }

        /// <summary>
        /// Compute unrayed moves for the board , and the specified piece.
        /// </summary>
        /// <param name="board">The board to calculate moves to</param>
        /// <param name="piece">The TerraFirma.Piece to calculate moves to</param>
        /// <returns>A list of unrayed moves</returns>
        private static LinkedList<Move> ComputeUnRayedMoves(Board board, int piece)
        {
            var moves = new LinkedList<Move>();

            // Fetch the bitboard containing positions of these pieces
            ulong pieces;

            pieces = board.BitBoards[piece];

            // Check for pieces of type
            if (pieces == 0)
            {
                return moves;
            }

            int side = piece & 0x1;
            ulong friendBoard = board.BitBoards[Piece.AllBlacks + side];
            ulong oppBoard = board.BitBoards[Piece.AllBlacks + 1 - side];
            ulong oppKing = board.BitBoards[Piece.BlackKing + 1 - side];

            ulong[] pieceMoves;

            //the mapped bitboard of valid attacks
            switch (piece - side)
            {
                case Piece.BlackKnight:
                    pieceMoves = KnightMoves;
                    break;
                case Piece.BlackKing:
                    pieceMoves = KingMoves;
                    break;
                default:
                    pieceMoves = KingMoves;
                    break;
            }

            foreach (int sourceSquare in Position.FindPositions(pieces))
            {
                ulong possibleMoves = pieceMoves[sourceSquare];

                //remove all moves to friendly destination
                possibleMoves &= ~friendBoard;

                //iterate captures
                ulong captureMoves = possibleMoves & oppBoard;

                //if found attack on king , declare checkmate
                //remember we don't use throw-try-catch due to performance
                if ((captureMoves & oppKing) != 0)
                {
                    board.IsTerminal = (TerminalType.BlackWon + side);
                    return null;
                }

                foreach (int dest in Position.FindPositions(captureMoves))
                {
                    moves.AddLast(new Move
                                  {
                                      SourceSquare = sourceSquare,
                                      DestinationSquare = dest,
                                      MovingPiece = piece,
                                      MoveType = MoveTypes.Capture,
                                      CapturedPiece =board.FindPiece(dest, 1 - side)
                                  });
                };

                possibleMoves &= ~oppBoard;

                //iterate normal moves
                foreach (int dest in Position.FindPositions(possibleMoves))
                {
                    moves.AddLast(new Move
                                  {
                                      SourceSquare = sourceSquare,
                                      DestinationSquare = dest,
                                      MoveType = MoveTypes.Normal,
                                      MovingPiece = piece
                                  });
                };
            }

            return moves;
        }

        /// <summary>
        /// Get all pawn moves
        /// </summary>
        /// <param name="board">The board to compute pawn moves to.</param>
        /// <param name="side">The player side to compute pawn moves to</param>
        /// <returns>A list of pawn moves</returns>
        private static LinkedList<Move> ComputePawnMoves(Board board, int side)
        {
            var moves = new LinkedList<Move>();
            int movingPiece = Piece.BlackPawn + side;
            ulong pawns = board.BitBoards[movingPiece];
            ulong oppBoard = board.BitBoards[Piece.AllBlacks + 1 - side];
            ulong oppKing = board.BitBoards[Piece.BlackKing + 1 - side];
            ulong emptyBoard = board.BitBoards[Piece.EmptySquare];
            int startingPos = side == Side.White ? 0 : 7;
            ulong oneStepPromotion;
            ulong promotionRankMask;
            int directionSign = side == Side.White ? 1 : -1;
            //check which pawns can move one step by moving all pawns one row forward and AND-ing with the empty squares
            ulong canOneStep, canTwoSteps, canCaptureLeft, canCaptureRight;


            if (side == Side.White)
            {
                //check one-step positions are empty 
                canOneStep = ((pawns << 8) & emptyBoard) >> 8;

                //moves that lead to normal-move promotion
                promotionRankMask = rankMask[6];
                oneStepPromotion = canOneStep & promotionRankMask;

                //to check two-steps moves ( 7th row) we first clear all other pawn pieces by shifting 6 rows then we shift back to the position
                //of two steps forward. If empty then that pawn can move there.
                canTwoSteps = (((canOneStep & rankMask[1]) << 16) & emptyBoard) >> 16;
                //look for capture moves
                canCaptureLeft = (((pawns & ~fileMask[0]) << 7) & oppBoard) >> 7;
                canCaptureRight = (((pawns & ~fileMask[7]) << 9) & oppBoard) >> 9;

                if (((canCaptureLeft << 7 | canCaptureRight << 9) & oppKing) != 0)
                {
                    //move is a checkmate, change board and return null ( so move generation will stop).
                    board.IsTerminal = (TerminalType.BlackWon + side);
                    return null;
                }
            }
            else
            {
                canOneStep = ((pawns >> 8) & emptyBoard) << 8;

                promotionRankMask = rankMask[1];
                oneStepPromotion = canOneStep & promotionRankMask;


                //I only take the pawns that are on the seventh row
                canTwoSteps = (((canOneStep & rankMask[6]) >> 16) & emptyBoard) << 16;

                canCaptureLeft = (((pawns & ~fileMask[7]) >> 7) & oppBoard) << 7;
                canCaptureRight = (((pawns & ~fileMask[0]) >> 9) & oppBoard) << 9;

                if (((canCaptureLeft >> 7 | canCaptureRight >> 9) & oppKing) != 0)
                {
                    //move is a checkmate, change board and return null ( so move generation will stop).
                    board.IsTerminal = (TerminalType.BlackWon + side);
                    return null;
                }
            }




            canOneStep &= ~oneStepPromotion;

            //add normal one-step moves, (including promotion)
            var positions = Position.FindPositions(oneStepPromotion);
            for (int i = 0; i < positions.Length; i++)
            {
                int sourceSquare = positions[i];
                int destSquare = sourceSquare + directionSign * 8;
                moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  MoveType = MoveTypes.PromotionQueen,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = destSquare
                              });
                /*moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  MoveType = MoveTypes.PromotionKnight,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = destSquare
                              });*/

            }

            positions = Position.FindPositions(canOneStep);
            for (int i = 0; i < positions.Length; i++)
            {
                int sourceSquare = positions[i];
                moves.AddLast(new Move
                             {
                                 MovingPiece = movingPiece,
                                 MoveType = MoveTypes.Normal,
                                 SourceSquare = sourceSquare,
                                 DestinationSquare = sourceSquare + directionSign * 8
                             });
            }

            //add two-step moves
            positions = Position.FindPositions(canTwoSteps);
            for (int i = 0; i < positions.Length; i++)
            {
                int sourceSquare = positions[i];
                moves.AddLast(new Move
                                              {
                                                  MovingPiece = movingPiece,
                                                  MoveType = MoveTypes.DoublePawn,
                                                  SourceSquare = sourceSquare,
                                                  DestinationSquare = sourceSquare + directionSign * 16
                                              });
            }
            //add capture moves

            //capture left promotion
            positions = Position.FindPositions(canCaptureLeft & promotionRankMask);
            for (int i = 0; i < positions.Length; i++)
            {
                int sourceSquare = positions[i];
                int capturedPosition = sourceSquare + directionSign * 7;
                int captured = board.FindPiece(capturedPosition, 1 - side);

                moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  CapturedPiece = captured,
                                  MoveType = MoveTypes.Capture | MoveTypes.PromotionQueen,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = capturedPosition
                              });
                /*moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  CapturedPiece = captured,
                                  MoveType = MoveTypes.Capture | MoveTypes.
                                          PromotionKnight,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = capturedPosition
                              });*/

            }

            //capture left normal
            positions = Position.FindPositions(canCaptureLeft & ~promotionRankMask);
            for (int i = 0; i < positions.Length; i++)
            {
                int sourceSquare = positions[i];
                int capturedPosition = sourceSquare + directionSign * 7;
                int captured = board.FindPiece(capturedPosition, 1 - side);
                moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  CapturedPiece = captured,
                                  MoveType = MoveTypes.Capture,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = capturedPosition
                              });
            }

            //capture right promotion
            positions = Position.FindPositions(canCaptureRight & promotionRankMask);
            for (int i = 0; i < positions.Length; i++)
            {
                int sourceSquare = positions[i];
                int capturedPosition = sourceSquare + 9 * directionSign;
                int captured = board.FindPiece(capturedPosition, 1 - side);

                moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  CapturedPiece = captured,
                                  MoveType = MoveTypes.Capture | MoveTypes.PromotionQueen,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = capturedPosition
                              });
                moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  CapturedPiece = captured,
                                  MoveType = MoveTypes.Capture | MoveTypes.PromotionKnight,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = capturedPosition
                              });
            };

            //capture right normal
            foreach (int sourceSquare in Position.FindPositions(canCaptureRight & ~promotionRankMask))
            {
                int capturedPosition = sourceSquare + 9 * directionSign;
                int captured = board.FindPiece(capturedPosition, 1 - side);
                moves.AddLast(new Move
                              {
                                  MovingPiece = movingPiece,
                                  CapturedPiece = captured,
                                  MoveType = MoveTypes.Capture,
                                  SourceSquare = sourceSquare,
                                  DestinationSquare = capturedPosition
                              });
            };

            //check for an enPassant move
            if (board.EnPassantOptionColumn >= 0)
            {
                ulong EnPassantColumn = 0;

                if (board.EnPassantOptionColumn > 0)
                    EnPassantColumn |= fileMask[board.EnPassantOptionColumn - 1];

                if (board.EnPassantOptionColumn < 7)
                    EnPassantColumn |= fileMask[board.EnPassantOptionColumn + 1];

                foreach (int sourceSquare in Position.FindPositions(pawns & EnPassantColumn & rankMask[startingPos + (directionSign << 2)]))
                {
                    moves.AddLast(new Move
                      {
                          MovingPiece = movingPiece,
                          MoveType = MoveTypes.EnPassant,
                          CapturedPiece = movingPiece == Piece.BlackPawn ? Piece.WhitePawn : Piece.BlackPawn,
                          SourceSquare = sourceSquare,
                          DestinationSquare = (((sourceSquare >> 3) + directionSign) << 3) + board.EnPassantOptionColumn
                      });
                }
            }

            return moves;
        }

        /// <summary>
        /// Get all king moves
        /// </summary>
        /// <param name="board">The board to compute king moves for.</param>
        /// <param name="side">The player side to compute king moves to</param>
        /// <returns>A list of king moves</returns>
        private static LinkedList<Move> ComputeKingMoves(Board board, int side)
        {
            LinkedList<Move> moves = ComputeUnRayedMoves(board, Piece.BlackKing + side);

            if (moves == null)
                return null;

            int firstRank = side == Side.White ? 0 : 7;
            int last = firstRank << 3;
            ulong kingSidePathMask = 96;
            ulong queenSidePathMask = 14;

            //if side is black, the masks are the black king's masks
            if (side == Side.Black)
            {
                kingSidePathMask <<= 56;
                queenSidePathMask <<= 56;
            }

            if (board.Castles[side] != CastleOption.NoOption)
            {
                int king = Piece.BlackKing + side;
                if ((board.Castles[side] & CastleOption.HasKingSide) != 0)
                {
                    if (((board.BitBoards[Piece.EmptySquare] & kingSidePathMask) == kingSidePathMask) &&
                        (EnemyAttacksOn(board, last + 5, side) == 0) &&
                        (EnemyAttacksOn(board, last + 4, side) == 0) &&
                        (EnemyAttacksOn(board, last + 6, side) == 0))
                    {
                        moves.AddLast(new Move
                                      {
                                          DestinationSquare = last + 6,
                                          MoveType = MoveTypes.CastleKingSide,
                                          MovingPiece = king,
                                          SourceSquare = last + 4,
                                          CapturedPiece = Piece.BlackRook + side
                                      });
                    }
                }
                if ((board.Castles[side] & CastleOption.HasQueenSide) != 0)
                {
                    if (((board.BitBoards[Piece.EmptySquare] & queenSidePathMask) == queenSidePathMask) &&
                        (EnemyAttacksOn(board, last + 2, side) == 0) &&
                        (EnemyAttacksOn(board, last + 4, side) == 0) &&
                        (EnemyAttacksOn(board, last + 3, side) == 0))
                    {
                        moves.AddLast(new Move
                                      {
                                          DestinationSquare = last + 2,
                                          MoveType = MoveTypes.CastleQueenSide,
                                          MovingPiece = king,
                                          SourceSquare = last + 4,
                                          CapturedPiece = Piece.BlackRook + side
                                      });
                    }
                }
            }
            return moves;
        }
    }
}