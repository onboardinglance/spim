using Dapr.Client;

namespace dapr_demo.Services;

public class TokenStateService
{
    private readonly DaprClient _daprClient;
    private const string StateStore = "statestore";

    public TokenStateService(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    // Set state with TTL in the state store 
    public async Task StoreTokenAsync(TokenSession session)
    {
        // Ensure TTL is never negative to avoid Dapr sidecar errors
        var remainingTime = (session.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds;
        var ttl = Math.Max(0, (int)remainingTime);

        var metadata = new Dictionary<string, string>
        {
            { "ttlInSeconds", ttl.ToString() }
        };
        // key scoped per user - encrypted at rest via statestore.yaml
        await _daprClient.SaveStateAsync(StateStore, $"token-{session.UserId}", session, metadata: metadata);
    }

    // Get state from the state store
    public async Task<TokenSession?> GetTokenAsync(string userId)
    {
        return await _daprClient.GetStateAsync<TokenSession>(StateStore, $"token-{userId}");
    }

    // Delete state from the state store
    public async Task RevokeTokenAsync(string userId)
    {
        await _daprClient.DeleteStateAsync(StateStore, $"token-{userId}");
    }


}