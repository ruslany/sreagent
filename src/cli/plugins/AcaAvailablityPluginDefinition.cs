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
    [Description(
        "Get the total request count metrics of a specific Container App instance at per minute granularity" +
        " for the past 30 minutes, Container App is healthy if all data points are at least 99.9 availability.")]
    public async Task<IReadOnlyList<RequestCountTimeSeriesData>> GetContainerAppRequestMetrics(
        [Description("The resource ID of the ContainerApp resource.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppRequestMetrics(resourceId);
    }

    [KernelFunction("get_containerapp_memory_metrics")]
    [Description(
        "Get the average memory usage of a specific Container App instance at per minute granularity for the past 30 minutes," +
        " Container App is healthy if over half of the data points is less than 20% memory utilization.")]
    public async Task<IReadOnlyList<MemoryUsageTimeSeriesData>> GetContainerAppMemoryMetrics(
        [Description("The resource ID of the ContainerApp resource.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppMemoryMetrics(resourceId);
    }


    [KernelFunction("get_containerapp_cpu_metrics")]
    [Description(
        "Get the average CPU utilization metrics of a specific Container App instance at per minute granularity" +
        " for the past 30 minutes, Container App is healthy if over half of the data points is less than 80% CPU utilization, zero metric value doesn't indicate the app is unhealthy")]
    public async Task<IReadOnlyList<CpuUsageTimeSeriesData>> GetContainerAppCpuMetrics(
        [Description("The resource ID of the ContainerApp resource.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppCpuMetrics(resourceId);
    }

    #endregion


    [Description("Get the logs of the latest revision of a Container App instance.")]
    public async Task<string> GetContainerAppLogsAsync(
        [Description("The resource ID of the Container App instance.")]
            string resourceId)
    {
        return await _acaAvailabilityPlugin.GetContainerAppLogsAsync(resourceId);
    }
}
