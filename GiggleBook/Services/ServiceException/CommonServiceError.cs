namespace GiggleBook.Services.ServiceException;

public class CommonServiceError
{
    public string Message { get; init; }
    public int Code { get; init; }

    public CommonServiceError(int code, string message)
    {
        Code = code;
        Message = message;
    }
}