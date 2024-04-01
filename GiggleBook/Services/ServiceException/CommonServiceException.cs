namespace GiggleBook.Services.ServiceException;

public class CommonServiceException : Exception
{
    public int Code { get; }

    public CommonServiceException(int code, string message) : base(message)
    {
        Code = code;
    }

    public CommonServiceException(int code, string message, Exception inner) : base(message, inner)
    {;
        Code = code;
    }
}
