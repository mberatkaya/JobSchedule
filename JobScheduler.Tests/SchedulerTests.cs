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

    public static IEnumerable<object[]> DifferentGraphScenarios()
    {
        yield return
        [
            new[]
            {
                new TaskDefinition("A", 4),
                new TaskDefinition("B", 1),
                new TaskDefinition("C", 3, new[] { "A" }),
                new TaskDefinition("D", 6, new[] { "A" }),
                new TaskDefinition("E", 2, new[] { "B", "C" }),
                new TaskDefinition("F", 1, new[] { "D", "E" })
            },
            11,
            new[] { "A", "D", "F" },
            "F",
            10,
            11
        ];

        yield return
        [
            new[]
            {
                new TaskDefinition("P", 2),
                new TaskDefinition("Q", 5),
                new TaskDefinition("R", 4, new[] { "P" }),
                new TaskDefinition("S", 1, new[] { "P" }),
                new TaskDefinition("T", 3, new[] { "Q", "S" }),
                new TaskDefinition("U", 2, new[] { "R", "T" })
            },
            10,
            new[] { "Q", "T", "U" },
            "U",
            8,
            10
        ];
    }

    // Scenario 01: Prompt sample graph returns the expected critical path and finish time.
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

    // Scenario 02: The same graph should still work when task durations change.
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

    // Scenario 03: Independent tasks should finish based on the longest single task.
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

    // Scenario 04: Different dependency graphs should still produce valid timings and a valid path.
    [Theory]
    [MemberData(nameof(DifferentGraphScenarios))]
    public void Schedule_HandlesDifferentDependencyGraphs(
        TaskDefinition[] tasks,
        int expectedMinimumCompletionTime,
        string[] expectedCriticalPath,
        string finishTaskId,
        int expectedFinishTaskStart,
        int expectedFinishTaskEnd)
    {
        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(expectedMinimumCompletionTime, result.MinimumCompletionTime);
        Assert.Equal(expectedCriticalPath, result.CriticalPath);
        Assert.Equal(expectedFinishTaskStart, result.TaskTimings[finishTaskId].EarliestStart);
        Assert.Equal(expectedFinishTaskEnd, result.TaskTimings[finishTaskId].EarliestFinish);
        AssertDependenciesRespected(result.TopologicalOrder, tasks);
    }

    // Scenario 05: A simple chain should return the whole chain as the critical path.
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

    // Scenario 06: In a branching graph, the scheduler should pick the longer branch.
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

    // Scenario 07: An empty task list should return an empty result without errors.
    [Fact]
    public void Schedule_ReturnsEmptyResultWhenTaskListIsEmpty()
    {
        var result = new Scheduler().Schedule(Array.Empty<TaskDefinition>());

        Assert.Equal(0, result.MinimumCompletionTime);
        Assert.Empty(result.TopologicalOrder);
        Assert.Empty(result.CriticalPath);
        Assert.Empty(result.TaskTimings);
    }

    // Scenario 08: A single task should become the full result by itself.
    [Fact]
    public void Schedule_ReturnsSingleTaskAsWholeCriticalPath()
    {
        var tasks = new[]
        {
            new TaskDefinition("OnlyTask", 7)
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(7, result.MinimumCompletionTime);
        Assert.Equal(["OnlyTask"], result.TopologicalOrder);
        Assert.Equal(["OnlyTask"], result.CriticalPath);
        Assert.Equal(0, result.TaskTimings["OnlyTask"].EarliestStart);
        Assert.Equal(7, result.TaskTimings["OnlyTask"].EarliestFinish);
    }

    // Scenario 09: Disconnected subgraphs should use the longest component as the critical path.
    [Fact]
    public void Schedule_PicksLongestPathAcrossDisconnectedSubgraphs()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2),
            new TaskDefinition("B", 2, new[] { "A" }),
            new TaskDefinition("X", 1),
            new TaskDefinition("Y", 3, new[] { "X" }),
            new TaskDefinition("Z", 4, new[] { "Y" })
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(8, result.MinimumCompletionTime);
        Assert.Equal(["X", "Y", "Z"], result.CriticalPath);
        Assert.Equal(4, result.TaskTimings["Z"].EarliestStart);
        Assert.Equal(8, result.TaskTimings["Z"].EarliestFinish);
        AssertDependenciesRespected(result.TopologicalOrder, tasks);
    }

    // Scenario 10: A deeper merge should wait for the latest dependency before continuing.
    [Fact]
    public void Schedule_UsesLatestDependencyFinishInMultiStageMerge()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2),
            new TaskDefinition("B", 4),
            new TaskDefinition("C", 3, new[] { "A" }),
            new TaskDefinition("D", 5, new[] { "B" }),
            new TaskDefinition("E", 2, new[] { "C", "D" }),
            new TaskDefinition("F", 1, new[] { "E" })
        };

        var result = new Scheduler().Schedule(tasks);

        Assert.Equal(12, result.MinimumCompletionTime);
        Assert.Equal(["B", "D", "E", "F"], result.CriticalPath);
        Assert.Equal(9, result.TaskTimings["E"].EarliestStart);
        Assert.Equal(11, result.TaskTimings["E"].EarliestFinish);
        Assert.Equal(12, result.TaskTimings["F"].EarliestFinish);
        AssertDependenciesRespected(result.TopologicalOrder, tasks);
    }

    // Scenario 11: Duplicate task ids should be rejected during validation.
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

    // Scenario 12: Negative durations should be rejected during validation.
    [Fact]
    public void Schedule_ThrowsWhenDurationIsNegative()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", -1)
        };

        var act = () => new Scheduler().Schedule(tasks);

        Assert.Throws<ArgumentException>(act);
    }

    // Scenario 13: Cyclic graphs should fail because there is no valid topological order.
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

    // Scenario 14: Self-dependency should be treated as a cycle and rejected.
    [Fact]
    public void Schedule_ThrowsWhenTaskDependsOnItself()
    {
        var tasks = new[]
        {
            new TaskDefinition("A", 2, new[] { "A" })
        };

        var act = () => new Scheduler().Schedule(tasks);

        Assert.Throws<InvalidOperationException>(act);
    }

    // Scenario 15: Missing dependency ids should fail fast during validation.
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

    // Scenario 16: A null task collection should be rejected immediately.
    [Fact]
    public void Schedule_ThrowsWhenTasksArgumentIsNull()
    {
        IEnumerable<TaskDefinition> tasks = null!;

        var act = () => new Scheduler().Schedule(tasks);

        Assert.Throws<ArgumentNullException>(act);
    }

    // Scenario 17: Blank task ids should be rejected to keep the graph addressable.
    [Fact]
    public void Schedule_ThrowsWhenTaskIdIsBlank()
    {
        var tasks = new[]
        {
            new TaskDefinition("   ", 2)
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
