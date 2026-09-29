var app = await ApiBootstrap.BuildAsync(args);
var migrateOnly = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);
if (migrateOnly || app.Configuration.GetValue<bool>("Hosting:ApplyMigrationsOnStartup"))
    await app.MigrateDatabaseAsync();
if (migrateOnly) return;
await app.RunAsync();

public partial class Program { }
