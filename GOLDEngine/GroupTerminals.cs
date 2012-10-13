using System;
using System.Collections.Generic;
using System.Text;

using GOLDEngine.Tables;

namespace GOLDEngine
{
    class GroupTerminals
    {
        class GroupTerminal
        {
            private readonly Terminal Terminal;
            internal readonly Group Group;
            internal string Text;
            internal GroupTerminal(Terminal terminal, Group Group)
            {
                this.Terminal = terminal;
                this.Group = Group;
                this.Text = terminal.Text;
            }
            internal Terminal CreateTerminal()
            {
                //Change symbol to parent
                return new Terminal(Group.Container, Text, Terminal.Position.Value);
            }
        }
        private Stack<GroupTerminal> m_GroupStack = new Stack<GroupTerminal>();
        EGT m_loaded;
        Lexer m_Lexer;
        internal GroupTerminals(EGT loaded, Lexer lexer)
        {
            m_loaded = loaded;
            m_Lexer = lexer;
        }

        internal int Count
        {
            get { return m_GroupStack.Count; }
        }

        internal Token ProduceToken()
        {
            // ** VERSION 5.0 **
            //This function creates a token and also takes into account the current
            //lexing mode of the parser. In particular, it contains the group logic. 
            //
            //A stack is used to track the current "group". This replaces the comment
            //level counter. Also, text is appended to the token on the top of the 
            //stack. This allows the group text to returned in one chunk.

            bool NestGroup = false;

            for (; ; )
            {
                Terminal Read = m_Lexer.PeekNextTerminal();

                //The logic - to determine if a group should be nested - requires that the top of the stack 
                //and the symbol's linked group need to be looked at. Both of these can be unset. So, this section
                //sets a Boolean and avoids errors. We will use this boolean in the logic chain below. 
                if (Read.SymbolType == SymbolType.GroupStart)
                {
                    if (m_GroupStack.Count == 0)
                    {
                        NestGroup = true;
                    }
                    else
                    {
                        Group ReadGroup = m_loaded.GetGroup(Read.Symbol);
                        NestGroup = m_GroupStack.Peek().Group.CanNestGroup(ReadGroup);
                    }
                }
                else
                {
                    NestGroup = false;
                }

                //=================================
                // Logic chain
                //=================================

                if (NestGroup)
                {
                    m_Lexer.ConsumeBuffer(Read);
                    m_GroupStack.Push(new GroupTerminal(Read, m_loaded.GetGroup(Read.Symbol)));

                }
                else if (m_GroupStack.Count == 0)
                {
                    //The token is ready to be analyzed.             
                    m_Lexer.ConsumeBuffer(Read);
                    return Read;

                }
                else if ((object.ReferenceEquals(m_GroupStack.Peek().Group.End, Read.Symbol)))
                {
                    //End the current group
                    GroupTerminal popped = m_GroupStack.Pop();

                    //=== Ending logic
                    if (popped.Group.Ending == Group.EndingMode.Closed)
                    {
                        popped.Text += Read.Text;
                        //Append text
                        m_Lexer.ConsumeBuffer(Read);
                        //Consume token
                    }

                    //We are out of the group. Return pop'd token (which contains all the group text)
                    if (m_GroupStack.Count == 0)
                    {
                        return popped.CreateTerminal();
                    }
                    else
                    {
                        m_GroupStack.Peek().Text += popped.Text;
                        //Append group text to parent
                    }

                }
                else if (Read.SymbolType == SymbolType.End)
                {
                    //EOF always stops the loop. The caller function (Parse) can flag a runaway group error.
                    return Read;
                }
                else
                {
                    //We are in a group, Append to the Token on the top of the stack.
                    //Take into account the Token group mode  
                    GroupTerminal top = m_GroupStack.Peek();

                    if (top.Group.Advance == Group.AdvanceMode.Token)
                    {
                        top.Text += Read.Text;
                        // Append all text
                        m_Lexer.ConsumeBuffer(Read);
                    }
                    else
                    {
                        top.Text += Read.Text[0];
                        // Append one character
                        m_Lexer.ConsumeBuffer(1);
                    }
                }
            }
        }
    }
}
