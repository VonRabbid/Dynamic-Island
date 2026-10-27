# Dynamic Island for Windows

[![Build and Release](https://github.com/VonRabbid/Dynamic-Island/actions/workflows/release.yml/badge.svg)](https://github.com/VonRabbid/Dynamic-Island/actions/workflows/release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/VonRabbid/Dynamic-Island?color=blue&label=Release)](https://github.com/VonRabbid/Dynamic-Island/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-8.0%20(Windows%2010%2F11%20SDK)-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows%2010%20%7C%2011%20(x64)-0078D4?logo=windows)](https://microsoft.com/windows)
[![Licencia](https://img.shields.io/badge/Licencia-MIT-green.svg)](LICENSE)

**Dynamic Island for Windows** es un overlay flotante, no intrusivo y de alto rendimiento inspirado en la Dynamic Island de iOS, desarrollado en **C# / .NET 8 con WPF**. Ofrece control multimedia global en tiempo real (GSMTC), captura nativa de notificaciones de Windows (`UserNotificationListener`), HUD de volumen interactivo, alertas de energía y atajos rápidos de reposicionamiento libre en pantalla, diseñado para un impacto mínimo en recursos.

---

## ✨ Novedades Recientes

- **Micro-cápsula musical minimalista:** En reposo, la reproducción se contrae en una píldora ultra compacta de $54\times 28\text{ px}$ exhibiendo únicamente el indicador dinámico de audio (4 barras reactivas al compás de la música). Al posar el cursor, se expande fluidamente al panel completo ($360\times 148\text{ px}$).
- **Detección ampliada de navegadores y reproductores:** Identificación automática del origen de la pista con iconos vectoriales y colores oficiales:
  - **Reproductores dedicados:** Spotify, Apple Music, Deezer, TIDAL y Windows Media Player / Groove.
  - **Navegadores web:** Google Chrome, Microsoft Edge, Mozilla Firefox, Brave Browser, Vivaldi y Opera / Opera GX.
- **Controles extendidos para reproductores dedicados:** Al reproducir desde Spotify, Deezer, Apple Music o Tidal, la vista expandida despliega controles avanzados:
  - 🔀 **Aleatorio (*Shuffle*):** Activa o desactiva la reproducción aleatoria iluminándose con el color de acento.
  - 🔁 **Repetir (*Repeat*):** Alterna el modo de repetición en bucle de la lista o pista.
  - ❤️ **Favoritos persistentes:** Marca canciones con un clic, almacenando el catálogo en `%APPDATA%\DynamicIsland\favorites.json` e iluminando el corazón en rojo vibrante.
  - *Nota:* En navegadores web (YouTube en Chrome/Brave/Vivaldi), estos botones se ocultan automáticamente (`Collapsed`) para mantener la interfaz despejada.
- **Extracción algorítmica de color dominante:** Algoritmo perceptual ligero de submuestreo de píxeles sobre la portada del álbum para generar gradientes dinámicos y acentos visuales vibrantes (`AccentBrush`), con transiciones cinematográficas animadas (`ColorAnimation` de 480 ms).
- **Barra de reproducción interactiva (*Seek bar*):** Reemplazo de la barra estática por un `Slider` moderno de 4 px con esquinas redondeadas, soporte de clic directo y arrastre suave para adelantar/retroceder la pista con visualización de tiempo en tiempo real.
- **Fondo translúcido tipo cristal:** Estilo visual semi-transparente con contorno luminoso delimitador y opacidad personalizable del 40% al 100% desde los ajustes.
- **Reposicionamiento libre y anclaje simétrico:** Arrastra la cápsula libremente con `Ctrl + Clic Izquierdo` y anclaje equilibrado. El restablecimiento de posición se gestiona de forma limpia y exclusiva desde el menú de la bandeja del sistema (*Tray*), evitando conflictos de atajos de teclado globales.

---

## 🎮 Controles y Atajos Rápidos

| Acción / Atajo | Descripción |
| :--- | :--- |
| `Ctrl + Clic Izquierdo (arrastrar)` | Mueve y ancla la cápsula libremente en cualquier coordenada o monitor de tu pantalla. |
| `Bandeja > Reiniciar posición` | Restablece de inmediato la cápsula a la posición superior central por defecto con animación suave de retorno. |
| `Rueda del ratón sobre la cápsula` | Regula el volumen maestro del sistema (subir o bajar) mostrando un overlay HUD de retroalimentación inmediata. |
| `Hover / Desplazamiento del cursor` | Despliega automáticamente los detalles del tema y controles al pasar el ratón, contrayéndose al retirarlo. |
| `Arrastre en barra de progreso` | Permite navegar y cambiar la posición (*Seek*) de la pista multimedia en reproducción. |
| `Botones Shuffle / Repeat / Like` | Controlan la reproducción aleatoria, bucle y lista de favoritos en reproductores de música dedicados. |
| `Clic sobre notificación activa` | Descarta la alerta de inmediato y restaura el estado previo de la isla (música o reposo). |
| `Doble clic en icono de bandeja` | Abre directamente la ventana de **Configuración** de la aplicación. |
| `Clic derecho en icono de bandeja` | Despliega el menú contextual del System Tray (inicio con Windows, reiniciar posición, ajustes y salida). |

---

## ⚡ Arquitectura y Rendimiento

- **Consumo Mínimo de Recursos:** Menos de **35 MB de memoria RAM** en reposo y **0% de uso de CPU** continuo.
- **Sin Polling Innecesario:** Basado estrictamente en eventos reactivos nativos de Windows Runtime (WinRT) y estilos Win32 (`WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`).
- **Aceleración por GPU (60 / 120 / 144 Hz):** Renderizado directo sobre Direct3D con sincronización V-Sync automática y funciones de aceleración elásticas (`CubicEase`).
- **Downsampling de Carátulas:** Procesamiento optimizado con WIC que decodifica las imágenes a 80 px (`DecodePixelWidth`) e inmutabilidad (`Freeze()`), reduciendo el consumo en memoria hasta en un 75%.
- **No Intrusivo:** La ventana nunca roba el foco de teclado o ratón (`WS_EX_NOACTIVATE`), no aparece en `Alt+Tab` (`WS_EX_TOOLWINDOW`) y se oculta automáticamente ante juegos o vídeos en pantalla completa.

---

## 🚀 Tabla de Características Principales

| Módulo | Descripción |
| :--- | :--- |
| **Control Multimedia (GSMTC)** | Integración nativa con Spotify, Apple Music, Deezer, TIDAL, navegadores (YouTube, Netflix) y reproductores multimedia. Incluye carátula, ecualizador dinámico, barra seek deslizable, insignia de app y controles de shuffle/repeat/favoritos. |
| **Notificaciones Nativas** | Captura alertas del sistema (`UserNotificationListener`) desde WhatsApp, Discord, Telegram, navegadores y Windows, desplegando una píldora acentuada con prioridad visual sobre la música. |
| **Detección de Pantalla Completa** | Oculta automáticamente la isla de forma transparente cuando se ejecuta un juego o vídeo a pantalla completa y suspende los timers para liberar el 100% de la CPU. |
| **Control de Volumen Interactivo** | Permite subir o bajar el volumen maestro del sistema girando la rueda del ratón (*Scroll*) sobre la cápsula, mostrando un HUD sutil de retroalimentación. |
| **HUD de Batería y Cable de Carga** | Muestra alertas instantáneas al enchufar/desenchufar el cargador (⚡) y emite avisos de batería baja ($\le 20\%$). Se desactiva de forma segura en PCs de sobremesa. |
| **Indicador de Bloq Mayús** | Alerta visual de 1.5 segundos con la insignia `[A]` al activar o desactivar la tecla Caps Lock de forma pasiva. |
| **Soporte Multi-Monitor** | Permite seleccionar en cuál de las pantallas conectadas se posicionará la Dynamic Island, recalculando las coordenadas DPI y el área de trabajo (*WorkingArea*). |
| **Persistencia de Configuración** | Guarda ajustes en tiempo real en `%APPDATA%\DynamicIsland\settings.json` (desplazamiento vertical, duración de alertas, escala 80–120%, opacidad y coordenadas personalizadas). |
| **Bandeja del Sistema (Tray)** | Control de fondo persistente, inicio automático con Windows (`HKCU\Run`), recentrado de posición y ventana de ajustes. |

---

## 📥 Descarga e Instalación

### Opción 1: Instalador Formal (Recomendado)
Descarga el ejecutable de instalación desde la sección de **[Releases](https://github.com/VonRabbid/Dynamic-Island/releases/latest)**:
- **`DynamicIsland-Setup-v1.0.0.exe`**: Instalador creado con Inno Setup que configura accesos directos, desinstalador y opción de inicio automático con Windows.

### Opción 2: Versión Portable
- Descarga **`DynamicIsland-Windows-x64.zip`**, descomprímelo en cualquier carpeta y ejecuta `DynamicIsland.exe`. No requiere instalación previa.

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
   El binario optimizado y firmado quedará disponible en `./publish/DynamicIsland.exe`.

---

## 📄 Licencia

Este proyecto está bajo la Licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
