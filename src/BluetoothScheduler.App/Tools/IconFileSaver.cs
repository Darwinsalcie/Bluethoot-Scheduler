using System.Drawing;
using System.Drawing.Imaging;

namespace BluetoothScheduler.App.Tools;

public static class IconFileSaver
{
    public static void SaveIconFile(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        int size = 256;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Fondo circular azul Material
            using var bgBrush = new SolidBrush(Color.FromArgb(26, 115, 232));
            g.FillEllipse(bgBrush, 8, 8, 240, 240);

            // Símbolo Bluetooth blanco
            using var pen = new Pen(Color.White, 16f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };

            float cx = 128f;
            g.DrawLine(pen, cx, 40f, cx, 216f);

            float wingX = cx + 55f;
            PointF pLeftTop = new(cx - 45f, 90f);
            PointF pLeftBot = new(cx - 45f, 166f);
            PointF pRightTop = new(wingX, 90f);
            PointF pRightBot = new(wingX, 166f);
            PointF pCenter = new(cx, 128f);

            g.DrawLines(pen, new[] { pLeftBot, pRightTop, pCenter });
            g.DrawLines(pen, new[] { pLeftTop, pRightBot, pCenter });
        }

        // Guardar estructura básica de .ico
        using var stream = new FileStream(filePath, FileMode.Create);
        using var bw = new BinaryWriter(stream);

        // Header .ico
        bw.Write((short)0); // Reserved
        bw.Write((short)1); // Type 1 = ICO
        bw.Write((short)1); // Count = 1

        // Image Entry
        using var pngStream = new MemoryStream();
        bmp.Save(pngStream, ImageFormat.Png);
        byte[] pngData = pngStream.ToArray();

        bw.Write((byte)0); // Width 256 -> 0
        bw.Write((byte)0); // Height 256 -> 0
        bw.Write((byte)0); // Color count
        bw.Write((byte)0); // Reserved
        bw.Write((short)1); // Color planes
        bw.Write((short)32); // Bits per pixel
        bw.Write((int)pngData.Length); // Size of image data
        bw.Write((int)22); // Offset to image data

        // Image data (PNG payload)
        bw.Write(pngData);
    }
}
