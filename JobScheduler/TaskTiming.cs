namespace JobScheduler;

public sealed record TaskTiming(string TaskId, int EarliestStart, int EarliestFinish);
