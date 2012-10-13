using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using GOLDEngine.Tables;

namespace GOLDEngine
{
    public class Lexer
    {
        EGT m_loaded;
        TextReader m_source;
        StringBuilder m_buffer = new StringBuilder();
        Position m_SysPosition = new Position();
        internal Converter<char, ushort> m_charToShort = c => ((ushort)c);

        internal Lexer(EGT loaded, TextReader source, Converter<char, ushort> charToShort)
        {
            m_loaded = loaded;
            m_source = source;
            m_charToShort = charToShort;
        }

        public void PushFront(string text)
        {
            m_buffer.Insert(0, text);
        }

        internal Converter<char, ushort> charToShort
        {
            get { return m_charToShort; }
            set { m_charToShort = value; }
        }

        public Terminal PeekNextTerminal()
        {
            //This function implements the DFA for th parser's lexer.
            //It generates a token which is used by the LALR state
            //machine.

            //===================================================
            //Match DFA token
            //===================================================

            short CurrentDFA = m_loaded.InitialDFAState;
            //Next byte in the input Stream
            short LastAcceptState = -1;
            //We have not yet accepted a character string
            int LastAcceptPosition = -1;

            char? Ch = Lookahead(0);
            //NO MORE DATA
            if (!Ch.HasValue)
            {
                // End of file reached, create End Token
                return CreateTerminal(m_loaded.GetFirstSymbolOfType(SymbolType.End), 0);
            }

            for (int CurrentPosition = 0; ; ++CurrentPosition)
            {
                // This code searches all the branches of the current DFA state
                // for the next character in the input Stream. If found the
                // target state is returned.

                Ch = Lookahead(CurrentPosition);
                short? Found = null;
                //End reached, do not match
                if (Ch.HasValue)
                {
                    ushort charCode = m_charToShort(Ch.Value);
                    FAState faState = m_loaded.GetFAState(CurrentDFA);
                    foreach (FAEdge Edge in faState.Edges)
                    {
                        //==== Look for character in the Character Set Table
                        if (Edge.Characters.Contains(charCode))
                        {
                            Found = Edge.Target;
                            break;
                        }
                    }
                }

                // This block-if statement checks whether an edge was found from the current state. If so, the state and current
                // position advance. Otherwise it is time to exit the main loop and report the token found (if there was one). 
                // If the LastAcceptState is -1, then we never found a match and the Error Token is created. Otherwise, a new 
                // token is created using the Symbol in the Accept State and all the characters that comprise it.

                if (Found.HasValue)
                {
                    // This code checks whether the target state accepts a token.
                    // If so, it sets the appropiate variables so when the
                    // algorithm in done, it can return the proper token and
                    // number of characters.
                    short Target = Found.Value;

                    //NOT is very important!
                    if ((m_loaded.GetFAState(Target).Accept != null))
                    {
                        LastAcceptState = Target;
                        LastAcceptPosition = CurrentPosition;
                    }

                    CurrentDFA = Target;

                    //No edge found
                }
                else
                {
                    // Lexer cannot recognize symbol
                    if (LastAcceptState == -1)
                    {
                        return CreateTerminal(m_loaded.GetFirstSymbolOfType(SymbolType.Error), 1);
                    }
                    else
                    {
                        //Created Terminal contains the total number of accept characters
                        return CreateTerminal(m_loaded.GetFAState(LastAcceptState).Accept, LastAcceptPosition + 1);
                    }
                }
            }
        }
        
        Terminal CreateTerminal(Symbol symbol, int Count)
        {
            //Return Count characters from the lookahead buffer. DO NOT CONSUME
            //This is used to create the text stored in a token. It is disgarded
            //separately. Because of the design of the DFA algorithm, count should
            //never exceed the buffer length. The If-Statement below is fault-tolerate
            //programming, but not necessary.

            if (Count > m_buffer.Length)
            {
                Count = m_buffer.Length;
            }

            string text = m_buffer.ToString(0, Count);
            return new Terminal(symbol, text, m_SysPosition);
        }

        public void ConsumeBuffer(Terminal terminal)
        {
            ConsumeBuffer(terminal.Text.Length);
        }

        public void ConsumeBuffer(int CharCount)
        {
            //Consume/Remove the characters from the front of the buffer. 

            if (CharCount <= m_buffer.Length)
            {
                // Count Carriage Returns and increment the internal column and line
                // numbers. This is done for the Developer and is not necessary for the
                // DFA algorithm.
                for (int n = 0; n <= CharCount - 1; n++)
                {
                    switch (m_buffer[n])
                    {
                        case '\n':
                            m_SysPosition = m_SysPosition.NextLine;
                            break;
                        case '\r':
                            break;
                        //Ignore, LF is used to inc line to be UNIX friendly
                        default:
                            m_SysPosition = m_SysPosition.NextColumn;
                            break;
                    }
                }

                m_buffer.Remove(0, CharCount);
            }
        }

        public char? Lookahead(int CharIndex)
        {
            //Return single char at the index. This function will also increase 
            //buffer if the specified character is not present. It is used 
            //by the DFA algorithm.

            //Check if we must read characters from the Stream
            if (CharIndex >= m_buffer.Length)
            {
                int ReadCount = CharIndex + 1 - m_buffer.Length;
                for (int i = 0; i < ReadCount; ++i)
                {
                    int next = m_source.Read();
                    if (next == -1)
                        break;
                    // Assume that StreamReader was opened with appropriate Encoder
                    // so that what we read has already been converted to Unicode?
                    char c = (char)next;
                    m_buffer.Append(c);
                }
            }

            //If the buffer is still smaller than the index, we have reached
            //the end of the text. In this case, return a null string - the DFA
            //code will understand.
            if (CharIndex < m_buffer.Length)
            {
                return m_buffer[CharIndex];
            }
            else
            {
                return null;
            }
        }
    }
}
