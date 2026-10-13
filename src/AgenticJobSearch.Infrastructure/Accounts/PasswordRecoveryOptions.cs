using System.Net.Mail;

namespace AgenticJobSearch.Infrastructure.Accounts;

public sealed class PasswordRecoveryOptions
{
    public bool Enabled { get; set; }
    public string PublicBaseUrl { get; set; } = "";
    public string From { get; set; } = "";
    public string ResendApiKey { get; set; } = "";

    public bool IsValid() => !Enabled ||
        (Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var url) && url.Scheme == "https" &&
         url.AbsolutePath == "/" && url.Query.Length == 0 && url.Fragment.Length == 0 && url.UserInfo.Length == 0 &&
         MailAddress.TryCreate(From, out _) && !From.Contains('\r') && !From.Contains('\n') &&
         !string.IsNullOrWhiteSpace(ResendApiKey));
}
