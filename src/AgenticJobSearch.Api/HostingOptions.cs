using System.Security.Cryptography.X509Certificates;

public sealed class HostingOptions
{
    public const string SectionName = "Hosting";

    public bool ApplyMigrationsOnStartup { get; init; }
    public string DataProtectionCertificateBase64 { get; init; } = string.Empty;
    public string DataProtectionCertificatePassword { get; init; } = string.Empty;

    public X509Certificate2 LoadDataProtectionCertificate() => X509CertificateLoader.LoadPkcs12(
        Convert.FromBase64String(DataProtectionCertificateBase64),
        DataProtectionCertificatePassword,
        X509KeyStorageFlags.EphemeralKeySet);
}

public static class ProductionConfiguration
{
    public static void Validate(
        bool isProduction,
        IConfiguration configuration,
        HostingOptions hosting,
        AccountRegistrationOptions registration)
    {
        if (!isProduction) return;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("JobSearch")))
            errors.Add("ConnectionStrings__JobSearch is required.");
        if (string.IsNullOrWhiteSpace(configuration["AllowedHosts"]) || configuration["AllowedHosts"] == "*")
            errors.Add("AllowedHosts must identify the public host.");
        if (hosting.DataProtectionCertificateBase64.Length == 0)
            errors.Add("Hosting__DataProtectionCertificateBase64 is required.");
        if (hosting.DataProtectionCertificatePassword.Length < 16)
            errors.Add("Hosting__DataProtectionCertificatePassword must contain at least 16 characters.");
        if (registration.RegistrationEnabled && registration.RegistrationBootstrapToken.Length < 32)
            errors.Add("Accounts__RegistrationBootstrapToken must contain at least 32 characters while registration is enabled.");
        if (configuration.GetValue<bool>("Imports:Enabled"))
            errors.Add("Imports__Enabled must remain false in production.");

        if (errors.Count > 0)
            throw new InvalidOperationException($"Production configuration is invalid: {string.Join(' ', errors)}");
    }
}
