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
        private short m_State;
        private Position? m_Position;

        /// <summary>
        /// Create stack top item. Only needs state.
        /// </summary>
        internal static Token CreateFirstToken(short initialLRState)
        {
            return new Token(initialLRState);
        }
        Token(short initialLRState)
        {
            m_Parent = null;
            m_State = initialLRState;
            m_Position = null;
        }

        protected Token(Symbol Parent, Position? position, bool isTerminal)
        {
            if ((Parent.Type != SymbolType.Nonterminal) ^ isTerminal)
            {
                throw new ParserException("Unexpected SymbolType");
            }
            m_Parent = Parent;
            m_State = 0;
            m_Position = position;
        }

        internal short State
        {
            get { return m_State; }
            set { m_State = value; }
        }

        /// <summary>
        /// Returns the parent symbol of the token.
        /// </summary>
        public Symbol Parent
        {
            get { return m_Parent; }
            internal set { m_Parent = value; }
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

        internal Group Group()
        {
            return m_Parent.Group;
        }
    }

    internal class TokenQueueStack
    {
        private List<Token> m_Items;

        internal TokenQueueStack()
        {
            m_Items = new List<Token>();
        }

        internal int Count
        {
            get { return m_Items.Count; }
        }

        internal void Clear()
        {
            m_Items.Clear();
        }

        /// <summary>
        /// Add to end of list.
        /// </summary>
        internal void Enqueue(Token TheToken)
        {
            m_Items.Add(TheToken);
        }

        /// <summary>
        /// Fetch and remove from front of list.
        /// </summary>
        internal Token Pop()
        {
            Token Result = m_Items[0];
            m_Items.RemoveAt(0);
            return Result;
        }

        /// <summary>
        /// Fetch from front of list.
        /// </summary>
        internal Token Peek()
        {
            if (m_Items.Count >= 1)
            {
                return m_Items[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Add to front of list.
        /// </summary>
        internal void Push(Token TheToken)
        {
            m_Items.Insert(0, TheToken);
        }
    }
}
