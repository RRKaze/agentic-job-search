using System.Net.Mail;
using System.Security.Claims;
using AgenticJobSearch.Domain;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public static class AccountEndpoints
{
    public static void MapAccounts(this WebApplication app)
    {
        var accounts = app.MapGroup("/api/account").RequireRateLimiting("accounts");
        accounts.MapPost("/register", async (RegisterRequest request, JobSearchDbContext db, HttpContext context) =>
        {
            var email = request.Email?.Trim().ToLowerInvariant() ?? "";
            var name = request.DisplayName?.Trim() ?? "";
            if (name.Length is < 1 or > 200 || email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || request.Password is null || request.Password.Length is < 12 or > 128)
                return Results.BadRequest(new { message = "Enter a name, a valid email, and a password between 12 and 128 characters." });
            var user = new UserAccount { Email = email, DisplayName = name };
            user.PasswordHash = new PasswordHasher<UserAccount>().HashPassword(user, request.Password);
            db.UserAccounts.Add(user);
            // Create the user's blank candidate profile in the same transaction. Never copy demo evidence.
            db.CandidateProfiles.Add(new CandidateProfile { Id = Guid.NewGuid(), OwnerId = user.Id, DisplayName = name });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Results.Conflict(new { message = "Unable to create this account. Try signing in or use a different email." }); }
            await SignIn(context, user);
            return Results.Json(Profile(user), statusCode: 201);
        });
        accounts.MapPost("/login", async (LoginRequest request, JobSearchDbContext db, HttpContext context) =>
        {
            var email = request.Email?.Trim().ToLowerInvariant() ?? "";
            if (request.Password is null || request.Password.Length is < 1 or > 128 || email.Length > 254)
                return Results.Json(new { message = "Email or password is incorrect." }, statusCode: 401);
            var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Email == email);
            var hasher = new PasswordHasher<UserAccount>();
            var result = hasher.VerifyHashedPassword(user ?? new UserAccount(), user?.PasswordHash ?? DummyHash, request.Password);
            if (user == null || result == PasswordVerificationResult.Failed)
                return Results.Json(new { message = "Email or password is incorrect." }, statusCode: 401);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            { user.PasswordHash = hasher.HashPassword(user, request.Password); await db.SaveChangesAsync(); }
            await SignIn(context, user);
            return Results.Ok(Profile(user));
        });
        accounts.MapGet("/me", async (HttpContext context, JobSearchDbContext db) =>
        {
            var user = await FindUser(context, db);
            return user == null ? Results.Unauthorized() : Results.Ok(Profile(user));
        }).RequireAuthorization();
        accounts.MapPut("/profile", async (ProfileRequest request, HttpContext context, JobSearchDbContext db) =>
        {
            var user = await FindUser(context, db);
            if (user == null) return Results.Unauthorized();
            var name = request.DisplayName?.Trim() ?? "";
            if (name.Length is < 1 or > 200 || request.CareerStage is not ("new_graduate" or "experienced_worker"))
                return Results.BadRequest(new { message = "Enter your name and choose New graduate or Experienced worker." });
            user.DisplayName = name;
            user.CareerStage = request.CareerStage;
            var candidate = await db.CandidateProfiles.SingleAsync(x => x.OwnerId == user.Id);
            candidate.DisplayName = name;
            await db.SaveChangesAsync();
            return Results.Ok(Profile(user));
        }).RequireAuthorization();
        accounts.MapPost("/logout", async (HttpContext context) =>
        { await context.SignOutAsync(); return Results.NoContent(); });
    }
    private static readonly string DummyHash = new PasswordHasher<UserAccount>().HashPassword(new UserAccount(), "Fictional timing comparison only");
    private static object Profile(UserAccount user) => new { user.Id, user.Email, user.DisplayName, user.CareerStage, user.CreatedAt };
    private static Task<UserAccount?> FindUser(HttpContext context, JobSearchDbContext db) =>
        db.UserAccounts.SingleOrDefaultAsync(x => x.Id == Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    private static Task SignIn(HttpContext context, UserAccount user) => context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email)], CookieAuthenticationDefaults.AuthenticationScheme)));
    public sealed record RegisterRequest(string? DisplayName, string? Email, string? Password);
    public sealed record LoginRequest(string? Email, string? Password);
    public sealed record ProfileRequest(string? DisplayName, string? CareerStage);
}
