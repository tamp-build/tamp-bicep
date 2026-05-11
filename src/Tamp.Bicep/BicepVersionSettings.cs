namespace Tamp.Bicep;

/// <summary>Settings for <c>bicep --version</c>.</summary>
public sealed class BicepVersionSettings : BicepSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "--version";
    }
}
