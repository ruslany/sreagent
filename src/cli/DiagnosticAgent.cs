using Microsoft.SemanticKernel;

namespace sreagent;

public class DiagnosticAgent
{
    private string _specialization;
    private Kernel _kernel;
    private AgentMemory _memory;
    private List<Tool> _diagnosticTools;

    public DiagnosticAgent(string specialization, AgentMemory memory)
    {
        _specialization = specialization;
        _memory = memory;
        _diagnosticTools = new List<Tool>();
    }

    public async Task InitializeAsync()
    {
        // Initialize kernel with specialized prompt and tools for this diagnostic domain
        _kernel = new KernelBuilder()
            .WithAzureOpenAIChatCompletionService(
                "gpt-4-turbo",
                "YOUR_AZURE_ENDPOINT",
                "YOUR_AZURE_API_KEY")
            .Build();

        // Load domain-specific tools
        await LoadDiagnosticToolsAsync();

        // Add specialized diagnostic prompt
        var promptConfig = new PromptTemplateConfig
        {
            Template = GetSpecializedPrompt(),
            InputVariables = new List<string> { "input", "conversationState", "toolResults" }
        };

        // Register the function
        var diagnosticFunction = _kernel.CreateFunctionFromPrompt(promptConfig);
        _kernel.Functions.AddFunction("Diagnose", diagnosticFunction);

        // Register domain-specific tools with the kernel
        RegisterTools();
    }

    private string GetSpecializedPrompt()
    {
        // Return specialized prompt based on agent type
        switch (_specialization)
        {
            case "networking":
                return @"
You are a specialized Azure networking diagnostic agent. Your job is to diagnose networking issues with Azure applications.

Current conversation state: {{$conversationState}}
User query: {{$input}}
Tool results: {{$toolResults}}

Focus on these common networking issues:
1. NSG rules blocking traffic
2. DNS resolution issues
3. Connectivity between services
4. Load balancer configuration issues

If you need more information, ask the user specific questions.
If you have enough information, use available tools to diagnose the issue.
If you've identified the issue, provide a clear diagnosis and recommend next steps.

Response:";

            case "database":
                // Database-specific prompt
                return "..."; // Database diagnostic prompt

            // Add other specializations
            default:
                return "..."; // Generic diagnostic prompt
        }
    }

    private async Task LoadDiagnosticToolsAsync()
    {
        // Load only the tools relevant to this diagnostic agent's specialization
        switch (_specialization)
        {
            case "networking":
                _diagnosticTools.Add(new Tool("CheckNsgRules", "Checks NSG rules for the specified resource"));
                _diagnosticTools.Add(new Tool("TestConnectivity", "Tests connectivity between two endpoints"));
                _diagnosticTools.Add(new Tool("ResolveDns", "Performs DNS resolution for a hostname"));
                // Add more networking-specific tools
                break;

            case "database":
                _diagnosticTools.Add(new Tool("CheckDatabaseConnectivity", "Tests connection to the database"));
                _diagnosticTools.Add(new Tool("AnalyzeQueryPerformance", "Analyzes performance of database queries"));
                // Add more database-specific tools
                break;

                // Add other specializations
        }
    }

    private void RegisterTools()
    {
        foreach (var tool in _diagnosticTools)
        {
            // Register each tool with the kernel
            // In a real implementation, this would create native functions or tool functions
            // For simplicity, we're just illustrating the concept here
        }
    }

    public async Task<string> DiagnoseAsync(string userInput, ConversationState conversationState)
    {
        try
        {
            // Start with empty tool results
            string toolResults = "";

            // Process user input and determine if we need to run any tools
            var arguments = new KernelArguments
            {
                ["input"] = userInput,
                ["conversationState"] = conversationState.GetFormattedState(),
                ["toolResults"] = toolResults
            };

            // Run the diagnostic function
            var result = await _kernel.InvokeAsync("Diagnose", arguments);
            string response = result.GetValue<string>();

            // Add to conversation state
            conversationState.AddAgentMessage(response);

            // Return the diagnostic response
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in diagnostic agent: {ex.Message}");
            return "I encountered a problem while diagnosing your issue. Could you provide more details about your Azure application?";
        }
    }
}
