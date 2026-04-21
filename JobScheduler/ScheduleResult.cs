namespace JobScheduler;

public sealed record ScheduleResult(
    int MinimumCompletionTime,
    IReadOnlyList<string> TopologicalOrder,
    IReadOnlyDictionary<string, TaskTiming> TaskTimings,
    IReadOnlyList<string> CriticalPath);
