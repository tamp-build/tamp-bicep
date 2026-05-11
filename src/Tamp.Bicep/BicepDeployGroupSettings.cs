namespace Tamp.Bicep;

/// <summary>
/// Settings for <c>az deployment group create</c> — resource-group
/// scoped ARM deployment. Note: this verb takes an <c>az</c> tool,
/// NOT a <c>bicep</c> tool. Bicep deploys flow through ARM, which is
/// what az exposes.
/// </summary>
public sealed class BicepDeployGroupSettings : BicepSettingsBase
{
    /// <summary>Resource group name. Required. Maps to <c>--resource-group</c> / <c>-g</c>.</summary>
    public string? ResourceGroup { get; set; }

    /// <summary>Local Bicep file. Either this OR <see cref="TemplateUri"/> OR <see cref="TemplateSpecId"/>. Maps to <c>--template-file</c> / <c>-f</c>.</summary>
    public string? TemplateFile { get; set; }

    /// <summary>Remote ARM/Bicep file URI. Maps to <c>--template-uri</c> / <c>-u</c>.</summary>
    public string? TemplateUri { get; set; }

    /// <summary>Resource ID of a template-spec. Maps to <c>--template-spec</c> / <c>-s</c>.</summary>
    public string? TemplateSpecId { get; set; }

    /// <summary>Parameter sources. Each can be a <c>.bicepparam</c> file, a JSON parameter file, or inline <c>key=value</c>. Repeated as <c>--parameters</c>.</summary>
    public List<string> Parameters { get; } = [];

    /// <summary>Deployment name. Maps to <c>--name</c> / <c>-n</c>. Defaults to template filename + timestamp if omitted.</summary>
    public string? Name { get; set; }

    /// <summary>Deployment mode. Maps to <c>--mode</c>. Values: <c>Incremental</c> (default), <c>Complete</c>.</summary>
    public string? Mode { get; set; }

    /// <summary>Roll back to last successful deployment on error. Maps to <c>--rollback-on-error</c>.</summary>
    public bool RollbackOnError { get; set; }

    /// <summary>Preview-mode deployment — show changes without applying. Maps to <c>--what-if</c>.</summary>
    public bool WhatIf { get; set; }

    /// <summary>What-if result format. Maps to <c>--what-if-result-format</c>. Values: <c>FullResourcePayloads</c>, <c>ResourceIdOnly</c>.</summary>
    public string? WhatIfResultFormat { get; set; }

    /// <summary>Run what-if and require explicit confirmation before applying. Maps to <c>--confirm-with-what-if</c>.</summary>
    public bool ConfirmWithWhatIf { get; set; }

    /// <summary>Proceed even if what-if shows no change. Maps to <c>--proceed-if-no-change</c>.</summary>
    public bool ProceedIfNoChange { get; set; }

    /// <summary>Disable interactive prompts. Maps to <c>--no-prompt</c>. Strongly recommended in CI.</summary>
    public bool NoPrompt { get; set; }

    /// <summary>Return immediately, don't wait for completion. Maps to <c>--no-wait</c>.</summary>
    public bool NoWait { get; set; }

    /// <summary>Subscription override. Maps to <c>--subscription</c>.</summary>
    public string? Subscription { get; set; }

    /// <summary>Output format. Maps to <c>--output</c> / <c>-o</c>. Values: json, jsonc, table, tsv, yaml, yamlc, none.</summary>
    public string? Output { get; set; }

    public BicepDeployGroupSettings SetResourceGroup(string name) { ResourceGroup = name; return this; }
    public BicepDeployGroupSettings SetTemplateFile(string path) { TemplateFile = path; return this; }
    public BicepDeployGroupSettings SetTemplateUri(string uri) { TemplateUri = uri; return this; }
    public BicepDeployGroupSettings SetTemplateSpecId(string id) { TemplateSpecId = id; return this; }
    public BicepDeployGroupSettings AddParameter(string fileOrInline) { Parameters.Add(fileOrInline); return this; }
    public BicepDeployGroupSettings AddParameterFile(string path) { Parameters.Add(path); return this; }
    public BicepDeployGroupSettings AddInlineParameter(string key, string value) { Parameters.Add($"{key}={value}"); return this; }
    public BicepDeployGroupSettings SetName(string name) { Name = name; return this; }
    public BicepDeployGroupSettings SetMode(string mode) { Mode = mode; return this; }
    public BicepDeployGroupSettings SetRollbackOnError(bool v = true) { RollbackOnError = v; return this; }
    public BicepDeployGroupSettings SetWhatIf(bool v = true) { WhatIf = v; return this; }
    public BicepDeployGroupSettings SetWhatIfResultFormat(string format) { WhatIfResultFormat = format; return this; }
    public BicepDeployGroupSettings SetConfirmWithWhatIf(bool v = true) { ConfirmWithWhatIf = v; return this; }
    public BicepDeployGroupSettings SetProceedIfNoChange(bool v = true) { ProceedIfNoChange = v; return this; }
    public BicepDeployGroupSettings SetNoPrompt(bool v = true) { NoPrompt = v; return this; }
    public BicepDeployGroupSettings SetNoWait(bool v = true) { NoWait = v; return this; }
    public BicepDeployGroupSettings SetSubscription(string nameOrId) { Subscription = nameOrId; return this; }
    public BicepDeployGroupSettings SetOutput(string format) { Output = format; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(ResourceGroup))
            throw new InvalidOperationException("az deployment group create: ResourceGroup is required.");
        var templateSources = new[] { TemplateFile, TemplateUri, TemplateSpecId }
            .Count(s => !string.IsNullOrEmpty(s));
        if (templateSources == 0)
            throw new InvalidOperationException("az deployment group create: one of TemplateFile, TemplateUri, or TemplateSpecId is required.");
        if (templateSources > 1)
            throw new InvalidOperationException("az deployment group create: TemplateFile, TemplateUri, and TemplateSpecId are mutually exclusive.");

        yield return "deployment";
        yield return "group";
        yield return "create";
        yield return "--resource-group"; yield return ResourceGroup!;
        if (!string.IsNullOrEmpty(TemplateFile)) { yield return "--template-file"; yield return TemplateFile!; }
        if (!string.IsNullOrEmpty(TemplateUri)) { yield return "--template-uri"; yield return TemplateUri!; }
        if (!string.IsNullOrEmpty(TemplateSpecId)) { yield return "--template-spec"; yield return TemplateSpecId!; }
        foreach (var p in Parameters) { yield return "--parameters"; yield return p; }
        if (!string.IsNullOrEmpty(Name)) { yield return "--name"; yield return Name!; }
        if (!string.IsNullOrEmpty(Mode)) { yield return "--mode"; yield return Mode!; }
        if (RollbackOnError) yield return "--rollback-on-error";
        if (WhatIf) yield return "--what-if";
        if (!string.IsNullOrEmpty(WhatIfResultFormat)) { yield return "--what-if-result-format"; yield return WhatIfResultFormat!; }
        if (ConfirmWithWhatIf) yield return "--confirm-with-what-if";
        if (ProceedIfNoChange) yield return "--proceed-if-no-change";
        if (NoPrompt) yield return "--no-prompt";
        if (NoWait) yield return "--no-wait";
        if (!string.IsNullOrEmpty(Subscription)) { yield return "--subscription"; yield return Subscription!; }
        if (!string.IsNullOrEmpty(Output)) { yield return "--output"; yield return Output!; }
    }
}
