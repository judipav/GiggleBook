namespace GiggleBook.Dto;

public class Post
{
    Guid Id { get; set; }
    public required List<string> Text { get; set; } 
}
