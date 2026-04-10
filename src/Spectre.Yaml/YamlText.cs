using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console.Rendering;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Spectre.Console;

/// <summary>
/// A renderable piece of YAML text.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="YamlText"/> class
/// from a YAML string.
/// </remarks>
/// <param name="yaml">The YAML string to render.</param>
public sealed class YamlText(string yaml) : JustInTimeRenderable
{
    readonly string yaml = yaml ?? throw new ArgumentNullException(nameof(yaml));

    /// <summary>
    /// Gets or sets the style used for mapping keys.
    /// </summary>
    public Style? KeyStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for string values.
    /// </summary>
    public Style? StringStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for number values.
    /// </summary>
    public Style? NumberStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for boolean values.
    /// </summary>
    public Style? BooleanStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for null values.
    /// </summary>
    public Style? NullStyle { get; set; }

    /// <summary>
    /// Gets or sets the style used for comments.
    /// </summary>
    public Style? CommentStyle { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlText"/> class
    /// from a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="json">The JSON node to convert and render as YAML.</param>
    public YamlText(JsonNode json)
        : this(ConvertJsonToYaml(json?.ToJsonString() ?? throw new ArgumentNullException(nameof(json))))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlText"/> class
    /// from a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="json">The JSON element to convert and render as YAML.</param>
    public YamlText(JsonElement json)
        : this(ConvertJsonToYaml(json.GetRawText()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlText"/> class
    /// from an arbitrary object, serialized via <see cref="JsonSerializer"/>.
    /// </summary>
    /// <param name="value">The object to serialize and render as YAML.</param>
    public YamlText(object value)
        : this(ConvertJsonToYaml(JsonSerializer.Serialize(value ?? throw new ArgumentNullException(nameof(value)))))
    {
    }

    /// <inheritdoc/>
    protected override IRenderable Build()
    {
        var keyStyle = KeyStyle ?? Color.Grey;
        var stringStyle = StringStyle ?? Color.Red;
        var numberStyle = NumberStyle ?? Color.Blue;
        var booleanStyle = BooleanStyle ?? Color.Green;
        var nullStyle = NullStyle ?? Color.Grey;
        var commentStyle = CommentStyle ?? new Style(Color.Grey, decoration: Decoration.Dim);

        var paragraph = new Paragraph();
        var parser = new Parser(new StringReader(yaml));

        var mappingDepth = 0;
        var sequenceDepth = 0;
        var expectingKey = false;
        var indent = 0;
        var needNewline = false;
        var firstEvent = true;
        var sequenceItemFirstKey = false;
        var indentStack = new Stack<int>();
        // Tracks mappingDepth at each sequence level to identify direct sequence items
        var sequenceBaseMappingDepth = new Stack<int>();

        while (parser.MoveNext())
        {
            var current = parser.Current;
            if (current == null)
                break;

            switch (current)
            {
                case StreamStart:
                case StreamEnd:
                case DocumentStart ds when ds.IsImplicit:
                case DocumentEnd de when de.IsImplicit:
                    break;

                case DocumentStart:
                    AppendLine(paragraph, "---", Style.Plain, ref needNewline);
                    break;

                case DocumentEnd:
                    AppendLine(paragraph, "...", Style.Plain, ref needNewline);
                    break;

                case MappingStart:
                    {
                        var isDirectSeqItem = sequenceDepth > 0 &&
                            mappingDepth == sequenceBaseMappingDepth.Peek();

                        // Nested mapping as a mapping value requires a newline after the parent key
                        if (mappingDepth > 0 && !expectingKey && !isDirectSeqItem)
                            needNewline = true;

                        if (isDirectSeqItem)
                            sequenceItemFirstKey = true;

                        indentStack.Push(indent);
                        mappingDepth++;
                        expectingKey = true;

                        if (!firstEvent)
                            indent += 2;

                        firstEvent = false;
                        break;
                    }

                case MappingEnd:
                    indent = indentStack.Pop();
                    mappingDepth--;
                    expectingKey = mappingDepth > 0;
                    break;

                case SequenceStart:
                    // Sequence as a mapping value requires a newline after the parent key
                    if (mappingDepth > 0 && !expectingKey)
                        needNewline = true;

                    sequenceBaseMappingDepth.Push(mappingDepth);
                    indentStack.Push(indent);
                    sequenceDepth++;

                    if (!firstEvent)
                        indent += 2;

                    firstEvent = false;
                    break;

                case SequenceEnd:
                    indent = indentStack.Pop();
                    sequenceBaseMappingDepth.Pop();
                    sequenceDepth--;
                    break;

                case Scalar scalar:
                    if (mappingDepth > 0 && expectingKey)
                    {
                        // Mapping key
                        if (needNewline)
                            paragraph.Append(Environment.NewLine);
                        needNewline = false;

                        if (sequenceItemFirstKey)
                        {
                            paragraph.Append(new string(' ', Math.Max(0, indent - 2)));
                            paragraph.Append("- ", Style.Plain);
                            sequenceItemFirstKey = false;
                        }
                        else
                        {
                            paragraph.Append(new string(' ', indent));
                        }
                        paragraph.Append(scalar.Value + ": ", keyStyle);
                        expectingKey = false;
                    }
                    else if (sequenceDepth > 0 &&
                        mappingDepth == sequenceBaseMappingDepth.Peek())
                    {
                        // Direct scalar sequence item
                        if (needNewline)
                            paragraph.Append(Environment.NewLine);
                        needNewline = false;

                        paragraph.Append(new string(' ', Math.Max(0, indent - 2)));
                        paragraph.Append("- ", Style.Plain);
                        AppendStyledValue(paragraph, scalar, stringStyle, numberStyle, booleanStyle, nullStyle);
                        needNewline = true;
                    }
                    else
                    {
                        // Mapping value or root scalar
                        AppendStyledValue(paragraph, scalar, stringStyle, numberStyle, booleanStyle, nullStyle);
                        needNewline = true;
                        if (mappingDepth > 0)
                            expectingKey = true;
                    }
                    break;

                case Comment comment:
                    if (needNewline)
                        paragraph.Append(Environment.NewLine);
                    needNewline = false;
                    paragraph.Append(new string(' ', indent));
                    paragraph.Append("# " + comment.Value, commentStyle);
                    needNewline = true;
                    break;
            }
        }

        return paragraph;
    }

    static void AppendStyledValue(
        Paragraph paragraph, Scalar scalar,
        Style stringStyle, Style numberStyle, Style booleanStyle, Style nullStyle)
    {
        var value = scalar.Value;

        if (scalar.IsKey)
        {
            paragraph.Append(value, Style.Plain);
            return;
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "~", StringComparison.Ordinal) ||
            value.Length == 0 && scalar.Style == ScalarStyle.Plain)
        {
            paragraph.Append(value.Length == 0 ? "null" : value, nullStyle);
        }
        else if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
        {
            paragraph.Append(value, booleanStyle);
        }
        else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                 scalar.Style == ScalarStyle.Plain)
        {
            paragraph.Append(value, numberStyle);
        }
        else
        {
            if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted)
                paragraph.Append($"\"{EscapeMarkup(value)}\"", stringStyle);
            else
                paragraph.Append(EscapeMarkup(value), stringStyle);
        }
    }

