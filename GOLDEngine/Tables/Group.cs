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

        internal bool CanNestGroup(Group otherGroup)
        {
            return Nesting.Contains(otherGroup.TableIndex);
        }

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
        internal GroupList(int Size)
            : base(Size)
        {
            //Increase the size of the array to Size empty elements.
            for (int i = 0; i < Size; ++i)
            {
                base.Add(null);
            }
        }
    }


    internal class IntegerList : List<int>
    {
    }
}
