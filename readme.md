# Spectre.Yaml

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Spectre.Yaml.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Spectre.Yaml)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Spectre.Yaml.svg?color=darkmagenta)](https://www.nuget.org/packages/Devlooped.Spectre.Yaml)
[![EULA](https://img.shields.io/badge/EULA-OSMF-blue?labelColor=black&color=C9FF30)](https://github.com/devlooped/oss/blob/main/osmfeula.txt)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/devlooped/oss/blob/main/license.txt)

<!-- include https://github.com/devlooped/.github/raw/main/osmf.md -->
<!-- #content -->
## Overview

**Devlooped.Spectre.Yaml** adds a `YamlText` renderable to [Spectre.Console](https://spectreconsole.net/) 
that displays YAML with syntax-highlighted tokens (keys, strings, numbers, booleans, nulls, and 
comments). It also accepts JSON and arbitrary .NET objects, automatically converting them to YAML 
before rendering.

![](https://raw.githubusercontent.com/devlooped/Spectre.Yaml/main/assets/img/order.png)

## Usage

### From a YAML string

```csharp
using Spectre.Console;

AnsiConsole.Write(new YamlText("""
    server:
      host: localhost
      port: 8080
      tls: true
    """));
```

### From a .NET object

```csharp
using Spectre.Console;

var config = new
{
    Server = new { Host = "localhost", Port = 8080, Tls = true },
    Retries = 3,
    Tags = new[] { "web", "api" },
};

AnsiConsole.Write(new YamlText(config));
```

`System.Text.Json.JsonSerializer` serializes the object; the resulting JSON is then converted 
to YAML. `JsonNode` and `JsonElement` overloads are also available.

### Inside a Panel

```csharp
using Spectre.Console;

AnsiConsole.Write(
    new Panel(new YamlText(myObject))
        .Header("Configuration")
        .BorderColor(Color.Yellow)
        .Padding(1, 1));
```

## Customizing colors

Each token type has a configurable `Style`. Use the fluent extension methods for the most 
concise syntax:

```csharp
var text = new YamlText(yaml)
    .KeyColor(Color.Yellow)
    .StringColor(Color.Cyan1)
    .NumberColor(Color.Blue)
    .BooleanColor(Color.Green)
    .NullColor(Color.Grey)
    .CommentColor(Color.DarkSlateGray1);

AnsiConsole.Write(text);
```

Or assign `Style` objects directly when you need full control (foreground, background, 
decorations):

```csharp
var text = new YamlText(yaml)
{
    KeyStyle    = new Style(Color.Yellow, decoration: Decoration.Bold),
    StringStyle = new Style(Color.Cyan1),
};
```

### Default colors

| Token    | Default             |
|----------|---------------------|
| Key      | `Color.Grey`        |
| String   | `Color.Red`         |
| Number   | `Color.Blue`        |
| Boolean  | `Color.Green`       |
| Null     | `Color.Grey`        |
| Comment  | `Color.Grey` + Dim  |

<!-- #content -->
---
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->