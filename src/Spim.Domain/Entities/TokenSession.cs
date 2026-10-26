namespace Spim.Domain.Entities;

public class TokenSession
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string IdToken { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public string UserId { get; set; } = null!;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}