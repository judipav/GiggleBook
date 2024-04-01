using GiggleBook.Dto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GiggleBook.Data;

public class Session
{
    public Session(User user)
    {
        Id = Guid.NewGuid();
        User = user;
    }
    public Guid Id { get; init; }
    public User User { get; set; }
    public DateTime Created { get; init; } = DateTime.Now;
    public DateTime BestBefore { get; set; } = DateTime.Now.AddHours(8);
}
