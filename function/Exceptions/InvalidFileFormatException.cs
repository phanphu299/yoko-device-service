namespace Function.Exception
{
    public class InvalidFileFormatException : System.Exception
    {
        public InvalidFileFormatException(string message = "") : base(message)
        {
        }
    }
}