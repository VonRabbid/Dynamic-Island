using System;
using System.IO;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DynamicIsland.Services;

public class TrayService : IDisposable
{
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DynamicIsland";

    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _startupMenuItem;

    public Action? OnResetPositionRequested { get; set; }
    public Action? OnExitRequested { get; set; }

    public void Initialize()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Dynamic Island for Windows",
            Visible = true
        };

        _notifyIcon.Icon = LoadAppIcon();

        var contextMenu = new Forms.ContextMenuStrip();

        // 1. Título de estado
        var titleItem = new Forms.ToolStripMenuItem("Dynamic Island (Activa)")
        {
            Enabled = false
        };
        contextMenu.Items.Add(titleItem);

        contextMenu.Items.Add(new Forms.ToolStripSeparator());

        // 2. Iniciar con Windows (Checkbox)
        _startupMenuItem = new Forms.ToolStripMenuItem("Iniciar con Windows")
        {
            CheckOnClick = true,
            Checked = IsStartupEnabled()
        };
        _startupMenuItem.CheckedChanged += OnStartupCheckedChanged;
        contextMenu.Items.Add(_startupMenuItem);

        contextMenu.Items.Add(new Forms.ToolStripSeparator());

        // 3. Reiniciar posición
        var resetItem = new Forms.ToolStripMenuItem("Reiniciar posición");
        resetItem.Click += (s, e) => OnResetPositionRequested?.Invoke();
        contextMenu.Items.Add(resetItem);

        // 4. Salir
        var exitItem = new Forms.ToolStripMenuItem("Salir");
        exitItem.Click += (s, e) => OnExitRequested?.Invoke();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        _notifyIcon.DoubleClick += (s, e) => OnResetPositionRequested?.Invoke();
    }

    private Drawing.Icon LoadAppIcon()
    {
        try
        {
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
            {
                return new Drawing.Icon(icoPath);
            }
        }
        catch { }

        return Drawing.SystemIcons.Application;
    }

    private bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    private void OnStartupCheckedChanged(object? sender, EventArgs e)
    {
        if (_startupMenuItem == null) return;
        SetStartupEnabled(_startupMenuItem.Checked);
    }

    public void SetStartupEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayService] Error modificando registro de inicio: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
