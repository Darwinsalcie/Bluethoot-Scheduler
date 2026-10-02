using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BluetoothScheduler.App.UI.Controls;

public class TimeField : Control
{
    private int _hours = 8;
    private int _minutes = 0;
    private bool _isHovered;
    private bool _isFocused;
    private bool _focusMinutes;
    private bool _hasError;
    private string _inputBuffer = "";

    private RectangleF _btnUpRect;
    private RectangleF _btnDownRect;
    private bool _hoverUp;
    private bool _hoverDown;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TimeOnly Value
    {
        get => new(_hours, _minutes);
        set
        {
            if (_hours != value.Hour || _minutes != value.Minute)
            {
                _hours = value.Hour;
                _minutes = value.Minute;
                _inputBuffer = "";
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError != value)
            {
                _hasError = value;
                Invalidate();
            }
        }
    }

    public event EventHandler? ValueChanged;

    public TimeField()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true
        );

        Size = new Size(130, 40);
        TabStop = true;
        Cursor = Cursors.IBeam;
        AccessibleRole = AccessibleRole.SpinButton;
    }

    public void Increment()
    {
        _inputBuffer = "";
        if (_focusMinutes)
        {
            _minutes = (_minutes + 1) % 60;
        }
        else
        {
            _hours = (_hours + 1) % 24;
        }
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Decrement()
    {
        _inputBuffer = "";
        if (_focusMinutes)
        {
            _minutes = (_minutes - 1 + 60) % 60;
        }
        else
        {
            _hours = (_hours - 1 + 24) % 24;
        }
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
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
        _hoverUp = false;
        _hoverDown = false;
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _isFocused = true;
        _inputBuffer = "";
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _isFocused = false;
        _inputBuffer = "";
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta > 0) Increment();
        else if (e.Delta < 0) Decrement();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Up)
        {
            Increment();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Down)
        {
            Decrement();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Left)
        {
            _focusMinutes = false;
            _inputBuffer = "";
            Invalidate();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Right)
        {
            _focusMinutes = true;
            _inputBuffer = "";
            Invalidate();
            e.Handled = true;
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);

        if (char.IsDigit(e.KeyChar))
        {
            int digit = e.KeyChar - '0';
            if (!_focusMinutes)
            {
                // Escribiendo horas (00-23)
                if (_inputBuffer.Length == 0)
                {
                    if (digit > 2)
                    {
                        // Si escribe 3..9, solo puede ser 03..09 -> pasa directo a minutos
                        _hours = digit;
                        _inputBuffer = "";
                        _focusMinutes = true;
                    }
                    else
                    {
                        _hours = digit;
                        _inputBuffer = digit.ToString();
                    }
                }
                else
                {
                    int combined = int.Parse(_inputBuffer) * 10 + digit;
                    if (combined < 24)
                    {
                        _hours = combined;
                    }
                    else
                    {
                        _hours = digit;
                    }
                    _inputBuffer = "";
                    _focusMinutes = true; // Avanza a minutos automáticamente tras 2 dígitos
                }
            }
            else
            {
                // Escribiendo minutos (00-59)
                if (_inputBuffer.Length == 0)
                {
                    if (digit > 5)
                    {
                        _minutes = digit;
                        _inputBuffer = "";
                    }
                    else
                    {
                        _minutes = digit;
                        _inputBuffer = digit.ToString();
                    }
                }
                else
                {
                    int combined = int.Parse(_inputBuffer) * 10 + digit;
                    if (combined < 60)
                    {
                        _minutes = combined;
                    }
                    else
                    {
                        _minutes = digit;
                    }
                    _inputBuffer = "";
                }
            }

            e.Handled = true;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (e.KeyChar == ':' || e.KeyChar == ' ' || e.KeyChar == '\t')
        {
            _focusMinutes = true;
            _inputBuffer = "";
            e.Handled = true;
            Invalidate();
        }
        else if (e.KeyChar == (char)Keys.Back)
        {
            if (_focusMinutes)
            {
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer = "";
                    _minutes = 0;
                }
                else
                {
                    _focusMinutes = false;
                    _inputBuffer = "";
                }
            }
            else
            {
                _inputBuffer = "";
                _hours = 0;
            }

            e.Handled = true;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool oldUp = _hoverUp;
        bool oldDown = _hoverDown;

        _hoverUp = _btnUpRect.Contains(e.Location);
        _hoverDown = _btnDownRect.Contains(e.Location);

        if (oldUp != _hoverUp || oldDown != _hoverDown)
        {
            Invalidate();
        }

        Cursor = (_hoverUp || _hoverDown) ? Cursors.Hand : Cursors.IBeam;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (_btnUpRect.Contains(e.Location))
        {
            Increment();
        }
        else if (_btnDownRect.Contains(e.Location))
        {
            Decrement();
        }
        else
        {
            float textAreaWidth = Width - 28f;
            _focusMinutes = e.X > (textAreaWidth / 2f + 4f);
            _inputBuffer = "";
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var boxRect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
        using var boxPath = Theme.CreateRoundedRectangle(boxRect, 6f);

        using var bgBrush = new SolidBrush(Theme.Surface);
        g.FillPath(bgBrush, boxPath);

        if (_hasError)
        {
            g.DrawPath(Theme.ErrorPen, boxPath);
        }
        else if (_isFocused)
        {
            g.DrawPath(Theme.AccentPen, boxPath);
        }
        else
        {
            using var borderPen = new Pen(_isHovered ? Theme.TextSecondary : Theme.Border, 1f);
            g.DrawPath(borderPen, boxPath);
        }

        float btnWidth = 24f;
        float btnHeight = Height / 2f;
        _btnUpRect = new RectangleF(Width - btnWidth - 3f, 2f, btnWidth, btnHeight - 2f);
        _btnDownRect = new RectangleF(Width - btnWidth - 3f, btnHeight, btnWidth, btnHeight - 2f);

        DrawArrow(g, _btnUpRect, true, _hoverUp);
        DrawArrow(g, _btnDownRect, false, _hoverDown);

        // Dibujar dígitos con resaltado del segmento activo (horas o minutos)
        float contentWidth = Width - btnWidth - 8f;
        float segmentWidth = 34f;
        float colonWidth = 12f;
        float totalTextWidth = (segmentWidth * 2) + colonWidth;
        float startX = (contentWidth - totalTextWidth) / 2f + 4f;
        float segmentHeight = Height - 8f;
        float segmentY = 4f;

        var hourRect = new RectangleF(startX, segmentY, segmentWidth, segmentHeight);
        var colonRect = new RectangleF(startX + segmentWidth, segmentY, colonWidth, segmentHeight);
        var minRect = new RectangleF(startX + segmentWidth + colonWidth, segmentY, segmentWidth, segmentHeight);

        using var sfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        // Resaltar visualmente el segmento seleccionado
        if (_isFocused)
        {
            var activeRect = _focusMinutes ? minRect : hourRect;
            using var activeBgBrush = new SolidBrush(Theme.AccentTonal);
            using var activePath = Theme.CreateRoundedRectangle(activeRect, 4f);
            g.FillPath(activeBgBrush, activePath);
        }

        string hourStr = $"{_hours:D2}";
        string minStr = $"{_minutes:D2}";

        Brush hourBrush = (_isFocused && !_focusMinutes) ? Theme.AccentBrush : Theme.TextPrimaryBrush;
        Brush minBrush = (_isFocused && _focusMinutes) ? Theme.AccentBrush : Theme.TextPrimaryBrush;

        g.DrawString(hourStr, Theme.TimeDisplayFont, hourBrush, hourRect, sfCenter);
        g.DrawString(":", Theme.TimeDisplayFont, Theme.TextSecondaryBrush, colonRect, sfCenter);
        g.DrawString(minStr, Theme.TimeDisplayFont, minBrush, minRect, sfCenter);
    }

    private static void DrawArrow(Graphics g, RectangleF rect, bool up, bool hover)
    {
        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float size = 3.5f;

        PointF[] pts;
        if (up)
        {
            pts = new[]
            {
                new PointF(cx - size, cy + 2f),
                new PointF(cx + size, cy + 2f),
                new PointF(cx, cy - 2f)
            };
        }
        else
        {
            pts = new[]
            {
                new PointF(cx - size, cy - 2f),
                new PointF(cx + size, cy - 2f),
                new PointF(cx, cy + 2f)
            };
        }

        using var brush = new SolidBrush(hover ? Theme.Accent : Theme.TextSecondary);
        g.FillPolygon(brush, pts);
    }
}
