# 02 — Arquitectura

## Tecnología

| Pieza | Elección | Motivo |
|---|---|---|
| Lenguaje | C# 14 | Pedido por la persona usuaria |
| Runtime | **.NET 10 (LTS)** | Soporte largo (hasta nov. 2028). .NET 8 deja de tener soporte en nov. 2026. Lo usan también OrbitDrop y nabyscape. |
| Interfaz | **WPF** | Ventanas transparentes y animaciones sencillas, XAML |
| Patrón | MVVM + `CommunityToolkit.Mvvm` | Menos código repetido, estándar |
| Inyección de dependencias | `Microsoft.Extensions.DependencyInjection` | Desacopla Core de Windows |
| Música (Windows) | `Windows.Media.Control` (WinRT / SMTC), sin paquetes extra | La misma API que usan Kissa y Nock. **Decisión 2026-10-27:** dos fuentes, YouTube Music (navegador o app) y música local en carpetas reproducida con un reproductor externo que publique en SMTC (MusicBee, foobar2000, Reproductor multimedia). NotchBar **no** reproduce archivos (reproductor propio = idea v2). |
| Audio (volumen) | `NAudio` (MIT) — `MMDeviceEnumerator` | Evita escribir COM a mano |
| Bandeja | `H.NotifyIcon.Wpf` (MIT) | Icono de bandeja para WPF |
| Pruebas | xUnit | Estándar |
| Instalador (más adelante) | Inno Setup | Lo usan Nock y OrbitDrop |

**NuGet permitidos:** `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting` (opcional), `NAudio`, `H.NotifyIcon.Wpf`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `FluentAssertions` (opcional, ojo: v8+ cambió licencia; usar 6.x o no usarla).

## Estructura de carpetas

```
NotchBar/
├─ AGENTS.md  README.md  THIRD-PARTY-NOTICES.md
├─ docs/
├─ NotchBar.sln
├─ src/
│  ├─ NotchBar.Core/                  (net10.0, SIN WPF, SIN APIs de Windows)
│  │  ├─ Models/        MediaInfo.cs, LyricLine.cs, ClipboardEntry.cs, AppSettings.cs
│  │  ├─ Abstractions/  IMediaService.cs, ILyricsService.cs, IVolumeService.cs,
│  │  │                 IClipboardService.cs, ISettingsStore.cs, IAutoStart.cs
│  │  ├─ Lyrics/        LrcParser.cs, LyricsSyncEngine.cs
│  │  └─ Clipboard/     ClipboardHistory.cs
│  ├─ NotchBar.Windows/               (net10.0-windows10.0.19041.0)
│  │  ├─ Media/         SmtcMediaService.cs
│  │  ├─ Lyrics/        LrclibClient.cs
│  │  ├─ Audio/         NAudioVolumeService.cs
│  │  ├─ Clipboard/     ClipboardMonitor.cs
│  │  ├─ Shell/         NativeMethods.cs (P/Invoke), WindowHelper.cs, AppBar.cs (pantalla completa y franja reservada), EscapeKeyHook.cs, AutoStart.cs
│  │  └─ Storage/       JsonSettingsStore.cs
│  └─ NotchBar.App/                   (WPF, net10.0-windows10.0.19041.0, WinExe)
│     ├─ App.xaml / App.xaml.cs       (arranque, DI, bandeja)
│     ├─ Windows/       NotchWindow.xaml(.cs), SettingsWindow.xaml(.cs)
│     ├─ Views/         CollapsedView, NowPlayingView, LyricsView, VinylView, ClipboardView, NotesView
│     ├─ ViewModels/    NotchViewModel, NowPlayingViewModel, LyricsViewModel, ...
│     ├─ Themes/        Tokens.Layout.xaml (medidas y tiempos), Tokens.Dark.xaml, Tokens.Light.xaml (colores), Controls.xaml
│     └─ Assets/        icono.ico, fuentes (si se incluyen)
└─ tests/
   └─ NotchBar.Core.Tests/            (xUnit: LrcParser, LyricsSyncEngine, ClipboardHistory)
```

Regla de dependencias: `App → Windows → Core` y `App → Core`. **Core nunca referencia a los otros.**

## Datos en disco

