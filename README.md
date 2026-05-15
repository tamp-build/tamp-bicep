# Tamp.Bicep

Wrapper for the **Bicep CLI** (build/lint/format/version) plus
`az deployment group create` via a unified facade.

```csharp
using Tamp.Bicep;
```

| Package | Bicep | Status |
|---|---|---|
| `Tamp.Bicep` | pre-1.0 (0.4x.x+) | preview |

Requires `Tamp.Core ≥ 1.0.5`. No V-pin since Bicep is pre-1.0 and
the CLI surface is stable across minor releases. A `Tamp.Bicep.V1`
sibling will ship when Bicep 1.0 lands.

## Two tools, one facade

Bicep "deploy" isn't actually a `bicep` CLI verb — ARM deployment
flows through `az`. The facade reflects that:

| Verb | Tool needed |
|---|---|
| `Bicep.Build` / `Lint` / `Format` / `Version` / `Raw` | `bicep` |
| `Bicep.Deploy.Group` | `az` |

```csharp
[NuGetPackage("bicep", UseSystemPath = true)]
readonly Tool BicepTool = null!;

[NuGetPackage("az", UseSystemPath = true)]
readonly Tool AzTool = null!;
```

## Verbs (v0.1.0)

### Bicep CLI

| Verb | Notes |
|---|---|
| `Build` | Compile .bicep → ARM JSON. `--file` / `--pattern` (glob batch), `--outdir` / `--outfile` / `--stdout`, `--no-restore`, `--diagnostics-format default/sarif`. |
| `Lint` | Static analysis. `--diagnostics-format sarif` for CI integration (Code Scanning, Sonar). |
| `Format` | Formatter. `--newline auto/LF/CRLF/CR`, `--indent-kind space/tab`, `--indent-size`, `--insert-final-newline`. |
| `Version` | `bicep --version`. |
| `Raw` | Escape hatch for `decompile`, `restore`, `generate-params`, `publish`. |

### ARM deploy via az

| Verb | Notes |
|---|---|
| `Deploy.Group` | `az deployment group create`. RG-scope. `--template-file` / `--template-uri` / `--template-spec`, repeated `--parameters` (`.bicepparam`, JSON file, or `key=value`), `--mode`, `--rollback-on-error`, `--what-if`, `--confirm-with-what-if`, `--no-prompt`, `--no-wait`. |

## Quick example — pipeline-friendly deploy

```csharp
using Tamp;
using Tamp.Bicep;

[NuGetPackage("bicep", UseSystemPath = true)] readonly Tool BicepTool = null!;
[NuGetPackage("az", UseSystemPath = true)] readonly Tool AzTool = null!;

AbsolutePath Artifacts => RootDirectory / "artifacts";

Target CompileBicep => _ => _.Executes(() =>
    // Produce ARM JSON for review at approval-gate time.
    Bicep.Build(BicepTool, s => s
        .SetFile("infra/main.bicep")
        .SetOutFile(Artifacts / "main.json")));

Target LintBicep => _ => _.Executes(() =>
    Bicep.Lint(BicepTool, s => s
        .SetPattern("./infra/**/*.bicep")
        .SetDiagnosticsFormat("sarif")));

Target WhatIfDeploy => _ => _
    .DependsOn(nameof(CompileBicep))
    .Executes(() =>
        Bicep.Deploy.Group(AzTool, s => s
            .SetResourceGroup($"rg-strata-{Env}")
            .SetTemplateFile("infra/main.bicep")
            .AddParameterFile($"infra/params/{Env}.bicepparam")
            .SetWhatIf()
            .SetNoPrompt()));

Target DeployStrata => _ => _
    .DependsOn(nameof(WhatIfDeploy))
    .Executes(() =>
        Bicep.Deploy.Group(AzTool, s => s
            .SetResourceGroup($"rg-strata-{Env}")
            .SetTemplateFile("infra/main.bicep")
            .AddParameterFile($"infra/params/{Env}.bicepparam")
            .SetMode("Incremental")
            .SetRollbackOnError()
            .SetNoPrompt()));
```

## CI behaviour to know about

**Bicep CLI is NOT preinstalled on GitHub-hosted runners.** The CI
workflow installs it via the official Azure/bicep release assets
(single binary per OS — quicker than `az bicep install`). Consumers'
pipelines should do the same.

**`az` IS preinstalled.** Only `az login` (interactive / SPN / MI /
WIF) is the consumer's responsibility before `Bicep.Deploy.Group`.

**`--no-prompt` is recommended in CI** — without it, `az deployment`
will prompt for missing required parameters and hang on stdin.

## What's NOT in v0.1.0

Available via `Bicep.Raw(BicepTool, ...)`:
- `decompile` — ARM JSON → .bicep
- `restore` — pull external modules
- `generate-params` — derive params file from .bicep
- `publish` — push to module registry

Deploy scopes beyond resource group:
- `Deploy.Subscription` (az deployment sub create)
- `Deploy.ManagementGroup` (az deployment mg create)
- `Deploy.Tenant` (az deployment tenant create)

All slated for v0.2.0 if there's demand.

## Releasing

See [MAINTAINERS.md](MAINTAINERS.md).

## Settings authoring style

Examples above use the fluent `Set*`-chain shape. Every wrapper verb also accepts a `new XxxSettings { ... }` object-init form — both produce identical `CommandPlan`s. The fluent shape stays canonical in docs and the `tamp init` template; opt into object-init scaffolding via `tamp init --settings-style=init`.

See [Build Script Authoring → Two authoring styles](https://github.com/tamp-build/tamp/wiki/Build-Script-Authoring#two-authoring-styles-for-wrapper-calls-120) on the wiki for the side-by-side comparison.
