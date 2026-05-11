namespace Tamp.Bicep;

/// <summary>Settings for <c>bicep lint [file]</c>.</summary>
public sealed class BicepLintSettings : BicepSettingsBase
{
    public string? File { get; set; }
    public string? Pattern { get; set; }
    /// <summary>Diagnostics format. Maps to <c>--diagnostics-format</c>. Values: <c>default</c>, <c>sarif</c>. Use <c>sarif</c> for CI integration with Code Scanning / Sonar.</summary>
    public string? DiagnosticsFormat { get; set; }
    public bool NoRestore { get; set; }

    public BicepLintSettings SetFile(string path) { File = path; return this; }
    public BicepLintSettings SetPattern(string glob) { Pattern = glob; return this; }
    public BicepLintSettings SetDiagnosticsFormat(string format) { DiagnosticsFormat = format; return this; }
    public BicepLintSettings SetNoRestore(bool v = true) { NoRestore = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(File) && string.IsNullOrEmpty(Pattern))
            throw new InvalidOperationException("bicep lint: File or Pattern is required.");
        if (!string.IsNullOrEmpty(File) && !string.IsNullOrEmpty(Pattern))
            throw new InvalidOperationException("bicep lint: File and Pattern are mutually exclusive.");

        yield return "lint";
        if (!string.IsNullOrEmpty(DiagnosticsFormat)) { yield return "--diagnostics-format"; yield return DiagnosticsFormat!; }
        if (NoRestore) yield return "--no-restore";
        if (!string.IsNullOrEmpty(Pattern)) { yield return "--pattern"; yield return Pattern!; }
        if (!string.IsNullOrEmpty(File)) yield return File!;
    }
}
