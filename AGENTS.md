# Spectre.Yaml — Agent & Contributor Guide

This file documents the architecture, conventions, and workflows for contributors and AI agents
working on this repository.

## Repository layout

```
Spectre.Yaml/
├── readme.md                      # User-facing documentation (rendered on GitHub + NuGet)
├── src/
│   ├── Spectre.Yaml/
│   │   ├── YamlText.cs            # Core implementation (YamlText + YamlTextExtensions)
│   │   └── readme.md              # NuGet package readme (includes root readme.md#content)
│   ├── Tests/
│   │   └── YamlTextTests.cs       # xUnit tests
│   ├── Sample/
│   │   └── Program.cs             # Runnable demo showing YamlText inside a Panel
│   ├── Directory.Build.props      # Shared build/NuGet/versioning defaults
│   └── Directory.Build.targets    # Shared build targets (packaging, source control)
└── .github/
    └── workflows/                 # CI (build, test, release)
```

## Build and test

```shell
dotnet build Spectre.Yaml.slnx
dotnet test  Spectre.Yaml.slnx
```

- Target framework: **net10.0**
- Language version: **Latest** (C# 13+), **Nullable** enabled, `strict` features
- Warnings-as-errors in CI and Release configurations

## Architecture

### `YamlText` (Spectre.Console namespace)

`YamlText` extends `JustInTimeRenderable` (from `Spectre.Console`). The heavy work is deferred
to `Build()`, which is called by Spectre's rendering pipeline exactly once and must return an
`IRenderable`.

```
YamlText : JustInTimeRenderable
  ├── string yaml          (raw YAML, never null)
  ├── Style? KeyStyle      (default: Color.Grey)
  ├── Style? StringStyle   (default: Color.Red)
  ├── Style? NumberStyle   (default: Color.Blue)
  ├── Style? BooleanStyle  (default: Color.Green)
  ├── Style? NullStyle     (default: Color.Grey)
  └── Style? CommentStyle  (default: Color.Grey + Decoration.Dim)
```

**Constructors**

| Constructor | Input | Notes |
|---|---|---|
| `YamlText(string yaml)` | Raw YAML | Primary constructor |
| `YamlText(JsonNode json)` | `System.Text.Json.Nodes.JsonNode` | Converted via `ConvertJsonToYaml` |
| `YamlText(JsonElement json)` | `System.Text.Json.JsonElement` | Converted via `ConvertJsonToYaml` |
| `YamlText(object value)` | Arbitrary .NET object | Serialized with `JsonSerializer`, then converted |

All constructors funnel through the primary `YamlText(string yaml)`.

### `Build()` — YAML rendering pipeline

`Build()` uses a **YamlDotNet streaming parser** (`YamlDotNet.Core.Parser`) to walk the event
stream and accumulate styled spans into a single Spectre `Paragraph`.

Key state variables:

| Variable | Purpose |
|---|---|
| `mappingDepth` | How many mapping levels deep we are |
| `sequenceDepth` | How many sequence levels deep we are |
| `expectingKey` | Whether the next scalar is a mapping key |
| `indent` | Current indentation in spaces |
| `indentStack` | Stack to restore indent when leaving a mapping/sequence |
| `needNewline` | Whether to emit a newline before the next token |
| `firstEvent` | Suppresses indent increase on the outermost container |
| `sequenceItemFirstKey` | Marks the first key of a direct sequence-item mapping (emits `- `) |
| `sequenceBaseMappingDepth` | Tracks mapping depth at each sequence level to detect scalar items |

**Token classification** (`AppendStyledValue`):
1. Null: `"null"`, `"~"`, or empty plain scalar → `nullStyle`
2. Boolean: `"true"` / `"false"` (case-insensitive) → `booleanStyle`
3. Number: parses with `double.TryParse` + `ScalarStyle.Plain` guard → `numberStyle`
4. Quoted string: single/double-quoted → `stringStyle` (wrapped in `"…"`)
5. Plain string → `stringStyle`

Markup brackets are escaped via `EscapeMarkup` (`[` → `[[`, `]` → `]]`) to avoid conflicts
with Spectre's markup syntax.

### JSON → YAML conversion

When a `JsonNode`, `JsonElement`, or object is passed, `ConvertJsonToYaml` converts it first:

```
ConvertJsonToYaml(string json)
  └── WriteElement(writer, element, indent, isSequenceItem)
        ├── Object  → property-by-property, recursive
        ├── Array   → each item with isSequenceItem=true (emits "- " prefix)
        └── Scalar  → WriteScalar (handles String/Number/True/False/Null)
```

String values containing `\n`, `:`, `#`, `'`, or `"` are double-quoted and escaped.

### `YamlTextExtensions`

Fluent extension methods pair with every `Style` property:
- `KeyStyle(Style?)` / `KeyColor(Color)` → sets `YamlText.KeyStyle`
- Same pattern for `String`, `Number`, `Boolean`, `Null`, `Comment`

All extension methods return `YamlText` for chaining and throw `ArgumentNullException` for a
null `text` argument (via the global `ThrowIfNull` using from `Directory.Build.props`).

## Code conventions

- **Namespace**: `Spectre.Console` — `YamlText` sits in the same namespace as Spectre itself so
  users don't need an extra `using`.
- **RootNamespace**: set to the first segment of the project name (`Spectre` for
  `Spectre.Yaml`) via `Directory.Build.props`.
- **Global usings**: `ThrowIfNull` (from `System.ArgumentNullException`) and
  `ThrowIfNullOrEmpty` are available without explicit `using` statements.
- **Nullable**: enabled project-wide; all public API parameters must be null-checked.
- **XML docs**: required on all public members; `GenerateDocumentationFile` is `true` by default
  for non-test projects.
- **No pager / no interactive output** in tests: use `Spectre.Console.Testing.TestConsole`.

## Testing

Tests live in `src/Tests/YamlTextTests.cs` and use **xUnit** with `Spectre.Console.Testing`.

`TestConsole` captures rendered output as plain text. Tests assert that expected tokens appear
in `console.Output`. Style/color verification is intentionally lightweight (just "no throw +
non-empty output") because `TestConsole` strips ANSI codes.

Add a new test for every new constructor, property, or rendering behaviour. Follow the
`Can_<verb>_<scenario>` naming pattern.

## NuGet packaging

- Package ID: **`Devlooped.Spectre.Yaml`**
- Packager: **NuGetizer** (via `<PackageReference Include="NuGetizer" />`)
- `src/Spectre.Yaml/readme.md` is the NuGet package readme; it includes `../../readme.md#content`
  via the `<!-- include … -->` directive processed by the Devlooped include toolchain.
- Local builds use version `42.42.42` to always exceed published versions.

## Dependency chain

```
Spectre.Console  (rendering framework)
YamlDotNet       (YAML streaming parser — YamlDotNet.Core.Parser)
System.Text.Json (built-in; used for JSON→YAML conversion)
```
