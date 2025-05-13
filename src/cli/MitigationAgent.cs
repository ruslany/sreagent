namespace sreagent;

public class MitigationAgent
{
    private readonly string _specialization;
    private readonly IPromptExecutionService _promptService;
    private readonly List<ITool> _tools;
    private readonly AgentMemory _memory;
    private readonly ILogger _logger;

    public MitigationAgent(
        string specialization,
        IPromptExecutionService promptService,
        List<ITool> tools,
        AgentMemory memory,
        ILogger logger)
    {
        _specialization = specialization;
        _promptService = promptService;
        _tools = tools;
        _memory = memory;
        _logger = logger;
    }

    public async Task<string> MitigateAsync(string userInput, ConversationState conversationState)
    {
        _logger.LogInformation("Mitigating issue in {Specialization} category", _specialization);

        // Get the diagnosis result from the conversation state
        string diagnosisResult = conversationState.GetDiagnosisResult();

        // Get available tools for mitigation
        string toolsText = string.Join("\n", _tools.Select(t => $"- {t.Name}: {t.Description}"));

        // Build the specialized prompt for mitigation
        string prompt = GetSpecializedPrompt(toolsText);

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
            ["diagnosisResult"] = diagnosisResult,
            ["toolResults"] = string.Empty
        };

        // First pass: determine mitigation plan
        var initialResponse = await _promptService.ExecutePromptAsync(
            prompt,
            parameters,
            promptOptions);

        string response = initialResponse.Completion;

        // Check if the agent wants to use a tool
        if (response.Contains("USE_TOOL:"))
        {
            _logger.LogInformation("Mitigation agent wants to use a tool");

            // Parse the tool request
            string toolRequestLine = response.Split("\n")
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

        // Check if we've completed the mitigation
        if (response.Contains("MITIGATION_COMPLETE:"))
        {
            string mitigationLine = response.Split("\n")
                .FirstOrDefault(line => line.StartsWith("MITIGATION_COMPLETE:"));

            if (mitigationLine != null)
            {
                string mitigationSummary = mitigationLine.Substring("MITIGATION_COMPLETE:".Length).Trim();
                conversationState.SetMitigationResult(mitigationSummary);
            }
        }

        // Clean up the response before returning it to the user
        response = CleanResponse(response);

        return response;
    }

    private string GetSpecializedPrompt(string tools)
    {
        switch (_specialization)
        {
            case "networking":
                return @$"
You are a specialized Azure networking mitigation agent. Your job is to fix networking issues with Azure applications.

Current conversation state:
{{$conversationState}}

Diagnosis result: {{$diagnosisResult}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Available tools:
{tools}

Based on the diagnosis, determine the best way to fix the networking issue:
1. If NSG rules are blocking traffic, use UpdateNsgRule or CreateNsgRule tools
2. If DNS resolution is failing, use UpdateDnsRecord or CreateDnsRecord tools
3. If services can't connect, recommend appropriate connectivity solutions
4. If load balancer is misconfigured, provide configuration fixes

If you need to execute a fix, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: UpdateNsgRule resource-group-name nsg-name rule-name allow tcp 443

Present options to the user before making significant changes.
Provide clear explanations for recommended actions.

If you've completed the mitigation, respond with a line starting with MITIGATION_COMPLETE: followed by a brief summary.
Example: MITIGATION_COMPLETE: Updated NSG rule to allow inbound HTTPS traffic

Response:";

            case "database":
                return @$"
You are a specialized Azure database mitigation agent. Your job is to fix database issues with Azure applications.

Current conversation state:
{{$conversationState}}

Diagnosis result: {{$diagnosisResult}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Available tools:
{tools}

Based on the diagnosis, determine the best way to fix the database issue:
1. If database is experiencing high CPU/memory, recommend scaling options
2. If firewall rules are blocking connections, use UpdateFirewallRule tool
3. If queries are slow, suggest index optimizations or query modifications
4. If connection string is incorrect, provide correct format

If you need to execute a fix, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: UpdateDatabaseTier resource-group-name server-name database-name S1

Present options to the user before making significant changes.
Provide clear explanations for recommended actions.

If you've completed the mitigation, respond with a line starting with MITIGATION_COMPLETE: followed by a brief summary.
Example: MITIGATION_COMPLETE: Scaled database to S1 tier to address CPU constraints

Response:";

            // More specializations...

            default:
                return @$"
You are a specialized Azure mitigation agent. Your job is to fix issues with Azure applications.

Current conversation state:
{{$conversationState}}

Diagnosis result: {{$diagnosisResult}}

User query: {{$userInput}}

Tool results: {{$toolResults}}

Available tools:
{tools}

Based on the diagnosis, determine the best way to fix the issue.

If you need to execute a fix, respond with a line starting with USE_TOOL: followed by the tool name and arguments.
Example: USE_TOOL: ToolName arg1 arg2

Present options to the user before making significant changes.
Provide clear explanations for recommended actions.

If you've completed the mitigation, respond with a line starting with MITIGATION_COMPLETE: followed by a brief summary.
Example: MITIGATION_COMPLETE: Brief summary of the fix applied

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
        // Remove the special command lines from the response
        var lines = response.Split("\n");
        var cleanedLines = lines.Where(line =>
            !line.StartsWith("USE_TOOL:") &&
            !line.StartsWith("MITIGATION_COMPLETE:"));

        return string.Join("\n", cleanedLines);
    }
}
