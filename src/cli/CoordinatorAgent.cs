using Microsoft.SemanticKernel;

namespace sreagent;

public class CoordinatorAgent
{
    private Kernel _kernel;
    private Dictionary<string, DiagnosticAgent> _diagnosticAgents;
    private Dictionary<string, MitigationAgent> _mitigationAgents;
    private AgentMemory _memory;
    private ConversationState _conversationState;

    public CoordinatorAgent(
        Dictionary<string, DiagnosticAgent> diagnosticAgents,
        Dictionary<string, MitigationAgent> mitigationAgents,
        AgentMemory memory)
    {
        _diagnosticAgents = diagnosticAgents;
        _mitigationAgents = mitigationAgents;
        _memory = memory;
        _conversationState = new ConversationState();
    }

    public async Task InitializeAsync()
    {
        // Setup kernel with minimal tools needed for coordination
        _kernel = new KernelBuilder()
            .WithAzureOpenAIChatCompletionService(
                "gpt-4-turbo",
                "YOUR_AZURE_ENDPOINT",
                "YOUR_AZURE_API_KEY")
            .Build();

        // Add coordinator prompt and basic tools
        var promptConfig = new PromptTemplateConfig
        {
            Template = @"
You are a coordinator for an Azure support system. Your job is to:
1. Understand the user's problem with their Azure application
2. Determine which specialized diagnostic agent to use
3. Gather required information from the user
4. Route the conversation to the appropriate specialist agent

Current conversation state: {{$conversationState}}
User query: {{$input}}

Determine the next action:
- If you need more information, ask the user specific questions
- If ready to diagnose, respond with a JSON classification: {""action"": ""diagnose"", ""category"": ""[category]""}
- If already diagnosed and ready to mitigate, respond with: {""action"": ""mitigate"", ""category"": ""[category]""}

Available diagnostic categories: networking, database, authentication, performance

Response:",
            InputVariables = new List<string> { "input", "conversationState" }
        };

        // Add the prompt to the kernel
        var coordinatorFunction = _kernel.CreateFunctionFromPrompt(promptConfig);

        // Register the function
        _kernel.Functions.AddFunction("Coordinator", coordinatorFunction);
    }

    public async Task<string> ProcessUserInputAsync(string userInput)
    {
        try
        {
            // Add the user's input to conversation state
            _conversationState.AddUserMessage(userInput);

            // Get coordinator's response
            var arguments = new KernelArguments
            {
                ["input"] = userInput,
                ["conversationState"] = _conversationState.GetFormattedState()
            };

            var result = await _kernel.InvokeAsync("Coordinator", arguments);
            string coordinatorResponse = result.GetValue<string>();

            // Check if we need to route to a specialist agent
            if (coordinatorResponse.Contains("\"action\""))
            {
                // Parse the JSON response (simplified for example)
                // In real code, use JsonSerializer to parse properly
                string action = ExtractJsonValue(coordinatorResponse, "action");
                string category = ExtractJsonValue(coordinatorResponse, "category");

                if (action == "diagnose")
                {
                    // Route to appropriate diagnostic agent
                    if (_diagnosticAgents.TryGetValue(category, out var agent))
                    {
                        _conversationState.SetCurrentPhase("diagnosis");
                        _conversationState.SetCurrentCategory(category);
                        return await agent.DiagnoseAsync(userInput, _conversationState);
                    }
                }
                else if (action == "mitigate")
                {
                    // Route to appropriate mitigation agent
                    if (_mitigationAgents.TryGetValue(category, out var agent))
                    {
                        _conversationState.SetCurrentPhase("mitigation");
                        return await agent.MitigateAsync(userInput, _conversationState);
                    }
                }
            }

            // If no routing needed, return coordinator's response
            _conversationState.AddAgentMessage(coordinatorResponse);
            return coordinatorResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing input: {ex.Message}");
            return "I encountered an error processing your request. Could you please try rephrasing or provide more details?";
        }
    }

    // Simplified JSON value extraction (use proper JSON parsing in production)
    private string ExtractJsonValue(string json, string key)
    {
        // Very simplified - use System.Text.Json in real code
        int keyIndex = json.IndexOf($"\"{key}\":");
        if (keyIndex >= 0)
        {
            int valueStart = json.IndexOf("\"", keyIndex + key.Length + 3) + 1;
            int valueEnd = json.IndexOf("\"", valueStart);
            return json.Substring(valueStart, valueEnd - valueStart);
        }
        return string.Empty;
    }
}
