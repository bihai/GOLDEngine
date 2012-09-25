using System;
using System.Collections.Generic;
using System.Text;

namespace GOLDEngine
{
    /// <summary>
    /// The Terminal subclass of Token is used, for all SymbolTypes except SymbolType.Nonterminal.
    /// Each Terminal has associated string data: which you can get using the Text property, or ToString() method.
    /// </summary>
    public class Terminal : Token
    {
        string m_Text;

        /// <summary>
        /// Applications can use this method to create a 'virtual' terminal,
        /// which they can then push into the parser.
        /// </summary>
        public static Terminal CreateVirtual(Symbol Parent, string Text)
        {
            return new Terminal(Parent, Text);
        }

        private Terminal(Symbol Parent, string Text)
            : base(Parent, null, true)
        {
            m_Text = Text;
        }

        internal Terminal(Symbol Parent, string Text, Position sysPosition)
            : base(Parent, sysPosition, true)
        {
            m_Text = Text;
        }

        /// <summary>
        /// Returns the string associated with the token.
        /// </summary>
        public string Text
        {
            get { return m_Text; }
        }
        public override string ToString()
        {
            return m_Text;
        }

        // Internal methods.

        internal int TextLength
        {
            get { return m_Text.Length; }
        }

        internal void TextAppend(Terminal rhs)
        {
            m_Text = m_Text + rhs.m_Text;
        }

        internal void TextAppendFirstChar(Terminal rhs)
        {
            m_Text = m_Text + rhs.m_Text[0];
        }
    }
}
