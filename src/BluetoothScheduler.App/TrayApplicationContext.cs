using System.Drawing;
using System.Windows.Forms;
using BluetoothScheduler.App.Services;
using BluetoothScheduler.App.UI;
using BluetoothScheduler.Core.Interfaces;

namespace BluetoothScheduler.App;

public class TrayApplicationContext : ApplicationContext
{
    private readonly AppScheduler _scheduler;
    private readonly IStartupRegistrar _startupRegistrar;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private SettingsForm? _settingsForm;

    private readonly ToolStripMenuItem _openMenuItem;
    private readonly ToolStripMenuItem _pauseTodayMenuItem;
    private readonly ToolStripMenuItem _startWithWindowsMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;

    public TrayApplicationContext(AppScheduler scheduler, IStartupRegistrar startupRegistrar)
    {
        _scheduler = scheduler;
        _startupRegistrar = startupRegistrar;

        _contextMenu = new ContextMenuStrip
        {
            Renderer = new MaterialMenuRenderer(),
            ShowImageMargin = false
        };

        _openMenuItem = new ToolStripMenuItem(Strings.MenuOpen, null, OnOpenSettingsClicked)
        {
            Font = Theme.BodyBoldFont
        };

        _pauseTodayMenuItem = new ToolStripMenuItem(Strings.MenuPauseToday, null, OnPauseTodayClicked);

        _startWithWindowsMenuItem = new ToolStripMenuItem(Strings.MenuStartWithWindows, null, OnToggleStartWithWindowsClicked)
        {
            CheckOnClick = true,
            Checked = _startupRegistrar.IsRegistered()
        };

        _exitMenuItem = new ToolStripMenuItem(Strings.MenuExit, null, OnExitClicked);

        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            _openMenuItem,
            new ToolStripSeparator(),
            _pauseTodayMenuItem,
            _startWithWindowsMenuItem,
            new ToolStripSeparator(),
            _exitMenuItem
        });

        _contextMenu.Opening += OnContextMenuOpening;

        _notifyIcon = new NotifyIcon
        {
            Icon = _scheduler.AppIcon,
            ContextMenuStrip = _contextMenu,
            Text = Strings.TrayTooltip,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettingsForm();

        _scheduler.StatusChanged += OnSchedulerStatusChanged;
        _scheduler.ShowNotificationRequested += OnShowNotificationRequested;
    }

    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        bool isPaused = _scheduler.State.IsPaused(today);
        _pauseTodayMenuItem.Text = isPaused ? Strings.MenuUnpause : Strings.MenuPauseToday;
        _pauseTodayMenuItem.Checked = isPaused;

        _startWithWindowsMenuItem.Checked = _startupRegistrar.IsRegistered();
    }

    private void OnOpenSettingsClicked(object? sender, EventArgs e)
    {
        ShowSettingsForm();
    }

    private async void OnPauseTodayClicked(object? sender, EventArgs e)
    {
        await _scheduler.TogglePauseTodayAsync();
    }

    private async void OnToggleStartWithWindowsClicked(object? sender, EventArgs e)
    {
        bool newState = _startWithWindowsMenuItem.Checked;
        var config = _scheduler.Config with { StartWithWindows = newState };
        await _scheduler.UpdateConfigAsync(config);
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _scheduler.Dispose();
        ExitThread();
    }

    private void ShowSettingsForm()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_scheduler);
            _settingsForm.FormClosed += (_, _) => _settingsForm = null;
            _settingsForm.Show();
        }
        else
        {
            if (_settingsForm.WindowState == FormWindowState.Minimized)
            {
                _settingsForm.WindowState = FormWindowState.Normal;
            }
            _settingsForm.Activate();
        }
    }

    private void OnSchedulerStatusChanged(object? sender, EventArgs e)
    {
        _notifyIcon.Icon = _scheduler.AppIcon;
        string status = _scheduler.GetStatusDescription();
        // NotifyIcon text límite de 63 caracteres
        if (status.Length > 63) status = status[..60] + "...";
        _notifyIcon.Text = status;
    }

    private void OnShowNotificationRequested(object? sender, (string Title, string Message) args)
    {
        _notifyIcon.ShowBalloonTip(3000, args.Title, args.Message, ToolTipIcon.Info);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _contextMenu.Dispose();
            _settingsForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
