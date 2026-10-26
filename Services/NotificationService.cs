using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DynamicIsland.Models;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DynamicIsland.Services;

public class NotificationService
{
    private UserNotificationListener? _listener;

    public static NotificationService Instance { get; } = new();

    public event EventHandler<NotificationModel>? OnNotificationReceived;

    public bool IsAccessGranted { get; private set; }

    public NotificationService()
    {
        _ = InitializeAsync();
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            _listener = UserNotificationListener.Current;
            if (_listener == null)
            {
                Debug.WriteLine("[NotificationService] UserNotificationListener no está soportado en este sistema.");
                return false;
            }

            var accessStatus = await _listener.RequestAccessAsync();
            if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
            {
                IsAccessGranted = true;
                _listener.NotificationChanged += Listener_NotificationChanged;
                Debug.WriteLine("[NotificationService] Acceso a notificaciones concedido exitosamente.");
                return true;
            }
            else
            {
                IsAccessGranted = false;
                Debug.WriteLine("[NotificationService] ADVERTENCIA: Permiso de notificaciones no concedido. Habilite el acceso en: Configuración de Windows > Privacidad y seguridad > Notificaciones.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Error al inicializar UserNotificationListener: {ex.Message}");
            return false;
        }
    }

    private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added)
            return;

        try
        {
            var userNotification = sender.GetNotification(args.UserNotificationId);
            if (userNotification == null)
                return;

            string rawAppName = userNotification.AppInfo?.DisplayInfo?.DisplayName ?? "Sistema";

            var binding = userNotification.Notification?.Visual?.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding == null && userNotification.Notification?.Visual?.Bindings?.Count > 0)
            {
                binding = userNotification.Notification.Visual.Bindings[0];
            }

            string rawTitle = rawAppName;
            string rawMessage = string.Empty;

            if (binding != null)
            {
                var textElements = binding.GetTextElements();
                var textList = new List<string>();
                foreach (var el in textElements)
                {
                    if (!string.IsNullOrWhiteSpace(el.Text))
                        textList.Add(el.Text);
                }

                if (textList.Count > 0)
                {
                    rawTitle = textList[0];
                    if (textList.Count > 1)
                    {
                        rawMessage = string.Join(" ", textList.Skip(1));
                    }
                }
            }

            // Sanitización defensiva contra caracteres de control, secuencias Bidi maliciosas y DoS de layout
            string cleanAppName = NotificationModel.Sanitize(rawAppName, 60);
            string cleanTitle = NotificationModel.Sanitize(rawTitle, 100);
            string cleanMessage = NotificationModel.Sanitize(rawMessage, 250);

            var model = new NotificationModel(cleanAppName, cleanTitle, cleanMessage);
            OnNotificationReceived?.Invoke(this, model);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Error al procesar notificación: {ex.Message}");
        }
    }

    /// <summary>
    /// Permite disparar una notificación de prueba o programática.
    /// </summary>
    public void TriggerNotification(NotificationModel notification)
    {
        OnNotificationReceived?.Invoke(this, notification);
    }
}
