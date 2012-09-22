// ERROR: Not supported in C#: OptionDeclaration

namespace GOLDEngine.Tables
{
    internal enum LRConflict
    {
        ShiftShift = 1,
        //Never happens
        ShiftReduce = 2,
        ReduceReduce = 3,
        AcceptReduce = 4,
        //Never happens with this implementation
        None = 5
    }

    //===== NOTE: MUST MATCH FILE DEFINITION
    internal enum LRActionType
    {
        Shift = 1,
        //Shift a symbol and goto a state
        Reduce = 2,
        //Reduce by a specified rule
        Goto = 3,
        //Goto to a state on reduction
        Accept = 4,
        //Input successfully parsed
        Error = 5
        //Programmars see this often!
    }

    internal class LRAction
    {
        public Symbol Symbol;
        public LRActionType Type;
        //shift to state, reduce rule, goto state
        public short Value;

        public LRAction(Symbol TheSymbol, LRActionType Type, short Value)
        {
            this.Symbol = TheSymbol;
            this.Type = Type;
            this.Value = Value;
        }
    }

    internal class LRState : ArrayList
    {

        public new short IndexOf(Symbol Item)
        {
            //Returns the index of SymbolIndex in the table, -1 if not found
            short n = 0;
            short Index = 0;
            bool Found = false;

            n = 0;
            Found = false;
            while ((!Found) & n < base.Count)
            {
                if (Item.Equals(base.Item(n).Symbol))
                {
                    Index = n;
                    Found = true;
                }
                n += 1;
            }

            if (Found)
            {
                return Index;
            }
            else
            {
                return -1;
            }
        }

        public new void Add(LRAction Action)
        {
            base.Add(Action);
        }

        public new LRAction this[short Index]
        {
            get { return base.Item(Index); }
            set { base.Item(Index) = value; }
        }

        public new LRAction this[Symbol Sym]
        {
            get
            {
                int Index = IndexOf(Sym);
                if (Index != -1)
                {
                    return base.Item(Index);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                int Index = IndexOf(Sym);
                if (Index != -1)
                {
                    base.Item(Index) = value;
                }
            }
        }
    }

    internal class LRStateList : ArrayList
    {


        public short InitialState;
        public LRStateList()
            : base()
        {
            InitialState = 0;
        }

        internal LRStateList(int Size)
            : base()
        {
            ReDimension(Size);
            InitialState = 0;
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

        public new LRState this[int Index]
        {
            get { return base.Item(Index); }

            set { base.Item(Index) = value; }
        }

        public new int Add(ref LRState Item)
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
