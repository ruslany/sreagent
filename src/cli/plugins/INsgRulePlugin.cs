using Azure.ResourceManager.Network;

namespace sreagent.plugins;

public interface INsgRulePlugin
{
    Task<IDictionary<string, IReadOnlyList<SecurityRuleData>>> GetNSGRulesAsync(string nsgResourceId);

    Task<bool> CreateOrUpdateNSGRuleAsync(string nsgResourceId, SecurityRuleData rule);

    Task<bool> RemoveNSGRuleAsync(string nsgResourceId, string ruleName);
}