    static void AppendLine(Paragraph paragraph, string text, Style style, ref bool needNewline)
    {
        if (needNewline)
            paragraph.Append(Environment.NewLine);
        paragraph.Append(text, style);
        needNewline = true;
    }

    static string EscapeMarkup(string text) => text.Replace("[", "[[").Replace("]", "]]");

    static string ConvertJsonToYaml(string json)
    {
        using var doc = JsonDocument.Parse(json);
        using var writer = new StringWriter();
        WriteElement(writer, doc.RootElement, 0, false);
        return writer.ToString().TrimEnd();
    }

    static void WriteElement(StringWriter writer, JsonElement element, int indent, bool isSequenceItem)
    {
        var prefix = isSequenceItem
            ? new string(' ', Math.Max(0, indent - 2)) + "- "
            : new string(' ', indent);

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var objectProps = element.EnumerateObject().ToList();
                if (objectProps.Count == 0)
                {
                    writer.Write(prefix + "{}");
                    writer.WriteLine();
                    return;
                }
                var first = true;
                foreach (var prop in objectProps)
                {
                    if (first && isSequenceItem)
                    {
                        writer.Write(new string(' ', Math.Max(0, indent - 2)) + "- ");
                        writer.Write(prop.Name + ":");
                        first = false;
                    }
                    else
                    {
                        writer.Write(new string(' ', indent) + prop.Name + ":");
                    }

                    if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        writer.WriteLine();
                        WriteElement(writer, prop.Value, indent + 2, false);
                    }
                    else
                    {
                        writer.Write(" ");
                        WriteScalar(writer, prop.Value);
                        writer.WriteLine();
                    }
                }
                break;

