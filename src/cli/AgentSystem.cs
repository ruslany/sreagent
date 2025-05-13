namespace sreagent;

public class AgentSystem
{
    private CoordinatorAgent _coordinator;
    private Dictionary<string, DiagnosticAgent> _diagnosticAgents;
    private Dictionary<string, MitigationAgent> _mitigationAgents;
    private AgentMemory _sharedMemory;

    public async Task InitializeAsync()
    {
        // Initialize shared memory/knowledge base
        _sharedMemory = new AgentMemory();
        await _sharedMemory.InitializeAsync();

        // Create specialized diagnostic agents
        _diagnosticAgents = new Dictionary<string, DiagnosticAgent>
        {
            ["networking"] = new DiagnosticAgent("networking", _sharedMemory),
            ["database"] = new DiagnosticAgent("database", _sharedMemory),
            ["authentication"] = new DiagnosticAgent("authentication", _sharedMemory),
            ["performance"] = new DiagnosticAgent("performance", _sharedMemory),
            // Add more specialized diagnostic agents as needed
        };

        // Create specialized mitigation agents
        _mitigationAgents = new Dictionary<string, MitigationAgent>
        {
            ["networking"] = new MitigationAgent("networking", _sharedMemory),
            ["database"] = new MitigationAgent("database", _sharedMemory),
            ["authentication"] = new MitigationAgent("authentication", _sharedMemory),
            ["performance"] = new MitigationAgent("performance", _sharedMemory),
            // Add more specialized mitigation agents as needed
        };

        // Initialize coordinator agent last (needs references to other agents)
        _coordinator = new CoordinatorAgent(_diagnosticAgents, _mitigationAgents, _sharedMemory);

        // Initialize each agent (load prompts, tools, etc.)
        await _coordinator.InitializeAsync();

        foreach (var agent in _diagnosticAgents.Values)
            await agent.InitializeAsync();

        foreach (var agent in _mitigationAgents.Values)
            await agent.InitializeAsync();
    }

    public async Task StartConversationAsync()
    {
        Console.WriteLine("Azure Support Agent: How can I help you with your application today?");

        while (true)
        {
            Console.Write("\nYou: ");
            string userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "exit")
                break;

            string response = await _coordinator.ProcessUserInputAsync(userInput);
            Console.WriteLine($"\nAzure Support Agent: {response}");
        }
    }
}
