namespace GiggleBook.Dto;

public class Post
{
    public Guid Id { get; set; }
    public required string Text { get; set; } 
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
