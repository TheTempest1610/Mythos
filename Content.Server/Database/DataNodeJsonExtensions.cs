using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Database;

public static class DataNodeJsonExtensions
{
    private static JsonNode ToJsonNode(this MappingDataNode node)
    {
        return new JsonObject(node.Children.Select(kvp => new KeyValuePair<string, JsonNode?>(kvp.Key, kvp.Value.ToJsonNode())));
    }

    private static JsonNode ToJsonNode(this SequenceDataNode node)
    {
        return new JsonArray(node.Select(ToJsonNode).ToArray());
    }

	public static JsonNode? ToJsonNode(this DataNode node)
	{
        return node switch
        {
            ValueDataNode valueDataNode => JsonValue.Create(valueDataNode.IsNull ? null : valueDataNode.Value),
            MappingDataNode mappingDataNode => mappingDataNode.ToJsonNode(),
            SequenceDataNode sequenceNode => sequenceNode.ToJsonNode(),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
	}

    public static DataNode ToDataNode(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => new MappingDataNode(element.EnumerateObject().ToDictionary(kvp => kvp.Name, kvp => kvp.Value.ToDataNode())),
            JsonValueKind.Array => new SequenceDataNode(element.EnumerateArray().Select(item => item.ToDataNode()).ToList()),
            JsonValueKind.Number => new ValueDataNode(element.GetRawText()),
            JsonValueKind.String => new ValueDataNode(element.GetString()),
            JsonValueKind.True => new ValueDataNode("true"),
            JsonValueKind.False => new ValueDataNode("false"),
            // Mythos: must be the IsNull-sentinel ValueDataNode, not the
            // literal string "null". Otherwise nullable value-typed
            // [DataField]s (e.g. Marking.MythosSizeIndex which is int?)
            // try to parse the string "null" as their underlying type
            // and crash profile load. The sibling JsonNode overload
            // below already uses ValueDataNode.Null() correctly; this
            // line drifted before any null-valued non-string field
            // existed in the saved schema.
            JsonValueKind.Null => ValueDataNode.Null(),
            _ => throw new ArgumentOutOfRangeException(nameof(element)),
        };
    }

    public static DataNode ToDataNode(this JsonNode? node)
    {
        return node switch
        {
            null => ValueDataNode.Null(),
            JsonValue value => new ValueDataNode(value.GetValue<string>()),
            JsonArray array => new SequenceDataNode(array.Select(item => item.ToDataNode()).ToList()),
            JsonObject obj => new MappingDataNode(obj.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDataNode())),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }
}
