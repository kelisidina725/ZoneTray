using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// args: <output.ico> <preview.png>
var icoPath = args[0];
var previewPath = args[1];

var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
var pngs = new List<byte[]>();
byte[]? preview256 = null;

foreach (var s in sizes)
{
    using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    Draw(g, s);
    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    var bytes = ms.ToArray();
    pngs.Add(bytes);
    if (s == 256) preview256 = bytes;
}

// Build multi-image ICO with PNG payloads (Vista+).
using (var fs = File.Create(icoPath))
using (var bw = new BinaryWriter(fs))
{
    bw.Write((ushort)0); // reserved
    bw.Write((ushort)1); // type: icon
    bw.Write((ushort)sizes.Length);
    uint offset = 6 + (uint)(16 * sizes.Length);
    for (var i = 0; i < sizes.Length; i++)
    {
        var s = sizes[i];
        var data = pngs[i];
        bw.Write((byte)(s >= 256 ? 0 : s));
        bw.Write((byte)(s >= 256 ? 0 : s));
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((ushort)1);
        bw.Write((ushort)32);
        bw.Write((uint)data.Length);
        bw.Write(offset);
        offset += (uint)data.Length;
    }
    foreach (var data in pngs) bw.Write(data);
}

if (preview256 is not null) File.WriteAllBytes(previewPath, preview256);
Console.WriteLine($"ICO={icoPath} ({new FileInfo(icoPath).Length} bytes), sizes={sizes.Length}");

static void Draw(Graphics g, int s)
{
    // Design: three rounded tiles representing zone layout; distinct from Windows system icons.
    var pad = s * 0.10f;
    var gap = Math.Max(1f, s * 0.055f);
    var inner = s - 2 * pad;
    var radius = s * 0.18f;

    // Main left zone (indigo), top-right (cyan), bottom-right (violet).
    var leftW = inner * 0.54f;
    var rightW = inner - leftW - gap;
    var halfH = (inner - gap) / 2f;

    FillRoundRect(g, pad, pad, leftW, inner, radius, Color.FromArgb(255, 79, 70, 229));
    FillRoundRect(g, pad + leftW + gap, pad, rightW, halfH, radius * 0.72f, Color.FromArgb(255, 8, 145, 178));
    FillRoundRect(g, pad + leftW + gap, pad + halfH + gap, rightW, halfH, radius * 0.72f, Color.FromArgb(255, 139, 92, 246));
}

static void FillRoundRect(Graphics g, float x, float y, float w, float h, float r, Color color)
{
    r = Math.Min(r, Math.Min(w, h) / 2f);
    using var path = new GraphicsPath();
    path.AddArc(x, y, r * 2, r * 2, 180, 90);
    path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
    path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
    path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
    path.CloseFigure();
    using var brush = new SolidBrush(color);
    g.FillPath(brush, path);
}
