 public class ToolRegistry
   {
       private readonly IServiceProvider _serviceProvider;
       private readonly ILogger<ToolRegistry> _logger;
       
       public ToolRegistry(
           IServiceProvider serviceProvider,
           ILogger<ToolRegistry> logger)
       {
           _serviceProvider = serviceProvider;
           _logger = logger;
       }
       
       public List<ITool> GetToolsForCategory(string category)
       {
           _logger.LogInformation("Getting tools for category: {Category}", category);
           
           switch (category.ToLower())
           {
               case "networking":
                   var networkingService = _serviceProvider.GetRequiredService<AzureNetworkingService>();
                   return new List<ITool>
                   {
                       new CheckNsgRulesTool(networkingService),
                       new TestConnectivityTool(networkingService),
                       new UpdateNsgRuleTool(networkingService),
                       new CreateNsgRuleTool(networkingService),
                       new CheckDnsResolutionTool(networkingService)
                   };
                   
               case "database":
                   var databaseService = _serviceProvider.GetRequiredService<AzureDatabaseService>();
                   return new List<ITool>
                   {
                       new CheckDatabaseConnectivityTool(databaseService),
                       new AnalyzeQueryPerformanceTool(databaseService),
                       new UpdateDatabaseTierTool(databaseService),
                       new UpdateFirewallRuleTool(databaseService)
                   };
                   
               case "authentication":
                   var authService = _serviceProvider.GetRequiredService<AzureAuthenticationService>();
                   return new List<ITool>
                   {
                       new CheckServicePrincipalTool(authService),
                       new VerifyRbacPermissionsTool(authService),
                       new UpdateCorsSettingsTool(authService)
                   };
                   
               case "performance":
                   var perfService = _serviceProvider.GetRequiredService<AzurePerformanceService>();
                   return new List<ITool>
                   {
                       new CheckAppServiceMetricsTool(perfService),
                       new AnalyzeMemoryUsageTool(perfService),
                       new ScaleAppServiceTool(perfService)
                   };
                   
               default:
                   _logger.LogWarning("No specialized tools found for category: {Category}", category);
                   return new List<ITool>();
           }
       }
   }
   