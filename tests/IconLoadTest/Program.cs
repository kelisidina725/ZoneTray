using System.Drawing;

var asm = typeof(Program).Assembly;
var stream = asm.GetManifestResourceStream("ZoneTray.ZoneTray.ico");
if (stream is null) { Console.WriteLine("FAIL resource stream null"); return 1; }
using var icon = new Icon(stream);

Console.WriteLine($"ICON_SIZE={icon.Width}x{icon.Height}");
using var bmp = icon.ToBitmap();
// Sample distinctive colors: indigo (79,70,229), cyan (8,145,178), violet (139,92,246)
var colors = new HashSet<string>();
for (var y = 0; y < bmp.Height; y++)
    for (var x = 0; x < bmp.Width; x++)
    {
        var c = bmp.GetPixel(x, y);
        if (c.A > 200) colors.Add($"{c.R},{c.G},{c.B}");
    }
var hasIndigo = colors.Any(c => { var p = c.Split(','); return Math.Abs(int.Parse(p[0]) - 79) < 30 && Math.Abs(int.Parse(p[1]) - 70) < 30 && Math.Abs(int.Parse(p[2]) - 229) < 40; });
var hasCyan = colors.Any(c => { var p = c.Split(','); return Math.Abs(int.Parse(p[0]) - 8) < 40 && Math.Abs(int.Parse(p[1]) - 145) < 30 && Math.Abs(int.Parse(p[2]) - 178) < 30; });
Console.WriteLine($"DISTINCT_COLORS={colors.Count}");
Console.WriteLine($"HAS_INDIGO={hasIndigo} HAS_CYAN={hasCyan}");
return hasIndigo && hasCyan ? 0 : 1;
