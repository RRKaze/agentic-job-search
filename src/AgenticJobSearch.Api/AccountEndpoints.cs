using System.Security.Claims;
using AgenticJobSearch.Application.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

public static class AccountEndpoints
{
    public static void MapAccounts(this WebApplication app)
    {
        var accounts = app.MapGroup("/api/account").RequireRateLimiting("accounts");

        accounts.MapPost("/register", async (
            RegisterAccountRequest request,
            RegisterAccountHandler handler,
            HttpContext context,
            IOptionsSnapshot<AccountRegistrationOptions> registration,
            CancellationToken cancellationToken) =>
        {
            if (!registration.Value.RegistrationEnabled) return Results.NotFound();
            if (!registration.Value.Accepts(request.BootstrapToken))
                return Results.Json(new { message = "Registration requires the private bootstrap token." }, statusCode: 403);
            return await HandleAuthenticated(context, () => handler.HandleAsync(request, cancellationToken), created: true);
        });

        accounts.MapPost("/login", async (
            LoginAccountRequest request,
            LoginAccountHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            await HandleAuthenticated(context, () => handler.HandleAsync(request, cancellationToken)));

        accounts.MapGet("/me", async (GetAccountProfileHandler handler, CancellationToken cancellationToken) =>
            await Handle(() => handler.HandleAsync(cancellationToken)))
            .RequireAuthorization();

        accounts.MapPut("/profile", async (
            UpdateAccountProfileRequest request,
            UpdateAccountProfileHandler handler,
            CancellationToken cancellationToken) =>
            await Handle(() => handler.HandleAsync(request, cancellationToken)))
            .RequireAuthorization();

        accounts.MapPost("/forgot-password", async (ForgotPasswordRequest request, IPasswordRecoveryService recovery,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await recovery.RequestAsync(request.Email, cancellationToken);
                return Results.Json(new { message = "If an account exists for that address, we’ll email a reset link." }, statusCode: 202);
            }
            catch (AccountFailure exception)
            { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); }
        }).RequireRateLimiting("recovery");

        accounts.MapPost("/reset-password", async (ResetPasswordRequest request, IPasswordRecoveryService recovery,
            HttpContext context, CancellationToken cancellationToken) =>
        {
            try
            {
                await recovery.ResetAsync(request, cancellationToken);
                await context.SignOutAsync();
                return Results.Ok(new { message = "Your password has been reset. Sign in with your new password." });
            }
            catch (AccountFailure exception)
            { return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode); }
        }).RequireRateLimiting("recovery");

        accounts.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync();
            return Results.NoContent();
        });
    }

    private static async Task<IResult> HandleAuthenticated(
        HttpContext context,
        Func<Task<AccountProfileDto>> action,
        bool created = false)
    {
        try
        {
            var profile = await action();
            await SignIn(context, profile);
            return created ? Results.Json(profile, statusCode: 201) : Results.Ok(profile);
        }
        catch (AccountFailure exception)
        {
            return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
        }
    }

    private static async Task<IResult> Handle(Func<Task<AccountProfileDto>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (AccountFailure exception)
        {
            return Results.Json(new { message = exception.Message }, statusCode: exception.StatusCode);
        }
    }

    private static Task SignIn(HttpContext context, AccountProfileDto profile) => context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()), new Claim(ClaimTypes.Email, profile.Email),
             new Claim("session_version", profile.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))],
            CookieAuthenticationDefaults.AuthenticationScheme)));
}
