using Microsoft.Extensions.Logging;

namespace sreagent;

public class ConversationService(
    CoordinatorAgent coordinator,
    ILogger<ConversationService> logger)
{
    private readonly CoordinatorAgent _coordinator = coordinator;
    private readonly ILogger<ConversationService> _logger = logger;
    private readonly ConversationState _conversationState = new();

    public async Task StartConversationAsync()
    {
        _logger.LogInformation("Starting conversation with user");
        Console.WriteLine("Azure Support Agent: How can I help you with your application today?");

        while (true)
        {
            Console.Write("\nYou: ");
            string? userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput) || userInput.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                break;

            _conversationState.AddUserMessage(userInput);

            try
            {
                string response = await _coordinator.ProcessUserInputAsync(userInput, _conversationState);
                Console.WriteLine($"\nAzure Support Agent: {response}");
                _conversationState.AddAgentMessage(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user input");
                Console.WriteLine("\nAzure Support Agent: I apologize, but I encountered an error processing your request. Could you please try again or rephrase your question?");
            }
        }
    }
}
