using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Collections;
namespace TerraFirma
{
    internal static class AIPlayer
    {
        private static Thread m_MoveThread;
        private static Thread m_PonderThread;
        private static Dictionary<Move, IterativeDeepeningResult> m_Pondering;
        private static TranspositionTable m_TranspositionTable;

        private static int m_NodesSearched;
        private static int ASSPIRATION_WINDOW = 50;
        private static int SHALLOWER_DEPTH_R = 2; 
        internal static Clock Clock { get; set; }
        internal static int Depth { get; set; }
        internal static bool StopPondering { get; set; }
        internal static event MoveEventHandler Moved;
        internal static bool ToForce = false;

        //just a random otherwise impossible number
        private const int TIMES_UP = int.MinValue + 100;

        internal static bool IsThinking { get { return m_MoveThread.ThreadState == ThreadState.Running; } }

        static AIPlayer()
        {
            Depth = 15;
            m_MoveThread = new Thread(new ParameterizedThreadStart(MakeMove));
            m_PonderThread = new Thread(new ParameterizedThreadStart(MakePondering));
            m_Pondering = new Dictionary<Move, IterativeDeepeningResult>();
            m_TranspositionTable = new TranspositionTable();
        }

        private static void MakeMove(object _board)
        {
            Board board = _board as Board;

            //check if we only have one move
            var moves = MovesGenerator.GetMoves(board, false);
            if (moves.Count == 1)
            {
                Moved(moves[0]);
            }

            Move move;
            if (OpeningBook.IsInBook)
            {
                move = OpeningBook.GetRandomMove();
            }
            else
            {

                BoardEvaluator.m_EvaluationCounter = 0;
                //Move move = AlphaBetaRoot(m_GameBoard, Depth);
                move = IterativeDeepeningAlphaBeta(board).BestMove;
            }

            Clock.Stop();

            if (Moved != null)
                Moved(move);


            m_MoveThread.Abort();
        }

        private static IterativeDeepeningResult IterativeDeepeningAlphaBeta(Board _board)
        {
            IterativeDeepeningResult result = new IterativeDeepeningResult();

            //Move bestMove = null;
            //Move currentMove = null;


            var moves = MovesGenerator.GetMoves(_board, true);

            //iterative deepening current depth
            int depth;

            Move lastMove = _board.LastMove[1 - _board.CurrentPlayer];
            if (lastMove != null && m_Pondering.Keys.Any(x => x.EqualMove(lastMove)))
            {
                Move lastMoveOnPondering = m_Pondering.Keys.First(x => x.EqualMove(lastMove));
                IterativeDeepeningResult ponderingResult = m_Pondering[lastMoveOnPondering];
                foreach (Move move in ponderingResult.PV_Moves)
                {
                    Move moveToReorder = moves.First(x => x.EqualMove(move));
                    moves.Remove(moveToReorder);
                    moves.Insert(0, moveToReorder);
                }
                depth = ponderingResult.DepthReeched + 1;
            }
            else
            {
                depth = 1;
            }

            int alpha = -31000, beta = 32000;

            //iterative deepening
            for (; !Clock.TimesUp && depth <= Depth; depth++)
            {
                _board.NullMoveOk = true;
                m_NodesSearched = 0;
                AlphaBetaRootResult alphaBetaResult = AlphaBetaRoot(_board, depth, moves, false, alpha, beta);

                if (!Clock.TimesUp && alphaBetaResult.BestMove != null && alphaBetaResult.Finished == true)
                {
                    //used in case of researching
                    bool isSearchOk = true;

                    /*if (alphaBetaResult.Score <= alpha || alphaBetaResult.Score >= beta)
                    {
                        alpha = -31000;
                        beta = 32000;
                        alphaBetaResult = AlphaBetaRoot(_board, depth, moves, false, alpha, beta);
                        if (Clock.TimesUp || alphaBetaResult.BestMove == null && alphaBetaResult.Finished == false)
                            isSearchOk = false; ;
                    }
                    else
                    {
                        alpha = alphaBetaResult.Score - 50;
                        beta = alphaBetaResult.Score + 50;
                    }*/

                    if (isSearchOk)
                    {
                        result.BestMove = alphaBetaResult.BestMove;
                        result.PV_Moves = alphaBetaResult.PV_Moves;
                        result.DepthReeched = depth;
                    }
                    //order moves by the PV moves.
                    foreach (Move move in alphaBetaResult.PV_Moves)
                    {
                        moves.Remove(move);
                        moves.Insert(0, move);
                    }
                }

                Global.WriteToLog(depth.ToString() + ' ' + "100" + ' ' + (Clock.CurrentMoveTimeElapsed.TotalMilliseconds) + ' ' + m_NodesSearched + ' ' + (alphaBetaResult.BestMove != null));
            }
            return result;
        }

