using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualBasic;

using System.IO;

using GOLDEngine.Tables;

namespace GOLDEngine
{
    public class ParserException : System.Exception
    {
        public string Method;

        internal ParserException(string Message)
            : base(Message)
        {
            this.Method = "";
        }

        internal ParserException(string Message, Exception Inner, string Method)
            : base(Message, Inner)
        {
            this.Method = Method;
        }
    }

    //===== Parsing messages 
    public enum ParseMessage
    {
        TokenRead = 0,
        //A new token is read
        Reduction = 1,
        //A production is reduced
        Accept = 2,
        //Grammar complete
        NotLoadedError = 3,
        //The tables are not loaded
        LexicalError = 4,
        //Token not recognized
        SyntaxError = 5,
        //Token is not expected
        GroupError = 6,
        //Reached the end of the file inside a block
        InternalError = 7
        //Something is wrong, very wrong
    }


    public class GrammarProperties
    {
        private const int PropertyCount = 8;
        private enum PropertyIndex
        {
            Name = 0,
            Version = 1,
            Author = 2,
            About = 3,
            CharacterSet = 4,
            CharacterMapping = 5,
            GeneratedBy = 6,
            GeneratedDate = 7
        }


        private string[] m_Property = new string[PropertyCount + 1];
        internal GrammarProperties()
        {
            int n = 0;

            for (n = 0; n <= PropertyCount - 1; n++)
            {
                m_Property[n] = "";
            }
        }

        internal void SetValue(int Index, string Value)
        {
            if (Index >= 0 & Index < PropertyCount)
            {
                m_Property[(int)Index] = Value;
            }
        }

        public string Name
        {
            get { return m_Property[(int)PropertyIndex.Name]; }
        }

        public string Version
        {
            get { return m_Property[(int)PropertyIndex.Version]; }
        }

        public string Author
        {
            get { return m_Property[(int)PropertyIndex.Author]; }
        }

        public string About
        {
            get { return m_Property[(int)PropertyIndex.About]; }
        }

        public string CharacterSet
        {
            get { return m_Property[(int)PropertyIndex.CharacterSet]; }
        }

        public string CharacterMapping
        {
            get { return m_Property[(int)PropertyIndex.CharacterMapping]; }
        }

        public string GeneratedBy
        {
            get { return m_Property[(int)PropertyIndex.GeneratedBy]; }
        }

        public string GeneratedDate
        {
            get { return m_Property[(int)PropertyIndex.GeneratedDate]; }
        }
    }


    public class Parser
    {
        //===================================================================
        // Class Name:
        //    Parser
        //
        // Purpose:
        //    This is the main class in the GOLD Parser Engine and is used to perform
        //    all duties required to the parsing of a source text string. This class
        //    contains the LALR(1) State Machine code, the DFA State Machine code,
        //    character table (used by the DFA algorithm) and all other structures and
        //    methods needed to interact with the developer.
        //===================================================================


        private const string kVersion = "5.0";

        private string m_LookaheadBuffer;
        private short m_CurrentLALR;

        private Stack<Token> m_Stack = new Stack<Token>();
        //===== Used for Reductions & Errors
        //This ENTIRE list will available to the user
        private SymbolList m_ExpectedSymbols = null;
        //NEW 12/2001
        private bool m_TrimReductions = false;

        //===== Private control variables
        //Tokens to be analyzed - Hybred object!
        private TokenQueueStack m_InputTokens = new TokenQueueStack();

        private TextReader m_Source;
        //=== Line and column information. 
        //Internal - so user cannot mess with values
        private Position m_SysPosition = new Position();
        //Last read terminal
        private Position m_CurrentPosition = new Position();

        private EGT m_loaded;

        //===== The ParseLALR() function returns this value
        private enum ParseResult
        {
            Accept = 1,
            Shift = 2,
            ReduceNormal = 3,
            ReduceEliminated = 4,
            //Trim
            SyntaxError = 5,
            InternalError = 6
        }

        //===== Lexical Groups
        private Stack<Terminal> m_GroupStack = new Stack<Terminal>();

        public Parser()
        {
            Restart();
        }

        [Description("Opens a string for parsing.")]
        public bool Open(ref string Text)
        {
            return Open(new StringReader(Text));
        }

