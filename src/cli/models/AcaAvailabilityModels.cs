namespace sreagent.models;

public sealed record RequestCountTimeSeriesData(
    DateTime TimeStamp,
    double TotalRequestCount);

public sealed record CpuUsageTimeSeriesData(
    DateTime TimeStamp,
    double Percent);

public sealed record MemoryUsageTimeSeriesData(
    DateTime TimeStamp,
    double Percent);