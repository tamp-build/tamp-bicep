using System.IO;
using Tamp;
using Xunit;

namespace Tamp.Bicep.Tests;

/// <summary>
/// TAM-161 (satellite fanout): every wrapper verb that accepts an
/// <c>Action&lt;TSettings&gt;</c> configurer also exposes a parallel
/// object-init overload that takes a pre-populated settings instance.
/// Both authoring styles must emit byte-equal <see cref="CommandPlan"/>s.
/// </summary>
public sealed class ObjectInitTests
{
    private static Tool FakeTool(string name = "bicep") =>
        new(AbsolutePath.Create(Path.Combine(Path.GetTempPath(), name)));

    [Fact]
    public void Build_ObjectInit_Emits_Identical_Plan_To_Fluent()
    {
        var bicep = FakeTool("bicep");

        var fluent = Bicep.Build(bicep, s => s
            .SetFile("infra/main.bicep")
            .SetOutFile("infra/main.json")
            .SetNoRestore()
            .SetDiagnosticsFormat("sarif"));

        var objectInit = Bicep.Build(bicep, new BicepBuildSettings
        {
            File = "infra/main.bicep",
            OutFile = "infra/main.json",
            NoRestore = true,
            DiagnosticsFormat = "sarif",
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void Deploy_Group_ObjectInit_Round_Trips_Against_Fluent()
    {
        var az = FakeTool("az");

        var fluent = Bicep.Deploy.Group(az, s => s
            .SetResourceGroup("rg-strata-test")
            .SetTemplateFile("infra/main.bicep")
            .AddParameter("infra/params/test.bicepparam")
            .AddInlineParameter("location", "eastus")
            .SetMode("Incremental")
            .SetNoPrompt()
            .SetSubscription("sub-id"));

        var objectInit = Bicep.Deploy.Group(az, new BicepDeployGroupSettings
        {
            ResourceGroup = "rg-strata-test",
            TemplateFile = "infra/main.bicep",
            Parameters = { "infra/params/test.bicepparam", "location=eastus" },
            Mode = "Incremental",
            NoPrompt = true,
            Subscription = "sub-id",
        });

        Assert.Equal(fluent.Executable, objectInit.Executable);
        Assert.Equal(fluent.Arguments, objectInit.Arguments);
    }

    [Fact]
    public void All_ObjectInit_Overloads_Surface_Compiles_And_Returns_CommandPlan()
    {
        // Smoke test: each wrapper accepts an object-init settings argument and
        // returns a non-null CommandPlan. One assertion per added overload.
        var bicep = FakeTool("bicep");
        var az = FakeTool("az");

        Assert.NotNull(Bicep.Build(bicep, new BicepBuildSettings { File = "a.bicep" }));
        Assert.NotNull(Bicep.Lint(bicep, new BicepLintSettings { File = "a.bicep" }));
        Assert.NotNull(Bicep.Format(bicep, new BicepFormatSettings { File = "a.bicep" }));
        Assert.NotNull(Bicep.Version(bicep, new BicepVersionSettings()));
        Assert.NotNull(Bicep.Deploy.Group(az, new BicepDeployGroupSettings
        {
            ResourceGroup = "rg",
            TemplateFile = "a.bicep",
        }));
    }
}
