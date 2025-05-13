using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sreagent;

var config = new ConfigurationBuilder()
    .AddConfigurationSources()
    .Build();

var serviceCollection = new ServiceCollection()
    .AddLogging(configure => configure.AddConsole())
    .AddSingleton<IConfiguration>(config)
    .AddAzureOpenAIOptions(config)
    .AddSemanticKernelChatCompletion()
    .AddSingleton<CoordinatorAgent>()
    .AddSingleton<ConversationService>();

serviceCollection.AddPlugins();
serviceCollection.AddAgents();

serviceCollection.AddSingleton<ConversationService>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var conversationService = serviceProvider.GetRequiredService<ConversationService>();

await conversationService.StartConversationAsync();

Console.WriteLine("\nPress any key to exit...");

Console.ReadLine();