| Qué | Dónde |
|---|---|
| Ajustes | `%APPDATA%\NotchBar\settings.json` |
| Letras en caché | `%LOCALAPPDATA%\NotchBar\lyrics\<hash>.json` |
| Portapapeles fijado | `%APPDATA%\NotchBar\pinned-clips.json` |
| Notas rápidas | `%USERPROFILE%\Documents\NotchBar\notes\*.md` |
| Registros (logs) | `%LOCALAPPDATA%\NotchBar\logs\` (sin contenido personal) |

## Cómo funciona cada pieza

### 1. La ventana del notch (hito M1–M2)
Se construye como una ventana WPF "flotante":
- `WindowStyle="None"`, `AllowsTransparency="True"`, `Background="Transparent"`, `ShowInTaskbar="False"`, `Topmost="True"`, `ResizeMode="NoResize"`.
- Al cargarse, por **P/Invoke** se le añaden los estilos extendidos `WS_EX_TOOLWINDOW` (no aparece en Alt+Tab) y `WS_EX_NOACTIVATE` (no roba el foco al hacer clic). Funciones: `GetWindowLong` / `SetWindowLong` sobre el `HWND` obtenido con `WindowInteropHelper`.
- **Posición:** centrada horizontalmente en el monitor principal, `Top = 0`. Usar `SystemParameters.PrimaryScreenWidth` (ya en unidades independientes de DPI).
- **Expandir/contraer:** `MouseEnter` → anima `Width` y `Height` con `DoubleAnimation` (~220 ms, `CubicEase EaseOut`). `MouseLeave` → espera ~400 ms (para no parpadear) y contrae. Estado en `NotchViewModel.IsExpanded`.
- **Tamaños:** contraído ~ 220×34 px; expandido ~ 560×200 px (valores en `Tokens.*.xaml`).
- **Pantalla completa:** la ventana se registra como *AppBar* (`SHAppBarMessage(ABM_NEW)`) **sin reservar espacio**; Windows envía entonces el aviso `ABN_FULLSCREENAPP` al entrar y salir de pantalla completa. Al recibirlo se comprueba que la ventana activa tape nuestro monitor y no sea el escritorio (`FullscreenRules` en Core, con pruebas). Sin temporizadores. (Cambio 2026-10-27: sustituye a `SHQueryUserNotificationState` + `SetWinEventHook`, que obligaba a consultar en cada cambio de ventana.)
- **Riesgo conocido:** `AllowsTransparency` en WPF usa una ventana en capas (renderizado por software). Con una ventana pequeña es aceptable. Si diera problemas de rendimiento, plan B: dejar la ventana opaca con esquinas redondeadas mediante `Window.Clip`/`SetWindowRgn`.

### 2. Ahora suena (hito M3)
- Clase `SmtcMediaService : IMediaService` en `NotchBar.Windows`.
- Usa `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()` para obtener el administrador, y `GetCurrentSession()` para la sesión activa.
- Eventos (no sondeo): `CurrentSessionChanged`, `MediaPropertiesChanged`, `PlaybackInfoChanged`, `TimelinePropertiesChanged`.
- Datos: `TryGetMediaPropertiesAsync()` → título, artista, álbum, miniatura (`Thumbnail` → `IRandomAccessStreamReference` → `BitmapImage`).
- Controles: `TryTogglePlayPauseAsync`, `TrySkipNextAsync`, `TrySkipPreviousAsync`, `TryChangePlaybackPositionAsync` (para arrastrar la barra; no todos los reproductores lo soportan).
- Progreso: `GetTimelineProperties()` da posición y duración; entre eventos, el reloj se **interpola localmente** con un `DispatcherTimer` de ~250 ms **solo mientras se reproduce y el notch está expandido**.
- Los eventos llegan en hilos de fondo: pasar a la interfaz con `Dispatcher.InvokeAsync`.
- Funciona con Spotify, Apple Music, TIDAL y navegadores Chromium (YouTube Music en Chrome/Edge).
- **Nota .NET:** para usar WinRT desde WPF, el TFM debe ser `net10.0-windows10.0.19041.0`; así se puede usar `Windows.Media.Control` sin paquetes extra.

### 3. Letras sincronizadas (hito M4)
- **Servicio:** LRCLIB, gratuito y sin clave. `GET https://lrclib.net/api/get?artist_name=...&track_name=...&album_name=...&duration=<segundos>` devuelve JSON con `syncedLyrics` (formato LRC) y `plainLyrics`.
- **Cortesía:** enviar un `User-Agent` descriptivo (`NotchBar/0.1 (contacto)`). Verificar la documentación vigente en <https://lrclib.net/docs> antes de implementar.
- **Parser (`LrcParser`, en Core):** convierte líneas `[mm:ss.xx] texto` en `LyricLine(TimeSpan Start, string Text)`. Manejar varias marcas por línea, líneas vacías y metadatos `[ar:]`, `[ti:]`.
- **Sincronía (`LyricsSyncEngine`, en Core):** dada la posición actual + desfase del usuario (−5.0 s a +5.0 s), devuelve el índice de la línea activa con búsqueda binaria. Es lo más fácil de probar con xUnit.
- **Validación:** descartar resultados cuya duración difiera más de ~3 s de la pista (suele ser otra versión).
- **Búsqueda en tres pasos (`LrclibClient`):** 1) `/api/get` con todos los datos; 2) igual pero solo con el primer artista (`ArtistNames.Primary`: YouTube Music junta "A, B y C"); 3) `/api/search` y elegir con `LyricsMatching.BestIndex` (misma duración, prefiere sincronizada). LRCLIB responde 404 `TrackNotFound` si no existe y 503 si está saturado (se muestra "No se pudo conectar").
- **Interfaz:** pestañas "Ahora suena" / "Letras" abajo del notch abierto (`ExpandedView`, `NotchContentViewModel`). La letra activa usa un temporizador que despierta justo al empezar la línea siguiente (`LyricsSyncEngine.TimeUntilNext`), solo con la pestaña visible y la música sonando.
- **Caché:** guardar el resultado por hash de `artista|título|duración` para no volver a consultar.
- **Privacidad:** la consulta envía artista, título y duración a lrclib.net. Debe poder desactivarse en Ajustes (`LyricsEnabled`).

