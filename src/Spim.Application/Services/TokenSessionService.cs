using Spim.Application.Abstractions;
using Spim.Domain.Entities;

namespace Spim.Application.Services;

public sealed class TokenSessionService(ITokenSessionStore tokenStore)
{
    public Task StoreTokenAsync(TokenSession session)
    {
        var remainingSeconds = (session.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds;
        var ttlSeconds = Math.Max(0, (int)remainingSeconds);
        return tokenStore.SaveAsync(session, ttlSeconds);
    }

    public Task<TokenSession?> GetTokenAsync(string userId) => tokenStore.GetAsync(userId);

    public Task RevokeTokenAsync(string userId) => tokenStore.DeleteAsync(userId);
}