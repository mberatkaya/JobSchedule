using JobScheduler;

namespace JobScheduler.Tests;

public sealed class SchedulerTests
{
    [Fact]
    public void Schedule_ReturnsCriticalPathForSampleCase()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 3),
            new TaskDefinition("B", 2),
            new TaskDefinition("C", 4),
            new TaskDefinition("D", 5, new[] { "A" }),
            new TaskDefinition("E", 2, new[] { "B", "C" }),
            new TaskDefinition("F", 3, new[] { "D", "E" })
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(11, result.MinimumCompletionTime);
        Assert.Equal(["A", "D", "F"], result.CriticalPath);
        Assert.Equal(8, result.TaskTimings["F"].EarliestStart);
        Assert.Equal(11, result.TaskTimings["F"].EarliestFinish);
        AssertDependenciesRespected(result.TopologicalOrder, tasks);
    }

    [Fact]
    public void Schedule_UsesLongestTaskWhenEverythingIsIndependent()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2),
            new TaskDefinition("B", 6),
            new TaskDefinition("C", 4)
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(6, result.MinimumCompletionTime);
        Assert.Equal(["B"], result.CriticalPath);
        AssertDependenciesRespected(result.TopologicalOrder, tasks);
    }

    [Fact]
    public void Schedule_ReturnsWholeChainWhenTasksDependOnEachOther()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2),
            new TaskDefinition("B", 3, new[] { "A" }),
            new TaskDefinition("C", 4, new[] { "B" })
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(9, result.MinimumCompletionTime);
        Assert.Equal(["A", "B", "C"], result.TopologicalOrder);
        Assert.Equal(["A", "B", "C"], result.CriticalPath);
    }

    [Fact]
    public void Schedule_ThrowsWhenGraphContainsCycle()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2, new[] { "C" }),
            new TaskDefinition("B", 3, new[] { "A" }),
            new TaskDefinition("C", 1, new[] { "B" })
        };

        var act = () => new Scheduler().Schedule(tasks);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Schedule_ThrowsWhenDependencyDoesNotExist()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2, new[] { "X" })
        };

        var act = () => new Scheduler().Schedule(tasks);

        Assert.Throws<ArgumentException>(act);
    }

    private static void AssertDependenciesRespected(
        IReadOnlyList<string> topologicalOrder,
        IEnumerable<TaskDefinition> tasks)
    {
        var positions = topologicalOrder
            .Select((taskId, index) => new { taskId, index })
            .ToDictionary(x => x.taskId, x => x.index, StringComparer.Ordinal);

        foreach (var task in tasks)
        {
            foreach (var dependency in task.Dependencies)
            {
                Assert.True(
                    positions[dependency] < positions[task.Id],
                    $"Expected '{dependency}' to come before '{task.Id}'.");
            }
        }
    }
}
