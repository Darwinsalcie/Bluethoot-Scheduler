using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BluetoothScheduler.App.UI.Controls;

public class DayChip : Control
{
    private readonly DayOfWeek _dayOfWeek;
    private readonly string _label;
    private bool _isSelected;
    private bool _isHovered;
    private bool _isFocused;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DayOfWeek DayOfWeek => _dayOfWeek;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                Invalidate();
                SelectedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? SelectedChanged;

    public DayChip(DayOfWeek dayOfWeek, string label)
    {
        _dayOfWeek = dayOfWeek;
        _label = label;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true
        );

        Size = new Size(36, 36);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;
        AccessibleName = dayOfWeek.ToString();
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

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        IsSelected = !IsSelected;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            IsSelected = !IsSelected;
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(1.5f, 1.5f, Width - 3f, Height - 3f);

        if (IsSelected)
        {
            using var brush = new SolidBrush(_isHovered ? Theme.AccentHover : Theme.Accent);
            g.FillEllipse(brush, rect);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(_label, Theme.BodyBoldFont, Theme.WhiteBrush, rect, sf);
        }
        else
        {
            if (_isHovered)
            {
                using var hoverBrush = new SolidBrush(Theme.Surface);
                g.FillEllipse(hoverBrush, rect);
            }

            using var pen = new Pen(Theme.Border, 1.2f);
            g.DrawEllipse(pen, rect);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(_label, Theme.BodyBoldFont, Theme.TextSecondaryBrush, rect, sf);
        }

        if (_isFocused)
        {
            using var focusPen = new Pen(Theme.Accent, 1.5f);
            var focusRect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            g.DrawEllipse(focusPen, focusRect);
        }
    }
}
