using System.Text.Json;
using System.Text.Json.Nodes;

namespace ZoneTray;

/// <summary>
/// Built-in layout templates. Writes FancyZones default-layouts.json, which the
/// engine re-applies on every start (default layouts in applied-layouts.json are
/// ignored by the official parser), so a restart is sufficient to switch layout.
/// </summary>
internal static class LayoutTemplates
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "PowerToys", "FancyZones");

    internal static readonly string FilePath = Path.Combine(DataDir, "default-layouts.json");

    internal sealed record Template(string Label, string Type, int ZoneCount);

    internal static readonly Template[] All =
    {
        new("左右各半（2 列）", "columns", 2),
        new("三等分列", "columns", 3),
        new("三等分行", "rows", 3),
        new("2×2 网格", "grid", 4),
        new("主区 + 右侧两列", "priority-grid", 3),
        new("焦点 + 两侧", "focus", 3),
    };

    internal static string? ReadCurrentType()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }
            var node = JsonNode.Parse(File.ReadAllText(FilePath));
            var list = node?["default-layouts"]?.AsArray();
            var first = list?.FirstOrDefault(a => a?["monitor-configuration"]?.GetValue<string>() == "horizontal");
            return first?["layout"]?["type"]?.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }

    internal static void Apply(Template template)
    {
        Directory.CreateDirectory(DataDir);

        JsonObject root;
        if (File.Exists(FilePath))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(FilePath))!.AsObject();
            }
            catch
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        if (root["default-layouts"] is not JsonArray layouts)
        {
            layouts = new JsonArray();
            root["default-layouts"] = layouts;
        }

        // Both monitor orientations get the same template for predictability.
        foreach (var orientation in new[] { "horizontal", "vertical" })
        {
            var entry = layouts
                .OfType<JsonObject>()
                .FirstOrDefault(a => a["monitor-configuration"]?.GetValue<string>() == orientation);

            if (entry is null)
            {
                entry = new JsonObject { ["monitor-configuration"] = orientation };
                layouts.Add(entry);
            }

            entry["layout"] = new JsonObject
            {
                ["uuid"] = "",
                ["type"] = template.Type,
                ["show-spacing"] = true,
                ["spacing"] = 16,
                ["zone-count"] = template.ZoneCount,
                ["sensitivity-radius"] = 20,
            };
        }

        File.WriteAllText(FilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
