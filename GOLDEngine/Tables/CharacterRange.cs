using System;
using System.Collections.Generic;
using System.Text;

namespace GOLDEngine.Tables
{
    internal class CharacterRange
    {
        public UInt16 Start;

        public UInt16 End;
        public CharacterRange(UInt16 Start, UInt16 End)
        {
            this.Start = Start;
            this.End = End;
        }
    }

    internal class CharacterSet : List<CharacterRange>
    {
        public bool Contains(ushort CharCode)
        {
            //This procedure searchs the set to deterimine if the CharCode is in one
            //of the ranges - and, therefore, the set.
            //The number of ranges in any given set are relatively small - rarely 
            //exceeding 10 total. As a result, a simple linear search is sufficient 
            //rather than a binary search. In fact, a binary search overhead might
            //slow down the search!
            return base.Exists(range => (CharCode >= range.Start & CharCode <= range.End));
        }
    }

    internal class CharacterSetList : List<CharacterSet>
    {
        internal CharacterSetList(int Size)
            : base(Size)
        {
            //Increase the size of the array to Size empty elements.
            for (int i = 0; i < Size; ++i )
            {
                base.Add(null);
            }
        }
    }
}