### 4. Vinilo (hito M5)
Vista con la carátula recortada en círculo y un `RotateTransform` animado (`DoubleAnimation` 0→360, repetición infinita). **Pausar la animación** cuando no suena nada o el notch está contraído, para no gastar CPU.

### 5. Volumen y reloj (hito M6)
- **Volumen:** `NAudio` → `MMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).AudioEndpointVolume`. Evento `OnVolumeNotification` para reflejar cambios externos.
- **Batería:** descartada (la app es para un PC de escritorio).
- **Reloj:** `DispatcherTimer` de 1 s **solo si el reloj es visible**; al contraer, actualizar cada minuto.
- **Brillo:** fuera de la v1 (depende del driver; Nock usa driver directo con WMI como respaldo).

### 6. Portapapeles (hito M7)
- `AddClipboardFormatListener(hwnd)` + manejar el mensaje `WM_CLIPBOARDUPDATE` (0x031D) con `HwndSource.AddHook`. Es un evento, sin sondeo.
- Al recibirlo: leer texto con `Clipboard.GetText()` (con reintentos: el portapapeles puede estar bloqueado por otra app) y añadir a `ClipboardHistory` (Core): máximo 50 elementos, sin duplicados consecutivos.
- **Ignorar gestores de contraseñas:** si el portapapeles contiene el formato registrado `ExcludeClipboardContentFromMonitorProcessing` (o `CanIncludeInClipboardHistory` = 0), no guardar.
- Solo los **fijados** se guardan en disco; el resto vive en memoria.
- Acción: clic en un elemento → copiar de nuevo al portapapeles.

### 7. Notas rápidas (hito M8)
- Una caja de texto en el notch; al pulsar Guardar (o Ctrl+Enter) crea `yyyy-MM-dd_HHmmss.md` en la carpeta de notas.
- Lista de las últimas 10 notas; clic abre el archivo con la app predeterminada.
- Formato simple, compatible con Morrow (misma carpeta si la persona lo desea: opción "Carpeta de notas").
- Escritura **atómica**: escribir a `.tmp` y renombrar.

### 8. Ajustes, tema, arranque (hito M9)
- `AppSettings` (Core) serializado con `System.Text.Json`.
- Tema: `Dark | Light | System`. Cambiar reemplazando el `ResourceDictionary` de tokens en `Application.Resources.MergedDictionaries`. Detectar el tema de Windows leyendo `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` y suscribirse a `SystemEvents.UserPreferenceChanged`.
- Arranque con Windows: valor en `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (sin permisos de administrador).
- Módulos activables: cada widget tiene un booleano en `AppSettings`.

### 9. Arranque de la app y DI
`App.xaml.cs`: construir el contenedor de servicios → crear la bandeja → mostrar `NotchWindow`. Un solo proceso: usar un `Mutex` con nombre para evitar abrir dos copias.

## Flujo de datos (resumen)
```
SMTC (Windows) ──eventos──> SmtcMediaService ──> NowPlayingViewModel ──> Vista
                                   │
                                   └─ cambio de pista ──> LyricsService (caché → LRCLIB)
                                                              └─> LrcParser ─> LyricsSyncEngine ─> LyricsViewModel
Posición (interpolada) ───────────────────────────────────────────────────┘
```

## Rendimiento y calidad
- Sin hilos de sondeo. Todo por eventos.
- Liberar suscripciones al cerrar (`IDisposable`).
- Manejar excepciones en cada llamada a Windows (`try/catch` con registro), porque las APIs pueden fallar según el reproductor.
- Pruebas manuales por hito en `docs/03-ROADMAP.md`.
