using System;
using System.Collections.Generic;
using System.Text;

namespace GOLDEngine
{
    public interface ITokenVisitor
    {
        void OnTerminal(Terminal terminal);
        void OnReduction(Reduction reduction);
    }

    internal class TokenToText : ITokenVisitor
    {
        StringBuilder m_stringBuilder = new StringBuilder();

        #region ITokenVisitor Members

        public void OnTerminal(Terminal terminal)
        {
            m_stringBuilder.Append(terminal.Text);
        }

        public void OnReduction(Reduction reduction)
        {
            foreach (Token token in reduction.Tokens)
            {
                token.Visit(this);
            }
        }

        #endregion

        public override string ToString()
        {
            return m_stringBuilder.ToString();
        }
    }

    internal class TokenToJson : ITokenVisitor
    {
        StringBuilder m_stringBuilder = new StringBuilder();
        int m_nesting = 0;

        #region ITokenVisitor Members

        public void OnTerminal(Terminal terminal)
        {
            TokenStart(terminal);
            m_stringBuilder.Append(Escape(terminal.Text));
            TokenEnd();
        }

        public void OnReduction(Reduction reduction)
        {
            TokenStart(reduction);
            int nTokens = reduction.Tokens.Length;
            switch (nTokens)
            {
                case 0:
                    m_stringBuilder.Append("[]");
                    break;
                //case 1:
                //    m_stringBuilder.Append("[ ");
                //    reduction.Tokens[0].Visit(this);
                //    m_stringBuilder.Append(" ]");
                //    break;
                default:
                    m_stringBuilder.AppendLine("[");
                    ++m_nesting;
                    for (int i = 0; i < nTokens; ++i)
                    {
                        Token token = reduction.Tokens[i];
                        //m_stringBuilder.Append(' ', m_nesting * 4);
                        token.Visit(this);
                        if (i != nTokens - 1)
                        {
                            m_stringBuilder.AppendLine(",");
                        }
                    }
                    --m_nesting;
                    m_stringBuilder.AppendLine();
                    AppendIndent();
                    m_stringBuilder.Append("]");
                    break;
            }
            TokenEnd();
        }

        void AppendIndent()
        {
            m_stringBuilder.Append(' ', m_nesting * 4);
        }
        void TokenStart(Token token)
        {
            AppendIndent();
            m_stringBuilder.AppendLine("{");
            ++m_nesting;
            AppendIndent();
            m_stringBuilder.Append(Escape(token.SymbolName));
            m_stringBuilder.Append(": ");
        }

        void TokenEnd()
        {
            --m_nesting;
            m_stringBuilder.AppendLine();
            AppendIndent();
            m_stringBuilder.Append("}");
        }

        static string Escape(string text)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\"");
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\f': sb.Append(@"\f"); break;
                    case '\b': sb.Append(@"\b"); break;
                    case '\t': sb.Append(@"\t"); break;
                    case '\'': sb.Append(@"\'"); break;
                    case '"': sb.Append(@"\"""); break;
                    default:
                        sb.Append(c); break;
                }
            }
            sb.Append("\"");
            return sb.ToString();
        }

        #endregion

        public override string ToString()
        {
            return m_stringBuilder.ToString();
        }
    }

    internal class TokenTerminalAction : ITokenVisitor
    {
        Action<Terminal> m_action;

        public TokenTerminalAction(Action<Terminal> action)
        {
            m_action = action;
        }

        #region ITokenVisitor Members

        public void OnTerminal(Terminal terminal)
        {
            m_action(terminal);
        }

        public void OnReduction(Reduction reduction)
        {
            foreach (Token token in reduction.Tokens)
            {
                token.Visit(this);
            }
        }

        #endregion
    }

    public class TokenParser : ITokenVisitor
    {
        protected Predicate<Terminal> ExpectTerminal { get; set; }
        Predicate<Terminal> m_isNoise;
        Predicate<Reduction> m_isSynthetic;
        Dictionary<string, Action<Reduction>> m_reductionHandler = new Dictionary<string, Action<Reduction>>();

        protected TokenParser(Predicate<Terminal> isNoise, Predicate<Reduction> isSynthetic)
        {
            m_isNoise = isNoise;
            m_isSynthetic = isSynthetic;
        }

        protected static bool ExpectNoTerminals(Terminal terminal) { return false; }

        protected void AddHandler(string symbolName, Action<Reduction> action)
        {
            m_reductionHandler.Add(symbolName, action);
        }

        protected void OnCreate(Predicate<Terminal> expectTerminal, Reduction reduction)
        {
            ExpectTerminal = expectTerminal;
            VisitTokens(reduction);
        }

        #region ITokenVisitor Members

        public void OnTerminal(Terminal terminal)
        {
            if (m_isNoise(terminal))
                return;
            Assert(ExpectTerminal(terminal));
        }

        public void OnReduction(Reduction reduction)
        {
            if (m_isSynthetic(reduction))
                VisitTokens(reduction);
            else
            {
                Assert(m_reductionHandler.ContainsKey(reduction.SymbolName));
                m_reductionHandler[reduction.SymbolName](reduction);
            }
        }

        #endregion

        protected Action<Reduction> this[string symbolName]
        {
            get { return m_reductionHandler[symbolName]; }
        }

        void VisitTokens(Reduction reduction)
        {
            foreach (Token token in reduction.Tokens)
            {
                token.Visit(this);
            }
        }

        // Call this method from your HandleTokens implementation.
        protected void RecursiveList(Reduction reduction)
        {
            VisitTokens(reduction);
        }

        protected Terminal ExpectOneTerminal(Reduction reduction)
        {
            Terminal found = null;
            Predicate<Terminal> expectTerminal = terminal =>
            {
                if (found != null)
                    return false;
                found = terminal;
                return true;
            };
            TokenParser parser = new TokenParser(m_isNoise, m_isSynthetic);
            parser.OnCreate(expectTerminal, reduction);
            Assert(found != null);
            return found;
        }

        protected List<Terminal> ExpectTerminalList(Reduction reduction)
        {
            List<Terminal> found = new List<Terminal>();
            Predicate<Terminal> expectTerminal = terminal =>
            {
                found.Add(terminal);
                return true;
            };
            TokenParser parser = new TokenParser(m_isNoise, m_isSynthetic);
            parser.OnCreate(expectTerminal, reduction);
            Assert(found.Count > 0);
            return found;
        }

        protected static void Assert(bool b)
        {
            if (!b)
                throw new Exception();
        }
    }
}
