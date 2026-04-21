using JobScheduler;

var tasks = new[]
{
    new TaskDefinition("A", 3),
    new TaskDefinition("B", 2),
    new TaskDefinition("C", 4),
    new TaskDefinition("D", 5, new[] { "A" }),
    new TaskDefinition("E", 2, new[] { "B", "C" }),
    new TaskDefinition("F", 3, new[] { "D", "E" })
};

var scheduler = new Scheduler();
var result = scheduler.Schedule(tasks);

Console.WriteLine($"Minimum completion time: {result.MinimumCompletionTime}");
Console.WriteLine($"Topological order: {string.Join(" -> ", result.TopologicalOrder)}");
Console.WriteLine($"Critical path: {string.Join(" -> ", result.CriticalPath)}");
Console.WriteLine();
Console.WriteLine("Task timings:");

foreach (var taskId in result.TopologicalOrder)
{
    var timing = result.TaskTimings[taskId];
    Console.WriteLine(
        $"{taskId}: start={timing.EarliestStart}, finish={timing.EarliestFinish}");
}
