using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wokki.Application.Common.Interfaces;
using Wokki.Application.Scheduling;

namespace Wokki.Infrastructure.Scheduling;

public static class SchedulingServiceCollectionExtensions
{
    public static IServiceCollection AddScheduleSuggestions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ScheduleSuggestionFeatureSettings>(
            configuration.GetSection(ScheduleSuggestionFeatureSettings.SectionName));

        services.AddScoped<ScheduleSuggestionContextLoader>();
        services.AddScoped<ScheduleSuggestionPromptBuilder>();
        services.AddScoped<CpSatScheduleSuggestionService>();
        services.AddScoped<BedrockScheduleSuggestionService>();
        services.AddScoped<IScheduleSuggestionOrchestrator, ScheduleSuggestionOrchestrator>();
        services.AddScoped<IScheduleSuggestionService>(sp =>
            sp.GetRequiredService<CpSatScheduleSuggestionService>());

        return services;
    }
}

public sealed class ScheduleSuggestionFeatureSettings
{
    public const string SectionName = "Features:ScheduleSuggestions";
}
