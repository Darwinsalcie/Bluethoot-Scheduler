using System.Drawing;
using System.Drawing.Drawing2D;

namespace BluetoothScheduler.App.UI;

public static class IconGenerator
{
    public static Icon CreateBluetoothIcon(bool isEnabled, bool isDarkTheme = false)
    {
        // Creamos un bitmap con el glifo de Bluetooth nítido
        int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            Color iconColor;
            if (isEnabled)
            {
                iconColor = isDarkTheme ? Theme.DarkAccent : Theme.LightAccent;
            }
            else
            {
                iconColor = isDarkTheme ? Color.FromArgb(180, 180, 180) : Color.FromArgb(128, 128, 128);
            }

            using var pen = new Pen(iconColor, 2.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            // Dibujar el clásico símbolo de Bluetooth:
            // Línea vertical central
            float cx = size / 2f;
            float topY = 5f;
            float botY = size - 5f;
            g.DrawLine(pen, cx, topY, cx, botY);

            // Trazos diagonales
            float wingX = cx + 7f;
            float midTopY = size * 0.32f;
            float midBotY = size * 0.68f;

            // Desde inferior-izquierda -> superior-derecha -> centro -> inferior-derecha -> superior-izquierda
            PointF pLeftTop = new(cx - 6f, midTopY);
            PointF pLeftBot = new(cx - 6f, midBotY);
            PointF pRightTop = new(wingX, midTopY);
            PointF pRightBot = new(wingX, midBotY);
            PointF pCenter = new(cx, size / 2f);

            // Trazo superior: (pLeftBot -> pRightTop -> pCenter)
            g.DrawLines(pen, new[] { pLeftBot, pRightTop, pCenter });

            // Trazo inferior: (pLeftTop -> pRightBot -> pCenter)
            g.DrawLines(pen, new[] { pLeftTop, pRightBot, pCenter });

            if (!isEnabled)
            {
                // Línea diagonal tachada sutil indicando apagado
                using var slashPen = new Pen(Color.FromArgb(217, 48, 37), 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawLine(slashPen, 6f, size - 6f, size - 6f, 6f);
            }
        }

        IntPtr hIcon = bmp.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
