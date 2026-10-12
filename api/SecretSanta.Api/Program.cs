using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;
using SecretSanta.Api.Services;
using SecretSanta.Api.Stores;

var builder = WebApplication.CreateBuilder(args);

// Railway tells the app which port to listen on via PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ConnectionString.Resolve(builder.Configuration)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IExchangeStore, ExchangeStore>();
builder.Services.AddScoped<IParticipantStore, ParticipantStore>();
builder.Services.AddScoped<IExchangeService, ExchangeService>();
builder.Services.AddScoped<IInviteService, InviteService>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Unknown /api routes are real 404s; everything else is handled by the React app's router.
app.MapFallback("/api/{**rest}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
