using Microsoft.EntityFrameworkCore;
using SecretSanta.Api;
using SecretSanta.Api.Data;
using SecretSanta.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Railway tells the app which port to listen on via PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ConnectionString.Resolve(builder.Configuration)));
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAdminEndpoints();
app.MapInviteEndpoints();

// Unknown /api routes are real 404s; everything else is handled by the React app's router.
app.MapFallback("/api/{**rest}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
