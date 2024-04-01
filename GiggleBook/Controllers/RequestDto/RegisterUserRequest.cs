namespace GiggleBook.Controllers.RequestDto;

public class RegisterUserRequest
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string SecondName { get; set; }
    public DateTime BirthDate { get; set; }
    public required string Biography { get; set; }
    public required string City { get; set; }
    public char Sex { get; set; }
}
