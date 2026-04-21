# JobScheduler

Small .NET 8 implementation for the Wingie / Enuygun scheduling case.

Chosen interpretation:

- tasks can run in parallel once all dependencies are satisfied
- minimum completion time is the critical-path length in the dependency graph
- the scheduler also returns one valid topological order

For the sample case in the prompt, the result is `11`, not `8`.
That is because `F` cannot start until both `D` and `E` finish, and `D` finishes at time `8`.

Run the sample case with:

```bash
dotnet run --project JobScheduler/JobScheduler.csproj
```
