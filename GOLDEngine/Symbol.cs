using System.Collections.Generic;
using System.ComponentModel;

using GOLDEngine.Tables;

namespace GOLDEngine
{
    public enum SymbolType
    {
        Nonterminal = 0,
        //Nonterminal 
        Content = 1,
        //Passed to the parser
        Noise = 2,
        //Ignored by the parser
        End = 3,
        //End character (EOF)
        GroupStart = 4,
        //Group start  
        GroupEnd = 5,
        //Group end   
        //Note: There is no value 6. CommentLine was deprecated.
        Error = 7
        //Error symbol
    }

    public class Symbol
    {
        //================================================================================
        // Class Name:
        //      Symbol
        //
        // Purpose:
        //       This class is used to store of the nonterminals used by the Deterministic
        //       Finite Automata (DFA) and LALR Parser. Symbols can be either
        //       terminals (which represent a class of tokens - such as identifiers) or
        //       nonterminals (which represent the rules and structures of the grammar).
        //       Terminal symbols fall into several catagories for use by the GOLD Parser
        //       Engine which are enumerated below.
        //
        // Author(s):
        //      Devin Cook
        //
        // Dependacies:
        //      (None)
        //
        //================================================================================

        private string m_Name;
        private SymbolType m_Type;

        private short m_TableIndex;

        internal Symbol(string Name, SymbolType Type, short TableIndex)
        {
            m_Name = Name;
            m_Type = Type;
            m_TableIndex = TableIndex;
        }

        [Description("Returns the type of the symbol.")]
        public SymbolType Type
        {
            get { return m_Type; }
        }

        [Description("Returns the index of the symbol in the Symbol Table,")]
        public short TableIndex()
        {
            return m_TableIndex;
        }

        [Description("Returns the name of the symbol.")]
        public string Name()
        {
            return m_Name;
        }

        [Description("Returns the text representing the text in BNF format.")]
        public string Text(bool AlwaysDelimitTerminals)
        {
            string Result = null;

            switch (m_Type)
            {
                case SymbolType.Nonterminal:
                    Result = "<" + Name() + ">";
                    break;
                case SymbolType.Content:
                    Result = LiteralFormat(Name(), AlwaysDelimitTerminals);
                    break;
                default:
                    Result = "(" + Name() + ")";
                    break;
            }

            return Result;
        }

        [Description("Returns the text representing the text in BNF format.")]
        public string Text()
        {
            return this.Text(false);
        }

        private string LiteralFormat(string Source, bool ForceDelimit)
        {
            short n = 0;
            char ch = '\0';

            if (Source == "'")
            {
                return "''";
            }
            else
            {
                n = 0;
                while (n < Source.Length & (!ForceDelimit))
                {
                    ch = Source[n];
                    ForceDelimit = !(char.IsLetter(ch) | ch == '.' | ch == '_' | ch == '-');
                    n += 1;
                }

                if (ForceDelimit)
                {
                    return "'" + Source + "'";
                }
                else
                {
                    return Source;
                }
            }
        }

        public override string ToString()
        {
            return Text();
        }
    }

    public class SymbolList
    {
        //CANNOT inherit, must hide methods that edit the list
        private List<Symbol> m_Array;

        internal SymbolList(List<Symbol> symbols)
        {
            m_Array = symbols;
        }

        internal SymbolList(int Size)
        {
            m_Array = new List<Symbol>(Size);
            //Increase the size of the array to Size empty elements.
            for (int n = 0; n <= Size - 1; n++)
            {
                m_Array.Add(null);
            }
        }

        [Description("Returns the symbol with the specified index.")]
        public Symbol this[int Index]
        {
            get
            {
                if (Index >= 0 & Index < m_Array.Count)
                {
                    return m_Array[Index];
                }
                else
                {
                    return null;
                }
            }

            internal set { m_Array[Index] = value; }
        }

        [Description("Returns the total number of symbols in the list.")]
        public int Count
        {
            get { return m_Array.Count; }
        }

        internal Symbol GetFirstOfType(SymbolType Type)
        {
            return m_Array.Find(symbol => symbol.Type == Type);
        }

        public override string ToString()
        {
            return Text();
        }

        [Description("Returns a list of the symbol names in BNF format.")]
        public string Text(string Separator, bool AlwaysDelimitTerminals)
        {
            string Result = "";
            int n = 0;
            Symbol Sym = default(Symbol);

            for (n = 0; n <= m_Array.Count - 1; n++)
            {
                Sym = m_Array[n];
                Result += (n == 0 ? "" : Separator) + Sym.Text(AlwaysDelimitTerminals);
            }

            return Result;
        }

        [Description("Returns a list of the symbol names in BNF format.")]
        public string Text()
        {
            return this.Text(", ", false);
        }
    }
}
