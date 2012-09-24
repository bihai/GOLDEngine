using System.Collections.Generic;

namespace GOLDEngine.Tables
{
    internal class Group
    {
        public enum AdvanceMode
        {
            Token = 0,
            Character = 1
        }

        public enum EndingMode
        {
            Open = 0,
            Closed = 1
        }


        internal short TableIndex;
        internal string Name;
        internal Symbol Container;
        internal Symbol Start;

        internal Symbol End;
        internal AdvanceMode Advance;

        internal EndingMode Ending;

        internal IntegerList Nesting;
        internal Group()
        {
            Advance = AdvanceMode.Character;
            Ending = EndingMode.Closed;
            Nesting = new IntegerList();
            //GroupList
        }
    }


    internal class GroupList : List<Group>
    {

        public GroupList()
            : base()
        {
        }

        internal GroupList(int Size)
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
    }


    internal class IntegerList : List<int>
    {
    }
}
