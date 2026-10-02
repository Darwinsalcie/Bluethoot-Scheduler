using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluetoothScheduler.App.UI;

public class MaterialMenuRenderer : ToolStripProfessionalRenderer
{
    public MaterialMenuRenderer() : base(new MaterialColorTable())
    {
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Border, 1f);
        var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (e.Item.Selected || e.Item.Pressed)
        {
            var rect = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
            using var path = Theme.CreateRoundedRectangle(rect, 4f);
            using var brush = new SolidBrush(Theme.Surface);
            g.FillPath(brush, path);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.TextPrimary : Theme.TextDisabled;
        e.TextFont = Theme.BodyFont;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var g = e.Graphics;
        int y = e.Item.Height / 2;
        using var pen = new Pen(Theme.Border, 1f);
        g.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // No pintar margen lateral de imagen
    }
}

public class MaterialColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Theme.Surface;
    public override Color MenuItemSelectedGradientBegin => Theme.Surface;
    public override Color MenuItemSelectedGradientEnd => Theme.Surface;
    public override Color MenuItemPressedGradientBegin => Theme.Surface;
    public override Color MenuItemPressedGradientEnd => Theme.Surface;
    public override Color ToolStripDropDownBackground => Theme.Background;
    public override Color ImageMarginGradientBegin => Theme.Background;
    public override Color ImageMarginGradientMiddle => Theme.Background;
    public override Color ImageMarginGradientEnd => Theme.Background;
    public override Color MenuBorder => Theme.Border;
}
