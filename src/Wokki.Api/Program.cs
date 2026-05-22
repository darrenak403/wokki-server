using Serilog;
using Microsoft.OpenApi;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Wokki API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

});
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

var enableDocs = app.Configuration.GetValue<bool?>("ApiDocs:Enabled")
                 ?? app.Environment.IsDevelopment();

app.UseApplicationPipeline();

if (enableDocs)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
}

app.MapEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.ApplyMigrationsAsync();
    await SeedData.InitializeAsync(app.Services);
}

app.Run();

public partial class Program;
