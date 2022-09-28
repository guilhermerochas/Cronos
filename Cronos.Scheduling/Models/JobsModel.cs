namespace Cronos.Scheduling.Models;
public class JobsModel
{
    public string CommandParser { get; set; } = string.Empty;
    public List<Job> Jobs { get; set; } = new();
}

public class Job
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
}
