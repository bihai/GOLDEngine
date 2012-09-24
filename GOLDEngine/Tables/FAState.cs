using System.Collections.Generic;

namespace GOLDEngine.Tables
{
    internal class FAState
    {
        //================================================================================
        // Class Name:
        //      FAState
        //
        // Purpose:
        //      Represents a state in the Deterministic Finite Automata which is used by
        //      the tokenizer.
        //
        // Author(s):
        //      Devin Cook
        //
        // Dependacies:
        //      FAEdge, Symbol
        //
        //================================================================================

        public FAEdgeList Edges;

        public Symbol Accept;
        public FAState(Symbol Accept)
        {
            this.Accept = Accept;
            this.Edges = new FAEdgeList();
        }

        public FAState()
        {
            this.Accept = null;
            this.Edges = new FAEdgeList();
        }
    }

    internal class FAStateList : List<FAState>
    {
        public short InitialState;
        //===== DFA runtime variables

        public Symbol ErrorSymbol;

        internal FAStateList(int Size)
            : base(Size)
        {
            //Increase the size of the array to Size empty elements.
            for (int i = 0; i < Size; ++i)
            {
                base.Add(null);
            }
            InitialState = 0;
            ErrorSymbol = null;
        }
    }
}
