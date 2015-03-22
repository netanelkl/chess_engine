using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace TerraFirma
{
    /// <summary>
    /// Provides support for parsing, loading and saving of PGN opening book files.
    /// </summary>
    internal static class OpeningBook
    {

        private static OpeningMove m_Root;

        /// <summary>
        /// true if the Game is still in the opening book
        /// </summary>
        internal static bool IsInBook;

        private static OpeningMove CurrentMove;

        /// <summary>
        /// Provides a random move out of the valid opening moves availible at the current node.
        /// First try excellent moves, then good, and so forth.
        /// </summary>
        /// <returns>The returned move, must be played.</returns>
        internal static Move GetRandomMove()
        {
            //we always make sure we are still inside the book
            if (IsInBook == false)
                return null;

            Random rand = new Random();
            //look for excellent moves
            var GreatResponses = CurrentMove.Responses.Where(x => x.MoveType == OpeningMoveType.Excellent);

            //if no such exists, look for good move.
            if (GreatResponses.Count() == 0)
                GreatResponses = CurrentMove.Responses.Where(x => x.MoveType == OpeningMoveType.Good);

            //if no such exists, look for any move (I don't load bad moves at all)
            if (GreatResponses.Count() == 0)
                GreatResponses = CurrentMove.Responses;

            //pick one randomly , perheps as a more balanced move, the random distribution should corraspond to the probability of node.
            int movePosition = rand.Next(0, GreatResponses.Sum(x => x.Count) - 1);

            //now we pick one according to the probability
            int counter = 0;
            foreach (OpeningMove openingMove in GreatResponses)
            {
                counter += openingMove.Count;

                if (movePosition < counter)
                {
                    CurrentMove = openingMove;
                    break;
                }
            }

            //if the found move would draw us out of the book, make sure the user won't ask for another move in the future.
            if (CurrentMove.Responses.Count == 0)
                IsInBook = false;

            return CurrentMove.Move;
        }

        /// <summary>
        /// Resets to the starting position
        /// </summary>
        static internal void ResetBook()
        {
            if (m_Root != null)
                IsInBook = true;

            //root is always the starting position
            CurrentMove = m_Root;
        }

        /// <summary>
        /// Tells the opening book what move did the oponent played , so the opening book could update itself.
        /// </summary>
        /// <param name="move">The move the opponent played</param>
        static internal void MoveApplied(Move move)
        {
            //if not in book, break away.
            if (!IsInBook)
                return;

            //look for such a move
            OpeningMove applied = CurrentMove.Responses.FirstOrDefault(x => x.Move.EqualMove(move));

            //if not found, we are leaving the book, otherwise we apply move and check if we will then be leaving the book.
            if (applied == null)
            {
                IsInBook = false;
            }
            else
            {
                if (applied.Responses.Count == 0)
                    IsInBook = false;

                CurrentMove = applied;
            }
        }

        static OpeningBook()
        {
            try
            {
                //CreateOpeningBooks();
                Generic<OpeningMove>.LoadFromDisk(out m_Root, "ChessEngineBook.xml");
                CurrentMove = m_Root;
                IsInBook = false;
            }
            catch (Exception ex)
            {
                IsInBook = false;
                Global.WriteToLog(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// Parses PGN files and saves them to the 'ChessEngineBook.xml' file.
        /// NOTE: we should also zip the files to conserve space.
        /// </summary>
        private static void CreateOpeningBooks()
        {
            List<string> m_OpeningBookLocations = new List<string>
                {
                    //"RJF60.pgn",
                    "strong.PGN",
                    "CMX.PGN",
                };

            OpeningMove startingPoint = new OpeningMove();

            foreach (string openingBookLocation in m_OpeningBookLocations)
            {
                TextReader tr = null;
                tr = new StreamReader(openingBookLocation);
                string book = tr.ReadToEnd();
                tr.Close();
                AnalyzeBook(book, startingPoint);
            }

            Generic<OpeningMove>.SaveToDisk(startingPoint, "ChessEngineBook.xml");

        }

        /// <summary>
        /// Parses the book string in the standard PGN format, and inserts it into the starting position node
        /// </summary>
        /// <param name="book">A standard PGN formatted string</param>
        /// <param name="startingPoint">The starting node to add responses to</param>
        private static void AnalyzeBook(string book, OpeningMove startingPoint)
        {
            try
            {
                string[] tags = book.Replace("\r\n", " ").Split(new string[] { "[" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 6; i < tags.Length; i += 7)
                {
                    string movesString = tags[i].Substring(tags[i].IndexOf(']') + 1);
                    string[] movesArray = movesString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    Board board = new Board();
                    board.StartingPosition();

                    //parentMove will slide, containing the parent to the move.
                    OpeningMove parentMove = startingPoint;
                    bool abortMatch = false;

                    for (int j = 0; j < movesArray.Length && !abortMatch; j++)
                    {
                        if (j % 3 == 0)
                        {
                            continue;
                        }

                        if (parentMove.LegalMoves == null)
                            parentMove.LegalMoves = MovesGenerator.GetLegalMoves(board);

                        OpeningMove move = PGNMoveParser(board, movesArray[j], parentMove.LegalMoves);

                        if (move.MoveType != OpeningMoveType.Excellent && move.MoveType != OpeningMoveType.Good)
                        {
                            abortMatch = true;
                            break;
                        }

                        //to avoid duplicate entries problem, we make sure to use same instance of a move.
                        OpeningMove storedSameMove = parentMove.Responses.FirstOrDefault(x => x.Move.EqualMove(move.Move));

                        if (storedSameMove == null)
                        {
                            move.Count = 1;
                            parentMove.Responses.Add(move);
                        }
                        else
                        {
                            parentMove.Count++;
                            move = storedSameMove;
                        }

                        parentMove = move;

                        board.ApplyMove(move.Move);

                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Parses one single move.
        /// </summary>
        /// <param name="_board">The current board , current opening book's game board</param>
        /// <param name="_pgnMove">the move's PGN notation</param>
        /// <param name="moves">A list of all availible moves</param>
        /// <returns>An opening move represents the parsed response.</returns>
        private static OpeningMove PGNMoveParser(Board _board, string _pgnMove, List<Move> moves)
        {
            bool isCapture = _pgnMove.Contains('x');

            bool test = false;

            if (test)
                moves = MovesGenerator.GetLegalMoves(_board);

            OpeningMove move = new OpeningMove();
            try
            {
                //first check if it is castle
                if (_pgnMove.StartsWith("O-O-O"))
                    move.Move = moves.First(x => x.MoveType == MoveTypes.CastleQueenSide);
                else if (_pgnMove.StartsWith("O-O"))
                    move.Move = moves.First(x => x.MoveType == MoveTypes.CastleKingSide);
                else
                {
                    int piece = PGNletterToPieceParser(_pgnMove[0], _board.CurrentPlayer);

                    var pieceMoves = moves.Where(x => x.MovingPiece == piece);



                    if (pieceMoves.Count() == 1)
                    {
                        move.Move = pieceMoves.ElementAt(0);
                    }
                    else
                    {
                        if (piece == Piece.BlackPawn || piece == Piece.WhitePawn)
                        {
                            var sourceMoves = pieceMoves.Where(x => x.SourceFile == _pgnMove[0]);
                            if (sourceMoves.Count() == 1)
                                move.Move = sourceMoves.ElementAt(0);
                            else
                            {
                                if (isCapture)
                                {
                                    string dest = _pgnMove.Substring(_pgnMove.IndexOf('x') + 1, 2);
                                    move.Move = sourceMoves.First(x => x.DestinationSquareString == dest);
                                }
                                else
                                {
                                    string dest = _pgnMove.Substring(0, 2);
                                    move.Move = sourceMoves.First(x => x.DestinationSquareString.StartsWith(dest));
                                }
                            }
                        }
                        else
                        {
                            IEnumerable<Move> destMoves;
                            if (isCapture)
                            {
                                string dest = _pgnMove.Substring(_pgnMove.IndexOf('x') + 1, 2);
                                destMoves = pieceMoves.Where(x => x.DestinationSquareString == dest);
                            }
                            else
                            {
                                int indexDestRank = _pgnMove.LastIndexOfAny(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                                string dest = _pgnMove.Substring(indexDestRank - 1, 2);
                                destMoves = pieceMoves.Where(x => x.DestinationSquareString == dest);
                            }

                            switch (destMoves.Count())
                            {
                                case 1:
                                    move.Move = destMoves.ElementAt(0);
                                    break;
                                case 2:
                                    //either rank or file ambiguity
                                    if (_pgnMove[1] >= '1' && _pgnMove[1] <= '9')
                                        //rank ambiguity
                                        move.Move = destMoves.First(x => x.SourceRank == _pgnMove[1]);
                                    else
                                        move.Move = destMoves.First(x => x.SourceFile == _pgnMove[1]);
                                    break;
                                case 3:
                                    move.Move = destMoves.First(x => x.SourceSquareString == _pgnMove.Substring(1, 2));
                                    break;
                            }

                        }

                    }

                    //now get move type
                    if (_pgnMove.Contains("!!"))
                        move.MoveType = OpeningMoveType.Excellent;
                    else if (_pgnMove.Contains("!?"))
                        move.MoveType = OpeningMoveType.Interesting;
                    else if (_pgnMove.Contains("?!"))
                        move.MoveType = OpeningMoveType.Dubious;
                    else if (_pgnMove.Contains("!"))
                        move.MoveType = OpeningMoveType.Good;
                    else if (_pgnMove.Contains("??"))
                        move.MoveType = OpeningMoveType.Blunder;
                    else if (_pgnMove.Contains("?"))
                        move.MoveType = OpeningMoveType.Bad;

                }

                if (move.Move == null)
                    throw new System.Exception("not found");

                return move;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Retrieves the piece (colored) of the PGN move.
        /// </summary>
        /// <param name="piece">The first char of the move</param>
        /// <param name="player">The current player</param>
        /// <returns>An TerraFirma.Piece int represents the move's piece type.</returns>
        private static int PGNletterToPieceParser(char piece, int player)
        {
            switch (piece)
            {
                case 'N': return player == Side.White ? Piece.WhiteKnight : Piece.BlackKnight;
                case 'K': return player == Side.White ? Piece.WhiteKing : Piece.BlackKing;
                case 'B': return player == Side.White ? Piece.WhiteBishop : Piece.BlackBishop;
                case 'R': return player == Side.White ? Piece.WhiteRook : Piece.BlackRook;
                case 'Q': return player == Side.White ? Piece.WhiteQueen : Piece.BlackQueen;
                default: return player == Side.White ? Piece.WhitePawn : Piece.BlackPawn;
            }
        }


    }

    /// <summary>
    /// The quality of the current opening move (retrieved from book.)
    /// </summary>
    public enum OpeningMoveType
    {
        /// <summary>
        /// an excellent move
        /// </summary>
        Excellent,

        /// <summary>
        /// a good book move
        /// </summary>
        Good,

        /// <summary>
        /// a bad book move
        /// </summary>
        Bad,

        /// <summary>
        /// a blunder, should be avoided.
        /// </summary>
        Blunder,

        /// <summary>
        /// a move that might be interesting
        /// </summary>
        Interesting,

        /// <summary>
        /// a dubious move
        /// </summary>
        Dubious
    }

    /// <summary>
    /// Represents an opening move
    /// </summary>
    public class OpeningMove
    {
        /// <summary>
        /// The quality of the move.
        /// </summary>
        public OpeningMoveType MoveType { get; set; }

        /// <summary>
        /// The actual board move.
        /// </summary>
        public Move Move { get; set; }

        /// <summary>
        /// Used only when creating opening books, for performance purposes.
        /// </summary>
        [XmlIgnore]
        public List<Move> LegalMoves { get; set; }
        public int Count { get; set; }
        /// <summary>
        /// The valid responses of the current opening book move retrieved by book.
        /// </summary>
        public List<OpeningMove> Responses { get; set; }

        public OpeningMove()
        {
            Responses = new List<OpeningMove>();
        }
    }
}
