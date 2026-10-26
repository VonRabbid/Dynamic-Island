using System;
using System.Diagnostics;
using System.Windows.Threading;
using DynamicIsland.Models;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace DynamicIsland.Services;

public class PowerStatusService : IDisposable
{
    private readonly DispatcherTimer _pollTimer;
    private Forms.PowerLineStatus _lastPowerLineStatus;
    private bool _hasLowBatteryAlerted;

    public static PowerStatusService Instance { get; } = new();

    public event EventHandler<PowerAlertInfo>? PowerAlertTriggered;

    public bool HasBattery { get; private set; }

    public PowerStatusService()
    {
        _lastPowerLineStatus = Forms.SystemInformation.PowerStatus.PowerLineStatus;
        HasBattery = CheckHasBattery();

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _pollTimer.Tick += OnPollTimerTick;

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        if (HasBattery)
        {
            _pollTimer.Start();
        }
    }

    private static bool CheckHasBattery()
    {
        try
        {
            var ps = Forms.SystemInformation.PowerStatus;
            bool noBattery = ps.BatteryChargeStatus.HasFlag(Forms.BatteryChargeStatus.NoSystemBattery);
            return !noBattery && ps.BatteryLifePercent >= 0.0f;
        }
        catch
        {
            return false;
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.StatusChange)
        {
            CheckStatus();
        }
    }

    private void OnPollTimerTick(object? sender, EventArgs e)
    {
        CheckStatus();
    }

    public void CheckStatus()
    {
        if (!SettingsService.Instance.CurrentSettings.EnablePowerAlert)
            return;

        if (!HasBattery)
        {
            HasBattery = CheckHasBattery();
            if (!HasBattery) return;
        }

        try
        {
            var ps = Forms.SystemInformation.PowerStatus;
            var currentLineStatus = ps.PowerLineStatus;
            int percent = (int)Math.Clamp(ps.BatteryLifePercent * 100f, 0f, 100f);

            // 1. Detección de cambio en cable de alimentación
            if (currentLineStatus != _lastPowerLineStatus && currentLineStatus != Forms.PowerLineStatus.Unknown)
            {
                _lastPowerLineStatus = currentLineStatus;

                if (currentLineStatus == Forms.PowerLineStatus.Online)
                {
                    _hasLowBatteryAlerted = false;
                    PowerAlertTriggered?.Invoke(this, new PowerAlertInfo
                    {
                        IsCharging = true,
                        BatteryPercent = percent,
                        Message = $"Cargando ({percent}%)",
                        IsLowBattery = false
                    });
                }
                else if (currentLineStatus == Forms.PowerLineStatus.Offline)
                {
                    PowerAlertTriggered?.Invoke(this, new PowerAlertInfo
                    {
                        IsCharging = false,
                        BatteryPercent = percent,
                        Message = $"En batería ({percent}%)",
                        IsLowBattery = false
                    });
                }
            }

            // 2. Alerta de batería baja (<= 20% en modo batería)
            if (currentLineStatus == Forms.PowerLineStatus.Offline && percent <= 20 && !_hasLowBatteryAlerted)
            {
                _hasLowBatteryAlerted = true;
                PowerAlertTriggered?.Invoke(this, new PowerAlertInfo
                {
                    IsCharging = false,
                    BatteryPercent = percent,
                    Message = $"Batería baja ({percent}%)",
                    IsLowBattery = true
                });
            }
            else if (percent > 25)
            {
                _hasLowBatteryAlerted = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PowerStatusService] Error al evaluar energía: {ex.Message}");
        }
    }

    public void TriggerTestAlert(bool isCharging, int percent)
    {
        PowerAlertTriggered?.Invoke(this, new PowerAlertInfo
        {
            IsCharging = isCharging,
            BatteryPercent = percent,
            Message = isCharging ? $"Cargando ({percent}%)" : $"En batería ({percent}%)",
            IsLowBattery = percent <= 20 && !isCharging
        });
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        GC.SuppressFinalize(this);
    }
}
