namespace AgenticJobSearch.Application.Abstractions;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool CanAccessLegacyWorkspace { get; }
}
