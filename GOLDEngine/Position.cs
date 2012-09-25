namespace GOLDEngine
{
    public struct Position
    {
        readonly public int Line;
        readonly public int Column;

        internal Position NextLine
        {
            get { return new Position(Line + 1, 0); }
        }

        internal Position NextColumn
        {
            get { return new Position(Line, Column + 1); }
        }

        Position(int Line, int Column)
        {
            this.Line = Line;
            this.Column = Column;
        }
    }
}
