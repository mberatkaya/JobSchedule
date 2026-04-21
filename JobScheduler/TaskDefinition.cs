namespace JobScheduler;

public sealed record TaskDefinition
{
    public TaskDefinition(string id, int duration, IEnumerable<string>? dependencies = null)
    {
        Id = id;
        Duration = duration;
        Dependencies = dependencies?.ToArray() ?? Array.Empty<string>();
    }

    public string Id { get; }

    public int Duration { get; }

    public IReadOnlyList<string> Dependencies { get; }
}
