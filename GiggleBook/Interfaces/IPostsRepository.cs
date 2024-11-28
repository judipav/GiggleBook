using GiggleBook.Dto;

namespace GiggleBook.Interfaces;

public interface IPostsRepository {
    Task<Post> GetAsync(Guid id);
    Task<IEnumerable<Post>> GetUserPosts(Guid userId);
    Task<Post> AddAsync(Post post);
    Task<Post> UpdateAsync(Guid postId, string text);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Post>> GetFeedAsync(Guid userId);
 }