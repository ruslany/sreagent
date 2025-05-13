using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace sreagent;

public class DiagnosticAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly ToolRegistry _toolRegistry;
    private readonly AgentMemory _memory;
    private readonly ILogger<DiagnosticAgentFactory> _logger;
    private readonly Dictionary<string, DiagnosticAgent> _agents = new();

    public DiagnosticAgentFactory(
        IChatClient chatClient,
        ToolRegistry toolRegistry,
        AgentMemory memory,
        ILogger<DiagnosticAgentFactory> logger)
    {
        _chatClient = chatClient;
        _toolRegistry = toolRegistry;
        _memory = memory;
        _logger = logger;
    }

    public DiagnosticAgent GetAgent(string specialization)
    {
        if (!_agents.TryGetValue(specialization, out var agent))
        {
            _logger.LogInformation("Creating new diagnostic agent for {Specialization}", specialization);

            agent = new DiagnosticAgent(
                specialization,
                _promptService,
                _toolRegistry.GetToolsForCategory(specialization),
                _memory,
                _logger);

            _agents[specialization] = agent;
        }

        return agent;
    }
}
