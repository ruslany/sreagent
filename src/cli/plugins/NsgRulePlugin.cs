using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Microsoft.Extensions.Logging;

namespace sreagent.plugins;

public class NSGRulePlugin : INSGRulePlugin
{
    private readonly ILogger<NSGRulePlugin> _logger;

    private readonly ArmClient _armClient;

    public NSGRulePlugin(ILogger<NSGRulePlugin> logger)
    {
        _logger = logger;

        _logger.LogInformation("Initializing NSGRulePlugin.");

        var options = new ArmClientOptions
        {
            Diagnostics =
            {
#if DEBUG
                // log request and response content
                IsLoggingContentEnabled = true,
                // don't redact any headers for debugging
                LoggedHeaderNames = {"*"},
                LoggedQueryParameters = {"*"}, 

#else
                IsLoggingContentEnabled = false,
#endif
                IsLoggingEnabled = true,
            },
        };

        var cred = new DefaultAzureCredential();
        _logger.LogInformation("Created DefaultAzureCredential for ArmClient.");
        _armClient = new ArmClient(cred, default, options);
        _logger.LogInformation("ArmClient initialized.");
    }

    public async Task<IDictionary<string, IReadOnlyList<SecurityRuleData>>> GetNSGRulesAsync(string nsgResourceId)
    {
        _logger.LogInformation("GetNSGRulesAsync called with NSG Resource ID: {nsgResourceId}", nsgResourceId);

        if (string.IsNullOrWhiteSpace(nsgResourceId))
        {
            _logger.LogError("Resource ID is null or empty.");
            throw new ArgumentException("Resource ID cannot be null or empty.", nameof(nsgResourceId));
        }

        var result = new Dictionary<string, IReadOnlyList<SecurityRuleData>>()
        {
            { "DefaultSecurityRules", Array.Empty<SecurityRuleData>()},
            { "SecurityRules", Array.Empty<SecurityRuleData>()}
        };

        try
        {
            // Get the NSG resource
            _logger.LogDebug("Attempting to get NetworkSecurityGroupResource for ID: {nsgResourceId}", nsgResourceId);
            var nsgResource = _armClient.GetNetworkSecurityGroupResource(new ResourceIdentifier(nsgResourceId));

            try
            {
                // Check if the NSG exists and get its data
                _logger.LogDebug("Fetching NSG data for resource: {nsgResourceId}", nsgResourceId);
                var nsgData = await nsgResource.GetAsync();

                result["DefaultSecurityRules"] = nsgData.Value.Data.DefaultSecurityRules.ToList();
                result["SecurityRules"] = nsgData.Value.Data.SecurityRules.ToList();

                _logger.LogInformation("Successfully retrieved NSG rules for resource: {nsgResourceId}", nsgResourceId);

                return result;

            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("NSG resource with ID {nsgResourceId} not found.", nsgResourceId);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in getting NSG Rules with resourceId {nsgResourceId}", nsgResourceId);
            return result;
        }
    }

    public async Task<bool> CreateOrUpdateNSGRuleAsync(string nsgResourceId, SecurityRuleData rule)
    {
        _logger.LogInformation("CreateOrUpdateNSGRuleAsync called for NSG Resource ID: {nsgResourceId}, Rule Name: {ruleName}", nsgResourceId, rule?.Name);

        if(string.IsNullOrWhiteSpace(nsgResourceId))
        {
            _logger.LogError("NSG resource ID is null or empty.");
            throw new ArgumentException("NSG resource ID cannot be null or empty.", nameof(nsgResourceId));
        }

        if (rule == null)
        {
            _logger.LogError("Security rule is null.");
            throw new ArgumentNullException(nameof(rule), "Security rule cannot be null.");
        }

        if(string.IsNullOrWhiteSpace(rule.Name))
        {
            _logger.LogError("Rule name is null or empty.");
            throw new ArgumentException("Rule name cannot be null or empty.", nameof(rule.Name));
        }    

        try
        {            
            // Get the NSG resource
            _logger.LogDebug("Attempting to get NetworkSecurityGroupResource for ID: {nsgResourceId}", nsgResourceId);
            var nsgResource = _armClient.GetNetworkSecurityGroupResource(new ResourceIdentifier(nsgResourceId));

            // Check if the NSG exists
            await nsgResource.GetAsync();

            // Get the security rules collection and create/update the rule
            SecurityRuleCollection securityRules = nsgResource.GetSecurityRules();

            string operationType = "update";

            try
            {
                // Check if the rule exists
                await securityRules.GetAsync(rule.Name);
                _logger.LogDebug("Rule {ruleName} exists. Will update.", rule.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                operationType = "create";
                _logger.LogDebug("Rule {ruleName} does not exist. Will create.", rule.Name);
            }

            // CreateOrUpdate handles both creating a new rule and updating an existing one
            await securityRules.CreateOrUpdateAsync(WaitUntil.Completed, rule.Name, rule);

            _logger.LogInformation("Successfully {operationType}d NSG rule {ruleName} for resource {nsgResourceId}.", operationType, rule.Name, nsgResourceId);

            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to create or update NSG rule {ruleName} for resource {nsgResourceId}.", rule?.Name, nsgResourceId);
            return false;
        }
    }

    public async Task<bool> RemoveNSGRuleAsync(string nsgResourceId, string ruleName)
    {
        _logger.LogInformation("RemoveNSGRuleAsync called for NSG Resource ID: {nsgResourceId}, Rule Name: {ruleName}", nsgResourceId, ruleName);

        if (string.IsNullOrWhiteSpace(nsgResourceId))
        {
            _logger.LogError("NSG resource ID is null or empty.");
            throw new ArgumentException("NSG resource ID cannot be null or empty.", nameof(nsgResourceId));
        }

        if (string.IsNullOrWhiteSpace(ruleName))
        {
            _logger.LogError("Rule name is null or empty.");
            throw new ArgumentException("Rule name cannot be null or empty.", nameof(ruleName));
        }

        try
        {            
            // Get the NSG resource
            _logger.LogDebug("Attempting to get NetworkSecurityGroupResource for ID: {nsgResourceId}", nsgResourceId);
            var nsgResource = _armClient.GetNetworkSecurityGroupResource(new ResourceIdentifier(nsgResourceId));

            // Check if the NSG exists
            await nsgResource.GetAsync();

            // Get the security rules collection
            SecurityRuleCollection securityRules = nsgResource.GetSecurityRules();

            try
            {
                // Check if the rule exists
                var existingRule = await securityRules.GetAsync(ruleName);

                _logger.LogDebug("Rule {ruleName} exists. Deleting...", ruleName);
                
                var armOperation = await existingRule.Value.DeleteAsync(WaitUntil.Completed);
                                
                _logger.LogInformation("Successfully deleted NSG rule {ruleName} for resource {nsgResourceId}.", ruleName, nsgResourceId);

                return armOperation.HasCompleted;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {                
                _logger.LogWarning("Rule {ruleName} not found in NSG {nsgResourceId}. Nothing to delete.", ruleName, nsgResourceId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove NSG rule {ruleName} for resource {nsgResourceId}.", ruleName, nsgResourceId);
            return false;
        }
    }
}
