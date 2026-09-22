using System.Security.Claims;
using AgenticJobSearch.Application.Abstractions;

public sealed class CurrentUser(IHttpContextAccessor accessor, IConfiguration configuration) : ICurrentUser
{
    public Guid? Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool CanAccessLegacyWorkspace => Id != null &&
        Guid.TryParse(configuration["Accounts:LegacyWorkspaceOwnerId"], out var ownerId) && Id == ownerId;
}
