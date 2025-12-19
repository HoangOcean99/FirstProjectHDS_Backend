public class DuplicateSQLException : Exception
{
    public int StatusCode { get; }

    public DuplicateSQLException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
