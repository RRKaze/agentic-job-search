using System.Security.Claims;
using AgenticJobSearch.Application.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

public static class AccountEndpoints
{
    public static void MapAccounts(this WebApplication app)
    {
        var accounts = app.MapGroup("/api/account").RequireRateLimiting("accounts");

        accounts.MapPost("/register", async (
            RegisterAccountRequest request,
            RegisterAccountHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            await HandleAuthenticated(context, () => handler.HandleAsync(request, cancellationToken), created: true));

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
            [new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()), new Claim(ClaimTypes.Email, profile.Email)],
            CookieAuthenticationDefaults.AuthenticationScheme)));
}
