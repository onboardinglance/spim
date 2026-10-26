using Dapr.Client;
using Spim.Application.Abstractions;
using Spim.Domain.Entities;

namespace Spim.Persistence.Dapr;

public sealed class DaprTokenSessionStore(DaprClient daprClient) : ITokenSessionStore
{
    private const string StateStore = "statestore";

    public Task SaveAsync(TokenSession session, int ttlSeconds)
    {
        var metadata = new Dictionary<string, string> { ["ttlInSeconds"] = ttlSeconds.ToString() };
        return daprClient.SaveStateAsync(StateStore, $"token-{session.UserId}", session, metadata: metadata);
    }

    public async Task<TokenSession?> GetAsync(string userId) =>
        await daprClient.GetStateAsync<TokenSession>(StateStore, $"token-{userId}");

    public Task DeleteAsync(string userId) => daprClient.DeleteStateAsync(StateStore, $"token-{userId}");
}