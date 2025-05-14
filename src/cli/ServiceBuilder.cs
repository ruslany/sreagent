using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using sreagent.plugins;

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
        _ = serviceCollection.AddSingleton<IKernelBuilder>(sp =>
        {
            AzureOpenAISettings settings = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;

            ArgumentNullException.ThrowIfNull(settings.DeploymentName, nameof(settings.DeploymentName));
            ArgumentNullException.ThrowIfNull(settings.Endpoint, nameof(settings.Endpoint));
            ArgumentNullException.ThrowIfNull(settings.ApiKey, nameof(settings.ApiKey));

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging(configure => configure.AddConsole());
            kernelBuilder.Services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Warning));

            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: settings.DeploymentName,
                endpoint: settings.Endpoint,
                apiKey: settings.ApiKey);

            return kernelBuilder;
        });

        return serviceCollection;
    }

    public static IServiceCollection AddPlugins(this IServiceCollection serviceCollection)
    {
        _ = serviceCollection.AddSingleton<IAcaAvailabilityPlugin, AcaAvailabilityPlugin>();
        _ = serviceCollection.AddSingleton<AcaAvailabilityPluginDefinition>();
        _ = serviceCollection.AddSingleton<INsgRulePlugin, NsgRulePlugin>();
        _ = serviceCollection.AddSingleton<NsgRulePluginDefinition>();

        return serviceCollection;
    }

    public static IServiceCollection AddAgents(this IServiceCollection serviceCollection)
    {
        _ = serviceCollection.AddSingleton<CoordinatorAgent>();
        _ = serviceCollection.AddSingleton<AvailabilityDiagnosticAgent>();
        _ = serviceCollection.AddSingleton<NetworkingDiagnosticAgent>();

        _ = serviceCollection.AddSingleton<IDictionary<string, DiagnosticAgent>>(sp =>
        {
            var agents = new Dictionary<string, DiagnosticAgent>
            {
                { "availability", sp.GetRequiredService<AvailabilityDiagnosticAgent>() },
                { "networking", sp.GetRequiredService<NetworkingDiagnosticAgent>() }
            };

            return agents;
        });

        return serviceCollection;
    }
}
