using System.IO;
using Tamp;
using Xunit;
using Xunit.Abstractions;

namespace Tamp.Bicep.IntegrationTests;

/// <summary>
/// Exercises the wrapper against real <c>bicep</c> + <c>az</c>
/// binaries. Sticks to local operations (compile + lint + --help
/// shapes). Real ARM deployment lives in consumer pipelines.
/// </summary>
public sealed class BicepIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AbsolutePath _workdir;

    public BicepIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _workdir = AbsolutePath.Create(Path.Combine(Path.GetTempPath(), $"tamp-bicep-it-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_workdir.Value);

        // Trivial Bicep template that exercises params + outputs.
        File.WriteAllText(Path.Combine(_workdir.Value, "trivial.bicep"), """
            param location string = resourceGroup().location
            param env string = 'test'

            output loc string = location
            output environment string = env
            """);
    }

    private static string? ResolveOnPath(string baseName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var names = OperatingSystem.IsWindows()
            ? new[] { $"{baseName}.exe", $"{baseName}.cmd", $"{baseName}.bat", baseName }
            : new[] { baseName };
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(dir)) continue;
            foreach (var n in names)
            {
                var c = Path.Combine(dir, n);
                if (File.Exists(c)) return c;
            }
        }
        return null;
    }

    private static Tool ResolveTool(string name) =>
        new(AbsolutePath.Create(ResolveOnPath(name)
            ?? throw new InvalidOperationException($"{name} not found on PATH.")));

    private CaptureResult Run(CommandPlan plan)
    {
        _output.WriteLine($"$ {plan.Executable} {string.Join(' ', plan.Arguments)}");
        var result = ProcessRunner.Capture(plan);
        foreach (var line in result.Lines)
            _output.WriteLine($"  [{line.Type}] {line.Text}");
        _output.WriteLine($"  → exit {result.ExitCode}");
        return result;
    }

    [Fact]
    public void Version_Reports_Bicep_Version()
    {
        var bicep = ResolveTool("bicep");
        var plan = Bicep.Version(bicep);
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"\d+\.\d+\.\d+", result.StdoutText + result.StderrText);
    }

    [Fact]
    public void Build_Produces_ARM_Json()
    {
        var bicep = ResolveTool("bicep");
        var outPath = Path.Combine(_workdir.Value, "trivial.json");
        var plan = Bicep.Build(bicep, s => s
            .SetFile(Path.Combine(_workdir.Value, "trivial.bicep"))
            .SetOutFile(outPath));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outPath), $"Expected ARM output at {outPath}");
        var arm = File.ReadAllText(outPath);
        Assert.Contains("\"$schema\"", arm);
        Assert.Contains("\"outputs\"", arm);
        Assert.Contains("\"loc\"", arm);
        Assert.Contains("\"environment\"", arm);
    }

    [Fact]
    public void Build_Stdout_Returns_ARM_To_StdOut()
    {
        var bicep = ResolveTool("bicep");
        var plan = Bicep.Build(bicep, s => s
            .SetFile(Path.Combine(_workdir.Value, "trivial.bicep"))
            .SetStdout());
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("\"$schema\"", result.StdoutText);
        // Validate the JSON parses.
        using var doc = System.Text.Json.JsonDocument.Parse(result.StdoutText);
        Assert.True(doc.RootElement.TryGetProperty("outputs", out _));
    }

    [Fact]
    public void Build_Diagnostics_Sarif_Format()
    {
        // Write a file with an obvious lint issue — unused param.
        var dir = Path.Combine(_workdir.Value, "sarif-test");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "unused.bicep"),
            "param unused string = 'value'\noutput x string = 'hello'\n");

        var bicep = ResolveTool("bicep");
        var plan = Bicep.Build(bicep, s => s
            .SetFile(Path.Combine(dir, "unused.bicep"))
            .SetStdout()
            .SetDiagnosticsFormat("sarif"));
        var result = Run(plan);
        // bicep build with lint issues exits 0 (warnings) — exit > 0 only on errors.
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Lint_On_Clean_File_Exits_Zero()
    {
        var bicep = ResolveTool("bicep");
        var plan = Bicep.Lint(bicep, s => s.SetFile(Path.Combine(_workdir.Value, "trivial.bicep")));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Format_Stdout_Returns_Reformatted_Source()
    {
        var bicep = ResolveTool("bicep");
        var plan = Bicep.Format(bicep, s => s
            .SetFile(Path.Combine(_workdir.Value, "trivial.bicep"))
            .SetStdout());
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("param location", result.StdoutText);
    }

    [Fact]
    public void Deploy_Group_Help_Surfaces_Expected_Flags()
    {
        // `az deployment group create --help` confirms our typed surface
        // matches the CLI's flag names. Real deploy needs a sub + auth.
        var az = ResolveTool("az");
        // Driving az --help via Bicep.Raw would be ugly (wrong tool); use Raw escape on Bicep
        // facade just to construct the plan with the correct tool path.
        var plan = new CommandPlan
        {
            Executable = az.Executable.Value,
            Arguments = new[] { "deployment", "group", "create", "--help" },
        };
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var combined = result.StdoutText + result.StderrText;
        foreach (var flag in new[] { "--resource-group", "--template-file", "--parameters", "--mode", "--what-if", "--no-prompt" })
        {
            Assert.Contains(flag, combined);
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_workdir.Value, recursive: true); } catch { }
    }
}
