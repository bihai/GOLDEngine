using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

using GOLDEngine;

namespace TestGOLDEngine
{
    /// <summary>
    /// Implements a typical usage of the engine.
    /// </summary>
    class WrapGOLDEngine
    {
        Parser m_parser;

        internal WrapGOLDEngine(Stream grammar)
        {
            m_parser = new Parser();
            // Get the stream that holds the resource
            using (BinaryReader binaryReader = new BinaryReader(grammar))
            {
                m_parser.LoadTables(binaryReader);
            }
        }

        internal Reduction Parse(TextReader content)
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

        string FailMessage;

        bool? parse()
        {
            ParseMessage response = m_parser.Parse();

            switch (response)
            {
                case ParseMessage.LexicalError:
                    //Cannot recognize token
                    FailMessage = "Lexical Error:\n" +
                                  "Position: " + m_parser.CurrentPosition.Line + ", " + m_parser.CurrentPosition.Column + "\n" +
                                  "Read: " + m_parser.CurrentToken.ToString();
                    return false;

                case ParseMessage.SyntaxError:
                    //Expecting a different token
                    FailMessage = "Syntax Error:\n" +
                                  "Position: " + m_parser.CurrentPosition.Line + ", " + m_parser.CurrentPosition.Column + "\n" +
                                  "Read: " + m_parser.CurrentToken.ToString() + "\n" +
                                  "Expecting: " + m_parser.ExpectedSymbols.Text();
                    return false;

                case ParseMessage.Reduction:
                    //For this project, we will let the m_parser build a tree of Reduction objects
                    return null;

                case ParseMessage.Accept:
                    //Accepted!
                    return true;

                case ParseMessage.TokenRead:
                    //You don't have to do anything here.
                    return null;

                case ParseMessage.InternalError:
                    //INTERNAL ERROR! Something is horribly wrong.
                    FailMessage = "Internal error";
                    return false;

                case ParseMessage.NotLoadedError:
                    //This error occurs if the CGT was not loaded.                   
                    FailMessage = "Tables not loaded";
                    return false;

                case ParseMessage.GroupError:
                    //GROUP ERROR! Unexpected end of file
                    FailMessage = "Runaway group";
                    return false;

                default:
                    FailMessage = "Unexpected response";
                    return false;
            }
        }
    }
}
