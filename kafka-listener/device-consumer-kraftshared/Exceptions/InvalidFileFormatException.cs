namespace Device.Consumer.KraftShared.Exceptions
{
    public class InvalidFileFormatException : System.Exception
    {
        public InvalidFileFormatException(string message = "") : base(message)
        {
        }
    }
}