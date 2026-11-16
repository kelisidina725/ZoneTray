using System.Text.Json;
using ZoneTray;

// Redirect LocalApplicationData before LayoutTemplates static init.
var sandbox = Path.Combine(Path.GetTempPath(), "ZoneTray-LayoutTest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);
Environment.SetEnvironmentVariable("LOCALAPPDATA", sandbox);

int pass = 0, fail = 0;
void Check(string name, bool ok)
{
    if (ok) { pass++; Console.WriteLine("PASS " + name); }
    else { fail++; Console.WriteLine("FAIL " + name); }
}

var t = LayoutTemplates.All[3]; // 2x2 grid
LayoutTemplates.Apply(t);
Check("file created", File.Exists(LayoutTemplates.FilePath));

var root = JsonDocument.Parse(File.ReadAllText(LayoutTemplates.FilePath)).RootElement;
Check("has default-layouts", root.TryGetProperty("default-layouts", out var arr));
var items = arr.EnumerateArray().ToList();
Check("two orientations", items.Count == 2);

foreach (var label in new[] { "horizontal", "vertical" })
{
    var entry = items.FirstOrDefault(x => x.TryGetProperty("monitor-configuration", out var m) && m.GetString() == label);
    Check(label + " present", entry.ValueKind != JsonValueKind.Undefined);
    if (entry.ValueKind != JsonValueKind.Undefined)
    {
        var layout = entry.GetProperty("layout");
        Check(label + " type", layout.GetProperty("type").GetString() == "grid");
        Check(label + " zone-count", layout.GetProperty("zone-count").GetInt32() == 4);
        Check(label + " uuid empty", layout.GetProperty("uuid").GetString() == "");
    }
}

var readBack = LayoutTemplates.ReadCurrentType();
Check("ReadCurrentType", readBack == "grid");

// Apply another template, verify overwrite (not append)
LayoutTemplates.Apply(LayoutTemplates.All[1]); // columns 3
var root2 = JsonDocument.Parse(File.ReadAllText(LayoutTemplates.FilePath)).RootElement;
var items2 = root2.GetProperty("default-layouts").EnumerateArray().ToList();
Check("no duplicate entries", items2.Count == 2);
var horiz = items2.First(x => x.GetProperty("monitor-configuration").GetString() == "horizontal");
Check("overwrite type", horiz.GetProperty("layout").GetProperty("type").GetString() == "columns");
Check("overwrite zone-count", horiz.GetProperty("layout").GetProperty("zone-count").GetInt32() == 3);

Directory.Delete(sandbox, true);
Console.WriteLine($"RESULT {pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
