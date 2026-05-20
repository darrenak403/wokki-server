using Serilog;
using Wokki.Api.Bootstrapping;
using Wokki.Api.Extensions;
using Wokki.Application.DependencyInjection;
using Wokki.Infrastructure.DependencyInjection;
using Wokki.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddOpenApi();
builder.Services.AddApiServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseApplicationPipeline();
app.MapEndpoints();

await app.ApplyMigrationsAsync();
await SeedData.InitializeAsync(app.Services);

app.Run();

public partial class Program;
