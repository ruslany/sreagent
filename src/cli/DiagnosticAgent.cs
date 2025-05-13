using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace sreagent;

public class DiagnosticAgent
{
    private readonly string _specialization;
    private readonly IChatClient _chatClient;
    private readonly List<ITool> _tools;
    private readonly AgentMemory _memory;
    private readonly ILogger<DiagnosticAgent> _logger;

    public DiagnosticAgent(
        string specialization,
        IChatClient chatClient,
        List<ITool> tools,
        AgentMemory memory,
        ILogger<DiagnosticAgent> logger)
    {
        _specialization = specialization;
        _chatClient = chatClient;
        _tools = tools;
        _memory = memory;
        _logger = logger;
    }

    public async Task<string> DiagnoseAsync(string userInput, ConversationState conversationState)
    {
        _logger.LogInformation("Diagnosing issue in {Specialization} category", _specialization);

        // Get relevant patterns from memory
        var patterns = await _memory.SearchPatternsAsync(_specialization);
        string patternsText = string.Join("\n", patterns.Select(p => $"- {p}"));

        // Get available tools
        string toolsText = string.Join("\n", _tools.Select(t => $"- {t.Name}: {t.Description}"));

        // Build the specialized prompt
        string prompt = GetSpecializedPrompt(patternsText, toolsText);

        var promptOptions = new PromptExecutionOptions
        {
            Model = "gpt-4-turbo",
            Temperature = 0.2,
            MaxTokens = 1500
        };

        var parameters = new Dictionary<string, string>
        {
            ["conversationState"] = conversationState.GetFormattedState(),
            ["userInput"] = userInput,
            ["toolResults"] = string.Empty
        };

        // First pass: determine if we need to use any tools
        var initialResponse = await _promptService.ExecutePromptAsync(
            prompt,
            parameters,
            promptOptions);

        string response = initialResponse.Completion;

        // Check if the agent wants to use a tool
        if (response.Contains("USE_TOOL:"))
        {
            _logger.LogInformation("Diagnostic agent wants to use a tool");

            // Parse the tool request
            string? toolRequestLine = response.Split("\n")
                .FirstOrDefault(line => line.StartsWith("USE_TOOL:"));

            if (toolRequestLine != null)
            {
                string[] parts = toolRequestLine.Substring("USE_TOOL:".Length).Trim().Split(' ', 2);

                if (parts.Length >= 1)
                {
                    string toolName = parts[0];
                    string toolArgs = parts.Length > 1 ? parts[1] : string.Empty;

                    // Execute the tool
                    string toolResult = await ExecuteToolAsync(toolName, toolArgs);

                    // Update the prompt with tool results and get final response
                    parameters["toolResults"] = toolResult;

                    var finalResponse = await _promptService.ExecutePromptAsync(
                        prompt,
                        parameters,
                        promptOptions);

                    response = finalResponse.Completion;
                }
            }
        }

        // Check if we've identified the issue
        if (response.Contains("DIAGNOSIS:"))
        {
            string? diagnosisLine = response.Split("\n")
                .FirstOrDefault(line => line.StartsWith("DIAGNOSIS:"));

            if (diagnosisLine != null)
            {
                string diagnosis = diagnosisLine.Substring("DIAGNOSIS:".Length).Trim();
                conversationState.SetDiagnosisResult(diagnosis);
            }
        }

        // Clean up the response before returning it to the user
        response = CleanResponse(response);

        return response;
    }

    private string GetSpecializedPrompt(string patterns, string tools)
    {
        switch (_specialization)
        {
            case "networking":
                return @$"
You are a specialized Azure networking diagnostic agent. Your job is to diagnose networking issues with Azure applications.

Current conversation state:
{{$conversationState}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Common Azure networking troubleshooting patterns:
{patterns}

Available tools:
{tools}

Focus on these common networking issues:
1. NSG rules blocking traffic
2. DNS resolution issues
3. Connectivity between services
4. Load balancer configuration issues
5. Virtual network configuration
6. Public IP and private IP address issues

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: CheckNsgRules resource-group-name nsg-name

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: NSG rule blocking port 443 traffic to the web tier

After any tool usage or diagnosis, provide a clear explanation to the user.

Response:";

            case "database":
                return @$"
You are a specialized Azure database diagnostic agent. Your job is to diagnose database issues with Azure applications.

Current conversation state:
{{$conversationState}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Common Azure database troubleshooting patterns:
{patterns}

Available tools:
{tools}

Focus on these common database issues:
1. Connection string problems
2. Firewall rules blocking connections
3. Query timeouts and performance issues
4. Database capacity and scaling
5. High CPU or memory usage
6. Authentication and permission issues

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: CheckDatabaseConnectivity server-name database-name

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: Database CPU utilization at 100% causing query timeouts

After any tool usage or diagnosis, provide a clear explanation to the user.

Response:";

            case "authentication":
                return @$"
You are a specialized Azure authentication diagnostic agent. Your job is to diagnose authentication and authorization issues with Azure applications.

Current conversation state:
{{$conversationState}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Common Azure authentication troubleshooting patterns:
{patterns}

Available tools:
{tools}

Focus on these common authentication issues:
1. Azure AD integration problems
2. Token acquisition failures
3. CORS configuration issues
4. Service principal problems
5. Managed identity configuration
6. RBAC permission issues

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: CheckServicePrincipal app-id

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: Service principal missing required permissions for Key Vault access

After any tool usage or diagnosis, provide a clear explanation to the user.

Response:";

            case "performance":
                return @$"
You are a specialized Azure performance diagnostic agent. Your job is to diagnose performance issues with Azure applications.

Current conversation state:
{{$conversationState}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Common Azure performance troubleshooting patterns:
{patterns}

Available tools:
{tools}

Focus on these common performance issues:
1. App Service plan scaling and limitations
2. High CPU or memory usage
3. Slow database queries
4. Network latency issues
5. Cache configuration
6. Resource contention

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: CheckAppServiceMetrics resource-group-name app-service-name

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: App Service hitting memory limits causing frequent application restarts

After any tool usage or diagnosis, provide a clear explanation to the user.

Response:";

            default:
                return @$"
You are a specialized Azure diagnostic agent. Your job is to diagnose issues with Azure applications.

Current conversation state:
{{$conversationState}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Common Azure troubleshooting patterns:
{patterns}

Available tools:
{tools}

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: ToolName arg1 arg2

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: Brief description of the issue

After any tool usage or diagnosis, provide a clear explanation to the user.

Response:";
        }
    }

    private async Task<string> ExecuteToolAsync(string toolName, string arguments)
    {
        try
        {
            _logger.LogInformation("Executing tool {ToolName} with arguments {Arguments}", toolName, arguments);

            var tool = _tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                return $"ERROR: Tool '{toolName}' not found. Available tools: {string.Join(", ", _tools.Select(t => t.Name))}";
            }

            return await tool.ExecuteAsync(arguments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return $"ERROR: Failed to execute tool '{toolName}': {ex.Message}";
        }
    }

    private string CleanResponse(string response)
    {
        // Remove the USE_TOOL and DIAGNOSIS lines from the response
        var lines = response.Split("\n");
        var cleanedLines = lines.Where(line =>
            !line.StartsWith("USE_TOOL:") &&
            !line.StartsWith("DIAGNOSIS:"));

        return string.Join("\n", cleanedLines);
    }
}
