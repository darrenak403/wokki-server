using Amazon;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wokki.Application.Services.Bedrock.Interfaces;

namespace Wokki.Infrastructure.Bedrock;

public static class BedrockServiceCollectionExtensions
{
    public static IServiceCollection AddBedrock(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AwsSettings>(configuration.GetSection(AwsSettings.SectionName));
        services.Configure<BedrockSettings>(configuration.GetSection(BedrockSettings.SectionName));

        services.AddSingleton<IAmazonBedrockRuntime>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var bedrock = sp.GetRequiredService<IOptions<BedrockSettings>>().Value;
            var aws = sp.GetRequiredService<IOptions<AwsSettings>>().Value;
            var regionName = BedrockRegionResolver.Resolve(bedrock, aws);
            var credentials = AwsCredentialsFactory.Resolve(configuration, aws);

            return new AmazonBedrockRuntimeClient(
                credentials,
                new AmazonBedrockRuntimeConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(regionName)
                });
        });

        services.AddScoped<IBedrockService, BedrockService>();
        return services;
    }
}
