using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace sreagent;

public static class ServicBuilder
{
    public static IConfigurationBuilder AddConfigurationSources(this IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Sources.Clear();

        _ = configurationBuilder
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        return configurationBuilder;
    }

    public static IServiceCollection AddAzureOpenAIOptions(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        // AppSettings IOptions configuration
        _ = serviceCollection.AddOptions<AzureOpenAISettings>()
            .Bind(configuration.GetSection(nameof(AzureOpenAISettings)));

        return serviceCollection;
    }

    public static IServiceCollection AddSemanticKernelChatCompletion(this IServiceCollection serviceCollection)
    {
        _ = serviceCollection.AddSingleton(sp =>
        {
            AzureOpenAISettings settings = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;

            ArgumentNullException.ThrowIfNull(settings.DeploymentName, nameof(settings.DeploymentName));
            ArgumentNullException.ThrowIfNull(settings.Endpoint, nameof(settings.Endpoint));
            ArgumentNullException.ThrowIfNull(settings.ApiKey, nameof(settings.ApiKey));

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging(configure => configure.AddConsole());
            kernelBuilder.Services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Information));

            var kernel = kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: settings.DeploymentName,
                endpoint: settings.Endpoint,
                apiKey: settings.ApiKey)
            .Build();

            return kernel;
        });

        return serviceCollection;
    }
}