        private static void MakePondering(object _board)
        {
            Board board = _board as Board;

            m_Pondering = new Dictionary<Move, IterativeDeepeningResult>();

            var moves = MovesGenerator.GetMoves(board, true);
            Dictionary<Move, List<Move>> responseMoves = new Dictionary<Move, List<Move>>();
            bool finishedDepth = true;
            for (int depth = 1; finishedDepth && depth <= Depth; depth = depth + 1)
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    Move move = moves[i];

                    //order moves by the PV moves.
                    if (depth > 1)
                    {
                        foreach (Move pvMove in m_Pondering[move].PV_Moves)
                        {
                            responseMoves[move].Remove(pvMove);
                            responseMoves[move].Insert(0, pvMove);
                        }
                    }

                    //now clone the game board (to avoid any changes)
                    Board clone = Board.Clone(board);
                    clone.ApplyMove(move);

                    List<Move> response;
                    if (responseMoves.ContainsKey(move))
                        response = responseMoves[move];
                    else
                    {
                        response = MovesGenerator.GetMoves(clone, true);

                        if (response == null)
                            continue;

                        responseMoves.Add(move, response);
                    }

                    //launch alpha beta
                    AlphaBetaRootResult alphaBetaResult = AlphaBetaRoot(clone, depth, response, true);

                    if (alphaBetaResult.Finished == false)
                    {
                        finishedDepth = false;
                        break;
                    }

                    IterativeDeepeningResult moveResult;
                    if (m_Pondering.ContainsKey(move))
                    {
                        moveResult = m_Pondering[move];

                        moveResult.BestMove = alphaBetaResult.BestMove;
                        moveResult.PV_Moves = alphaBetaResult.PV_Moves;
                        moveResult.DepthReeched++;
                    }
                    else
                    {
                        m_Pondering.Add(move, new IterativeDeepeningResult { BestMove = alphaBetaResult.BestMove, DepthReeched = 1, PV_Moves = alphaBetaResult.PV_Moves });
                    }
                }

            }

