using ClassworksPlugin.Models;
using ClassworksPlugin.Services;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ClassworksPlugin.Tests;

public sealed class ClassworksServiceTests
{
    [Fact]
    public void MergeAssignmentsPreservesBoardFieldsAndExistingCardMetadata()
    {
        var original = JObject.Parse(
            """
            {
              "attendance": { "absent": ["Alice"], "late": [], "exclude": [] },
              "extension": { "owner": "Classworks" },
              "homework": {
                "Math": {
                  "content": "old content",
                  "tags": ["required"],
                  "order": 3
                },
                "exam-1": {
                  "type": "exam",
                  "name": "Midterm",
                  "content": "Room 201"
                },
                "custom-1": {
                  "type": "custom",
                  "name": "Bring materials",
                  "content": "Compass"
                }
              }
            }
            """);
        Assignment[] assignments =
        [
            new() { Title = "Math", Description = "new content" },
            new() { Title = "Physics", Description = "chapter 2" }
        ];

        var merged = ClassworksService.MergeAssignmentsIntoBoard(original, assignments);

        Assert.Equal("new content", merged["homework"]?["Math"]?["content"]?.ToString());
        Assert.Equal("required", merged["homework"]?["Math"]?["tags"]?[0]?.ToString());
        Assert.Equal(3, merged["homework"]?["Math"]?["order"]?.Value<int>());
        Assert.Equal("exam", merged["homework"]?["exam-1"]?["type"]?.ToString());
        Assert.Equal("Midterm", merged["homework"]?["exam-1"]?["name"]?.ToString());
        Assert.Equal("custom", merged["homework"]?["custom-1"]?["type"]?.ToString());
        Assert.Equal("Alice", merged["attendance"]?["absent"]?[0]?.ToString());
        Assert.Equal("Classworks", merged["extension"]?["owner"]?.ToString());
        Assert.Equal("chapter 2", merged["homework"]?["Physics"]?["content"]?.ToString());

        Assert.Equal("old content", original["homework"]?["Math"]?["content"]?.ToString());
        Assert.Null(original["homework"]?["Physics"]);
    }
}
