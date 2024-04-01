using GiggleBook.Data;
using GiggleBook.Dto;

namespace GiggleBook.Interfaces;

public interface ISessionStoreService
{
    Session Get(string token);
    void Create(User session);
    void Drop(Session session);
    void Refresh(string sessionId);
}
