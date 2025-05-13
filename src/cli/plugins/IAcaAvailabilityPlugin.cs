// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

using sreagent.models;

namespace sreagent.plugins;

public interface IAcaAvailabilityPlugin
{
    Task<IReadOnlyList<RequestCountTimeSeriesData>> GetContainerAppRequestMetrics(string resourceId);

    Task<IReadOnlyList<MemoryUsageTimeSeriesData>> GetContainerAppMemoryMetrics(string resourceId);

    Task<IReadOnlyList<CpuUsageTimeSeriesData>> GetContainerAppCpuMetrics(string resourceId);

    Task<string> GetContainerAppLogsAsync(string resourceId, string? revisionName = null);
}
