using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using sreagent.plugins;

namespace sreagent;

public class AvailabilityDiagnosticAgent : DiagnosticAgent
{
    private readonly Kernel _kernel;

    public AvailabilityDiagnosticAgent(
        IKernelBuilder kernelBuilder,
        AcaAvailabilityPluginDefinition acaAvailabilityPluginDefinition,
        ILogger<NetworkingDiagnosticAgent> logger)
        : base(logger)
    {        
        _kernel = kernelBuilder.Build();
        _kernel.Plugins.AddFromObject(acaAvailabilityPluginDefinition, "availability");
    }

    public override Kernel KernelWithTools => _kernel;

    public override string Specialization => "availability";

    public override string GetSpecializedPrompt()
    {
        return """
<message role="system">
You are a specialized Azure Container Apps availability diagnostic agent. Your job is to diagnose availability issues with Azure Container Apps applications.

Current conversation state:
{{$conversationState}}

Focus on these common availability issues:
1. High CPU or memory usage makes the app unresponsive
2. High request count makes the app unresponsive
3. Image pull failures in the logs result in the latest revision unable to activate

If you need more information, ask the user specific questions.
If you need to run a diagnostic tool, use the appropriate function.

If you've identified the issue, respond with a line starting with DIAGNOSIS: followed by a brief description of the issue.
Example: DIAGNOSIS: Image pull failure due to incorrect credentials

After any tool usage or diagnosis, provide a clear explanation to the user.
</message>
<message role="user">
{{$userInput}}
</message>
""";
    }
}