            m_PonderThread.Abort();
        }

        internal static void Move(Board _board)
        {
            if (m_MoveThread.ThreadState == ThreadState.Stopped)
            {
                m_MoveThread = new Thread(new ParameterizedThreadStart(MakeMove));
            }


            if (_board.Turn == 40)
                Clock.Reset();

            m_MoveThread.Start(_board);
        }

        internal static void Ponder(Board _board)
        {
            StopPondering = false;

            if (m_PonderThread.ThreadState == ThreadState.Stopped)
            {
                m_PonderThread = new Thread(new ParameterizedThreadStart(MakePondering));
            }

            //m_PonderThread.Start(_board);
        }

        internal static void Dispose()
        {
            StopPondering = true;

            if (m_PonderThread.ThreadState == ThreadState.Running)
                m_PonderThread.Abort();
            if (m_MoveThread.ThreadState == ThreadState.Running)
                m_MoveThread.Abort();

        }

        private static AlphaBetaRootResult AlphaBetaRoot(Board _board, int _depth, List<Move> _sortedMoves, bool _ponderingMode)
        {
            return AlphaBetaRoot(_board, _depth, _sortedMoves, _ponderingMode, -31000, 32000);
        }

        private static AlphaBetaRootResult AlphaBetaRoot(Board _board, int _depth, List<Move> _sortedMoves, bool _ponderingMode, int alpha, int beta)
        {
            m_NodesSearched++;


            AlphaBetaRootResult result = new AlphaBetaRootResult { PV_Moves = new List<Move>(), Finished = true };

            for (int i = 0; i < _sortedMoves.Count; i++)
            {
                //check if forced to quit
                if (_ponderingMode)
                {
                    if (StopPondering)
                        result.Finished = false;
                }
                else
                {
                    if (Clock.TimesUp)
                    {
                        result.Finished = false;
                        break;
                    }
                }

                Move move = _sortedMoves[i];
                Board clone = Board.Clone(_board);
                clone.ApplyMove(move);

                int childAlphaBeta = -AlphaBeta(clone, _depth - 1, -beta, -alpha);
                if (childAlphaBeta > alpha)
                {
                    alpha = childAlphaBeta;
                    result.BestMove = move;
                    result.PV_Moves.Add(move);
                }

                if (childAlphaBeta == TIMES_UP)
                {
                    result.Finished = false;
                    break;
                }
                if (beta <= alpha)
                    break;
            }

            result.Score = alpha;
            return result;
        }

        private static int AlphaBeta(Board _board, int _depth, int alpha, int beta)
        {
            m_NodesSearched++;

            /*TranspositionEntry _tableEntry;
            if (_depth > 1 && m_TranspositionTable.TryLookupBoard(_board, out _tableEntry) && _tableEntry.Player == _board.CurrentPlayer)
            {
                if (_tableEntry.Depth >= _depth && _tableEntry.EvaluationType == EvaluationType.Exact)
                    return _tableEntry.Value;
            }*/

            if (_depth <= 0)
            {
                int score = QuiescenceSearch(_board, 0, alpha, beta);

                return score;
            }

            if (_board.IsTerminal != TerminalType.NotTerminal)
            {
                if (_board.IsTerminal == TerminalType.Stalemate)
                    return 0;
            }

            if (_depth > 4 && Clock.TimesUp)
                return TIMES_UP;

            //try a null move first, maybe we will get a cutoff (fail-high) , or just a better lower bound
            if (_board.NullMoveOk)
            {
                Board nullMoveClone = Board.Clone(_board);
                nullMoveClone.ApplyNullMove();
                int nullMoveSearchValue = -AlphaBeta(nullMoveClone, _depth - SHALLOWER_DEPTH_R - 1,-beta, -beta + 1);
                if (nullMoveSearchValue >= beta)
                    return nullMoveSearchValue;
            }
            _board.NullMoveOk = true;

            List<Move> movesBoards;
            if (_depth == 1)
                movesBoards = MovesGenerator.GetMoves(_board, false);
            else
                movesBoards = MovesGenerator.GetMoves(_board, true);

            //if terminal, evaluate
            if (movesBoards == null)
            {
                switch (_board.IsTerminal)
                {
                    case TerminalType.BlackWon:
                        if (_board.CurrentPlayer == Side.Black)
                            return 32000;
                        else
                            return -32000;
                    case TerminalType.WhiteWon:
                        if (_board.CurrentPlayer == Side.White)
                            return 32000;
                        else
                            return -32000;
                    case TerminalType.Stalemate:
                        return 0;
                }
            }

            foreach (Move move in movesBoards)
            {


                Board clone = Board.Clone(_board);
                clone.ApplyMove(move);

                int childAlphaBeta = -AlphaBeta(clone, _depth - 1, -beta, -alpha);
                if (childAlphaBeta > alpha)
                {
                    alpha = childAlphaBeta;
                }

                if (childAlphaBeta == TIMES_UP)
                    return TIMES_UP;

                if (beta <= alpha)
                {
                    break;
                }
            }

            //m_TranspositionTable.StoreBoard(_board, alpha, EvaluationType.Exact, _depth);
            return alpha;
        }

        private static int QuiescenceSearch(Board _board, int _depth, int alpha, int beta)
        {
            m_NodesSearched++;

            int stand_pat = BoardEvaluator.Evaluate(_board, _board.CurrentPlayer);
            if (stand_pat >= beta)
                return beta;

            //I use delta pruning to reduce the number of nodes checked
            int BIG_DELTA = 500; // rook value

            if (stand_pat < alpha - BIG_DELTA)
            {
                return alpha;
            }

            if (alpha < stand_pat)
                alpha = stand_pat;

            List<Move> movesBoards = QuiescenceMovesGenerator.GetMoves(_board, _board.CurrentPlayer, true);

            if (movesBoards == null)
            {
                switch (_board.IsTerminal)
                {
                    case TerminalType.BlackWon:
                        if (_board.CurrentPlayer == Side.Black)
                            return 32000;
                        else
                            return -32000;
                    case TerminalType.WhiteWon:
                        if (_board.CurrentPlayer == Side.White)
                            return 32000;
                        else
                            return -32000;
                    case TerminalType.Stalemate:
                        return 0;
                }
            }

            foreach (Move move in movesBoards)
            {
                Board clone = Board.Clone(_board);
                clone.ApplyMove(move);

                int score = -QuiescenceSearch(clone, _depth - 1, -beta, -alpha);

                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }

            return alpha;

        }

        private  class IterativeDeepeningResult
        {
            public int DepthReeched { get; set; }

            public Move BestMove { get; set; }

            public List<Move> PV_Moves { get; set; }
        }

        private class AlphaBetaRootResult
        {
            public bool Finished { get; set; }

            public Move BestMove { get; set; }

            public int Score { get; set; }

            public List<Move> PV_Moves { get; set; }
        }
    }
}
