using System;
using System.Collections.Generic;

using System.Text;

namespace TerraFirma
{
    //enum casting has bad performance when run so many times (for example board.BitBoard[Piece.BlackPawn + _side] ).

    internal static class Side
    {
        public const byte Black = 0;
        public const byte White = 1;
    }

    //enum casting has bad performance when run so many times (for example board.BitBoard[Piece.BlackPawn] ).
    internal static class Piece
    {
        public const int BlackPawn = 0;
        public const int BlackKnight = 2;
        public const int BlackBishop = 4;
        public const int BlackRook = 6;
        public const int BlackQueen = 8;
        public const int BlackKing = 10;
        public const int WhitePawn = 1;
        public const int WhiteKnight = 3;
        public const int WhiteBishop = 5;
        public const int WhiteRook = 7;
        public const int WhiteQueen = 9;
        public const int WhiteKing = 11;
        public const int AllBlacks = 12;
        public const int AllWhites = 13;
        public const int EmptySquare = 14;
    }

    internal static class MoveTypes
    {
        public const int NullMove = 1;
        public const int Normal = 8;
        public const int DoublePawn = 16;
        public const int PromotionKnight = 32;
        public const int EnPassant = 64;
        public const int Capture = 128;
        public const int CastleQueenSide = 256;
        public const int CastleKingSide = 512;
        public const int PromotionQueen = 1024;
    }

    [Flags]
    internal enum CastleOption
    {
        HasKingSide = 1,
        HasQueenSide = 2,
        CastledKingSide = 4,
        CastledQueenSide = 8,
        NoOption = 16
    }

    internal enum PlayerType
    {
        Human,
        Computer
    }
    internal enum WinBoardCommands
    {
        New,
        Xboard,
        Quit,
        Random,
        Force,
        Go,
        White,
        Black,
        Level,
        St,
        Sd,
        Time,
        Otim,
        Usermove,
        QMark,
        Draw,
        Result,
        Setboard,
        Edit,
        Hint,
        Bk,
        Undo,
        Remove,
        Hard,
        Easy,
        Post,
        Nopost,
        Analyze,
        Name,
        Rating,
        ICS,
        Computer,
        Protover,
        Accepted,
        Unknown
    }

    internal enum EngineFeatureCommands
    {
        Ping,
        Setboard,
        Playouther,
        San,
        Usermove,
        Time,
        Draw,
        Reuse,
        Analyze,
        Myname,
        Colors,
        Ics,
        Name,
        Pause,
        Done
    }

    internal enum EngineCommands
    {
        Feature,
        Illegal_move,
        Error_Unknown,
        Error_NotLegal,
        Error_TooMany,
        Move,
        Result,
        Resign,
        OfferDraw,
        Tellopponent,
        Tellothers,
        Tellall,
        TellUser,
        TellUserError,
        tellics,

    }

    internal enum TerminalType
    {
        NotTerminal = 0,
        WhiteWon = 2,
        BlackWon = 1,
        Stalemate = 3
    }

    internal enum EvaluationType
    {
        None,
        Exact,
        UpperBound,
        LowerBound
    }
}
