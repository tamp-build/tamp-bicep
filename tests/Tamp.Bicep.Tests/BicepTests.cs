using System.IO;
using Tamp;
using Xunit;

namespace Tamp.Bicep.Tests;

public sealed class BicepTests
{
    private static Tool FakeTool(string name = "bicep") =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    private static int IndexOf(IReadOnlyList<string> args, string value, int start = 0)
    {
        for (var i = start; i < args.Count; i++)
            if (args[i] == value) return i;
        return -1;
    }

    // ---- shape ----

    [Fact]
    public void Bicep_Verbs_Use_Bicep_Tool_Path()
    {
        var bicep = FakeTool("bicep");
        Assert.Equal(bicep.Executable.Value, Bicep.Build(bicep, s => s.SetFile("a.bicep")).Executable);
        Assert.Equal(bicep.Executable.Value, Bicep.Lint(bicep, s => s.SetFile("a.bicep")).Executable);
        Assert.Equal(bicep.Executable.Value, Bicep.Format(bicep, s => s.SetFile("a.bicep")).Executable);
        Assert.Equal(bicep.Executable.Value, Bicep.Version(bicep).Executable);
        Assert.Equal(bicep.Executable.Value, Bicep.Raw(bicep, "--help").Executable);
    }

    [Fact]
    public void Deploy_Verbs_Use_Az_Tool_Path()
    {
        // Deploy.Group takes an `az` tool because ARM deploy flows through Azure CLI.
        var az = FakeTool("az");
        Assert.Equal(az.Executable.Value,
            Bicep.Deploy.Group(az, s => s.SetResourceGroup("rg").SetTemplateFile("a.bicep")).Executable);
    }

    // ---- build ----

    [Fact]
    public void Build_Requires_File_Or_Pattern()
    {
        Assert.Throws<InvalidOperationException>(() => Bicep.Build(FakeTool(), s => { }));
    }

