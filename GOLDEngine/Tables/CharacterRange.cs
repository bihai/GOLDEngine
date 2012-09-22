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

    internal class CharacterSet : ArrayList
    {

        public new CharacterRange this[int Index]
        {
            get { return base.Item(Index); }

            set { base.Item(Index) = value; }
        }

        public new int Add(ref CharacterRange Item)
        {
            return base.Add(Item);
        }

        public new bool Contains(int CharCode)
        {
            //This procedure searchs the set to deterimine if the CharCode is in one
            //of the ranges - and, therefore, the set.
            //The number of ranges in any given set are relatively small - rarely 
            //exceeding 10 total. As a result, a simple linear search is sufficient 
            //rather than a binary search. In fact, a binary search overhead might
            //slow down the search!

            bool Found = false;
            int n = 0;
            CharacterRange Range = default(CharacterRange);

            while ((n < base.Count) & (!Found))
            {
                Range = base.Item(n);

                Found = (CharCode >= Range.Start & CharCode <= Range.End);
                n += 1;
            }

            return Found;
        }

    }

    internal class CharacterSetList : ArrayList
    {

        public CharacterSetList()
            : base()
        {
        }

        internal CharacterSetList(int Size)
            : base()
        {
            ReDimension(Size);
        }

        internal void ReDimension(int Size)
        {
            //Increase the size of the array to Size empty elements.
            int n = 0;

            base.Clear();
            for (n = 0; n <= Size - 1; n++)
            {
                base.Add(null);
            }
        }

        public new CharacterSet this[int Index]
        {
            get { return base.Item(Index); }

            set { base.Item(Index) = value; }
        }

        public new int Add(ref CharacterSet Item)
        {
            return base.Add(Item);
        }
    }

    //=======================================================
    //Service provided by Telerik (www.telerik.com)
    //Conversion powered by NRefactory.
    //Twitter: @telerik, @toddanglin
    //Facebook: facebook.com/telerik
    //=======================================================
}
