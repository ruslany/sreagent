using sreagent;

public class Program
    {
        static async Task Main(string[] args)
        {
            var agentSystem = new AgentSystem();
            await agentSystem.InitializeAsync();
            
            // Start conversation with user
            await agentSystem.StartConversationAsync();
        }
    }