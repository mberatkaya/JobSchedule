using JobScheduler;

namespace JobScheduler.Tests;

public sealed class SchedulerTests
{
    public static IEnumerable<object[]> PromptGraphDurationScenarios()
    {
        yield return
        [
            CreatePromptGraph(1, 5, 7, 2, 4, 1),
            12,
            new[] { "C", "E", "F" },
            11,
            12
        ];

        yield return
        [
            CreatePromptGraph(6, 1, 2, 4, 3, 2),
            12,
            new[] { "A", "D", "F" },
            10,
            12
        ];
    }

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

    [Theory]
    [MemberData(nameof(PromptGraphDurationScenarios))]
    public void Schedule_HandlesDifferentDurationsOnSameDependencyGraph(
        TaskDefinition[] tasks,
        int expectedMinimumCompletionTime,
        string[] expectedCriticalPath,
        int expectedFinishTaskStart,
        int expectedFinishTaskEnd)
    {
        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(expectedMinimumCompletionTime, result.MinimumCompletionTime);
        Assert.Equal(expectedCriticalPath, result.CriticalPath);
        Assert.Equal(expectedFinishTaskStart, result.TaskTimings["F"].EarliestStart);
        Assert.Equal(expectedFinishTaskEnd, result.TaskTimings["F"].EarliestFinish);
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
    public void Schedule_PicksLongerBranchInBranchingGraph()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2),
            new TaskDefinition("B", 5, new[] { "A" }),
            new TaskDefinition("C", 1, new[] { "A" }),
            new TaskDefinition("D", 3, new[] { "B", "C" })
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(10, result.MinimumCompletionTime);
        Assert.Equal(["A", "B", "D"], result.CriticalPath);
        Assert.Equal(7, result.TaskTimings["D"].EarliestStart);
        Assert.Equal(10, result.TaskTimings["D"].EarliestFinish);
        AssertDependenciesRespected(result.TopologicalOrder, tasks);
    }

    [Fact]
    public void Schedule_ReturnsEmptyResultWhenTaskListIsEmpty()
    {
        var result = new Scheduler().Schedule(Array.Empty<TaskDefinition>());

        Assert.Equal(0, result.MinimumCompletionTime);
        Assert.Empty(result.TopologicalOrder);
        Assert.Empty(result.CriticalPath);
        Assert.Empty(result.TaskTimings);
    }

    [Fact]
    public void Schedule_ThrowsWhenTaskIdsAreDuplicated()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2),
            new TaskDefinition("A", 4)
        };

        var act = () => new Scheduler().Schedule(tasks);

        Assert.Throws<ArgumentException>(act);
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

    private static TaskDefinition[] CreatePromptGraph(
        int durationA,
        int durationB,
        int durationC,
        int durationD,
        int durationE,
        int durationF)
    {
        return
        [
            new TaskDefinition("A", durationA),
            new TaskDefinition("B", durationB),
            new TaskDefinition("C", durationC),
            new TaskDefinition("D", durationD, new[] { "A" }),
            new TaskDefinition("E", durationE, new[] { "B", "C" }),
            new TaskDefinition("F", durationF, new[] { "D", "E" })
        ];
    }
}