    [Fact]
    public void Build_File_And_Pattern_Are_Mutually_Exclusive()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Bicep.Build(FakeTool(), s => s.SetFile("a.bicep").SetPattern("**/*.bicep")));
    }

    [Fact]
    public void Build_File_Trails_Verb_After_Flags()
    {
        var plan = Bicep.Build(FakeTool(), s => s
            .SetFile("infra/main.bicep")
            .SetOutFile("infra/main.json")
            .SetNoRestore()
            .SetDiagnosticsFormat("sarif"));
        var args = plan.Arguments;
        Assert.Equal("build", args[0]);
        Assert.Equal("infra/main.bicep", args[^1]);
        Assert.Contains("--outfile", args);
        Assert.Contains("infra/main.json", args);
        Assert.Contains("--no-restore", args);
        Assert.Contains("--diagnostics-format", args);
        Assert.Contains("sarif", args);
    }

    [Fact]
    public void Build_Pattern_Drops_File_Positional()
    {
        var plan = Bicep.Build(FakeTool(), s => s.SetPattern("./infra/**/*.bicep"));
        var args = plan.Arguments;
        Assert.Equal("build", args[0]);
        Assert.Contains("--pattern", args);
        Assert.Equal("./infra/**/*.bicep", args[^1]);
    }

    [Fact]
    public void Build_Stdout_Mode()
    {
        var plan = Bicep.Build(FakeTool(), s => s.SetFile("a.bicep").SetStdout());
        Assert.Contains("--stdout", plan.Arguments);
    }

    [Fact]
    public void Build_OutDir_And_OutFile_Both_Round_Trip()
    {
        // The CLI rejects this combo at runtime, but the wrapper
        // shouldn't pre-judge — emit what the user said.
        var plan = Bicep.Build(FakeTool(), s => s
            .SetFile("a.bicep")
            .SetOutDir("out/")
            .SetOutFile("a.json"));
        Assert.Contains("--outdir", plan.Arguments);
        Assert.Contains("out/", plan.Arguments);
        Assert.Contains("--outfile", plan.Arguments);
        Assert.Contains("a.json", plan.Arguments);
    }

    // ---- lint ----

    [Fact]
    public void Lint_Requires_File_Or_Pattern()
    {
        Assert.Throws<InvalidOperationException>(() => Bicep.Lint(FakeTool(), s => { }));
    }

    [Fact]
    public void Lint_Sarif_Format()
    {
        var plan = Bicep.Lint(FakeTool(), s => s
            .SetFile("a.bicep")
            .SetDiagnosticsFormat("sarif"));
        Assert.Equal("lint", plan.Arguments[0]);
        Assert.Contains("--diagnostics-format", plan.Arguments);
        Assert.Contains("sarif", plan.Arguments);
        Assert.Equal("a.bicep", plan.Arguments[^1]);
    }

    [Fact]
    public void Lint_Pattern_And_NoRestore()
    {
        var plan = Bicep.Lint(FakeTool(), s => s
            .SetPattern("**/*.bicep")
            .SetNoRestore());
        Assert.Contains("--pattern", plan.Arguments);
        Assert.Contains("**/*.bicep", plan.Arguments);
        Assert.Contains("--no-restore", plan.Arguments);
    }

    // ---- format ----

    [Fact]
    public void Format_Requires_File()
    {
        Assert.Throws<InvalidOperationException>(() => Bicep.Format(FakeTool(), s => { }));
    }

    [Fact]
    public void Format_All_Flags_Round_Trip()
    {
        var plan = Bicep.Format(FakeTool(), s => s
            .SetFile("a.bicep")
            .SetNewline("LF")
            .SetIndentKind("Space")
            .SetIndentSize(2)
            .SetInsertFinalNewline());
        var args = plan.Arguments;
        Assert.Contains("--newline", args); Assert.Contains("LF", args);
        Assert.Contains("--indent-kind", args); Assert.Contains("Space", args);
        Assert.Contains("--indent-size", args); Assert.Contains("2", args);
        Assert.Contains("--insert-final-newline", args);
        Assert.Equal("a.bicep", args[^1]);
    }

    // ---- version ----

    [Fact]
    public void Version_Is_Just_The_Flag()
    {
        Assert.Equal(["--version"], Bicep.Version(FakeTool()).Arguments);
    }

    // ---- raw ----

    [Fact]
    public void Raw_Requires_Args()
    {
        Assert.Throws<ArgumentException>(() => Bicep.Raw(FakeTool()));
    }

    [Fact]
    public void Raw_Forwards_Verbatim()
    {
        var plan = Bicep.Raw(FakeTool(), "decompile", "main.json");
        Assert.Equal(["decompile", "main.json"], plan.Arguments);
    }

    // ---- deploy.group ----

    [Fact]
    public void Deploy_Group_Requires_ResourceGroup()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Bicep.Deploy.Group(FakeTool(), s => s.SetTemplateFile("a.bicep")));
    }

    [Fact]
    public void Deploy_Group_Requires_Template_Source()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Bicep.Deploy.Group(FakeTool(), s => s.SetResourceGroup("rg")));
    }

    [Fact]
    public void Deploy_Group_Template_Sources_Are_Mutually_Exclusive()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Bicep.Deploy.Group(FakeTool(), s => s
                .SetResourceGroup("rg")
                .SetTemplateFile("a.bicep")
                .SetTemplateUri("https://example.com/a.json")));
    }

    [Fact]
    public void Deploy_Group_Verb_Prefix_Is_Three_Tokens()
    {
        var plan = Bicep.Deploy.Group(FakeTool(), s => s
            .SetResourceGroup("rg-strata-test")
            .SetTemplateFile("infra/main.bicep"));
        Assert.Equal(["deployment", "group", "create"], plan.Arguments.Take(3));
    }

    [Fact]
    public void Deploy_Group_Round_Trips_All_Knobs()
    {
        var plan = Bicep.Deploy.Group(FakeTool(), s => s
            .SetResourceGroup("rg-strata-test")
            .SetTemplateFile("infra/main.bicep")
            .AddParameter("infra/params/test.bicepparam")
            .AddInlineParameter("location", "eastus")
            .AddInlineParameter("env", "test")
            .SetName("strata-deploy-2026-05-11")
            .SetMode("Incremental")
            .SetRollbackOnError()
            .SetNoPrompt()
            .SetSubscription("sub-id")
            .SetOutput("json"));
        var args = plan.Arguments;
        Assert.Contains("--resource-group", args); Assert.Contains("rg-strata-test", args);
        Assert.Contains("--template-file", args); Assert.Contains("infra/main.bicep", args);
        // --parameters repeats — once per Add* call.
        var first = IndexOf(args, "--parameters");
        var second = IndexOf(args, "--parameters", first + 1);
        var third = IndexOf(args, "--parameters", second + 1);
        Assert.True(first >= 0 && second > first && third > second);
        Assert.Equal("infra/params/test.bicepparam", args[first + 1]);
        Assert.Equal("location=eastus", args[second + 1]);
        Assert.Equal("env=test", args[third + 1]);
        Assert.Contains("--name", args); Assert.Contains("strata-deploy-2026-05-11", args);
        Assert.Contains("--mode", args); Assert.Contains("Incremental", args);
        Assert.Contains("--rollback-on-error", args);
        Assert.Contains("--no-prompt", args);
        Assert.Contains("--subscription", args); Assert.Contains("sub-id", args);
        Assert.Contains("--output", args); Assert.Contains("json", args);
    }

    [Fact]
    public void Deploy_Group_WhatIf_Mode()
    {
        var plan = Bicep.Deploy.Group(FakeTool(), s => s
            .SetResourceGroup("rg")
            .SetTemplateFile("a.bicep")
            .SetWhatIf()
            .SetWhatIfResultFormat("FullResourcePayloads"));
        Assert.Contains("--what-if", plan.Arguments);
        Assert.Contains("--what-if-result-format", plan.Arguments);
        Assert.Contains("FullResourcePayloads", plan.Arguments);
    }

    [Fact]
    public void Deploy_Group_NoWait_Round_Trips()
    {
        var plan = Bicep.Deploy.Group(FakeTool(), s => s
            .SetResourceGroup("rg")
            .SetTemplateFile("a.bicep")
            .SetNoWait());
        Assert.Contains("--no-wait", plan.Arguments);
    }

    [Fact]
    public void Deploy_Group_TemplateUri_Variant()
    {
        var plan = Bicep.Deploy.Group(FakeTool(), s => s
            .SetResourceGroup("rg")
            .SetTemplateUri("https://example.com/main.json"));
        Assert.Contains("--template-uri", plan.Arguments);
        Assert.Contains("https://example.com/main.json", plan.Arguments);
        Assert.DoesNotContain("--template-file", plan.Arguments);
    }

    [Fact]
    public void Deploy_Group_TemplateSpec_Variant()
    {
        var plan = Bicep.Deploy.Group(FakeTool(), s => s
            .SetResourceGroup("rg")
            .SetTemplateSpecId("/subscriptions/x/providers/Microsoft.Resources/templateSpecs/myspec/versions/1.0"));
        Assert.Contains("--template-spec", plan.Arguments);
    }

    // ---- nulls ----

    [Fact]
    public void Null_Tool_Throws_For_Every_Verb()
    {
        Assert.Throws<ArgumentNullException>(() => Bicep.Build(null!, s => s.SetFile("a")));
        Assert.Throws<ArgumentNullException>(() => Bicep.Lint(null!, s => s.SetFile("a")));
        Assert.Throws<ArgumentNullException>(() => Bicep.Format(null!, s => s.SetFile("a")));
        Assert.Throws<ArgumentNullException>(() => Bicep.Version(null!));
        Assert.Throws<ArgumentNullException>(() => Bicep.Raw(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => Bicep.Deploy.Group(null!, s => s.SetResourceGroup("rg").SetTemplateFile("a")));
    }

    [Fact]
    public void Null_Configurer_Throws_For_Required_Verbs()
    {
        // Object-init overloads (TAM-161 fanout) made these calls ambiguous against
        // a bare `null!`; cast the null to the configurer delegate type to keep this
        // test scoped to the fluent shape. The object-init null-check is exercised
        // in ObjectInitTests.
        Assert.Throws<ArgumentNullException>(() => Bicep.Build(FakeTool(), (Action<BicepBuildSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => Bicep.Lint(FakeTool(), (Action<BicepLintSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => Bicep.Format(FakeTool(), (Action<BicepFormatSettings>)null!));
        Assert.Throws<ArgumentNullException>(() => Bicep.Deploy.Group(FakeTool(), (Action<BicepDeployGroupSettings>)null!));
    }

    [Fact]
    public void Working_Directory_Flows_To_Plan()
    {
        var cwd = Path.GetTempPath();
        var plan = Bicep.Build(FakeTool(), s => s.SetFile("a.bicep").SetWorkingDirectory(cwd));
        Assert.Equal(cwd, plan.WorkingDirectory);
    }
}
