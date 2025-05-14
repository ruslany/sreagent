// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

using System.ComponentModel;
using Microsoft.SemanticKernel;
using sreagent.models;

namespace sreagent.plugins;

public class AcaAvailabilityPluginDefinition
{
    private readonly IAcaAvailabilityPlugin _acaAvailabilityPlugin;

    public AcaAvailabilityPluginDefinition(IAcaAvailabilityPlugin acaAvailabilityPlugin)
    {
        _acaAvailabilityPlugin = acaAvailabilityPlugin;
    }

    #region Metrics

    [KernelFunction("get_containerapp_request_count_metrics")]
    public async Task<IReadOnlyList<RequestCountTimeSeriesData>> GetContainerAppRequestMetrics(
        [Description("The resource ID of the ContainerApp resource.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppRequestMetrics(resourceId);
    }

    [KernelFunction("get_containerapp_memory_metrics")]
    public async Task<IReadOnlyList<MemoryUsageTimeSeriesData>> GetContainerAppMemoryMetrics(
        [Description("The resource ID of the ContainerApp resource.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppMemoryMetrics(resourceId);
    }


    [KernelFunction("get_containerapp_cpu_metrics")]
    public async Task<IReadOnlyList<CpuUsageTimeSeriesData>> GetContainerAppCpuMetrics(
        [Description("The resource ID of the ContainerApp resource.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppCpuMetrics(resourceId);
    }

    #endregion

    [KernelFunction("get_containerapp_logs")]
    public async Task<string> GetContainerAppLogsAsync(
        [Description("The resource ID of the Container App instance.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppLogsAsync(resourceId);
    }
}
