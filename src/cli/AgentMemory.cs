using Microsoft.Extensions.Logging;

namespace sreagent;

public class AgentMemory
{
    private readonly Dictionary<string, List<string>> _troubleshootingPatterns;
    private readonly ILogger<AgentMemory> _logger;

    public AgentMemory(ILogger<AgentMemory> logger)
    {
        _logger = logger;
        _troubleshootingPatterns = InitializeTroubleshootingPatterns();
    }

    private Dictionary<string, List<string>> InitializeTroubleshootingPatterns()
    {
        // In a real implementation, these patterns could be loaded from a database or file
        return new Dictionary<string, List<string>>
        {
            ["networking"] = new List<string>
                {
                    "If application can't connect to database, check NSG rules between app subnet and database subnet",
                    "If web application is unreachable, verify NSG allows port 80/443 inbound",
                    "If services in different VNets can't communicate, check VNet peering or service endpoints",
                    "If experiencing intermittent connectivity issues, check DNS resolution and network latency",
                    "If load balancer endpoints are not responding, verify health probe configuration",
                    "If application gateway returns 502 errors, check backend pool health and settings"
                },
            ["database"] = new List<string>
                { "If SQL queries are timing out, check DTU usage and consider scaling up",
                   "If connection pooling errors occur, verify max pool settings in connection string",
                   "If database is unreachable, check firewall rules to ensure client IP is allowed",
                   "If experiencing deadlocks, review transaction isolation levels and query patterns",
                   "If seeing high wait times, check for blocking queries or resource contention",
                   "If database size is approaching limit, consider implementing data archiving strategy"
               },
            ["authentication"] = new List<string>
               {
                   "If seeing 401 Unauthorized errors, verify token acquisition and validity",
                   "If CORS errors appear in browser console, check CORS configuration in Azure",
                   "If managed identity isn't working, verify service principal assignments",
                   "If users can't access resources, check RBAC permissions at subscription and resource levels",
                   "If token acquisition fails, verify app registration and API permissions",
                   "If certificate authentication fails, check certificate validity and trust chain"
               },
            ["performance"] = new List<string>
               {
                   "If web app is slow, check App Service plan tier and scaling settings",
                   "If seeing high memory usage, look for memory leaks or inefficient caching",
                   "If CPU spikes occur, identify resource-intensive operations and optimize",
                   "If storage operations are slow, check throttling metrics and partition strategy",
                   "If application startup is slow, review initialization logic and dependencies",
                   "If experiencing timeouts, check connection limits and timeout configurations"
               }
        };
    }

    public Task<List<string>> SearchPatternsAsync(string category)
    {
        _logger.LogInformation("Searching patterns for category: {Category}", category);

        if (_troubleshootingPatterns.TryGetValue(category, out var patterns))
        {
            return Task.FromResult(patterns);
        }

        return Task.FromResult(new List<string>());
    }

    public Task<List<string>> SearchPatternsAsync(string query, string category)
    {
        // In a real implementation, this would perform semantic search on the patterns
        // For now, just return all patterns for the category
        return SearchPatternsAsync(category);
    }
}