            case JsonValueKind.Array:
                var items = element.EnumerateArray().ToList();
                if (items.Count == 0)
                {
                    writer.Write(prefix + "[]");
                    writer.WriteLine();
                    return;
                }
                foreach (var item in items)
                {
                    WriteElement(writer, item, indent + 2, true);
                }
                break;

            default:
                writer.Write(prefix);
                WriteScalar(writer, element);
                writer.WriteLine();
                break;
        }
    }

    static void WriteScalar(StringWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var str = element.GetString() ?? "";
                if (str.Contains('\n') || str.Contains(':') || str.Contains('#') ||
                    str.Contains('\'') || str.Contains('"'))
                    writer.Write($"\"{str.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
                else
                    writer.Write(str);
                break;
            case JsonValueKind.Number:
                writer.Write(element.GetRawText());
                break;
            case JsonValueKind.True:
                writer.Write("true");
                break;
            case JsonValueKind.False:
                writer.Write("false");
                break;
            case JsonValueKind.Null:
                writer.Write("null");
                break;
            default:
                writer.Write(element.GetRawText());
                break;
        }
    }
}

/// <summary>
/// Contains extension methods for <see cref="YamlText"/>.
/// </summary>
public static class YamlTextExtensions
{
    /// <summary>
    /// Sets the style used for mapping keys.
    /// </summary>
    public static YamlText KeyStyle(this YamlText text, Style? style)
    {
        ThrowIfNull(text);
        text.KeyStyle = style;
        return text;
    }

    /// <summary>
    /// Sets the color used for mapping keys.
    /// </summary>
    public static YamlText KeyColor(this YamlText text, Color color)
    {
        ThrowIfNull(text);
        text.KeyStyle = new Style(color);
        return text;
    }

    /// <summary>
    /// Sets the style used for string values.
    /// </summary>
    public static YamlText StringStyle(this YamlText text, Style? style)
    {
        ThrowIfNull(text);
        text.StringStyle = style;
        return text;
    }

    /// <summary>
    /// Sets the color used for string values.
    /// </summary>
    public static YamlText StringColor(this YamlText text, Color color)
    {
        ThrowIfNull(text);
        text.StringStyle = new Style(color);
        return text;
    }

    /// <summary>
    /// Sets the style used for number values.
    /// </summary>
    public static YamlText NumberStyle(this YamlText text, Style? style)
    {
        ThrowIfNull(text);
        text.NumberStyle = style;
        return text;
    }

    /// <summary>
    /// Sets the color used for number values.
    /// </summary>
    public static YamlText NumberColor(this YamlText text, Color color)
    {
        ThrowIfNull(text);
        text.NumberStyle = new Style(color);
        return text;
    }

    /// <summary>
    /// Sets the style used for boolean values.
    /// </summary>
    public static YamlText BooleanStyle(this YamlText text, Style? style)
    {
        ThrowIfNull(text);
        text.BooleanStyle = style;
        return text;
    }

    /// <summary>
    /// Sets the color used for boolean values.
    /// </summary>
    public static YamlText BooleanColor(this YamlText text, Color color)
    {
        ThrowIfNull(text);
        text.BooleanStyle = new Style(color);
        return text;
    }

    /// <summary>
    /// Sets the style used for null values.
    /// </summary>
    public static YamlText NullStyle(this YamlText text, Style? style)
    {
        ThrowIfNull(text);
        text.NullStyle = style;
        return text;
    }

    /// <summary>
    /// Sets the color used for null values.
    /// </summary>
    public static YamlText NullColor(this YamlText text, Color color)
    {
        ThrowIfNull(text);
        text.NullStyle = new Style(color);
        return text;
    }

    /// <summary>
    /// Sets the style used for comments.
    /// </summary>
    public static YamlText CommentStyle(this YamlText text, Style? style)
    {
        ThrowIfNull(text);
        text.CommentStyle = style;
        return text;
    }

    /// <summary>
    /// Sets the color used for comments.
    /// </summary>
    public static YamlText CommentColor(this YamlText text, Color color)
    {
        ThrowIfNull(text);
        text.CommentStyle = new Style(color);
        return text;
    }
}
