using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace GOLDEngine
{
    public class WrapGOLDEngine
    {
        Parser m_parser = new Parser();

        public WrapGOLDEngine()
        {
        }

        public Parser Parser { get { return m_parser; } }

        public void LoadTables(string grammarFilename)
        {
            using (Stream grammar = new FileStream(grammarFilename, FileMode.Open))
            {
                LoadTables(grammar);
            }
        }

        public void LoadTables(Stream grammar)
        {
            // Get the stream that holds the resource
            using (BinaryReader binaryReader = new BinaryReader(grammar))
            {
                m_parser.LoadTables(binaryReader);
            }
        }

        public Reduction ParseString(string contentString)
        {
            using (TextReader content = new StringReader(contentString))
            {
                return Parse(content);
            }
        }

        public Reduction ParseFile(string contentFilename)
        {
            using (TextReader content = new StreamReader(contentFilename))
            {
                return Parse(content);
            }
        }

        public Reduction Parse(TextReader content)
        {
            m_parser.Open(content);
            if (!doParsing())
                return null;
            return (Reduction)m_parser.CurrentReduction;
        }

        bool doParsing()
        {
            for (; ; )
            {
                bool? rc = parse();
                if (rc.HasValue)
                    return rc.Value;
            }
        }

        string m_FailMessage;
        public string FailMessage { get { return m_FailMessage; } }

        //Cannot recognize token
        protected virtual bool? OnLexicalError()
        {
            m_FailMessage = "Lexical Error:\n" +
                          "Position: " + m_parser.CurrentPosition.Line + ", " + m_parser.CurrentPosition.Column + "\n" +
                          "Read: " + m_parser.CurrentToken.ToString();
            return false;
        }

        //Cannot recognize token
        protected virtual bool? OnSyntaxError()
        {
            m_FailMessage = "Syntax Error:\n" +
                          "Position: " + m_parser.CurrentPosition.Line + ", " + m_parser.CurrentPosition.Column + "\n" +
                          "Read: " + m_parser.CurrentToken.ToString() + "\n" +
                          "Expecting: " + m_parser.ExpectedSymbols.Text();
            return false;
        }

        protected virtual bool? OnReduction()
        {
            //For this project, we will let the m_parser build a tree of Reduction objects
            //System.Diagnostics.Debug.WriteLine(m_parser.CurrentReduction);
            return null;
        }

        protected virtual bool? OnAccept()
        {
            return true;
        }

        protected virtual bool? OnTokenRead()
        {
            //You don't have to do anything here.
            //System.Diagnostics.Debug.WriteLine(m_parser.CurrentToken);
            return null;
        }

        protected virtual bool? OnError(ParseMessage response, string message)
        {
            m_FailMessage = message;
            return false;
        }

        bool? parse()
        {
            ParseMessage response = m_parser.Parse();

            switch (response)
            {
                case ParseMessage.LexicalError:
                    return OnLexicalError();

                case ParseMessage.SyntaxError:
                    return OnSyntaxError();

                case ParseMessage.Reduction:
                    return OnReduction();

                case ParseMessage.Accept:
                    return OnAccept();

                case ParseMessage.TokenRead:
                    return OnTokenRead();

                case ParseMessage.InternalError:
                    //INTERNAL ERROR! Something is horribly wrong.
                    return OnError(response, "Internal error");

                case ParseMessage.NotLoadedError:
                    //This error occurs if the CGT was not loaded.                   
                    return OnError(response, "Tables not loaded");

                case ParseMessage.GroupError:
                    //GROUP ERROR! Unexpected end of file
                    return OnError(response, "Runaway group");

                default:
                    return OnError(response, "Unexpected response");
            }
        }
    }
}