        [Description("Opens a text stream for parsing.")]
        public bool Open(TextReader Reader)
        {
            Restart();
            m_Source = Reader;

            //=== Create stack top item. Only needs state
            m_Stack.Push(Token.CreateFirstToken(m_loaded.InitialLRState));

            return true;
        }

        [Description("When the Parse() method returns a Reduce, this method will contain the current Reduction.")]
        public Reduction CurrentReduction
        {
            get { return m_Stack.Peek() as Reduction; }
        }

        [Description("Determines if reductions will be trimmed in cases where a production contains a single element.")]
        public bool TrimReductions
        {
            get { return m_TrimReductions; }
            set { m_TrimReductions = value; }
        }

        [Description("Returns information about the current grammar.")]
        public GrammarProperties Grammar()
        {
            return (m_loaded == null) ? null : m_loaded.Grammar;
        }

        [Description("Current line and column being read from the source.")]
        public Position CurrentPosition()
        {
            return m_CurrentPosition;
        }

        [Description("If the Parse() function returns TokenRead, this method will return that last read token.")]
        public Token CurrentToken()
        {
            return m_InputTokens.Peek();
        }

        [Description("Removes the next token from the input queue.")]
        public Token DiscardCurrentToken()
        {
            return m_InputTokens.Pop();
        }

        [Description("Added a token onto the end of the input queue.")]
        public void EnqueueInput(ref Token TheToken)
        {
            m_InputTokens.Enqueue(TheToken);
        }

        [Description("Pushes the token onto the top of the input queue. This token will be analyzed next.")]
        public void PushInput(ref Token TheToken)
        {
            m_InputTokens.Push(TheToken);
        }

        private string LookaheadBuffer(int Count)
        {
            //Return Count characters from the lookahead buffer. DO NOT CONSUME
            //This is used to create the text stored in a token. It is disgarded
            //separately. Because of the design of the DFA algorithm, count should
            //never exceed the buffer length. The If-Statement below is fault-tolerate
            //programming, but not necessary.

            if (Count > m_LookaheadBuffer.Length)
            {
                Count = m_LookaheadBuffer.Length;
            }

            return m_LookaheadBuffer.Substring(0, Count);
        }

        private string Lookahead(int CharIndex)
        {
            //Return single char at the index. This function will also increase 
            //buffer if the specified character is not present. It is used 
            //by the DFA algorithm.

            int ReadCount = 0;
            int n = 0;

            //Check if we must read characters from the Stream
            if (CharIndex > m_LookaheadBuffer.Length)
            {
                ReadCount = CharIndex - m_LookaheadBuffer.Length;
                for (n = 1; n <= ReadCount; n++)
                {
                    m_LookaheadBuffer += Strings.ChrW(m_Source.Read());
                }
            }

            //If the buffer is still smaller than the index, we have reached
            //the end of the text. In this case, return a null string - the DFA
            //code will understand.
            if (CharIndex <= m_LookaheadBuffer.Length)
            {
                return new string(m_LookaheadBuffer[CharIndex - 1], 1);
            }
            else
            {
                return "";
            }
        }

        [Description("Library name and version.")]
        public string About()
        {
            return "GOLD Parser Engine; Version " + kVersion;
        }

