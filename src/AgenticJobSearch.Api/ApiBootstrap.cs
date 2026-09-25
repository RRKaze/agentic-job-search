using AgenticJobSearch.Application;
using AgenticJobSearch.Application.Abstractions;
using AgenticJobSearch.Infrastructure;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;


public static class ApiBootstrap
{
    public static async Task<WebApplication> BuildAsync(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "agentic.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing") ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
                options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("accounts", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        builder.Services.AddScoped<AgenticJobSearch.Infrastructure.Imports.RecordImporter>();
        builder.Services.AddSingleton<AgenticJobSearch.Application.Imports.ISourceHeadVerifier, AgenticJobSearch.Infrastructure.Imports.GitHubHeadVerifier>();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        configure?.Invoke(builder);
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<JobSearchDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        // All browser mutations require a custom header: cross-origin forms cannot submit it.
        // CORS only grants preflight access to the trusted frontend origins above.
        app.Use(async (context, next) =>
        {
            if ((context.Request.Path.StartsWithSegments("/api/account") || context.Request.Path.StartsWithSegments("/api/jobs")) &&
                context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE" &&
                context.Request.Headers["X-Agentic-Request"] != "1")
            { context.Response.StatusCode = 403; return; }
            await next();
        });
        app.MapAccounts();
        app.MapCandidateEndpoints();
        app.MapJobEndpoints();
        app.MapHealthEndpoints();
        app.MapRecordImports();


        return app;
    }
}
