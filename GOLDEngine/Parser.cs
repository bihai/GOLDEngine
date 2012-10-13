using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;

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

    public class TokenStack : Stack<Token>
    {
        /// <summary>
        /// Add to end of list.
        /// </summary>
        public void Enqueue(Token item)
        {
            Stack<Token> copy = new Stack<Token>(base.Count);
            while (base.Count > 0)
                copy.Push(base.Pop());
            Push(item);
            while (copy.Count > 0)
                base.Push(copy.Pop());
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

        private SymbolList m_ExpectedSymbols = null;
        //NEW 12/2001
        private bool m_TrimReductions = false;

        //===== Private control variables
        private Lexer m_Lexer;
        private LALRStack m_LALRStack;
        GroupTerminals m_GroupTerminals;
        //Tokens to be analyzed - Hybred object!
        private TokenStack m_InputTokens = new TokenStack();
        //=== Line and column information of the last read terminal
        private Position m_CurrentPosition = new Position();
        private EGT m_loaded;

        private Converter<char, ushort> m_charToShort = c => ((ushort)c);

        public Parser()
        {
            Restart();
        }

        [Description("Opens a string for parsing.")]
        public bool Open(string Text)
        {
            return Open(new StringReader(Text));
        }

        [Description("Opens a text stream for parsing.")]
        public bool Open(TextReader Reader)
        {
            Restart();
            m_Lexer = new Lexer(m_loaded, Reader, m_charToShort);
            m_LALRStack = new LALRStack(m_loaded, m_TrimReductions);
            m_GroupTerminals = new GroupTerminals(m_loaded, m_Lexer);
            return true;
        }

        /// <summary>
        /// Use this to specify the algorithm used to convert 'char' (which are read from TextReader)
        /// to 'ushort' (which are defined in the CharacterSet tables);
        /// </summary>
        public Converter<char, ushort> CharConverter
        {
            set
            {
                m_charToShort = value;
                if (m_Lexer != null)
                    m_Lexer.m_charToShort = value;
            }
        }

        [Description("When the Parse() method returns a Reduce, this method will contain the current Reduction.")]
        public Reduction CurrentReduction
        {
            get { return m_LALRStack.CurrentReduction; }
        }

        [Description("Determines if reductions will be trimmed in cases where a production contains a single element.")]
        public bool TrimReductions
        {
            get { return m_TrimReductions; }
            set
            {
                m_TrimReductions = value;
                if (m_LALRStack != null)
                    m_LALRStack.m_TrimReductions = value;
            }
        }

        [Description("Returns information about the current grammar.")]
        public GrammarProperties Grammar()
        {
            return (m_loaded == null) ? null : m_loaded.Grammar;
        }

        [Description("Current line and column being read from the source.")]
        public Position CurrentPosition
        {
            get { return m_CurrentPosition; }
        }

        [Description("If the Parse() function returns TokenRead, this method will return that last read token.")]
        public Token CurrentToken
        {
            get { return m_InputTokens.Peek(); }
        }

        /// <summary>
        /// This gives the application read/write access to the parser's internal token stack.
        /// Handle with care.
        /// The application can manipulate the stack, for example:
        /// <list type="bullet">
        /// <item>TokenStack.Pop() to remove the next token from the input queue.</item>
        /// <item>TokenStack.Push() to push the token onto the top of the input queue. This token will be analyzed next.</item>
        /// <item>TokenStack.Enqueue() to append a token onto the end of the input queue.</item>
        /// </list>
        /// </summary>
        public TokenStack TokenStack
        {
            get { return m_InputTokens; }
        }

        public LALRStack LALRStack
        {
            get { return m_LALRStack; }
        }

        public Lexer Lexer
        {
            get { return m_Lexer; }
        }

        public bool ExpectsSymbol(short state, Symbol symbol)
        {
            return null != m_loaded.FindLRAction(state, symbol);
        }

        [Description("Library name and version.")]
        public string About
        {
            get { return "GOLD Parser Engine; Version " + kVersion; }
        }

        [Description("Loads parse tables from the specified filename. Only EGT (version 5.0) is supported.")]
        public void LoadTables(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read)))
                LoadTables(reader);
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
        public SymbolList SymbolTable
        {
            get { return (m_loaded == null) ? null : m_loaded.SymbolTable; }
        }

        [Description("Returns a list of Productions recognized by the grammar.")]
        public ProductionList ProductionTable
        {
            get { return (m_loaded == null) ? null : m_loaded.ProductionTable; }
        }

        [Description("If the Parse() method returns a SyntaxError, this method will contain a list of the symbols the grammar expected to see.")]
        public SymbolList ExpectedSymbols
        {
            get { return m_ExpectedSymbols; }
        }

        [Description("Restarts the parser. Loaded tables are retained.")]
        public void Restart()
        {
            //=== Lexer
            m_CurrentPosition = new Position();

            m_ExpectedSymbols = null;
            m_InputTokens.Clear();
            m_LALRStack = null;
            m_Lexer = null;

            //==== V4
            m_GroupTerminals = null;
        }

        [Description("Returns true if parse tables were loaded.")]
        public bool TablesLoaded
        {
            get { return (m_loaded != null); }
        }

        [Description("Performs a parse action on the input. This method is typically used in a loop until either grammar is accepted or an error occurs.")]
        public ParseMessage Parse()
        {
            if (!TablesLoaded)
            {
                return ParseMessage.NotLoadedError;
            }

            //===================================
            //Loop until breakable event
            //===================================
            for(;;)
            {
                if (m_InputTokens.Count == 0)
                {
                    Token Read = m_GroupTerminals.ProduceToken();
                    m_InputTokens.Push(Read);

                    return ParseMessage.TokenRead;
                }
                else
                {
                    Token Read = m_InputTokens.Peek();
                    //Update current position
                    if (Read.Position.HasValue)
                        m_CurrentPosition = Read.Position.Value;

                    //Runaway group
                    if (m_GroupTerminals.Count != 0)
                    {
                        return ParseMessage.GroupError;
                    }
                    else if (Read.SymbolType == SymbolType.Noise)
                    {
                        //Just discard. These were already reported to the user.
                        m_InputTokens.Pop();

                    }
                    else if (Read.SymbolType == SymbolType.Error)
                    {
                        //Finally, we can parse the token.
                        return ParseMessage.LexicalError;
                    }
                    else
                    {
                        LALRStack.ParseResult Action = m_LALRStack.ParseLALR(Read);
                        //SAME PROCEDURE AS v1

                        switch (Action)
                        {
                            case LALRStack.ParseResult.Accept:
                                return ParseMessage.Accept;

                            case LALRStack.ParseResult.InternalError:
                                return ParseMessage.InternalError;

                            case LALRStack.ParseResult.ReduceNormal:
                                return ParseMessage.Reduction;

                            case LALRStack.ParseResult.Shift:
                                //ParseToken() shifted the token on the front of the Token-Queue. 
                                //It now exists on the Token-Stack and must be eliminated from the queue.
                                m_InputTokens.Pop();
                                break;

                            case LALRStack.ParseResult.SyntaxError:
                                //=== Syntax Error! Fill Expected Tokens
                                m_ExpectedSymbols = m_LALRStack.GetExpectedSymbols();
                                return ParseMessage.SyntaxError;

                            default:
                                //Do nothing.
                                break;
                        }
                    }
                }
            }
        }
    }
}
