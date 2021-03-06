using System.ComponentModel;
using System.Collections.Generic;

namespace GOLDEngine
{
    public class Production
    {
        //================================================================================
        // Class Name:
        //      Production 
        //
        // Instancing:
        //      Public; Non-creatable  (VB Setting: 2- PublicNotCreatable)
        //
        // Purpose:
        //      The Rule class is used to represent the logical structures of the grammar.
        //      Rules consist of a head containing a nonterminal followed by a series of
        //      both nonterminals and terminals.
        //
        // Author(s):
        //      Devin Cook
        //      http://www.devincook.com/goldparser
        //
        // Dependacies:
        //      Symbol Class, SymbolList Class
        //
        //================================================================================

        private Symbol m_Head;
        private SymbolList m_Handle;
        private short m_TableIndex;

        internal Production(Symbol Head, short TableIndex, SymbolList Handle)
        {
            m_Head = Head;
            m_Handle = Handle;
            m_TableIndex = TableIndex;
        }

        [Description("Returns the head of the production.")]
        public Symbol Head()
        {
            return m_Head;
        }

        [Description("Returns the symbol list containing the handle (body) of the production.")]
        public SymbolList Handle()
        {
            return m_Handle;
        }

        [Description("Returns the index of the production in the Production Table.")]
        public short TableIndex()
        {
            return m_TableIndex;
        }

        public override string ToString()
        {
            return Text();
        }

        [Description("Returns the production in BNF.")]
        public string Text() { return Text(false); }
        public string Text(bool AlwaysDelimitTerminals)
        {
            return m_Head.Text() + " ::= " + m_Handle.Text(" ", AlwaysDelimitTerminals);
        }

        internal bool ContainsOneNonTerminal()
        {
            bool Result = false;

            if (m_Handle.Count == 1)
            {
                if (m_Handle[0].Type == SymbolType.Nonterminal)
                {
                    Result = true;
                }
            }

            return Result;
        }
    }

    public class ProductionList
    {
        //Cannot inherit, must hide methods that change the list
        private List<Production> m_Array;

        internal ProductionList(int Size)
        {
            m_Array = new List<Production>(Size);
            //Increase the size of the array to Size empty elements.
            for (int n = 0; n <= Size - 1; n++)
            {
                m_Array.Add(null);
            }
        }

        [Description("Returns the production with the specified index.")]
        public Production this[int Index]
        {
            get { return m_Array[Index]; }

            internal set { m_Array[Index] = value; }
        }

        [Description("Returns the total number of productions in the list.")]
        public int Count()
        {
            return m_Array.Count;
        }

        internal void Add(Production Item)
        {
            m_Array.Add(Item);
        }
    }
}
