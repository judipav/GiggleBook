namespace Chirper.Services;

public record Chirp(Guid From, Guid To, string Message, DateTime DateTime);
