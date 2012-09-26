using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GOLDEngine.Tables
{
    class EGT
    {
        //===== Grammar Attributes
        private GrammarProperties m_Grammar = new GrammarProperties();
        //===== Symbols recognized by the system
        private SymbolList m_SymbolTable;
        //===== DFA
        private FAStateList m_DFA;
        private CharacterSetList m_CharSetTable;
        //===== Productions
        private ProductionList m_ProductionTable;
        //===== LALR
        private LRStateList m_LRStates;
        //===== Lexical Groups
        private GroupList m_GroupTable;
        private Dictionary<Symbol, Group> m_GroupStart = new Dictionary<Symbol, Group>();

        internal short InitialLRState { get { return m_LRStates.InitialState; } }
        internal short InitialDFAState { get { return m_DFA.InitialState; } }
        internal GrammarProperties Grammar { get { return m_Grammar; } }
        internal ProductionList ProductionTable { get { return m_ProductionTable; } }
        internal SymbolList SymbolTable { get { return m_SymbolTable; } }
        internal Group GetGroup(Symbol start) { return m_GroupStart[start]; }

        internal LRAction FindLRAction(short CurrentLALR, Symbol symbolToFind)
        {
            LRState state = m_LRStates[CurrentLALR];
            return state.Find(action => action.Symbol.Equals(symbolToFind));
        }
        internal Production GetProduction(LRAction ParseAction)
        {
            return m_ProductionTable[ParseAction.Value];
        }
        internal SymbolList GetExpectedSymbols(short CurrentLALR)
        {
            LRState state = m_LRStates[CurrentLALR];
            List<Symbol> expectedSymbols = new List<Symbol>();
            state.ForEach(action =>
            {
                switch (action.Symbol.Type)
                {
                    case SymbolType.Content:
                    case SymbolType.End:
                    case SymbolType.GroupStart:
                    case SymbolType.GroupEnd:
                        expectedSymbols.Add(action.Symbol);
                        break;
                }
            });
            return new SymbolList(expectedSymbols);
        }
        internal FAState GetFAState(short CurrentDFA)
        {
            return m_DFA[CurrentDFA];
        }
        internal Symbol GetFirstSymbolOfType(SymbolType symbolTypeToFind)
        {
            return m_SymbolTable.GetFirstOfType(symbolTypeToFind);
        }

        internal EGT(BinaryReader Reader)
        {
            EGTReader EGT = new EGTReader(Reader);
            EGTRecord RecType = default(EGTRecord);

            try
            {
                while (!EGT.EndOfFile())
                {
                    EGT.GetNextRecord();

                    RecType = (EGTRecord)EGT.RetrieveByte();

                    switch (RecType)
                    {
                        case EGTRecord.Property:
                            {
                                //Index, Name, Value
                                int Index = 0;
                                string Name = null;

                                Index = EGT.RetrieveInt16();
                                Name = EGT.RetrieveString();
                                //Just discard
                                m_Grammar.SetValue(Index, EGT.RetrieveString());
                            }
                            break;
                        case EGTRecord.TableCounts:
                            //Symbol, CharacterSet, Rule, DFA, LALR
                            m_SymbolTable = new SymbolList(EGT.RetrieveInt16());
                            m_CharSetTable = new CharacterSetList(EGT.RetrieveInt16());
                            m_ProductionTable = new ProductionList(EGT.RetrieveInt16());
                            m_DFA = new FAStateList(EGT.RetrieveInt16());
                            m_LRStates = new LRStateList(EGT.RetrieveInt16());
                            m_GroupTable = new GroupList(EGT.RetrieveInt16());

                            break;
                        case EGTRecord.InitialStates:
                            //DFA, LALR
                            m_DFA.InitialState = EGT.RetrieveInt16();
                            m_LRStates.InitialState = EGT.RetrieveInt16();

                            break;
                        case EGTRecord.Symbol:
                            {
                                //#, Name, Kind
                                short Index = 0;
                                string Name = null;
                                SymbolType Type = default(SymbolType);

                                Index = EGT.RetrieveInt16();
                                Name = EGT.RetrieveString();
                                Type = (SymbolType)EGT.RetrieveInt16();

                                m_SymbolTable[Index] = new Symbol(Name, Type, Index);
                            }
                            break;
                        case EGTRecord.Group:
                            //#, Name, Container#, Start#, End#, Tokenized, Open Ended, Reserved, Count, (Nested Group #...) 
                            {
                                Group G = new Group();

                                G.TableIndex = EGT.RetrieveInt16();
                                //# 

                                G.Name = EGT.RetrieveString();
                                G.Container = m_SymbolTable[EGT.RetrieveInt16()];
                                G.Start = m_SymbolTable[EGT.RetrieveInt16()];
                                G.End = m_SymbolTable[EGT.RetrieveInt16()];

                                G.Advance = (Group.AdvanceMode)EGT.RetrieveInt16();
                                G.Ending = (Group.EndingMode)EGT.RetrieveInt16();
                                EGT.RetrieveEntry();
                                //Reserved

                                int Count = EGT.RetrieveInt16();
                                for (int n = 1; n <= Count; n++)
                                {
                                    G.Nesting.Add(EGT.RetrieveInt16());
                                }

                                //=== Link back
                                m_GroupStart.Add(G.Start, G);
                                m_GroupTable[G.TableIndex] = G;
                            }
                            break;
                        case EGTRecord.CharRanges:
                            //#, Total Sets, RESERVED, (Start#, End#  ...)
                            {
                                int Index = 0;
                                int Total = 0;

                                Index = EGT.RetrieveInt16();
                                EGT.RetrieveInt16();
                                //Codepage
                                Total = EGT.RetrieveInt16();
                                EGT.RetrieveEntry();
                                //Reserved

                                m_CharSetTable[Index] = new CharacterSet();
                                while (!(EGT.RecordComplete()))
                                {
                                    m_CharSetTable[Index].Add(new CharacterRange(EGT.RetrieveUInt16(), EGT.RetrieveUInt16()));
                                }
                            }
                            break;
                        case EGTRecord.Production:
                            //#, ID#, Reserved, (Symbol#,  ...)
                            {
                                short Index = 0;
                                int HeadIndex = 0;
                                int SymIndex = 0;

                                Index = EGT.RetrieveInt16();
                                HeadIndex = EGT.RetrieveInt16();
                                EGT.RetrieveEntry();
                                //Reserved

                                List<Symbol> symbols = new List<Symbol>();
                                while (!(EGT.RecordComplete()))
                                {
                                    SymIndex = EGT.RetrieveInt16();
                                    //m_ProductionTable[Index].Handle().Add(m_SymbolTable[SymIndex]);
                                    symbols.Add(m_SymbolTable[SymIndex]);
                                }
                                SymbolList symbolList = new SymbolList(symbols);
                                m_ProductionTable[Index] = new Production(m_SymbolTable[HeadIndex], Index, symbolList);
                            }
                            break;
                        case EGTRecord.DFAState:
                            //#, Accept?, Accept#, Reserved (CharSet#, Target#, Reserved)...
                            {
                                int Index = 0;
                                bool Accept = false;
                                int AcceptIndex = 0;
                                int SetIndex = 0;
                                short Target = 0;

                                Index = EGT.RetrieveInt16();
                                Accept = EGT.RetrieveBoolean();
                                AcceptIndex = EGT.RetrieveInt16();
                                EGT.RetrieveEntry();
                                //Reserved

                                if (Accept)
                                {
                                    m_DFA[Index] = new FAState(m_SymbolTable[AcceptIndex]);
                                }
                                else
                                {
                                    m_DFA[Index] = new FAState();
                                }

                                //(Edge chars, Target#, Reserved)...
                                while (!(EGT.RecordComplete()))
                                {
                                    SetIndex = EGT.RetrieveInt16();
                                    //Char table index
                                    Target = EGT.RetrieveInt16();
                                    //Target
                                    EGT.RetrieveEntry();
                                    //Reserved

                                    m_DFA[Index].Edges.Add(new FAEdge(m_CharSetTable[SetIndex], Target));
                                }
                            }
                            break;
                        case EGTRecord.LRState:
                            //#, Reserved (Symbol#, Action, Target#, Reserved)...
                            {
                                int Index = 0;
                                int SymIndex = 0;
                                LRActionType Action = 0;
                                short Target = 0;

                                Index = EGT.RetrieveInt16();
                                EGT.RetrieveEntry();
                                //Reserved

                                m_LRStates[Index] = new LRState();

                                //(Symbol#, Action, Target#, Reserved)...
                                while (!EGT.RecordComplete())
                                {
                                    SymIndex = EGT.RetrieveInt16();
                                    Action = (LRActionType)EGT.RetrieveInt16();
                                    Target = EGT.RetrieveInt16();
                                    EGT.RetrieveEntry();
                                    //Reserved

                                    m_LRStates[Index].Add(new LRAction(m_SymbolTable[SymIndex], Action, Target));
                                }
                            }
                            break;
                        default:
                            //RecordIDComment
                            throw new ParserException("File Error. A record of type '" + (char)RecType + "' was read. This is not a valid code.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ParserException(ex.Message, ex, "LoadTables");
            }
        }

        internal enum EGTRecord : byte
        {
            InitialStates = 73,
            //I
            Symbol = 83,
            //S
            Production = 82,
            //R   R for Rule (related productions)
            DFAState = 68,
            //D
            LRState = 76,
            //L
            Property = 112,
            //p
            CharRanges = 99,
            //c 
            Group = 103,
            //g
            TableCounts = 116
            //t   Table Counts
        }

        internal class EGTReader
        {
            public enum EntryType : byte
            {
                Empty = 69,
                //E
                UInt16 = 73,
                //I - Unsigned, 2 byte
                String = 83,
                //S - Unicode format
                Boolean = 66,
                //B - 1 Byte, Value is 0 or 1
                Byte = 98,
                //b
                Error = 0
            }


            public class IOException : System.Exception
            {
                public IOException(string Message, System.Exception Inner)
                    : base(Message, Inner)
                {
                }

                public IOException(EntryType Type, BinaryReader Reader)
                    : base("Type mismatch in file. Read '" + (char)Type + "' at " + Reader.BaseStream.Position)
                {
                }
            }

            public class Entry
            {
                public EntryType Type;

                public object Value;
                public Entry()
                {
                    Type = EntryType.Empty;
                    Value = "";
                }

                public Entry(EntryType Type, object Value)
                {
                    this.Type = Type;
                    this.Value = Value;
                }
            }

            //M
            private const byte kRecordContentMulti = 77;
            private string m_FileHeader;

            private BinaryReader m_Reader;
            //Current record 
            private ushort m_EntryCount;

            private ushort m_EntriesRead;

            internal EGTReader(BinaryReader Reader)
            {
                m_Reader = Reader;

                m_EntryCount = 0;
                m_EntriesRead = 0;
                m_FileHeader = RawReadCString();
            }

            public bool RecordComplete()
            {
                return m_EntriesRead >= m_EntryCount;
            }

            public ushort EntryCount()
            {
                return m_EntryCount;
            }

            public bool EndOfFile()
            {
                return m_Reader.BaseStream.Position == m_Reader.BaseStream.Length;
            }

            public string Header()
            {
                return m_FileHeader;
            }

            public Entry RetrieveEntry()
            {
                byte Type = 0;
                Entry Result = new Entry();

                if (RecordComplete())
                {
                    Result.Type = EntryType.Empty;
                    Result.Value = "";
                }
                else
                {
                    m_EntriesRead += 1;
                    Type = m_Reader.ReadByte();
                    //Entry Type Prefix
                    Result.Type = (EntryType)Type;

                    switch (Result.Type)
                    {
                        case EntryType.Empty:
                            Result.Value = "";

                            break;
                        case EntryType.Boolean:
                            byte b = 0;

                            b = m_Reader.ReadByte();
                            Result.Value = (b == 1);

                            break;
                        case EntryType.UInt16:
                            Result.Value = RawReadUInt16();

                            break;
                        case EntryType.String:
                            Result.Value = RawReadCString();

                            break;
                        case EntryType.Byte:
                            Result.Value = m_Reader.ReadByte();

                            break;
                        default:
                            Result.Type = EntryType.Error;
                            Result.Value = "";
                            break;
                    }
                }

                return Result;
            }

            private UInt16 RawReadUInt16()
            {
                //Read a uint in little endian. This is the format already used
                //by the .NET BinaryReader. However, it is good to specificially
                //define this given byte order can change depending on platform.

                int b0 = 0;
                int b1 = 0;
                UInt16 Result = default(UInt16);

                b0 = m_Reader.ReadByte();
                //Least significant byte first
                b1 = m_Reader.ReadByte();

                Result = (ushort)((b1 << 8) + b0);

                return Result;
            }

            private string RawReadCString()
            {
                UInt16 Char16 = default(UInt16);
                StringBuilder Text = new StringBuilder();
                bool Done = false;

                while (!(Done))
                {
                    Char16 = RawReadUInt16();
                    if (Char16 == 0)
                    {
                        Done = true;
                    }
                    else
                    {
                        Text.Append((char)Char16);
                    }
                }

                return Text.ToString();
            }


            public string RetrieveString()
            {
                Entry e = default(Entry);

                e = RetrieveEntry();
                if (e.Type == EntryType.String)
                {
                    return (string)e.Value;
                }
                else
                {
                    throw new IOException(e.Type, m_Reader);
                }
            }

            public short RetrieveInt16()
            {
                Entry e = default(Entry);

                e = RetrieveEntry();
                if (e.Type == EntryType.UInt16)
                {
                    return (short)(ushort)e.Value;
                }
                else
                {
                    throw new IOException(e.Type, m_Reader);
                }
            }

            public ushort RetrieveUInt16()
            {
                Entry e = default(Entry);

                e = RetrieveEntry();
                if (e.Type == EntryType.UInt16)
                {
                    return (ushort)e.Value;
                }
                else
                {
                    throw new IOException(e.Type, m_Reader);
                }
            }

            public bool RetrieveBoolean()
            {
                Entry e = default(Entry);

                e = RetrieveEntry();
                if (e.Type == EntryType.Boolean)
                {
                    return (bool)e.Value;
                }
                else
                {
                    throw new IOException(e.Type, m_Reader);
                }
            }

            public byte RetrieveByte()
            {
                Entry e = default(Entry);

                e = RetrieveEntry();
                if (e.Type == EntryType.Byte)
                {
                    return (byte)e.Value;
                }
                else
                {
                    throw new IOException(e.Type, m_Reader);
                }
            }

            public bool GetNextRecord()
            {
                byte ID = 0;
                bool Success = false;

                //==== Finish current record
                while (m_EntriesRead < m_EntryCount)
                {
                    RetrieveEntry();
                }

                //==== Start next record
                ID = m_Reader.ReadByte();

                if (ID == kRecordContentMulti)
                {
                    m_EntryCount = RawReadUInt16();
                    m_EntriesRead = 0;
                    Success = true;
                }
                else
                {
                    Success = false;
                }

                return Success;
            }
        }
    }
}
