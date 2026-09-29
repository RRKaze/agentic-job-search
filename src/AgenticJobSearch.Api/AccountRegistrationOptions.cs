using System.Security.Cryptography;
using System.Text;

public sealed class AccountRegistrationOptions
{
    public const string SectionName = "Accounts";

    public bool RegistrationEnabled { get; init; } = true;
    public string RegistrationBootstrapToken { get; init; } = string.Empty;

    public bool Accepts(string? candidate)
    {
        if (!RegistrationEnabled) return false;
        if (string.IsNullOrEmpty(RegistrationBootstrapToken)) return true;
        if (candidate is null) return false;

        var expected = Encoding.UTF8.GetBytes(RegistrationBootstrapToken);
        var actual = Encoding.UTF8.GetBytes(candidate);
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
