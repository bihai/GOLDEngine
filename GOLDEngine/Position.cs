namespace GOLDEngine
{

    public class Position
    {
        public int Line;

        public int Column;
        internal Position()
        {
            this.Line = 0;
            this.Column = 0;
        }

        internal void Copy(Position Pos)
        {
            this.Column = Pos.Column;
            this.Line = Pos.Line;
        }
    }

    //=======================================================
    //Service provided by Telerik (www.telerik.com)
    //Conversion powered by NRefactory.
    //Twitter: @telerik, @toddanglin
    //Facebook: facebook.com/telerik
    //=======================================================
}
