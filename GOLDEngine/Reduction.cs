using System.ComponentModel;

namespace GOLDEngine
{
    public class Reduction : Token
    {
        Production m_production;
        Token[] m_tokens;
        object m_Tag;

        public Reduction(Production production, Token[] tokens)
            : base(production.Head(), null, false)
        {
            m_tokens = tokens;
            m_production = production;
        }

        public int Count
        {
            get { return m_tokens.Length; } // Same as Symbols.Count
        }

        public Token this[int index]
        {
            get { return m_tokens[index]; }
        }

        public Token[] Tokens
        {
            get { return m_tokens; }
        }

        public SymbolList Symbols
        {
            get { return m_production.Handle(); }
        }

        /// <summary>
        /// Returns the parent production.
        /// </summary>
        public Production Production
        {
            get { return m_production; }
        }

        /// <summary>
        /// Returns/sets any additional user-defined data to this object.
        /// </summary>
        public object Tag
        {
            get { return m_Tag; }
            set { m_Tag = value; }
        }
    }
}
