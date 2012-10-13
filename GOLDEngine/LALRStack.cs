using System;
using System.Collections.Generic;
using System.Text;

using GOLDEngine.Tables;

namespace GOLDEngine
{
    public class LALRStack
    {
        //===== The ParseLALR() function returns this value
        public enum ParseResult
        {
            Accept = 1,
            Shift = 2,
            ReduceNormal = 3,
            ReduceEliminated = 4,
            //Trim
            SyntaxError = 5,
            InternalError = 6
        }
        public struct TokenState
        {
            public readonly Token Token;
            public readonly short GotoState;
            internal readonly short PrevState;
            internal TokenState(Token token, short gotoState, short prevState)
            {
                Token = token;
                GotoState = gotoState;
                PrevState = prevState;
            }
            internal bool IsExtra { get { return (PrevState == -1) && (Token != null); } }
        }

        EGT m_loaded;
        internal bool m_TrimReductions;
        short m_CurrentLALR;
        Stack<TokenState> m_Stack = new Stack<TokenState>();

        internal LALRStack(EGT loaded, bool trimReductions)
        {
            m_loaded = loaded;
            m_TrimReductions = trimReductions;
            m_CurrentLALR = m_loaded.InitialLRState;
            //=== Create stack top item. Only needs state
            m_Stack.Push(new TokenState(null, m_loaded.InitialLRState, -1));
        }

        public TokenState Peek() { return m_Stack.Peek(); }

        public TokenState Pop()
        {
            TokenState old = m_Stack.Pop();
            m_CurrentLALR = old.PrevState;
            return old;
        }

        public void PushExtra(Token token)
        {
            m_Stack.Push(new TokenState(token, Peek().GotoState, -1));
        }

        public int Count { get { return m_Stack.Count; } }

        internal Reduction CurrentReduction { get { return m_Stack.Peek().Token as Reduction; } }

        void Push(Token token, short gotoState)
        {
            m_Stack.Push(new TokenState(token, gotoState, m_CurrentLALR));
            m_CurrentLALR = gotoState;
        }

        internal ParseResult ParseLALR(Token NextToken)
        {
            //This function analyzes a token and either:
            //  1. Makes a SINGLE reduction and pushes a complete Reduction object on the m_Stack
            //  2. Accepts the token and shifts
            //  3. Errors and places the expected symbol indexes in the Tokens list
            //The Token is assumed to be valid and WILL be checked
            //If an action is performed that requires controlt to be returned to the user, the function returns true.
            //The Message parameter is then set to the type of action.

            LRAction ParseAction = m_loaded.FindLRAction(m_CurrentLALR, NextToken.Symbol);

            // Work - shift or reduce
            if ((ParseAction != null))
            {
                //'Debug.WriteLine("Action: " & ParseAction.Text)

                switch (ParseAction.Type)
                {
                    case LRActionType.Accept:
                        return ParseResult.Accept;

                    case LRActionType.Shift:
                        Push(NextToken, ParseAction.Value);
                        return ParseResult.Shift;

                    case LRActionType.Reduce:
                        //Produce a reduction - remove as many tokens as members in the rule & push a nonterminal token
                        Production Prod = m_loaded.GetProduction(ParseAction);

                        ParseResult Result;
                        Token Head;
                        //======== Create Reduction
                        if (m_TrimReductions & Prod.ContainsOneNonTerminal())
                        {
                            //The current rule only consists of a single nonterminal and can be trimmed from the
                            //parse tree. Usually we create a new Reduction, assign it to the Data property
                            //of Head and push it on the m_Stack. However, in this case, the Data property of the
                            //Head will be assigned the Data property of the reduced token (i.e. the only one
                            //on the m_Stack).
                            //In this case, to save code, the value popped of the m_Stack is changed into the head.

                            //Build a Reduction
                            Head = m_Stack.Pop().Token;
                            Head.TrimReduction(Prod.Head());

                            Result = ParseResult.ReduceEliminated;
                        }
                        else
                        {
                            int nTokens = Prod.Handle().Count;
                            List<Token> tokens = new List<Token>(nTokens);
                            for (int i = Prod.Handle().Count - 1; i >= 0; --i)
                            {
                                TokenState popped = Pop();
                                while (popped.IsExtra)
                                {
                                    tokens.Insert(0, popped.Token);
                                    popped = Pop();
                                }
                                tokens.Insert(0, popped.Token);
                            }
                            //Say that the reduction's Position is the Position of its first child.
                            Head = new Reduction(Prod, tokens.ToArray());
                            Result = ParseResult.ReduceNormal;
                        }

                        //========== Goto
                        short GotoState = m_Stack.Peek().GotoState;

                        LRAction found = m_loaded.FindLRAction(GotoState, Prod.Head());
                        if ((found != null) && (found.Type == LRActionType.Goto))
                        {
                            Push(Head, found.Value);
                            return Result;
                        }
                        else
                        {
                            //========= If action not found here, then we have an Internal Table Error!!!!
                            return ParseResult.InternalError;
                        }

                    default:
                        return ParseResult.InternalError;
                }

            }
            else
            {
                return ParseResult.SyntaxError;
            }
        }

        internal SymbolList GetExpectedSymbols()
        {
            return m_loaded.GetExpectedSymbols(m_CurrentLALR);
        }
    }
}
