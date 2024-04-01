namespace GiggleBook.Dto;

public class UserDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? SecondName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Biography { get; set; }
    public string? City { get; set; }
    public char? Sex { get; set; }
}