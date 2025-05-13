public class MitigationAgentFactory
{
    private readonly IPromptExecutionService _promptService;
    private readonly ToolRegistry _toolRegistry;
    private readonly AgentMemory _memory;
    private readonly ILogger<MitigationAgentFactory> _logger;
    private readonly Dictionary<string, MitigationAgent> _agents = new();

    public MitigationAgentFactory(
        IPromptExecutionService promptService,
        ToolRegistry toolRegistry,
        AgentMemory memory,
        ILogger<MitigationAgentFactory> logger)
    {
        _promptService = promptService;
        _toolRegistry = toolRegistry;
        _memory = memory;
        _logger = logger;
    }

    public MitigationAgent GetAgent(string specialization)
    {
        if (!_agents.TryGetValue(specialization, out var agent))
        {
            _logger.LogInformation("Creating new mitigation agent for {Specialization}", specialization);

            agent = new MitigationAgent(
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
