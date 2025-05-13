using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace sreagent;

public abstract class DiagnosticAgent
{
    private readonly ILogger<DiagnosticAgent> _logger;

    public DiagnosticAgent(
        ILogger<DiagnosticAgent> logger)
    {        
        _logger = logger;
    }

    public abstract Kernel KernelWithTools { get; }

    public abstract string Specialization { get; }

    public abstract string GetSpecializedPrompt();

    public async Task<string> DiagnoseAsync(string userInput, ConversationState conversationState)
    {
        _logger.LogInformation("Starting diagnosis for user input: {UserInput}", userInput);

        // Build the specialized prompt
        string prompt = GetSpecializedPrompt();
        _logger.LogDebug("Using specialized prompt: {Prompt}", prompt);

        // Set execution settings with automatic function invocation
        var executionSettings = new PromptExecutionSettings
        {
            ModelId = "gpt-4o",
            // Enable automatic function invocation
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var arguments = new KernelArguments(executionSettings)
        {
            { "conversationState", conversationState.GetFormattedState() },
            { "userInput", userInput }
        };

        _logger.LogDebug("Invoking LLM with arguments: {Arguments}", arguments);

        // Invoke the LLM with automatic function calling
        var result = await KernelWithTools.InvokePromptAsync(
            prompt,
            arguments);

        string response = result.ToString();
        _logger.LogDebug("Received response from LLM: {Response}", response);

        // Check if we've identified the issue
        if (response.Contains("DIAGNOSIS:"))
        {
            string? diagnosisLine = response.Split("\n")
                .FirstOrDefault(line => line.StartsWith("DIAGNOSIS:"));

            if (diagnosisLine != null)
            {
                string diagnosis = diagnosisLine.Substring("DIAGNOSIS:".Length).Trim();
                conversationState.SetDiagnosisResult(diagnosis);
                _logger.LogInformation("Diagnosis identified: {Diagnosis}", diagnosis);
            }
        }

        // Clean up the response before returning it to the user
        response = CleanResponse(response);
        _logger.LogDebug("Cleaned response: {CleanedResponse}", response);

        _logger.LogInformation("Diagnosis process completed.");
        return response;
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
