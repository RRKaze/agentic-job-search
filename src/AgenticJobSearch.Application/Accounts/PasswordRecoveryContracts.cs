namespace AgenticJobSearch.Application.Accounts;

public sealed record ForgotPasswordRequest(string? Email);
public sealed record ResetPasswordRequest(string? Token, string? Password, string? ConfirmPassword);
public interface IPasswordRecoveryService
{
    Task RequestAsync(string? email, CancellationToken cancellationToken);
    Task ResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
public sealed record RecoveryEmailMessage(string From, string To, string Subject, string Text);
public interface IRecoveryEmailSender
{
    Task SendAsync(Guid deliveryId, RecoveryEmailMessage message, CancellationToken cancellationToken);
}
