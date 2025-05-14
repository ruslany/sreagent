using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using sreagent.plugins;

namespace sreagent;

public class NetworkingDiagnosticAgent : DiagnosticAgent
{
    private readonly Kernel _kernel;

    public NetworkingDiagnosticAgent(
        IKernelBuilder kernelBuilder,
        NsgRulePluginDefinition nsgRulePluginDefinition,
        ILogger<NetworkingDiagnosticAgent> logger)
        : base(logger)
    {
        _kernel = kernelBuilder.Build();
        _kernel.Plugins.AddFromObject(nsgRulePluginDefinition, "networking");
    }

    public override Kernel KernelWithTools => _kernel;

    public override string Specialization => "networking";

    public override string GetSpecializedPrompt()
    {
        return """
<message role="system">
You are a specialized Azure networking diagnostic agent. Your job is to diagnose networking issues with Azure applications.

Current conversation state:
{{$conversationState}}

Focus on these common networking issues:
1. NSG rules blocking traffic
2. DNS resolution issues
3. Connectivity between services
4. Load balancer configuration issues
5. Virtual network configuration
6. Public IP and private IP address issues

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, use the appropriate function.

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: NSG rule blocking port 443 traffic to the web tier

After any tool usage or diagnosis, provide a clear explanation to the user.
</message>
<message role="user">
{{$userInput}}
</message>
""";
    }
}
