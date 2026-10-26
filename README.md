# Dynamic Island for Windows

[![Build and Release](https://github.com/VonRabbid/Dynamic-Island/actions/workflows/release.yml/badge.svg)](https://github.com/VonRabbid/Dynamic-Island/actions/workflows/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/VonRabbid/Dynamic-Island?color=blue&label=Release)](https://github.com/VonRabbid/Dynamic-Island/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-8.0%20(Windows%2010%2F11%20SDK)-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows%2010%20%7C%2011%20(x64)-0078D4?logo=windows)](https://microsoft.com/windows)
[![Licencia](https://img.shields.io/badge/Licencia-MIT-green.svg)](LICENSE)

**Dynamic Island for Windows** es un overlay flotante, no intrusivo y de alto rendimiento inspirado en la Dynamic Island de iOS, desarrollado en **C# / .NET 8 con WPF**. Ofrece control multimedia global en tiempo real (GSMTC), captura nativa de notificaciones de Windows (`UserNotificationListener`), HUD de volumen y alertas de sistema, diseñado desde cero para no impactar el rendimiento en juegos ni aplicaciones de trabajo.

---

## ⚡ Arquitectura y Rendimiento

- **Consumo Mínimo de Recursos:** Menos de **35 MB de memoria RAM** en reposo y **0% de uso de CPU** continuo.
- **Sin Polling Innecesario:** Basado estrictamente en eventos reactivos nativos de Windows Runtime (WinRT) y hooks Win32 de bajo nivel (`WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`).
- **Aceleración por GPU (60 / 120 / 144 Hz):** Renderizado directo sobre Direct3D con sincronización V-Sync automática y funciones de aceleración elásticas (`CubicEase`).
- **Downsampling de Carátulas:** Procesamiento optimizado con WIC que decodifica las imágenes a 80 px (`DecodePixelWidth`) e inmutabilidad (`Freeze()`), reduciendo el consumo en memoria hasta en un 75%.
- **No Intrusivo:** La ventana nunca roba el foco de teclado o ratón (`WS_EX_NOACTIVATE`), no aparece en `Alt+Tab` (`WS_EX_TOOLWINDOW`) y se oculta automáticamente ante juegos o vídeos en pantalla completa.

---

## 🚀 Tabla de Características Principales

| Módulo | Descripción |
| :--- | :--- |
| **Control Multimedia (GSMTC)** | Integración nativa con Spotify, Apple Music, navegadores (YouTube, Netflix) y reproductores multimedia. Incluye carátula, ecualizador senoidal animado, controles de reproducción y línea de tiempo con progreso y duración. |
| **Notificaciones Nativas** | Captura alertas del sistema (`UserNotificationListener`) desde WhatsApp, Discord, Telegram, navegadores y Windows, desplegando una píldora acentuada con prioridad visual sobre la música. |
| **Detección de Pantalla Completa** | Oculta automáticamente la isla de forma transparente cuando se ejecuta un juego o vídeo a pantalla completa y suspende los timers para liberar el 100% de la CPU. |
| **Control de Volumen Interactivo** | Permite subir o bajar el volumen maestro del sistema girando la rueda del ratón (*Scroll*) sobre la cápsula, mostrando un HUD sutil de retroalimentación. |
| **HUD de Batería y Cable de Carga** | Muestra alertas instantáneas al enchufar/desenchufar el cargador (⚡) y emite avisos de batería baja ($\le 20\%$). Se desactiva de forma segura en PCs de escritorio. |
| **Indicador de Bloq Mayús** | Alerta visual de 1.5 segundos con la insignia `[A]` al activar o desactivar la tecla Caps Lock. |
| **Soporte Multi-Monitor** | Permite seleccionar en cuál de las pantallas conectadas se posicionará la Dynamic Island, recalculando las coordenadas DPI y el área de trabajo (*WorkingArea*). |
| **Persistencia de Configuración** | Guarda ajustes en tiempo real en `%APPDATA%\DynamicIsland\settings.json` (desplazamiento vertical, duración de alertas, escala 80–120% y activación de módulos). |
| **Bandeja del Sistema (Tray)** | Control de fondo persistente, inicio automático con Windows (`HKCU\Run`), recentrado de posición y ventana de ajustes. |

---

## 📥 Descarga e Instalación

### Opción 1: Instalador Formal (Recomendado)
Descarga el ejecutable de instalación desde la sección de **[Releases](https://github.com/VonRabbid/Dynamic-Island/releases/latest)**:
- **`DynamicIsland-Setup-v1.0.0.exe`**: Instalador creado con Inno Setup que configura accesos directos, desinstalador y opción de inicio automático con Windows.

### Opción 2: Versión Portable
- Descarga **`DynamicIsland-Windows-x64.zip`**, descomprímelo en cualquier carpeta y ejecuta `DynamicIsland.exe`. No requiere instalación.

---

## 🛠️ Compilación Local

### Requisitos
- Windows 10 (versión 2004 / compilación 19041 o superior) o Windows 11.
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) o superior.

### Pasos
1. Clona el repositorio:
   ```powershell
   git clone https://github.com/VonRabbid/Dynamic-Island.git
   cd Dynamic-Island
   ```

2. Compila el proyecto en modo depuración:
   ```powershell
   dotnet build
   ```

3. Genera la versión de producción optimizada (Single-File & ReadyToRun):
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\publish.ps1
   ```
   El binario optimizado quedará disponible en `./publish/DynamicIsland.exe`.

---

## ⚙️ Configuración y Uso

- **Expandir / Contraer:** Pasa el cursor por encima (*hover*) de la cápsula para desplegar los controles multimedia y la línea de tiempo. Haz clic en el fondo para fijarla abierta.
- **Ajustar Posición:** Abre el menú de la bandeja del sistema > **Configuración...** y mueve el slider de *Separación Superior* para alinear la isla al borde o debajo de la cámara web.
- **Descartar Notificación:** Haz clic sobre cualquier alerta de notificación para cerrarla antes de que expire el temporizador.
- **Rueda del Ratón:** Gira la rueda del ratón sobre la cápsula para regular el volumen de tu equipo.

---

## 📄 Licencia

Este proyecto está bajo la Licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
