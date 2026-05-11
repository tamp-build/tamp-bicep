namespace Tamp.Bicep;

/// <summary>Escape hatch for verbs we haven't typed (decompile, restore, generate-params, publish).</summary>
public sealed class BicepRawSettings : BicepSettingsBase
{
    public List<string> RawArguments { get; } = [];

    public BicepRawSettings AddArgs(params string[] args) { RawArguments.AddRange(args); return this; }

    protected override IEnumerable<string> BuildVerbArguments() => RawArguments;
}
