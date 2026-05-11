namespace Tamp.Bicep;

/// <summary>
/// Common base for <c>bicep &lt;verb&gt;</c> settings. The Bicep CLI's
/// global flags are sparse — most knobs are per-verb.
/// </summary>
public abstract class BicepSettingsBase
{
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    protected abstract IEnumerable<string> BuildVerbArguments();

    public BicepSettingsBase SetWorkingDirectory(string? cwd) { WorkingDirectory = cwd; return this; }
    public BicepSettingsBase SetEnv(string key, string value) { EnvironmentVariables[key] = value; return this; }

    public CommandPlan ToCommandPlan(Tool tool)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = BuildVerbArguments().ToList(),
            Environment = new Dictionary<string, string>(EnvironmentVariables),
            WorkingDirectory = WorkingDirectory,
        };
    }
}
