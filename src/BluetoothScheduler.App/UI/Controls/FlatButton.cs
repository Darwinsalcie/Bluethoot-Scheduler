using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BluetoothScheduler.App.UI.Controls;

public enum FlatButtonVariant
{
    Text,
    Filled
}

public class FlatButton : Control
{
    private string _text = string.Empty;
    private FlatButtonVariant _variant = FlatButtonVariant.Text;
    private bool _isHovered;
    private bool _isPressed;
    private bool _isFocused;

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlatButtonVariant Variant
    {
        get => _variant;
        set
        {
            _variant = value;
            Invalidate();
        }
    }

    public FlatButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true
        );

        Size = new Size(180, 36);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _isFocused = true;
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _isFocused = false;
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
        using var path = Theme.CreateRoundedRectangle(rect, 6f);

        if (_variant == FlatButtonVariant.Filled)
        {
            Color bgColor = _isPressed ? Theme.AccentHover : (_isHovered ? Theme.AccentHover : Theme.Accent);
            using var brush = new SolidBrush(bgColor);
            g.FillPath(brush, path);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(_text, Theme.BodyBoldFont, Theme.WhiteBrush, rect, sf);
        }
        else // Text variant
        {
            if (_isHovered || _isPressed)
            {
                using var hoverBrush = new SolidBrush(Theme.AccentTonal);
                g.FillPath(hoverBrush, path);
            }

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(_isHovered ? Theme.AccentHover : Theme.Accent);
            g.DrawString(_text, Theme.BodyBoldFont, textBrush, rect, sf);
        }

        if (_isFocused)
        {
            using var focusPen = new Pen(Theme.Accent, 1.5f);
            var focusRect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using var focusPath = Theme.CreateRoundedRectangle(focusRect, 6f);
            g.DrawPath(focusPen, focusPath);
        }
    }
}
