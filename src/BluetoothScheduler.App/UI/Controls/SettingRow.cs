using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BluetoothScheduler.App.UI.Controls;

public class SectionLabel : Control
{
    private string _text = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            Invalidate();
        }
    }

    public SectionLabel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true
        );
        Height = 24;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var brush = new SolidBrush(Theme.Accent);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(_text.ToUpperInvariant(), Theme.SectionHeaderFont, brush, new RectangleF(0, 0, Width, Height), sf);
    }
}

public class SettingRow : Control
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private Control? _trailingControl;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? string.Empty;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Description
    {
        get => _description;
        set
        {
            _description = value ?? string.Empty;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? TrailingControl
    {
        get => _trailingControl;
        set
        {
            if (_trailingControl != null)
            {
                Controls.Remove(_trailingControl);
            }

            _trailingControl = value;
            if (_trailingControl != null)
            {
                Controls.Add(_trailingControl);
                PositionTrailingControl();
            }
        }
    }

    public SettingRow()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true
        );
        Height = 52;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionTrailingControl();
    }

    private void PositionTrailingControl()
    {
        if (_trailingControl != null)
        {
            _trailingControl.Location = new Point(Width - _trailingControl.Width - 4, (Height - _trailingControl.Height) / 2);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        float textRight = _trailingControl != null ? _trailingControl.Left - 12 : Width;
        var titleRect = new RectangleF(0, 4, textRight, 20);
        var descRect = new RectangleF(0, 24, textRight, 24);

        g.DrawString(_title, Theme.BodyBoldFont, Theme.TextPrimaryBrush, titleRect);
        g.DrawString(_description, Theme.CaptionFont, Theme.TextSecondaryBrush, descRect);
    }
}
