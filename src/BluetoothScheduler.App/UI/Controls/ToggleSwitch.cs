using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace BluetoothScheduler.App.UI.Controls;

public class ToggleSwitch : Control
{
    private bool _checked;
    private float _animationProgress = 0.0f;
    private readonly Timer _animTimer;
    private bool _isHovered;
    private bool _isFocused;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked != value)
            {
                _checked = value;
                if (!IsHandleCreated || !Visible)
                {
                    _animationProgress = _checked ? 1.0f : 0.0f;
                    Invalidate();
                }
                else
                {
                    StartAnimation();
                }
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true
        );

        Size = new Size(38, 22);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.CheckButton;

        _animTimer = new Timer { Interval = 15 };
        _animTimer.Tick += OnAnimationTick;
    }

    private void StartAnimation()
    {
        _animTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        float target = _checked ? 1.0f : 0.0f;
        float step = 0.18f;

        if (Math.Abs(_animationProgress - target) <= step)
        {
            _animationProgress = target;
            _animTimer.Stop();
        }
        else
        {
            _animationProgress += (_checked ? step : -step);
        }

        Invalidate();
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
        Checked = !Checked;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float trackHeight = 16f;
        float trackWidth = 34f;
        float trackX = (Width - trackWidth) / 2f;
        float trackY = (Height - trackHeight) / 2f;
        var trackRect = new RectangleF(trackX, trackY, trackWidth, trackHeight);

        Color trackColor = InterpolateColor(Theme.Border, Theme.AccentTonal, _animationProgress);
        using var trackPath = Theme.CreateRoundedRectangle(trackRect, trackHeight / 2f);
        using var trackBrush = new SolidBrush(trackColor);
        g.FillPath(trackBrush, trackPath);

        if (_animationProgress < 0.5f)
        {
            using var trackBorderPen = new Pen(Theme.Border, 1f);
            g.DrawPath(trackBorderPen, trackPath);
        }

        float thumbDiameter = 20f;
        float thumbMinX = trackX - 2f;
        float thumbMaxX = trackX + trackWidth - thumbDiameter + 2f;
        float thumbX = thumbMinX + (thumbMaxX - thumbMinX) * _animationProgress;
        float thumbY = (Height - thumbDiameter) / 2f;
        var thumbRect = new RectangleF(thumbX, thumbY, thumbDiameter, thumbDiameter);

        Color thumbColor = InterpolateColor(Color.White, Theme.Accent, _animationProgress);
        if (_animationProgress < 0.1f)
        {
            using var thumbBrush = new SolidBrush(Color.White);
            g.FillEllipse(thumbBrush, thumbRect);
            using var thumbBorderPen = new Pen(Theme.Border, 1.2f);
            g.DrawEllipse(thumbBorderPen, thumbRect);
        }
        else
        {
            using var thumbBrush = new SolidBrush(_isHovered ? Theme.AccentHover : thumbColor);
            g.FillEllipse(thumbBrush, thumbRect);
        }

        if (_isFocused)
        {
            using var focusPen = new Pen(Theme.Accent, 1.5f);
            var focusRect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using var focusPath = Theme.CreateRoundedRectangle(focusRect, 6f);
            g.DrawPath(focusPen, focusPath);
        }
    }

    private static Color InterpolateColor(Color c1, Color c2, float t)
    {
        int r = (int)(c1.R + (c2.R - c1.R) * t);
        int g = (int)(c1.G + (c2.G - c1.G) * t);
        int b = (int)(c1.B + (c2.B - c1.B) * t);
        return Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
