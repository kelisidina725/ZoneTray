using System.Text.Json;
using System.Text.Json.Nodes;

namespace ZoneTray;

/// <summary>
/// 读写 PowerToys FancyZones 的 settings.json。
/// 键名与官方 Settings.cpp 的 NonLocalizable 常量一致。
/// </summary>
internal static class SettingsStore
{
    private static readonly string DefaultSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "PowerToys", "FancyZones", "settings.json");

    // 供测试注入；生产运行时为 null，使用默认路径。
    internal static string? OverridePath { get; set; }

    private static string SettingsPath => OverridePath ?? DefaultSettingsPath;

    // 8 项窗口行为：显示文案 + 官方配置键 + 默认值
    internal static readonly (string Label, string Key, bool Default)[] Behaviors =
    [
        ("当屏幕分辨率或工作区发生变化时，使窗口保持在各自的区域内", "fancyzones_displayOrWorkAreaChange_moveWindows", true),
        ("在区域布局变化时，分配给区域的窗口将匹配新的大小/位置", "fancyzones_zoneSetChange_moveWindows", false),
        ("将新创建的窗口移动到上一个已知区域", "fancyzones_appLastZone_moveWindows", false),
        ("将新创建的窗口移动到当前活动的监视器（实验性）", "fancyzones_openWindowOnActiveMonitor", false),
        ("在取消贴靠时还原窗口的原始大小", "fancyzones_restoreSize", false),
        ("让被拖动的窗口透明", "fancyzones_makeDraggedWindowTransparent", false),
        ("允许子窗口对齐", "fancyzones_allowChildWindowSnap", false),
        ("贴靠窗口时禁用圆角", "fancyzones_disableRoundCornersOnSnap", false),
    ];

    internal static string FilePath => SettingsPath;

    internal static Dictionary<string, bool> Read()
    {
        var result = new Dictionary<string, bool>();
        foreach (var (_, key, def) in Behaviors)
        {
            result[key] = def;
        }

        if (!File.Exists(SettingsPath))
        {
            return result;
        }

        try
        {
            var json = JsonNode.Parse(File.ReadAllText(SettingsPath)) as JsonObject;
            var props = json?["properties"] as JsonObject;
            if (props is null)
            {
                return result;
            }

            foreach (var (_, key, _) in Behaviors)
            {
                var node = props[key];
                bool? value = node switch
                {
                    JsonObject obj when obj["value"] is JsonValue v => v.TryGetValue<bool>(out var b) ? b : null,
                    JsonValue v => v.TryGetValue<bool>(out var b2) ? b2 : null,
                    _ => null,
                };
                if (value.HasValue)
                {
                    result[key] = value.Value;
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // 配置损坏时回退到默认值，不阻断面板显示。
        }

        return result;
    }

    /// <summary>仅写入 8 个目标键，保留 settings.json 中的其他所有配置。</summary>
    internal static void Write(IReadOnlyDictionary<string, bool> values)
    {
        JsonObject root;
        if (File.Exists(SettingsPath))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(SettingsPath)) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        if (root["properties"] is not JsonObject props)
        {
            props = new JsonObject();
            root["properties"] = props;
        }

        foreach (var (_, key, _) in Behaviors)
        {
            if (props[key] is JsonObject obj)
            {
                obj["value"] = values[key];
            }
            else
            {
                props[key] = new JsonObject { ["value"] = values[key] };
            }
        }

        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(SettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
