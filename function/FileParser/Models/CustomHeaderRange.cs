namespace AHI.Device.Function.FileParser.Model
{
    public class CustomHeaderRange
    {
        public CustomHeaderRange()
        {
        }

        public CustomHeaderRange(int rowIndex, int startColumnIndex, int endColumnIndex = -1)
        {
            RowIndex = rowIndex;
            StartColumnIndex = startColumnIndex;
            EndColumnIndex = endColumnIndex;
        }

        public int RowIndex { get; set; }
        public int StartColumnIndex { get; set; }
        public int EndColumnIndex { get; set; }
    }

}