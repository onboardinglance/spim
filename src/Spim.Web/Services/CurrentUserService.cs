using System.Security.Claims;
using Spim.Application.Common.Interfaces;

namespace Spim.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue("sub");

    public string? UserName => User?.FindFirstValue("preferred_username");

    public string? Email => User?.FindFirstValue("email");
}
