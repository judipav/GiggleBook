namespace Chirper.Services;

public interface IDialogRepository
{
    Task Send(Chirp chirp, CancellationToken cancellationToken);
    IAsyncEnumerable<Chirp> List(Guid from, Guid to, int offset, CancellationToken cancellationToken);
}