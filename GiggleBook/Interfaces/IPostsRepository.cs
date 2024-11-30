using GiggleBook.Dto;

namespace GiggleBook.Interfaces;

public interface IPostsRepository {
    Task<Post> GetAsync(Guid id, CancellationToken token);
    Task<IEnumerable<Post>> GetUserPosts(Guid userId, CancellationToken token);
    Task AddAsync(Post post, CancellationToken token);
    Task<Post> UpdateAsync(Guid postId, string text, CancellationToken token);
    Task DeleteAsync(Guid id, CancellationToken token);
    IAsyncEnumerable<Post> GetFeedAsync(Guid userId, CancellationToken token);
    Task CacheMostActiveUsersAsync(CancellationToken token);
 }