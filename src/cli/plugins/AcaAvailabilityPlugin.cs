// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// ------------------------------------------------------------

using sreagent.models;

namespace sreagent.plugins
{
    public class AcaAvailabilityPlugin : IAcaAvailabilityPlugin
    {
        public Task<IReadOnlyList<CpuUsageTimeSeriesData>> GetContainerAppCpuMetrics(string resourceId)
        {
            // Generate sample CPU usage time series data
            var sampleData = new List<CpuUsageTimeSeriesData>
            {
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-13), 12.4),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-12), 14.8),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-11), 16.1),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-10), 13.9),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-9), 17.3),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-8), 19.0),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-7), 15.7),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-6), 18.2),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-5), 21.5),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-4), 16.9),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-3), 15.2),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-2), 23.7),
                new CpuUsageTimeSeriesData(DateTime.UtcNow.AddMinutes(-1), 18.5),
                new CpuUsageTimeSeriesData(DateTime.UtcNow, 20.1)
            };

            return Task.FromResult<IReadOnlyList<CpuUsageTimeSeriesData>>(sampleData);
        }

        public Task<string> GetContainerAppLogsAsync(string resourceId, string? revisionName = null)
        {            
            return Task.FromResult<string>("The logs of the latest revision of the Container App instance show that there is an image pull failure due to incorrect credentials. Please check the image registry credentials and try again.");
        }

        public Task<IReadOnlyList<MemoryUsageTimeSeriesData>> GetContainerAppMemoryMetrics(string resourceId)
        {
            // Generate sample memory usage time series data
            var sampleData = new List<MemoryUsageTimeSeriesData>
            {
                new(DateTime.UtcNow.AddMinutes(-13), 56),
                new(DateTime.UtcNow.AddMinutes(-12), 70),
                new(DateTime.UtcNow.AddMinutes(-11), 65),
                new(DateTime.UtcNow.AddMinutes(-10), 80),
                new(DateTime.UtcNow.AddMinutes(-9), 90),
                new(DateTime.UtcNow.AddMinutes(-8), 75),
                new(DateTime.UtcNow.AddMinutes(-7), 60),
                new(DateTime.UtcNow.AddMinutes(-6), 85),
                new(DateTime.UtcNow.AddMinutes(-5), 95),
                new(DateTime.UtcNow.AddMinutes(-4), 72),
                new(DateTime.UtcNow.AddMinutes(-3), 68),
                new(DateTime.UtcNow.AddMinutes(-2), 99),
                new(DateTime.UtcNow.AddMinutes(-1), 92),
                new(DateTime.UtcNow, 88)
            };

            return Task.FromResult<IReadOnlyList<MemoryUsageTimeSeriesData>>(sampleData);
        }

        public Task<IReadOnlyList<RequestCountTimeSeriesData>> GetContainerAppRequestMetrics(string resourceId)
        {
            // Generate sample request count time series data
            var sampleData = new List<RequestCountTimeSeriesData>
            {
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-13), 120),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-12), 135),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-11), 128),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-10), 142),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-9), 150),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-8), 138),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-7), 145),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-6), 152),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-5), 160),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-4), 148),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-3), 155),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-2), 162),
            new RequestCountTimeSeriesData(DateTime.UtcNow.AddMinutes(-1), 158),
            new RequestCountTimeSeriesData(DateTime.UtcNow, 165)
            };

            return Task.FromResult<IReadOnlyList<RequestCountTimeSeriesData>>(sampleData);
        }
    }
}
