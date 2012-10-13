using System.Collections.Generic;

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
        /// <summary>
        /// case LRActionType.Shift: GotoState(Value);
        /// case LRActionType.Accept: ;
        /// case LRActionType.Reduce: GetProduction(Value);
        /// </summary>
        public short Value;

        public LRAction(Symbol TheSymbol, LRActionType Type, short Value)
        {
            this.Symbol = TheSymbol;
            this.Type = Type;
            this.Value = Value;
        }
    }

    internal class LRState : List<LRAction>
    {
    }

    internal class LRStateList : List<LRState>
    {
        public short InitialState;

        internal LRStateList(int Size)
            : base(Size)
        {
            //Increase the size of the array to Size empty elements.
            for (int i = 0; i < Size; ++i)
            {
                base.Add(null);
            }
            InitialState = 0;
        }
    }
}
