namespace GiggleBook.Dto;

public class Post
{
    public Guid Id { get; set; } = Guid.Empty;
    public required string Text { get; set; } 
    public Guid AuthorId { get; set; } = Guid.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    public DateTime UpdatedAt { get; set; } = DateTime.MinValue;
}
