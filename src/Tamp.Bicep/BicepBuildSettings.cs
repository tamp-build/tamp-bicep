namespace Tamp.Bicep;

/// <summary>Settings for <c>bicep build [file]</c>.</summary>
public sealed class BicepBuildSettings : BicepSettingsBase
{
    /// <summary>Source .bicep file path. Required unless <see cref="Pattern"/> is set.</summary>
    public string? File { get; set; }

    /// <summary>Glob pattern for batch builds. Maps to <c>--pattern</c>. Mutually exclusive with <see cref="File"/>.</summary>
    public string? Pattern { get; set; }

    /// <summary>Output directory. Maps to <c>--outdir</c>.</summary>
    public string? OutDir { get; set; }

    /// <summary>Output file path. Maps to <c>--outfile</c>. Single-file builds only.</summary>
    public string? OutFile { get; set; }

    /// <summary>Print ARM JSON to stdout. Maps to <c>--stdout</c>.</summary>
    public bool Stdout { get; set; }

    /// <summary>Skip restoring external modules. Maps to <c>--no-restore</c>.</summary>
    public bool NoRestore { get; set; }

    /// <summary>Diagnostics format. Maps to <c>--diagnostics-format</c>. Values: <c>default</c>, <c>sarif</c>.</summary>
    public string? DiagnosticsFormat { get; set; }

    public BicepBuildSettings SetFile(string path) { File = path; return this; }
    public BicepBuildSettings SetPattern(string glob) { Pattern = glob; return this; }
    public BicepBuildSettings SetOutDir(string path) { OutDir = path; return this; }
    public BicepBuildSettings SetOutFile(string path) { OutFile = path; return this; }
    public BicepBuildSettings SetStdout(bool v = true) { Stdout = v; return this; }
    public BicepBuildSettings SetNoRestore(bool v = true) { NoRestore = v; return this; }
    public BicepBuildSettings SetDiagnosticsFormat(string format) { DiagnosticsFormat = format; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(File) && string.IsNullOrEmpty(Pattern))
            throw new InvalidOperationException("bicep build: File or Pattern is required.");
        if (!string.IsNullOrEmpty(File) && !string.IsNullOrEmpty(Pattern))
            throw new InvalidOperationException("bicep build: File and Pattern are mutually exclusive.");

        yield return "build";
        if (!string.IsNullOrEmpty(OutDir)) { yield return "--outdir"; yield return OutDir!; }
        if (!string.IsNullOrEmpty(OutFile)) { yield return "--outfile"; yield return OutFile!; }
        if (Stdout) yield return "--stdout";
        if (NoRestore) yield return "--no-restore";
        if (!string.IsNullOrEmpty(DiagnosticsFormat)) { yield return "--diagnostics-format"; yield return DiagnosticsFormat!; }
        if (!string.IsNullOrEmpty(Pattern)) { yield return "--pattern"; yield return Pattern!; }
        if (!string.IsNullOrEmpty(File)) yield return File!;
    }
}
