// ERROR: Not supported in C#: OptionDeclaration

namespace GOLDEngine.Tables
{
    internal class FAEdge
    {
        //================================================================================
        // Class Name:
        //      FAEdge
        //
        // Purpose:
        //      Each state in the Determinstic Finite Automata contains multiple edges which
        //      link to other states in the automata.
        //
        //      This class is used to represent an edge.
        //
        // Author(s):
        //      Devin Cook
        //      http://www.DevinCook.com/GOLDParser
        //
        // Dependacies:
        //      (None)
        //
        //================================================================================

        //Characters to advance on	
        public CharacterSet Characters;
        //FAState
        public int Target;

        public FAEdge(CharacterSet CharSet, int Target)
        {
            this.Characters = CharSet;
            this.Target = Target;
        }

        public FAEdge()
        {
            //Nothing for now
        }
    }

    internal class FAEdgeList : ArrayList
    {

        public new FAEdge this[int Index]
        {
            get { return base.Item(Index); }
            set { base.Item(Index) = value; }
        }

        public new int Add(FAEdge Edge)
        {
            return base.Add(Edge);
        }
    }

    //=======================================================
    //Service provided by Telerik (www.telerik.com)
    //Conversion powered by NRefactory.
    //Twitter: @telerik, @toddanglin
    //Facebook: facebook.com/telerik
    //=======================================================
}
