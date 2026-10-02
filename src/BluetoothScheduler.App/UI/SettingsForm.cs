using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BluetoothScheduler.App.Services;
using BluetoothScheduler.App.UI.Controls;
using BluetoothScheduler.Core.Models;
using Timer = System.Windows.Forms.Timer;

namespace BluetoothScheduler.App.UI;

public class SettingsForm : Form
{
    private readonly AppScheduler _scheduler;
    private ScheduleConfig _config;

    // Controles
    private readonly DayChip[] _dayChips = new DayChip[7];
    private readonly TimeField _onTimeField;
    private readonly TimeField _offTimeField;
    private readonly ToggleSwitch _forceOffToggle;
    private readonly ToggleSwitch _startWithWindowsToggle;
    private readonly ToggleSwitch _notificationsToggle;
    private readonly FlatButton _restoreDefaultsBtn;

    private readonly Label _titleLabel;
    private readonly Label _statusLabel;
    private readonly Label _savedLabel;
    private readonly Label _timeErrorLabel;

    private readonly Timer _autoSaveTimer;
    private readonly Timer _savedFadeTimer;
    private int _savedFadeOpacity = 0;
    private bool _isPopulating;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public SettingsForm(AppScheduler scheduler)
    {
        _scheduler = scheduler;
        _config = scheduler.Config;

        // Configuración de formulario
        Text = Strings.AppTitle;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 560);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        BackColor = Theme.Background;
        Icon = scheduler.AppIcon;

        ApplyDarkTitleBar();

        // 1. Encabezado
        _titleLabel = new Label
        {
            Text = Strings.HeaderTitle,
            Font = Theme.TitleFont,
            ForeColor = Theme.TextPrimary,
            Location = new Point(24, 20),
            Size = new Size(392, 32),
            AutoEllipsis = true
        };

        _statusLabel = new Label
        {
            Font = Theme.SubtitleFont,
            ForeColor = Theme.TextSecondary,
            Location = new Point(24, 52),
            Size = new Size(392, 22),
            AutoEllipsis = true
        };

        // 2. Días activos
        var daysHeader = new SectionLabel
        {
            Text = Strings.SectionActiveDays,
            Location = new Point(24, 88),
            Size = new Size(392, 20)
        };

