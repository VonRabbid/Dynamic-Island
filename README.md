# Dynamic Island for Windows

Dynamic Island for Windows es un overlay ligero e interactivo desarrollado en **C# / .NET 8** utilizando **WPF** (Windows Presentation Foundation), orientado a reproducir música mediante **GSMTC** (Global System Media Transport Controls) y capturar notificaciones del sistema sin impacto en el rendimiento.

## Características principales

- **Control Multimedia (GSMTC):** Integración nativa con Global System Media Transport Controls para visualizar y controlar la reproducción de música y medios en tiempo real.
- **Captura de Notificaciones:** Consumo directo de las APIs modernas de Windows (`UserNotificationListener`) para recibir y gestionar alertas del sistema.
- **Rendimiento y Eficiencia:** Arquitectura limpia con soporte MVVM (`CommunityToolkit.Mvvm`), llamadas Win32 ultraligeras y seguras mediante `Microsoft.Windows.CsWin32`, y consumo nativo de APIs WinRT bajo `net8.0-windows10.0.19041.0`.

## Requisitos del Sistema

- **SDK:** .NET 8.0 SDK (con soporte para Windows SDK 10.0.19041.0+)
- **Sistema Operativo:** Windows 10 (versión 2004 / compilación 19041) o Windows 11
