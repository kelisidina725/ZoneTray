using System.Text.Json.Nodes;
using ZoneTray;

var tempDir = Path.Combine(Path.GetTempPath(), "ZoneTray-Test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDir);
var testFile = Path.Combine(tempDir, "settings.json");
SettingsStore.OverridePath = testFile;

int passed = 0, failed = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}");
    if (ok) passed++; else failed++;
}

try
{
    // 用例 1：模拟真实 settings.json，含其他键与 8 个目标键
    var seed = JsonNode.Parse("""
    {"properties":{
      "fancyzones_shiftDrag":{"value":true},
      "fancyzones_displayOrWorkAreaChange_moveWindows":{"value":true},
      "fancyzones_zoneSetChange_moveWindows":{"value":false},
      "fancyzones_appLastZone_moveWindows":{"value":false},
      "fancyzones_openWindowOnActiveMonitor":{"value":false},
      "fancyzones_restoreSize":{"value":false},
      "fancyzones_makeDraggedWindowTransparent":{"value":false},
      "fancyzones_allowChildWindowSnap":{"value":false},
      "fancyzones_disableRoundCornersOnSnap":{"value":false},
      "fancyzones_zoneColor":{"value":"#F5FCFF"}
    },"name":"FancyZones","version":"1.0"}
    """)!;
    File.WriteAllText(testFile, seed.ToJsonString());

    var read = SettingsStore.Read();
    Check("读取 8 项数量", read.Count == 8);
    Check("读取 displayOrWorkArea=true", read["fancyzones_displayOrWorkAreaChange_moveWindows"]);
    Check("读取 openWindowOnActive=false", !read["fancyzones_openWindowOnActiveMonitor"]);

    // 用例 2：修改 3 项后写入
    read["fancyzones_openWindowOnActiveMonitor"] = true;
    read["fancyzones_restoreSize"] = true;
    read["fancyzones_disableRoundCornersOnSnap"] = true;
    SettingsStore.Write(read);

    var after = JsonNode.Parse(File.ReadAllText(testFile))!.AsObject();
    var props = after["properties"]!.AsObject();
    Check("写入 openWindowOnActive=true", props["fancyzones_openWindowOnActiveMonitor"]!["value"]!.GetValue<bool>());
    Check("写入 restoreSize=true", props["fancyzones_restoreSize"]!["value"]!.GetValue<bool>());
    Check("写入 disableRoundCorners=true", props["fancyzones_disableRoundCornersOnSnap"]!["value"]!.GetValue<bool>());

    // 用例 3：保留其他键
    Check("保留非目标键 shiftDrag", props["fancyzones_shiftDrag"]!["value"]!.GetValue<bool>());
    Check("保留非目标键 zoneColor", props["fancyzones_zoneColor"]!["value"]!.GetValue<string>() == "#F5FCFF");
    Check("保留根键 name", after["name"]!.GetValue<string>() == "FancyZones");
    Check("保留根键 version", after["version"]!.GetValue<string>() == "1.0");

    // 用例 4：损坏文件回退默认值
    File.WriteAllText(testFile, "{ not json");
    var fallback = SettingsStore.Read();
    Check("损坏文件回退默认 displayOrWorkArea=true", fallback["fancyzones_displayOrWorkAreaChange_moveWindows"]);
    Check("损坏文件回退默认 restoreSize=false", !fallback["fancyzones_restoreSize"]);

    Console.WriteLine($"\nTOTAL: {passed} passed, {failed} failed");
    Environment.ExitCode = failed == 0 ? 0 : 1;
}
finally
{
    Directory.Delete(tempDir, recursive: true);
}
