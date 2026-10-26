using Spim.Domain.Entities;

namespace Spim.Application.Abstractions;

public interface ITokenSessionStore
{
    Task SaveAsync(TokenSession session, int ttlSeconds);
    Task<TokenSession?> GetAsync(string userId);
    Task DeleteAsync(string userId);
}