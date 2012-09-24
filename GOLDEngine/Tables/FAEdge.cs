using System.Collections.Generic;

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
        public short Target;

        public FAEdge(CharacterSet CharSet, short Target)
        {
            this.Characters = CharSet;
            this.Target = Target;
        }

        public FAEdge()
        {
            //Nothing for now
        }
    }

    internal class FAEdgeList : List<FAEdge>
    {
    }
}
