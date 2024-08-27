namespace AHI.Device.Function.FileParser.BaseExcelParser
{
    public class CellIndex
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public CellIndex(int row, int col)
        {
            Row = row;
            Col = col;
        }
        public CellIndex Clone()
        {
            return new CellIndex(Row, Col);
        }
        public void CopyTo(CellIndex index)
        {
            index.Row = this.Row;
            index.Col = this.Col;
        }
    }
}