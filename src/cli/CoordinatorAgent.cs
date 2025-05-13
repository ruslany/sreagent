using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace sreagent;

public class CoordinatorAgent
{
    private readonly Kernel _kernel;
    private readonly ILogger<CoordinatorAgent> _logger;

    private readonly IDictionary<string, DiagnosticAgent> _diagnosticAgents;

    public CoordinatorAgent(
        IKernelBuilder kernelBuilder, 
        IDictionary<string, DiagnosticAgent> diagnosticAgents,
        ILogger<CoordinatorAgent> logger)
    {
        _logger = logger;
        _kernel = kernelBuilder.Build();
        _diagnosticAgents = diagnosticAgents;
    }

    public async Task<string> ProcessUserInputAsync(string userInput, ConversationState conversationState)
    {
        _logger.LogInformation("Processing user input: {UserInput}", userInput);

        // Determine the next action using the coordinator prompt
        var coordinatorPrompt = """
<message role="system">        
You are a coordinator for an Azure Container Apps SRE agent. Your job is to:
1. Understand the user's problem with their Azur Container app
2. Determine which specialized diagnostic agent to use
3. Gather required information from the user
4. Route the conversation to the appropriate specialist agent

Current conversation state:
{{$conversationState}}

Determine the next action:
- If you need more information, ask the user specific questions
- If ready to diagnose, respond with a JSON classification: {""action"": ""diagnose"", ""category"": ""[category]""}
- If already diagnosed and ready to mitigate, respond with: {""action"": ""mitigate"", ""category"": ""[category]""}

Available diagnostic categories: networking, availability
</message>
<message role="user">
{{$userInput}}
</message>
""";

        // Set execution settings
        var executionSettings = new PromptExecutionSettings
        {
            ModelId = "gpt-4o",
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        };

        var arguments = new KernelArguments(executionSettings)
        {
            { "userInput", userInput },
            { "conversationState", conversationState.GetFormattedState() }
        };

        _logger.LogInformation("Conversation state: {ConversationState}", conversationState.GetFormattedState());

        try
        {
            // Get the chat completion
            var result = await _kernel.InvokePromptAsync(
                coordinatorPrompt,
                arguments);

            string response = result.ToString();

            // Check if we need to route to a specialist agent
            if (response.Contains("\"action\"") && response.Contains("\"category\""))
            {
                try
                {
                    var routingInfo = ExtractRoutingInfo(response);

                    if (routingInfo != null)
                    {
                        string action = routingInfo.Action;
                        string category = routingInfo.Category;

                        _logger.LogInformation("Routing to {Action} agent for {Category}", action, category);

                        if (action == "diagnose")
                        {
                            // Route to appropriate diagnostic agent
                            var diagnosticAgent = _diagnosticAgents[category];
                            conversationState.SetCurrentPhase("diagnosis");
                            conversationState.SetCurrentCategory(category);

                            return await diagnosticAgent.DiagnoseAsync(userInput, conversationState);
                        }
                        else if (action == "mitigate")
                        {
                            // Route to appropriate mitigation agent
                            //var mitigationAgent = _mitigationAgentFactory.GetAgent(category);
                            conversationState.SetCurrentPhase("mitigation");

                            return "Mitigating issue in " + category + " category...";
                            //return await mitigationAgent.MitigateAsync(userInput, conversationState);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing routing information");
                }
            }

            // If no routing is needed or if routing failed, return the coordinator response
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion: {ErrorMessage}", ex.Message);
            return "I'm sorry, I encountered an error processing your request. Please try again.";
        }
    }

    private RoutingInfo? ExtractRoutingInfo(string response)
    {
        try
        {
            // Find JSON object in the response
            int startIndex = response.IndexOf('{');
            int endIndex = response.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                string jsonStr = response.Substring(startIndex, endIndex - startIndex + 1);
                var routingInfo = JsonSerializer.Deserialize<RoutingInfo>(jsonStr);
                return routingInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse routing JSON");
        }

        return null;
    }

    private record RoutingInfo(
        [property: JsonPropertyName("action")] string Action,
        [property: JsonPropertyName("category")] string Category);
}
