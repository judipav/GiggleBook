namespace GiggleBook.Dto;

public class User
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string FirstName { get; set; }
    public required string SecondName { get; set; }
    public DateTime BirthDate { get; set; }
    public required string Biography { get; set; }
    public required string City { get; set; }
    public required char Sex { get; set; }
}
