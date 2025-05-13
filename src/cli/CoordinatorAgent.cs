using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace sreagent;

public class CoordinatorAgent(
    IChatClient chatClient,
    DiagnosticAgentFactory diagnosticAgentFactory,
    MitigationAgentFactory mitigationAgentFactory,
    AgentMemory memory,
    ILogger<CoordinatorAgent> logger)
{
    private readonly IChatClient _chatClient = chatClient;
    private readonly DiagnosticAgentFactory _diagnosticAgentFactory = diagnosticAgentFactory;
    private readonly MitigationAgentFactory _mitigationAgentFactory = mitigationAgentFactory;
    private readonly AgentMemory _memory = memory;
    private readonly ILogger<CoordinatorAgent> _logger = logger;

    public async Task<string> ProcessUserInputAsync(string userInput, ConversationState conversationState)
    {
        _logger.LogInformation("Processing user input: {UserInput}", userInput);

        // Determine the next action using the coordinator prompt
        var coordinatorPrompt = @"
You are a coordinator for an Azure support system. Your job is to:
1. Understand the user's problem with their Azure application
2. Determine which specialized diagnostic agent to use
3. Gather required information from the user
4. Route the conversation to the appropriate specialist agent

Current conversation state:
{{$conversationState}}

User query: {{$userInput}}

Determine the next action:
- If you need more information, ask the user specific questions
- If ready to diagnose, respond with a JSON classification: {""action"": ""diagnose"", ""category"": ""[category]""}
- If already diagnosed and ready to mitigate, respond with: {""action"": ""mitigate"", ""category"": ""[category]""}

Available diagnostic categories: networking, database, authentication, performance

Response:";

        var chatOptions = new ChatOptions
        {
            ModelId = "gpt-4o",
            Temperature = (float?)0.2,
            MaxOutputTokens = 2000
        };

        var parameters = new Dictionary<string, string>
        {
            ["conversationState"] = conversationState.GetFormattedState(),
            ["userInput"] = userInput
        };

        var coordinatorResponse = await _chatClient.GetResponseAsync(
            ProcessPromptParameters(coordinatorPrompt, parameters),
            chatOptions);

        string response = coordinatorResponse.Text;

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
                        var diagnosticAgent = _diagnosticAgentFactory.GetAgent(category);
                        conversationState.SetCurrentPhase("diagnosis");
                        conversationState.SetCurrentCategory(category);

                        return await diagnosticAgent.DiagnoseAsync(userInput, conversationState);
                    }
                    else if (action == "mitigate")
                    {
                        // Route to appropriate mitigation agent
                        var mitigationAgent = _mitigationAgentFactory.GetAgent(category);
                        conversationState.SetCurrentPhase("mitigation");

                        return await mitigationAgent.MitigateAsync(userInput, conversationState);
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

    private string ProcessPromptParameters(string prompt, Dictionary<string, string> parameters)
    {
        foreach (var kvp in parameters)
        {
            prompt = prompt.Replace($"{{$kvp.Key}}", kvp.Value);
        }
        return prompt;
    }

    private class RoutingInfo
    {
        public required string Action { get; set; }
        public required string Category { get; set; }
    }
}
