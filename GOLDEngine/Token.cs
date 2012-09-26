using System.Collections.Generic;
using System.ComponentModel;

using GOLDEngine.Tables;

namespace GOLDEngine
{
    /// <summary>
    /// Token is a base class for two subclasses: Reduction, and Terminal.
    /// Every Token is either a Reduction or a Terminal.
    /// Data associated with the token is contained in the subclass.
    /// </summary>
    public class Token
    {
        private Symbol m_Parent;
        private Position? m_Position;

        protected Token(Symbol Parent, Position? position, bool isTerminal)
        {
            if ((Parent.Type != SymbolType.Nonterminal) ^ isTerminal)
            {
                throw new ParserException("Unexpected SymbolType");
            }
            m_Parent = Parent;
            m_Position = position;
        }

        /// <summary>
        /// Returns the parent symbol of the token.
        /// </summary>
        public Symbol Parent
        {
            get { return m_Parent; }
        }

        /// <summary>
        /// Returns the symbol type associated with this token.
        /// </summary>
        public SymbolType Type()
        {
            return m_Parent.Type;
        }

        /// <summary>
        /// Returns the line/column position where the token was read, if this Token is a Terminal;
        /// or returns null if this Token is a Reduction.
        /// </summary>
        public Position? Position
        {
            get { return m_Position; }
        }

        public Terminal AsTerminal { get { return this as Terminal; } }
        public Reduction AsReduction { get { return this as Reduction; } }

        internal void TrimReduction(Symbol newSymbol)
        {
            m_Parent = newSymbol;
        }
    }
}
