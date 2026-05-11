namespace Tamp.Bicep;

/// <summary>
/// Facade for the Bicep CLI plus ARM deployment via az. Most verbs
/// (<see cref="Build"/>, <see cref="Lint"/>, <see cref="Format"/>,
/// <see cref="Version"/>, <see cref="Raw"/>) take a <c>bicep</c>
/// <see cref="Tool"/>; <see cref="Deploy"/> verbs take an <c>az</c>
/// tool because ARM deployment goes through Azure CLI.
/// </summary>
/// <remarks>
/// <para>Resolve the tools via <c>[NuGetPackage(UseSystemPath = true)]</c>:</para>
/// <code>
/// [NuGetPackage("bicep", UseSystemPath = true)]
/// readonly Tool BicepTool;
///
/// [NuGetPackage("az", UseSystemPath = true)]
/// readonly Tool AzTool;
/// </code>
/// </remarks>
public static class Bicep
{
    // ---- bicep CLI verbs (take a `bicep` Tool) ----

    /// <summary><c>bicep build</c> — compile .bicep to ARM JSON.</summary>
    public static CommandPlan Build(Tool bicep, Action<BicepBuildSettings> configure)
    {
        if (bicep is null) throw new ArgumentNullException(nameof(bicep));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new BicepBuildSettings();
        configure(s);
        return s.ToCommandPlan(bicep);
    }

    /// <summary><c>bicep lint</c> — static analysis. <c>--diagnostics-format sarif</c> for CI integration.</summary>
    public static CommandPlan Lint(Tool bicep, Action<BicepLintSettings> configure)
    {
        if (bicep is null) throw new ArgumentNullException(nameof(bicep));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new BicepLintSettings();
        configure(s);
        return s.ToCommandPlan(bicep);
    }

    /// <summary><c>bicep format</c> — formatter.</summary>
    public static CommandPlan Format(Tool bicep, Action<BicepFormatSettings> configure)
    {
        if (bicep is null) throw new ArgumentNullException(nameof(bicep));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new BicepFormatSettings();
        configure(s);
        return s.ToCommandPlan(bicep);
    }

    /// <summary><c>bicep --version</c>.</summary>
    public static CommandPlan Version(Tool bicep, Action<BicepVersionSettings>? configure = null)
    {
        if (bicep is null) throw new ArgumentNullException(nameof(bicep));
        var s = new BicepVersionSettings();
        configure?.Invoke(s);
        return s.ToCommandPlan(bicep);
    }

    /// <summary>Escape hatch for verbs we haven't typed: decompile, restore, generate-params, publish.</summary>
    public static CommandPlan Raw(Tool bicep, params string[] arguments)
    {
        if (bicep is null) throw new ArgumentNullException(nameof(bicep));
        if (arguments is null || arguments.Length == 0)
            throw new ArgumentException("Raw requires at least one argument.", nameof(arguments));
        var s = new BicepRawSettings();
        s.AddArgs(arguments);
        return s.ToCommandPlan(bicep);
    }

    // ---- az deployment verbs (take an `az` Tool) ----

    /// <summary>Sub-facade for ARM deployment via <c>az deployment ...</c>. Takes an <c>az</c> tool, NOT a <c>bicep</c> tool.</summary>
    public static class Deploy
    {
        /// <summary><c>az deployment group create</c> — resource-group scoped deploy.</summary>
        public static CommandPlan Group(Tool az, Action<BicepDeployGroupSettings> configure)
        {
            if (az is null) throw new ArgumentNullException(nameof(az));
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            var s = new BicepDeployGroupSettings();
            configure(s);
            return s.ToCommandPlan(az);
        }
    }
}
