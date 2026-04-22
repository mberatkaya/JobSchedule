# JobScheduler

Small .NET 8 solution for the Wingie / Enuygun job scheduling case.

## Problem

The case asks for two things for a job made of dependent tasks:

- the minimum completion time for the whole job
- an order in which the tasks can be completed

Each task has:

- an id
- a duration
- zero or more dependencies on other tasks in the same job

## Chosen interpretation

The case note says the problem can have two valid answers depending on interpretation.
For this solution, I chose the following interpretation and kept it consistent everywhere in code and tests:

- tasks can run in parallel as soon as all of their dependencies are satisfied
- therefore, the minimum completion time is the length of the critical path in the dependency graph
- the returned order is one valid topological order, not a claim that tasks must run strictly one after another

I chose this approach because the wording focuses on the minimum completion time of the whole job, and once dependencies are satisfied there is no reason to delay an independent task.

## Sample case result

For the sample in the prompt:

- `A = 3`
- `B = 2`
- `C = 4`
- `D = 5`, depends on `A`
- `E = 2`, depends on `B` and `C`
- `F = 3`, depends on `D` and `E`

Under the chosen interpretation, the result is:

- minimum completion time: `11`
- one valid topological order: `A, B, C, D, E, F`
- critical path: `A -> D -> F`

### Why the result is 11 instead of 8

The case text says `8`, but under a parallel scheduling interpretation `F` cannot start until both `D` and `E` are finished.

| Task | Dependencies | Earliest Start | Duration | Earliest Finish |
| --- | --- | ---: | ---: | ---: |
| A | - | 0 | 3 | 3 |
| B | - | 0 | 2 | 2 |
| C | - | 0 | 4 | 4 |
| D | A | 3 | 5 | 8 |
| E | B, C | 4 | 2 | 6 |
| F | D, E | 8 | 3 | 11 |

Sample CPM-style view of the same task graph:

![Sample critical path diagram](docs/images/cpm.jpeg)

This diagram is included to make the `11` result easier to see.
It shows the earliest and latest timing values for each task, and it makes two things clear:

- `A -> D -> F` is the critical path because those tasks have no slack in this schedule
- even though `E` is done at time `6`, task `F` still cannot start before time `8` because it must also wait for `D`

`E` is ready at time `6`, but `F` still has to wait for `D`, which finishes at time `8`. After that, `F` takes `3` more units, so the total job completion time becomes `11`.

## Approach

The core problem is treated as DAG scheduling.

1. Validate the input.
   - no duplicate task ids
   - no missing dependency references
   - no negative durations
2. Build the dependency graph and in-degree map.
3. Run Kahn's algorithm to produce one valid topological order.
4. If not all tasks are processed, fail with a cycle error.
5. Walk the tasks in topological order and compute:
   - earliest start
   - earliest finish
   - predecessor on the current critical path
6. Pick the task with the largest earliest finish and backtrack to build the critical path.

Time complexity is `O(V + E)`, where:

- `V` = number of tasks
- `E` = number of dependency edges

## What the solution returns

The scheduler returns:

- `MinimumCompletionTime`
- `TopologicalOrder`
- `TaskTimings` for earliest start and earliest finish per task
- `CriticalPath`

## Running the sample

### Using .NET CLI

```bash
dotnet run --project JobScheduler/JobScheduler.csproj
```

### Using Docker

Build and run with Docker:

```bash
docker build -t jobscheduler .
docker run --rm jobscheduler
```

Or use Docker Compose:

```bash
docker-compose up
```

Expected sample output is aligned with the chosen interpretation and reports a minimum completion time of `11`.

## Running the tests

### Using .NET CLI

```bash
dotnet test JobScheduler.sln
```

### Using Docker

The Dockerfile automatically runs tests during the build process. To run tests explicitly:

```bash
docker build --target build -t jobscheduler-test .
```

## Test coverage

The test file now labels each case with a numbered comment header so the intent is easy to scan during review.

Scenario list:

1. Prompt sample case: Verifies the main example from the prompt and checks the expected completion time, critical path, and finish time for the last task.
2. Same graph, different durations: Verifies that the scheduler still computes the right answer when the dependency graph stays the same but durations change.
3. All tasks independent: Verifies that the minimum completion time comes from the longest single task when nothing depends on anything else.
4. Different dependency graphs: Verifies that the algorithm still works on alternative graph shapes, not only on the prompt graph.
5. Simple linear chain: Verifies that a pure chain returns the whole chain as both the topological order and the critical path.
6. Branching graph: Verifies that the longer branch is chosen when two branches merge into the same task.
7. Empty input: Verifies that an empty task list returns an empty result instead of failing.
8. Single task: Verifies that one task is handled as a complete schedule on its own.
9. Disconnected subgraphs: Verifies that the scheduler picks the longest path even when the input contains separate independent components.
10. Multi-stage merge: Verifies that a merge waits for the latest dependency finish time before starting the next task.
11. Duplicate task ids: Verifies that duplicate ids are rejected during validation.
12. Negative duration: Verifies that invalid negative durations are rejected.
13. Cyclic graph: Verifies that the scheduler throws when there is no valid topological order.
14. Self-dependency: Verifies that a task depending on itself is treated as an invalid cycle.
15. Missing dependency: Verifies that referencing a task id that does not exist fails fast.
16. Null task collection: Verifies that the public API rejects a null input collection immediately.
17. Blank task id: Verifies that empty or whitespace-only ids are rejected so every task remains addressable in the graph.

For success cases, the tests also verify that the returned order respects dependency direction, so the assertions are not tied to one brittle exact ordering unless the graph shape makes the order deterministic.
