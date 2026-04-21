namespace JobScheduler;

public sealed class Scheduler
{
    public ScheduleResult Schedule(IEnumerable<TaskDefinition> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var taskList = tasks.ToList();
        if (taskList.Count == 0)
        {
            return new ScheduleResult(
                0,
                Array.Empty<string>(),
                new Dictionary<string, TaskTiming>(),
                Array.Empty<string>());
        }

        var tasksById = new Dictionary<string, TaskDefinition>(StringComparer.Ordinal);
        var dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var indegree = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var task in taskList)
        {
            ValidateTask(task);

            if (!tasksById.TryAdd(task.Id, task))
            {
                throw new ArgumentException($"Duplicate task id '{task.Id}' was found.", nameof(tasks));
            }

            dependents[task.Id] = new List<string>();
            indegree[task.Id] = 0;
        }

        foreach (var task in taskList)
        {
            foreach (var dependencyId in task.Dependencies)
            {
                if (!tasksById.ContainsKey(dependencyId))
                {
                    throw new ArgumentException(
                        $"Task '{task.Id}' depends on missing task '{dependencyId}'.",
                        nameof(tasks));
                }

                dependents[dependencyId].Add(task.Id);
                indegree[task.Id]++;
            }
        }

        var ready = new Queue<string>();
        foreach (var task in taskList)
        {
            if (indegree[task.Id] == 0)
            {
                ready.Enqueue(task.Id);
            }
        }

        var topologicalOrder = new List<string>(taskList.Count);
        while (ready.Count > 0)
        {
            var current = ready.Dequeue();
            topologicalOrder.Add(current);

            foreach (var dependentId in dependents[current])
            {
                indegree[dependentId]--;
                if (indegree[dependentId] == 0)
                {
                    ready.Enqueue(dependentId);
                }
            }
        }

        if (topologicalOrder.Count != taskList.Count)
        {
            throw new InvalidOperationException("The task graph contains a cycle.");
        }

        var timings = new Dictionary<string, TaskTiming>(StringComparer.Ordinal);
        var previousOnCriticalPath = new Dictionary<string, string?>(StringComparer.Ordinal);
        var minimumCompletionTime = 0;
        string? criticalPathEnd = null;

        foreach (var taskId in topologicalOrder)
        {
            var task = tasksById[taskId];
            var earliestStart = 0;
            string? previousTaskId = null;

            foreach (var dependencyId in task.Dependencies)
            {
                var dependencyFinish = timings[dependencyId].EarliestFinish;
                if (dependencyFinish > earliestStart)
                {
                    earliestStart = dependencyFinish;
                    previousTaskId = dependencyId;
                }
            }

            var earliestFinish = earliestStart + task.Duration;
            timings[taskId] = new TaskTiming(taskId, earliestStart, earliestFinish);
            previousOnCriticalPath[taskId] = previousTaskId;

            if (earliestFinish > minimumCompletionTime)
            {
                minimumCompletionTime = earliestFinish;
                criticalPathEnd = taskId;
            }
        }

        return new ScheduleResult(
            minimumCompletionTime,
            topologicalOrder,
            timings,
            BuildCriticalPath(criticalPathEnd, previousOnCriticalPath));
    }

    private static void ValidateTask(TaskDefinition task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (string.IsNullOrWhiteSpace(task.Id))
        {
            throw new ArgumentException("Task id cannot be empty.", nameof(task));
        }

        if (task.Duration < 0)
        {
            throw new ArgumentException(
                $"Task '{task.Id}' has a negative duration.",
                nameof(task));
        }
    }

    private static IReadOnlyList<string> BuildCriticalPath(
        string? endTaskId,
        IReadOnlyDictionary<string, string?> previousOnCriticalPath)
    {
        if (endTaskId is null)
        {
            return Array.Empty<string>();
        }

        var path = new List<string>();
        var current = endTaskId;

        while (current is not null)
        {
            path.Add(current);
            current = previousOnCriticalPath[current];
        }

        path.Reverse();
        return path;
    }
}
