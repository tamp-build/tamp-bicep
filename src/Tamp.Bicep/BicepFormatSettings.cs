namespace Tamp.Bicep;

/// <summary>Settings for <c>bicep format [file]</c>.</summary>
public sealed class BicepFormatSettings : BicepSettingsBase
{
    public string? File { get; set; }
    public string? OutDir { get; set; }
    public string? OutFile { get; set; }
    public bool Stdout { get; set; }
    /// <summary>Maps to <c>--newline</c>. Values: <c>Auto</c>, <c>LF</c>, <c>CRLF</c>, <c>CR</c>.</summary>
    public string? Newline { get; set; }
    /// <summary>Maps to <c>--indent-kind</c>. Values: <c>Space</c>, <c>Tab</c>.</summary>
    public string? IndentKind { get; set; }
    /// <summary>Maps to <c>--indent-size</c>. Only meaningful when IndentKind = Space.</summary>
    public int? IndentSize { get; set; }
    public bool InsertFinalNewline { get; set; }

    public BicepFormatSettings SetFile(string path) { File = path; return this; }
    public BicepFormatSettings SetOutDir(string path) { OutDir = path; return this; }
    public BicepFormatSettings SetOutFile(string path) { OutFile = path; return this; }
    public BicepFormatSettings SetStdout(bool v = true) { Stdout = v; return this; }
    public BicepFormatSettings SetNewline(string newline) { Newline = newline; return this; }
    public BicepFormatSettings SetIndentKind(string kind) { IndentKind = kind; return this; }
    public BicepFormatSettings SetIndentSize(int spaces) { IndentSize = spaces; return this; }
    public BicepFormatSettings SetInsertFinalNewline(bool v = true) { InsertFinalNewline = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(File))
            throw new InvalidOperationException("bicep format: File is required.");
        yield return "format";
        if (!string.IsNullOrEmpty(OutDir)) { yield return "--outdir"; yield return OutDir!; }
        if (!string.IsNullOrEmpty(OutFile)) { yield return "--outfile"; yield return OutFile!; }
        if (Stdout) yield return "--stdout";
        if (!string.IsNullOrEmpty(Newline)) { yield return "--newline"; yield return Newline!; }
        if (!string.IsNullOrEmpty(IndentKind)) { yield return "--indent-kind"; yield return IndentKind!; }
        if (IndentSize is { } n) { yield return "--indent-size"; yield return n.ToString(); }
        if (InsertFinalNewline) yield return "--insert-final-newline";
        yield return File!;
    }
}
