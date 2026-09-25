using System.Net.Mail;
using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Accounts;

public sealed class RegisterAccountHandler(IAccountRepository accounts, IPasswordService passwords)
{
    public async Task<AccountProfileDto> HandleAsync(RegisterAccountRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var name = request.DisplayName?.Trim() ?? "";
        if (name.Length is < 1 or > 200 || email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) ||
            parsed.Address != email || request.Password is null || request.Password.Length is < 12 or > 128)
            throw new AccountFailure(400, "Enter a name, a valid email, and a password between 12 and 128 characters.");

        var account = new UserAccount { Email = email, DisplayName = name };
        account.PasswordHash = passwords.Hash(account, request.Password);
        var profile = new CandidateProfile { Id = Guid.NewGuid(), OwnerId = account.Id, DisplayName = name };
        if (!await accounts.AddAsync(account, profile, cancellationToken))
            throw new AccountFailure(409, "Unable to create this account. Try signing in or use a different email.");

        return account.ToProfile();
    }
}

public sealed class LoginAccountHandler(IAccountRepository accounts, IPasswordService passwords)
{
    public async Task<AccountProfileDto> HandleAsync(LoginAccountRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (request.Password is null || request.Password.Length is < 1 or > 128 || email.Length > 254)
            throw InvalidCredentials();

        var account = await accounts.FindByEmailAsync(email, cancellationToken);
        var result = passwords.Verify(account, request.Password);
        if (account is null || result == PasswordCheckResult.Failed) throw InvalidCredentials();
        if (result == PasswordCheckResult.RehashNeeded)
        {
            account.PasswordHash = passwords.Hash(account, request.Password);
            await accounts.UpdatePasswordHashAsync(account, cancellationToken);
        }

        return account.ToProfile();
    }

    private static AccountFailure InvalidCredentials() => new(401, "Email or password is incorrect.");
}

public sealed class GetAccountProfileHandler(IAccountRepository accounts, ICurrentUser currentUser)
{
    public async Task<AccountProfileDto> HandleAsync(CancellationToken cancellationToken)
    {
        var account = currentUser.Id is Guid id ? await accounts.FindByIdAsync(id, cancellationToken) : null;
        return account?.ToProfile() ?? throw new AccountFailure(401, "Sign in first.");
    }
}

public sealed class UpdateAccountProfileHandler(IAccountRepository accounts, ICurrentUser currentUser)
{
    public async Task<AccountProfileDto> HandleAsync(UpdateAccountProfileRequest request, CancellationToken cancellationToken)
    {
        var account = currentUser.Id is Guid id ? await accounts.FindByIdAsync(id, cancellationToken) : null;
        if (account is null) throw new AccountFailure(401, "Sign in first.");

        var name = request.DisplayName?.Trim() ?? "";
        if (name.Length is < 1 or > 200 || request.CareerStage is not ("new_graduate" or "experienced_worker"))
            throw new AccountFailure(400, "Enter your name and choose New graduate or Experienced worker.");

        account.DisplayName = name;
        account.CareerStage = request.CareerStage;
        await accounts.UpdateProfileAsync(account, cancellationToken);
        return account.ToProfile();
    }
}

internal static class AccountMappings
{
    public static AccountProfileDto ToProfile(this UserAccount account) =>
        new(account.Id, account.Email, account.DisplayName, account.CareerStage, account.CreatedAt);
}
