using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerraFirma
{
    internal struct TranspositionEntry
    {

        public EvaluationType EvaluationType;

        public int Value;

        public int Depth;

        // Board position signature, used to detect collisions
        public long CollisionKey;

        //the turn on which it was stored ( to erase old positions )
        public int Turn;

        public int Player;

        public override string ToString()
        {
            return "Value:" + Value + ' ' + "Depth:" + Depth + ' ' + "Turn:" + Turn + ' ' + "Player:" + Player + EvaluationType.ToString() ;  
        }
    }


    internal class TranspositionTable
    {
        // The size of a transposition table, in entries
        private static int TABLE_SIZE = 99971;

        // Data
        private TranspositionEntry[] Table;

        // Construction
        internal TranspositionTable()
        {
            Table = new TranspositionEntry[TABLE_SIZE];
            for (int i = 0; i < TABLE_SIZE; i++)
            {
                Table[i] = new TranspositionEntry();
            }
        }

        // bool LookupBoard( jcBoard theBoard, jcMove theMove )
        // Verify whether there is a stored evaluation for a given board.
        // If so, return TRUE and copy the appropriate values into the
        // output parameter
        internal bool TryLookupBoard(Board _board,out TranspositionEntry _entry)
        {
            // Find the board's hash position in Table
            int key = Math.Abs(_board.GetHashCode() % TABLE_SIZE);
            TranspositionEntry entry = Table[key];
            _entry = entry;
            // If the entry is an empty placeholder, we don't have a match
            if (entry.EvaluationType == EvaluationType.None)
                return false;

            // Check for a hashing collision!
            if (entry.CollisionKey != _board.HashCollisionsKey())
                return false;

            // Now, we know that we have a match!  Copy it into the output parameter
            // and return

            return true;
        }

        // public StoreBoard( theBoard, eval, evalType, depth, timeStamp )
        // Store a good evaluation found through alphabeta for a certain board position
        internal bool StoreBoard(Board _board, int _value, EvaluationType _evaluationType, int _depth)
        {
            int key = Math.Abs(_board.GetHashCode() % TABLE_SIZE);

            // Would we erase a more useful (i.e., higher) position if we stored this
            // one?  If so, don't bother!
            if ((Table[key].EvaluationType != EvaluationType.None) && (Table[key].Depth > _depth) && (Table[key].Turn >= _board.Turn))
                return true;

            // And now, do the actual work
            Table[key].CollisionKey = _board.HashCollisionsKey();
            Table[key].Value = _value;
            Table[key].Depth = _depth;
            Table[key].EvaluationType = _evaluationType;
            Table[key].Turn = _board.Turn;
            Table[key].Player = _board.CurrentPlayer;
            return true;
        }

    }
}
