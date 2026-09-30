# Production runtime configuration

The production image serves the Angular application and ASP.NET Core API from one HTTP port. The hosting platform terminates HTTPS and forwards the original protocol. Keep every secret in the hosting platform's encrypted environment settings; do not commit secret values or put them in a Docker build argument.

## Required settings

| Setting | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT=Production` | Enables production startup validation and secure-cookie behavior. |
| `ASPNETCORE_URLS=http://+:8080` | Listens on the container port used by the production image. |
| `AllowedHosts` | The exact public host name, such as `example.onrender.com`; wildcards are rejected in production. |
| `ConnectionStrings__JobSearch` | PostgreSQL connection string with TLS and host verification required. Use the managed provider's pooled connection string. |
| `Accounts__RegistrationEnabled` | Set to `true` only while creating the owner's account. |
| `Accounts__RegistrationBootstrapToken` | Random value of at least 32 characters while registration is enabled. |
| `Hosting__DataProtectionCertificateBase64` | Base64-encoded PKCS#12 certificate used to protect session-key material in PostgreSQL. |
| `Hosting__DataProtectionCertificatePassword` | Certificate password of at least 16 characters. |
| `Imports__Enabled=false` | Keeps local administrative import routes unavailable on the public service. |

Production startup stops with a list of missing or unsafe settings. It never falls back to the local development database.

## Generate the data-protection certificate

Generate this certificate on a trusted computer. Store the PFX content and password in the hosting platform, then delete the local key files after confirming the saved secrets.

```sh
openssl req -x509 -newkey rsa:3072 -sha256 -days 825 -nodes \
  -subj "/CN=Agentic Job Search Data Protection" \
  -keyout data-protection.key -out data-protection.crt
openssl pkcs12 -export -out data-protection.pfx \
  -inkey data-protection.key -in data-protection.crt
base64 < data-protection.pfx | tr -d '\n'
```

Use a unique password of at least 16 characters when `openssl pkcs12` prompts for one. Back up the PFX and its password separately from the database. Losing either prevents the application from decrypting the persisted key ring and invalidates existing sessions.

## Apply database migrations

Run the same application as a one-time task before starting a new application version:

```sh
dotnet AgenticJobSearch.Api.dll --migrate
```

The command applies pending EF Core migrations and exits without opening the web port. Normal production startup does not apply migrations, which prevents multiple application instances from racing schema changes. `Hosting__ApplyMigrationsOnStartup=true` exists for controlled single-instance environments but should remain `false` on the hosted service.

Render's free plan has no pre-deploy command. The checked-in GitHub deployment workflow therefore uses Neon's direct TLS endpoint to run `--migrate`, then calls Render's deploy hook with the exact migrated commit. The web service uses Neon's pooled endpoint for normal traffic. See the [free deployment runbook](free-deployment.md) for the complete sequence.

## Create the owner account

1. Deploy with registration enabled and a new bootstrap token.
2. Open the registration screen, enter the owner credentials and the bootstrap token, and create the account.
3. Change `Accounts__RegistrationEnabled` to `false`, clear the bootstrap-token secret, and redeploy.
4. Confirm that the registration endpoint returns `404` and that the owner can still sign in.

The bootstrap token is accepted only by registration and is never stored with the account. Authentication cookies remain valid across ordinary deployments because the protected key ring is stored in PostgreSQL.

## Health and request diagnostics

- `GET /api/health/live` confirms that the process is running and is safe for the hosting platform's health check.
- `GET /api/health/ready` checks PostgreSQL connectivity and returns `503` when the database is unavailable.
- Every response includes `X-Correlation-ID`. A valid incoming value is reused; otherwise the server creates one.
- Request logs include the correlation ID, method, path, status, and duration. They omit query strings, request bodies, cookies, credentials, job descriptions, resumes, and candidate evidence.
