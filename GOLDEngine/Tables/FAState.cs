// ERROR: Not supported in C#: OptionDeclaration

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

    internal class FAStateList : ArrayList
    {


        public short InitialState;
        //===== DFA runtime variables

        public Symbol ErrorSymbol;
        public FAStateList()
            : base()
        {

            InitialState = 0;
            ErrorSymbol = null;
        }

        internal FAStateList(int Size)
            : base()
        {
            ReDimension(Size);

            InitialState = 0;
            ErrorSymbol = null;
        }

        internal void ReDimension(int Size)
        {
            //Increase the size of the array to Size empty elements.
            int n = 0;

            base.Clear();
            for (n = 0; n <= Size - 1; n++)
            {
                base.Add(null);
            }
        }

        public new FAState this[int Index]
        {
            get { return base.Item(Index); }

            set { base.Item(Index) = value; }
        }

        public new int Add(FAState Item)
        {
            return base.Add(Item);
        }
    }

    //=======================================================
    //Service provided by Telerik (www.telerik.com)
    //Conversion powered by NRefactory.
    //Twitter: @telerik, @toddanglin
    //Facebook: facebook.com/telerik
    //=======================================================
}
