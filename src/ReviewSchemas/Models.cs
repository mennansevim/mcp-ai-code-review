using System.Text.Json.Serialization;

namespace ReviewSchemas;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity { Info, Low, Medium, High }

public sealed record Finding(
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("severity")] Severity Severity,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("explanation")] string Explanation,
    [property: JsonPropertyName("suggested_fix")] string SuggestedFix
);

public sealed record ReviewResponse(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("findings")] List<Finding> Findings
);
