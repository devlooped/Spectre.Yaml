using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Tests;

public class YamlTextTests
{
    [Fact]
    public void Can_render_simple_yaml_string()
    {
        // Arrange
        var yaml = "name: hello\nage: 42\nactive: true";
        var console = new TestConsole();

        // Act
        console.Write(new YamlText(yaml));

        // Assert
        var output = console.Output;
        Assert.Contains("name", output);
        Assert.Contains("hello", output);
        Assert.Contains("42", output);
        Assert.Contains("true", output);
    }

    [Fact]
    public void Can_render_nested_yaml()
    {
        var yaml = """
            person:
              name: Alice
              address:
                city: Wonderland
            """;
        var console = new TestConsole();

        console.Write(new YamlText(yaml));

        var output = console.Output;
        Assert.Contains("person", output);
        Assert.Contains("Alice", output);
        Assert.Contains("Wonderland", output);
    }

    [Fact]
    public void Can_render_yaml_with_list()
    {
        var yaml = """
            items:
              - first
              - second
              - third
            """;
        var console = new TestConsole();

        console.Write(new YamlText(yaml));

        var output = console.Output;
        Assert.Contains("items", output);
        Assert.Contains("first", output);
        Assert.Contains("second", output);
    }

    [Fact]
    public void Can_render_from_json_node()
    {
        var node = new JsonObject
        {
            ["name"] = "test",
            ["count"] = 5,
            ["enabled"] = true,
        };
        var console = new TestConsole();

        console.Write(new YamlText(node));

        var output = console.Output;
        Assert.Contains("name", output);
        Assert.Contains("test", output);
        Assert.Contains("5", output);
        Assert.Contains("true", output);
    }

    [Fact]
    public void Can_render_from_json_element()
    {
        var json = """{"key": "value", "num": 123}""";
        using var doc = JsonDocument.Parse(json);
        var console = new TestConsole();

        console.Write(new YamlText(doc.RootElement));

        var output = console.Output;
        Assert.Contains("key", output);
        Assert.Contains("value", output);
        Assert.Contains("123", output);
    }

    [Fact]
    public void Can_render_from_object()
    {
        var obj = new { Name = "Bob", Age = 30, Active = false };
        var console = new TestConsole();

        console.Write(new YamlText(obj));

        var output = console.Output;
        Assert.Contains("Bob", output);
        Assert.Contains("30", output);
        Assert.Contains("false", output);
    }

    [Fact]
    public void Can_render_null_value()
    {
        var yaml = "value: null";
        var console = new TestConsole();

        console.Write(new YamlText(yaml));

        var output = console.Output;
        Assert.Contains("null", output);
    }

    [Fact]
    public void Throws_on_null_yaml_string()
    {
        Assert.Throws<ArgumentNullException>(() => new YamlText((string)null!));
    }

    [Fact]
    public void Throws_on_null_json_node()
    {
        Assert.Throws<ArgumentNullException>(() => new YamlText((JsonNode)null!));
    }

    [Fact]
    public void Throws_on_null_object()
    {
        Assert.Throws<ArgumentNullException>(() => new YamlText((object)null!));
    }

    [Fact]
    public void Can_customize_styles()
    {
        var yaml = "name: hello";
        var console = new TestConsole();
        var text = new YamlText(yaml)
        {
            KeyStyle = new Style(Color.Yellow),
            StringStyle = new Style(Color.Cyan1),
        };

        console.Write(text);

        // Just verify it doesn't throw and produces output
        Assert.NotEmpty(console.Output);
    }

    [Fact]
    public void Extension_methods_are_fluent()
    {
        var text = new YamlText("key: value")
            .KeyColor(Color.Yellow)
            .StringColor(Color.Cyan1)
            .NumberColor(Color.Blue)
            .BooleanColor(Color.Green)
            .NullColor(Color.Grey)
            .CommentColor(Color.DarkSlateGray1);

        Assert.NotNull(text.KeyStyle);
        Assert.NotNull(text.StringStyle);
        Assert.NotNull(text.NumberStyle);
        Assert.NotNull(text.BooleanStyle);
        Assert.NotNull(text.NullStyle);
        Assert.NotNull(text.CommentStyle);
    }

    [Fact]
    public void Can_render_json_array_as_yaml()
    {
        var node = new JsonArray(
            JsonValue.Create("alpha"),
            JsonValue.Create("beta"));
        var console = new TestConsole();

        console.Write(new YamlText(node));

        var output = console.Output;
        Assert.Contains("alpha", output);
        Assert.Contains("beta", output);
    }
}