        [Description("Loads parse tables from the specified filename. Only EGT (version 5.0) is supported.")]
        public void LoadTables(string Path)
        {
            LoadTables(new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read)));
        }

        /// <summary>
        /// Loads parse tables from the specified BinaryReader.
        /// Only EGT (version 5.0) is supported.
        /// </summary>
        /// <param name="Reader">A BinaryReader instance wich wraps the EGT-format grammar file.</param>
        /// <exception cref="ParserException">If the passed-in grammar file has an unexpected format or cannot be read.</exception>
        /// <remarks>It is the caller's resposibility to call Reader.Close() after this method completes.</remarks>
        public void LoadTables(BinaryReader Reader)
        {
            m_loaded = new EGT(Reader);
        }

        [Description("Returns a list of Symbols recognized by the grammar.")]
        public SymbolList SymbolTable()
        {
            return (m_loaded == null) ? null : m_loaded.SymbolTable;
        }

        [Description("Returns a list of Productions recognized by the grammar.")]
        public ProductionList ProductionTable()
        {
            return (m_loaded == null) ? null : m_loaded.ProductionTable;
        }

        [Description("If the Parse() method returns a SyntaxError, this method will contain a list of the symbols the grammar expected to see.")]
        public SymbolList ExpectedSymbols()
        {
            return m_ExpectedSymbols;
        }

        private ParseResult ParseLALR(Token NextToken)
        {
            //This function analyzes a token and either:
            //  1. Makes a SINGLE reduction and pushes a complete Reduction object on the m_Stack
            //  2. Accepts the token and shifts
            //  3. Errors and places the expected symbol indexes in the Tokens list
            //The Token is assumed to be valid and WILL be checked
            //If an action is performed that requires controlt to be returned to the user, the function returns true.
            //The Message parameter is then set to the type of action.

            LRAction ParseAction = m_loaded.FindLRAction(m_CurrentLALR, NextToken.Parent);

            // Work - shift or reduce
            if ((ParseAction != null))
            {
                //'Debug.WriteLine("Action: " & ParseAction.Text)

                switch (ParseAction.Type)
                {
                    case LRActionType.Accept:
                        return ParseResult.Accept;

                    case LRActionType.Shift:
                        m_CurrentLALR = ParseAction.Value;
                        NextToken.State = m_CurrentLALR;
                        m_Stack.Push(NextToken);
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

                            Head = m_Stack.Pop();
                            Head.Parent = Prod.Head();

                            Result = ParseResult.ReduceEliminated;
                            //Build a Reduction
                        }
                        else
                        {
                            int nTokens = Prod.Handle().Count();
                            Token[] tokens = new Token[nTokens];
                            for (int i = Prod.Handle().Count() - 1; i >= 0; --i)
                            {
                                tokens[i] = m_Stack.Pop();
                            }
                            //Say that the reduction's Position is the Position of its first child.
                            Head = new Reduction(Prod, tokens);
                            Result = ParseResult.ReduceNormal;
                        }

                        //========== Goto
                        short Index = m_Stack.Peek().State;

                        LRAction found = m_loaded.FindLRAction(Index, Prod.Head());
                        if ((found != null) && (found.Type == LRActionType.Goto))
                        {
                            m_CurrentLALR = found.Value;

                            Head.State = m_CurrentLALR;
                            m_Stack.Push(Head);
                            return Result;
                        }
                        else
                        {
                            //========= If action not found here, then we have an Internal Table Error!!!!
                            return ParseResult.InternalError;
                        }

                    default:
                        return default(ParseResult);
                }

            }
            else
            {
                //=== Syntax Error! Fill Expected Tokens
                m_ExpectedSymbols = m_loaded.GetExpectedSymbols(m_CurrentLALR);
                return ParseResult.SyntaxError;
            }
        }

        [Description("Restarts the parser. Loaded tables are retained.")]
        public void Restart()
        {
            m_CurrentLALR = (m_loaded == null) ? (short)0 : m_loaded.InitialLRState;

            //=== Lexer
            m_SysPosition = new Position();
            m_CurrentPosition = new Position();

            m_ExpectedSymbols = null;
            m_InputTokens.Clear();
            m_Stack.Clear();
            m_LookaheadBuffer = "";

            //==== V4
            m_GroupStack.Clear();
        }

        [Description("Returns true if parse tables were loaded.")]
        public bool TablesLoaded()
        {
            return (m_loaded != null);
        }

        private Terminal LookaheadDFA()
        {
            //This function implements the DFA for th parser's lexer.
            //It generates a token which is used by the LALR state
            //machine.

            string Ch = null;
            int n = 0;
            bool Found = false;
            FAEdge Edge = default(FAEdge);
            int CurrentPosition = 0;
            int LastAcceptPosition = 0;
            //Token Result = new Token();

            short Target = 0;
            short CurrentDFA = 0;
            short LastAcceptState = 0;

            //===================================================
            //Match DFA token
            //===================================================

            CurrentDFA = m_loaded.InitialDFAState;
            CurrentPosition = 1;
            //Next byte in the input Stream
            LastAcceptState = -1;
            //We have not yet accepted a character string
            LastAcceptPosition = -1;

            Ch = Lookahead(1);
            //NO MORE DATA
            if (!(string.IsNullOrEmpty(Ch) | Strings.AscW(Ch) == 65535))
            {
                for (;;)
                {
                    // This code searches all the branches of the current DFA state
                    // for the next character in the input Stream. If found the
                    // target state is returned.

                    Ch = Lookahead(CurrentPosition);
                    //End reached, do not match
                    if (string.IsNullOrEmpty(Ch))
                    {
                        Found = false;
                    }
                    else
                    {
                        FAState faState = m_loaded.GetFAState(CurrentDFA);
                        n = 0;
                        Found = false;
                        while (n < faState.Edges.Count & !Found)
                        {
                            Edge = faState.Edges[n];

                            //==== Look for character in the Character Set Table
                            if (Edge.Characters.Contains(Strings.AscW(Ch)))
                            {
                                Found = true;
                                Target = Edge.Target;
                                //.TableIndex
                            }
                            n += 1;
                        }
                    }

                    // This block-if statement checks whether an edge was found from the current state. If so, the state and current
                    // position advance. Otherwise it is time to exit the main loop and report the token found (if there was one). 
                    // If the LastAcceptState is -1, then we never found a match and the Error Token is created. Otherwise, a new 
                    // token is created using the Symbol in the Accept State and all the characters that comprise it.

                    if (Found)
                    {
                        // This code checks whether the target state accepts a token.
                        // If so, it sets the appropiate variables so when the
                        // algorithm in done, it can return the proper token and
                        // number of characters.

                        //NOT is very important!
                        if ((m_loaded.GetFAState(Target).Accept != null))
                        {
                            LastAcceptState = Target;
                            LastAcceptPosition = CurrentPosition;
                        }

                        CurrentDFA = Target;
                        CurrentPosition += 1;

                        //No edge found
                    }
                    else
                    {
                        // Lexer cannot recognize symbol
                        if (LastAcceptState == -1)
                        {
                            return new Terminal(m_loaded.GetFirstSymbolOfType(SymbolType.Error),
                                LookaheadBuffer(1), m_SysPosition);
                            // Create Token, read characters
                        }
                        else
                        {
                            return new Terminal(m_loaded.GetFAState(LastAcceptState).Accept,
                                LookaheadBuffer(LastAcceptPosition), m_SysPosition);
                            //Data contains the total number of accept characters
                        }
                    }
                }
            }
            else
            {
                // End of file reached, create End Token
                return new Terminal(m_loaded.GetFirstSymbolOfType(SymbolType.End), "", m_SysPosition);
            }
        }

        private void ConsumeBuffer(int CharCount)
        {
            //Consume/Remove the characters from the front of the buffer. 

            int n = 0;

            if (CharCount <= m_LookaheadBuffer.Length)
            {
                // Count Carriage Returns and increment the internal column and line
                // numbers. This is done for the Developer and is not necessary for the
                // DFA algorithm.
                for (n = 0; n <= CharCount - 1; n++)
                {
                    switch (m_LookaheadBuffer[n])
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

                m_LookaheadBuffer = m_LookaheadBuffer.Remove(0, CharCount);
            }
        }

        private Token ProduceToken()
        {
            // ** VERSION 5.0 **
            //This function creates a token and also takes into account the current
            //lexing mode of the parser. In particular, it contains the group logic. 
            //
            //A stack is used to track the current "group". This replaces the comment
            //level counter. Also, text is appended to the token on the top of the 
            //stack. This allows the group text to returned in one chunk.

            bool NestGroup = false;

            for (;;)
            {
                Terminal Read = LookaheadDFA();

                //The logic - to determine if a group should be nested - requires that the top of the stack 
                //and the symbol's linked group need to be looked at. Both of these can be unset. So, this section
                //sets a Boolean and avoids errors. We will use this boolean in the logic chain below. 
                if (Read.Type() == SymbolType.GroupStart)
                {
                    if (m_GroupStack.Count == 0)
                    {
                        NestGroup = true;
                    }
                    else
                    {
                        NestGroup = m_GroupStack.Peek().Group().Nesting.Contains(Read.Group().TableIndex);
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
                    ConsumeBuffer(Read.TextLength);
                    m_GroupStack.Push(Read);

                }
                else if (m_GroupStack.Count == 0)
                {
                    //The token is ready to be analyzed.             
                    ConsumeBuffer(Read.TextLength);
                    return Read;

                }
                else if ((object.ReferenceEquals(m_GroupStack.Peek().Group().End, Read.Parent)))
                {
                    //End the current group
                    Terminal Pop = m_GroupStack.Pop();

                    //=== Ending logic
                    if (Pop.Group().Ending == Group.EndingMode.Closed)
                    {
                        Pop.TextAppend(Read);
                        //Append text
                        ConsumeBuffer(Read.TextLength);
                        //Consume token
                    }

                    //We are out of the group. Return pop'd token (which contains all the group text)
                    if (m_GroupStack.Count == 0)
                    {
                        Pop.Parent = Pop.Group().Container;
                        //Change symbol to parent
                        return Pop;
                    }
                    else
                    {
                        m_GroupStack.Peek().TextAppend(Pop);
                        //Append group text to parent
                    }

                }
                else if (Read.Type() == SymbolType.End)
                {
                    //EOF always stops the loop. The caller function (Parse) can flag a runaway group error.
                    return Read;
                }
                else
                {
                    //We are in a group, Append to the Token on the top of the stack.
                    //Take into account the Token group mode  
                    Terminal Top = m_GroupStack.Peek();

                    if (Top.Group().Advance == Group.AdvanceMode.Token)
                    {
                        Top.TextAppend(Read);
                        // Append all text
                        ConsumeBuffer(Read.TextLength);
                    }
                    else
                    {
                        Top.TextAppendFirstChar(Read);
                        // Append one character
                        ConsumeBuffer(1);
                    }
                }
            }
        }

        [Description("Performs a parse action on the input. This method is typically used in a loop until either grammar is accepted or an error occurs.")]
        public ParseMessage Parse()
        {
            ParseMessage Message = default(ParseMessage);
            bool Done = false;
            Token Read = default(Token);
            ParseResult Action = default(ParseResult);

            if (!TablesLoaded())
            {
                return ParseMessage.NotLoadedError;
            }

            //===================================
            //Loop until breakable event
            //===================================
            Done = false;
            while (!Done)
            {
                if (m_InputTokens.Count == 0)
                {
                    Read = ProduceToken();
                    m_InputTokens.Push(Read);

                    Message = ParseMessage.TokenRead;
                    Done = true;
                }
                else
                {
                    Read = m_InputTokens.Peek();
                    if (Read.Position.HasValue)
                        m_CurrentPosition = Read.Position.Value;
                    //Update current position

                    //Runaway group
                    if (m_GroupStack.Count != 0)
                    {
                        Message = ParseMessage.GroupError;
                        Done = true;
                    }
                    else if (Read.Type() == SymbolType.Noise)
                    {
                        //Just discard. These were already reported to the user.
                        m_InputTokens.Pop();

                    }
                    else if (Read.Type() == SymbolType.Error)
                    {
                        Message = ParseMessage.LexicalError;
                        Done = true;

                        //Finally, we can parse the token.
                    }
                    else
                    {
                        Action = ParseLALR(Read);
                        //SAME PROCEDURE AS v1

                        switch (Action)
                        {
                            case ParseResult.Accept:
                                Message = ParseMessage.Accept;
                                Done = true;

                                break;
                            case ParseResult.InternalError:
                                Message = ParseMessage.InternalError;
                                Done = true;

                                break;
                            case ParseResult.ReduceNormal:
                                Message = ParseMessage.Reduction;
                                Done = true;

                                break;
                            case ParseResult.Shift:
                                //ParseToken() shifted the token on the front of the Token-Queue. 
                                //It now exists on the Token-Stack and must be eliminated from the queue.
                                m_InputTokens.Pop();

                                break;
                            case ParseResult.SyntaxError:
                                Message = ParseMessage.SyntaxError;
                                Done = true;

                                break;
                            default:
                                break;
                            //Do nothing.
                        }
                    }
                }
            }

            return Message;
        }
    }
}