        var daysPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 112),
            Size = new Size(392, 42),
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = false,
            AutoSize = false
        };

        (DayOfWeek Day, string Label)[] days =
        {
            (DayOfWeek.Monday, "L"),
            (DayOfWeek.Tuesday, "M"),
            (DayOfWeek.Wednesday, "X"),
            (DayOfWeek.Thursday, "J"),
            (DayOfWeek.Friday, "V"),
            (DayOfWeek.Saturday, "S"),
            (DayOfWeek.Sunday, "D")
        };

        for (int i = 0; i < 7; i++)
        {
            var dayInfo = days[i];
            var chip = new DayChip(dayInfo.Day, dayInfo.Label)
            {
                Margin = new Padding(0, 0, 14, 0)
            };
            chip.SelectedChanged += (_, _) => TriggerAutoSave();
            _dayChips[i] = chip;
            daysPanel.Controls.Add(chip);
        }

        // 3. Horario
        var scheduleHeader = new SectionLabel
        {
            Text = Strings.SectionSchedule,
            Location = new Point(24, 166),
            Size = new Size(392, 20)
        };

        var onLabel = new Label
        {
            Text = Strings.LabelTurnOnAt,
            Font = Theme.CaptionFont,
            ForeColor = Theme.TextSecondary,
            Location = new Point(24, 192),
            Size = new Size(180, 16)
        };

        _onTimeField = new TimeField
        {
            Location = new Point(24, 210),
            Size = new Size(180, 38)
        };
        _onTimeField.ValueChanged += (_, _) => OnTimeFieldChanged();

        var offLabel = new Label
        {
            Text = Strings.LabelTurnOffAt,
            Font = Theme.CaptionFont,
            ForeColor = Theme.TextSecondary,
            Location = new Point(236, 192),
            Size = new Size(180, 16)
        };

        _offTimeField = new TimeField
        {
            Location = new Point(236, 210),
            Size = new Size(180, 38)
        };
        _offTimeField.ValueChanged += (_, _) => OnTimeFieldChanged();

        _timeErrorLabel = new Label
        {
            Text = Strings.ErrorTimeValidation,
            Font = Theme.CaptionFont,
            ForeColor = Theme.Error,
            Location = new Point(24, 252),
            Size = new Size(392, 18),
            Visible = false
        };

        // 4. Opciones
        var optionsHeader = new SectionLabel
        {
            Text = Strings.SectionOptions,
            Location = new Point(24, 276),
            Size = new Size(392, 20)
        };

        _forceOffToggle = new ToggleSwitch();
        _forceOffToggle.CheckedChanged += (_, _) => TriggerAutoSave();
        var rowForceOff = new SettingRow
        {
            Title = Strings.OptionForceOffTitle,
            Description = Strings.OptionForceOffDesc,
            Location = new Point(24, 300),
            Size = new Size(392, 52),
            TrailingControl = _forceOffToggle
        };

        _startWithWindowsToggle = new ToggleSwitch();
        _startWithWindowsToggle.CheckedChanged += (_, _) => TriggerAutoSave();
        var rowStartWindows = new SettingRow
        {
            Title = Strings.OptionStartWithWindowsTitle,
            Description = Strings.OptionStartWithWindowsDesc,
            Location = new Point(24, 356),
            Size = new Size(392, 52),
            TrailingControl = _startWithWindowsToggle
        };

        _notificationsToggle = new ToggleSwitch();
        _notificationsToggle.CheckedChanged += (_, _) => TriggerAutoSave();
        var rowNotifications = new SettingRow
        {
            Title = Strings.OptionNotificationsTitle,
            Description = Strings.OptionNotificationsDesc,
            Location = new Point(24, 412),
            Size = new Size(392, 52),
            TrailingControl = _notificationsToggle
        };

        // 5. Pie de página
        _restoreDefaultsBtn = new FlatButton
        {
            Text = Strings.BtnRestoreDefaults,
            Variant = FlatButtonVariant.Text,
            Location = new Point(20, 484),
            Size = new Size(210, 36)
        };
        _restoreDefaultsBtn.Click += OnRestoreDefaultsClicked;

        _savedLabel = new Label
        {
            Text = Strings.StatusSaved,
            Font = Theme.CaptionFont,
            ForeColor = Theme.TextSecondary,
            Location = new Point(320, 492),
            Size = new Size(96, 20),
            TextAlign = ContentAlignment.MiddleRight,
            Visible = false
        };

        // Agregar controles al formulario
        Controls.AddRange(new Control[]
        {
            _titleLabel, _statusLabel,
            daysHeader, daysPanel,
            scheduleHeader, onLabel, _onTimeField, offLabel, _offTimeField, _timeErrorLabel,
            optionsHeader, rowForceOff, rowStartWindows, rowNotifications,
            _restoreDefaultsBtn, _savedLabel
        });

        // Timers (Debounce de 1000ms para evitar guardados excesivos mientras se escribe o ajusta la hora)
        _autoSaveTimer = new Timer { Interval = 1000 };
        _autoSaveTimer.Tick += OnAutoSaveTick;

        _savedFadeTimer = new Timer { Interval = 40 };
        _savedFadeTimer.Tick += OnSavedFadeTick;

        // Cargar datos iniciales
        PopulateFromConfig(_config);
        UpdateStatusText();

        _scheduler.StatusChanged += OnSchedulerStatusChanged;
    }

    private void ApplyDarkTitleBar()
    {
        try
        {
            int useImmersiveDarkMode = Theme.IsDark ? 1 : 0;
            DwmSetWindowAttribute(Handle, 20, ref useImmersiveDarkMode, sizeof(int));
        }
        catch
        {
        }
    }

    private void PopulateFromConfig(ScheduleConfig config)
    {
        _isPopulating = true;
        try
        {
            for (int i = 0; i < 7; i++)
            {
                _dayChips[i].IsSelected = config.ActiveDays.Contains(_dayChips[i].DayOfWeek);
            }

            _onTimeField.Value = config.OnTime;
            _offTimeField.Value = config.OffTime;
            _forceOffToggle.Checked = config.ForceOff;
            _startWithWindowsToggle.Checked = config.StartWithWindows;
            _notificationsToggle.Checked = config.ShowNotifications;

            ValidateTimes();
        }
        finally
        {
            _isPopulating = false;
        }
    }

    private void OnTimeFieldChanged()
    {
        ValidateTimes();
        TriggerAutoSave();
    }

    private bool ValidateTimes()
    {
        bool isValid = _onTimeField.Value < _offTimeField.Value;
        _onTimeField.HasError = !isValid;
        _offTimeField.HasError = !isValid;
        _timeErrorLabel.Visible = !isValid;
        return isValid;
    }

    private void TriggerAutoSave()
    {
        if (_isPopulating) return;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private async void OnAutoSaveTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();

        if (!ValidateTimes())
        {
            return;
        }

        var activeDays = new HashSet<DayOfWeek>();
        foreach (var chip in _dayChips)
        {
            if (chip.IsSelected)
            {
                activeDays.Add(chip.DayOfWeek);
            }
        }

        _config = _config with
        {
            ActiveDays = activeDays,
            OnTime = _onTimeField.Value,
            OffTime = _offTimeField.Value,
            ForceOff = _forceOffToggle.Checked,
            StartWithWindows = _startWithWindowsToggle.Checked,
            ShowNotifications = _notificationsToggle.Checked
        };

        await _scheduler.UpdateConfigAsync(_config);
        ShowSavedAnimation();
        UpdateStatusText();
    }

    private void ShowSavedAnimation()
    {
        _savedLabel.Visible = true;
        _savedFadeOpacity = 100;
        _savedFadeTimer.Stop();
        _savedFadeTimer.Start();
    }

    private void OnSavedFadeTick(object? sender, EventArgs e)
    {
        _savedFadeOpacity -= 5;
        if (_savedFadeOpacity <= 0)
        {
            _savedFadeTimer.Stop();
            _savedLabel.Visible = false;
        }
    }

    private void OnRestoreDefaultsClicked(object? sender, EventArgs e)
    {
        var defaultConfig = new ScheduleConfig();
        PopulateFromConfig(defaultConfig);
        TriggerAutoSave();
    }

    private void OnSchedulerStatusChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateStatusText));
        }
        else
        {
            UpdateStatusText();
        }
    }

    private void UpdateStatusText()
    {
        string status = _scheduler.GetStatusDescription();
        _statusLabel.Text = status;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _scheduler.StatusChanged -= OnSchedulerStatusChanged;
        _autoSaveTimer.Dispose();
        _savedFadeTimer.Dispose();
        base.OnFormClosing(e);
    }
}
