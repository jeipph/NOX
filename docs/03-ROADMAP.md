# 03 — Hoja de ruta (ROADMAP)

Marca `[x]` cuando algo esté terminado **y verificado por la persona usuaria**. Un hito a la vez.

**MVP (v1) = M0 a M4 + M9 (solo el tema) + M2 (pantalla completa).** Después: M5, M6, M7, M8, M10, M11.

---

## M0 — Preparar el equipo (≈30–45 min)
Objetivo: poder compilar y ejecutar un programa C# en este PC.
- [x] Instalar el SDK de .NET 10 (10.0.403): `winget install Microsoft.DotNet.SDK.10` (cerrar y volver a abrir la terminal).
- [x] Verificar: `dotnet --version` muestra `10.x`.
- [x] Editor: **VS Code + extensión "C# Dev Kit"** (elegido 2026-10-27).
- [x] `git init` en `NotchBar/` y primer commit con estos documentos.
- [x] Crear `.gitignore` de .NET: `dotnet new gitignore`.

**Terminado cuando:** `dotnet --version` funciona y existe un repositorio git con los documentos.

## M1 — Esqueleto de la solución (≈1 h)
- [x] `dotnet new sln -n NotchBar`
- [x] Crear proyectos: `NotchBar.Core` (classlib net10.0), `NotchBar.Windows` (classlib `net10.0-windows10.0.19041.0`), `NotchBar.App` (WPF `net10.0-windows10.0.19041.0`), `NotchBar.Core.Tests` (xUnit).
- [x] Añadir referencias: App→Windows, App→Core, Windows→Core, Tests→Core.
- [x] Añadir paquetes NuGet de `02-ARCHITECTURE.md` (solo CommunityToolkit.Mvvm y DI por ahora).
- [x] `NotchWindow`: ventana sin borde, transparente, siempre encima, sin barra de tareas, pastilla negra redondeada de 220×34 arriba al centro.
- [x] Icono en la bandeja con menú: "Ajustes (pendiente)" y "Salir".
- [x] `Mutex` de instancia única.

**Terminado cuando:** al ejecutar, aparece la pastilla arriba al centro, no sale en la barra de tareas, y "Salir" cierra la app. `dotnet build` y `dotnet test` pasan.

## M2 — Comportamiento del notch (≈2 h)
- [x] Estilos extendidos por P/Invoke (`WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`).
- [x] Expandir al entrar el ratón y contraer al salir (con retraso de ~400 ms), con animación de 220 ms.
- [ ] Ajuste correcto con escalado de pantalla (DPI 100 %, 125 %, 150 %). Probar cambiando el escalado en Windows. *(Código listo; la prueba con 125/150 % se hace en M11.)*
- [x] Ocultar cuando una app está en pantalla completa (aviso `ABN_FULLSCREENAPP` de una AppBar sin espacio reservado + `FullscreenRules`).
- [x] Prueba: contenido de relleno ("Hola") dentro del panel expandido.

**Terminado cuando:** pasa el ratón → se despliega suave; sale → se pliega; abrir un video en pantalla completa lo oculta; al salir de pantalla completa vuelve.

## M2b — Estilos y modos de visibilidad (≈1.5 h) *(añadido tras la investigación)*
- [x] Tres estilos de la pastilla: **Notch** (pegado arriba), **Cápsula** (separada del borde), **Indicador** (puntito mínimo).
- [x] Modo "visible solo al pasar el ratón por el borde superior".
- [x] Tecla Esc contrae el notch.
- [x] Opción "No reservar la franja superior" (por defecto) frente a "Reservarla" con la API de barra de aplicaciones (`SHAppBarMessage`) — solo si se desea.

## M3 — Ahora suena (≈3 h)
- [x] Interfaz `IMediaService` + modelo `MediaInfo` en Core.
- [x] `SmtcMediaService` con los eventos descritos en la arquitectura.
- [x] Vista `NowPlayingView`: carátula, título, artista, botones ⏮ ⏯ ⏭, barra de progreso.
- [x] En el estado contraído mostrar título corto (máx. ~24 caracteres con "…").
- [ ] *(YouTube Music en Chrome verificado 2026-10-27: datos, carátula y los 3 botones. Falta MusicBee/carpetas locales.)* Probar con: YouTube Music en Chrome/Edge (y su app de escritorio si se usa) y música local con MusicBee / foobar2000 / Reproductor multimedia. Comprobar cuál publica bien en SMTC (MusicBee puede necesitar activar su integración con los controles de Windows; no confirmado).
- [x] Manejo de "no suena nada": mostrar texto discreto "Nada en reproducción".
- [x] **`FakeMediaService`** (canción y letra de mentira) para probar la interfaz sin abrir un reproductor; activable con el argumento `--fake` (idea del modo `--selftest` de Oyenza).

## M3b — Varios reproductores (≈1 h) *(añadido tras la investigación)*
- [ ] Listar todas las sesiones SMTC activas (`GetSessions()`), no solo la actual.
- [ ] Ajuste "Reproductor preferido" y cambio rápido desde el notch.

**Terminado cuando:** la información cambia sola al cambiar de canción y los 3 botones funcionan en YouTube Music.

## M4 — Letras sincronizadas (≈3 h)
- [ ] Tests primero: `LrcParserTests` (varias marcas, líneas vacías, metadatos) y `LyricsSyncEngineTests` (índice según posición y desfase).
- [ ] `LrcParser` y `LyricsSyncEngine` en Core.
- [ ] `LrclibClient` en Windows (HttpClient, User-Agent, tiempo de espera 5 s, manejo de 404).
- [ ] Validación de duración (±3 s) y caché en disco.
- [ ] `LyricsView`: línea actual resaltada y la anterior/siguiente atenuadas; desplazamiento suave.
- [ ] Ajuste de desfase −5…+5 s con botones.
- [ ] Opción para desactivar letras (privacidad).
- [ ] Importar un archivo `.lrc` manualmente para una canción sin letra. Opcional: carpeta de letras locales buscando `Artista - Título.lrc` (SMTC no da la ruta del archivo que suena, así que no se puede leer el `.lrc` "de al lado" automáticamente).
- [ ] Estado discreto "Sin letra sincronizada" (no es un error).

**Terminado cuando:** una canción con letra sincronizada la muestra en tiempo con el audio y los tests pasan.

## M5 — Vinilo (≈1.5 h)
- [ ] `VinylView`: carátula circular con giro, brazo opcional simple.
- [ ] Pausar animación si no suena o está contraído.
- [ ] Botón o clic para alternar entre Carátula / Vinilo / Letras.

## M6 — Volumen y reloj (≈1.5 h)
- [ ] `IVolumeService` con NAudio, control deslizante y botón de silencio.
- [ ] Reloj en la pastilla contraída (opcional).
- (Batería descartada: la app es para un PC de escritorio.)

## M6b — Enfoque (≈1.5 h) *(añadido tras la investigación)*
- [ ] Temporizador Pomodoro (25/5 por defecto, configurable) y cronómetro.
- [ ] **Keep-awake:** evitar que el PC se suspenda (`SetThreadExecutionState`).
- [ ] Temporizadores solo activos cuando se usan.

## M6c — Monitor del sistema (≈2 h) *(añadido tras la investigación)*
- [ ] CPU y RAM con `PerformanceCounter` o contadores de Windows; red por interfaz.
- [ ] Actualizar cada 1–2 s **solo** si la pestaña está visible y el notch expandido.
- [ ] GPU: evaluar (puede requerir contadores de rendimiento de GPU o LibreHardwareMonitor, cuya licencia hay que revisar).

## M6d — Salida de audio y mezclador (≈2 h) *(añadido tras la investigación)*
- [ ] Listar dispositivos de salida y cambiar el predeterminado (requiere una API no documentada de Windows; evaluar riesgo antes: si es frágil, solo mostrar el dispositivo actual).
- [ ] Volumen por aplicación con `AudioSessionManager` de NAudio.

## M7 — Portapapeles (≈2.5 h)
- [ ] Tests de `ClipboardHistory` (límite 50, sin duplicados consecutivos, fijar).
- [ ] `ClipboardMonitor` con `WM_CLIPBOARDUPDATE`, reintentos y exclusión de gestores de contraseñas.
- [ ] `ClipboardView`: lista, clic para copiar, botón fijar, botón borrar todo.

## M8 — Notas rápidas (≈1.5 h)
- [ ] `NotesView`: cuadro de texto, Ctrl+Enter guarda `.md` de forma atómica.
- [ ] Lista de las últimas 10 notas; clic abre el archivo.
- [ ] Carpeta configurable.

## M9 — Ajustes y temas (≈3 h)
- [ ] `AppSettings` + `JsonSettingsStore` (con valores por defecto y tolerancia a archivo dañado).
- [ ] `SettingsWindow`: tema (Oscuro/Claro/Sistema), módulos activables, tamaño del notch, desfase de letras por defecto, arranque con Windows, activar/desactivar letras.
- [ ] Cambio de tema **en vivo** sin reiniciar.
- [ ] Arranque con Windows por `HKCU\...\Run`.
- [ ] (Para el MVP basta con el tema; el resto puede esperar.)

## M9b — Atajos globales (≈1 h) *(añadido tras la investigación)*
- [ ] Atajos configurables con `RegisterHotKey`: mostrar/ocultar notch, play/pausa, letra −0.5 s, letra +0.5 s, reiniciar desfase (idea de LyricIsland).
- [ ] Detectar conflictos con atajos ya registrados y avisar.

## M10 — Empaquetado (≈2 h)
- [ ] `dotnet publish -c Release -r win-x64 --self-contained false` (requiere .NET Desktop Runtime) o `--self-contained true` (más pesado, sin requisitos).
- [ ] Probar el `.exe` en una carpeta limpia.
- [ ] Instalador con **Inno Setup** (script `installer/notchbar.iss`).
- [ ] Icono, nombre final, versión 0.1.0.
- [ ] Nota: el instalador sin firma digital hará que SmartScreen avise; es normal en proyectos personales.
- [ ] Investigar firma gratuita para código abierto (SignPath, usada por un proyecto similar). No confirmado que aplique.

## M11 — Pulido y calidad
- [ ] Revisar consumo (Administrador de tareas): objetivo **≤150 MB de RAM y ~0 % de CPU en reposo** con 1 monitor.
- [ ] Probar con un juego o video en pantalla completa: el notch se oculta y no baja los FPS.
- [ ] Probar con 1 y 2 monitores, y con escalados distintos.
- [ ] Revisar excepciones y mensajes de log.
- [ ] README de usuario con capturas.
- [ ] Revisar `THIRD-PARTY-NOTICES.md`.

---

## Decisiones pendientes (la persona usuaria debe elegir)
- [ ] Nombre definitivo de la app (provisional: NotchBar).
- [x] Editor: VS Code + C# Dev Kit.
- [ ] ¿Notas compartidas con Morrow (misma carpeta)?
- [ ] ¿Repositorio privado en GitHub? (opcional)

## Registro de sesiones
- **2026-10-26 — Planificación.** Se analizaron Kissa, Nock, Morrow y OrbitDrop. Se decidió: Programa 1 en C#/.NET 8/WPF, estilo minimalista blanco/negro con tema cambiable. Documentos creados. No hay código ni .NET instalado todavía. Siguiente paso: M0.
- **2026-10-26 — Investigación.** Se buscaron proyectos similares (ver `07-RESEARCH-SIMILAR-PROJECTS.md`). Cambios: se quitó la batería; se pasó a **.NET 10** (.NET 8 termina soporte en nov. 2026); se añadieron M2b, M3b, M6b, M6c, M6d, M9b; objetivo de RAM ≤150 MB. Instalar con `winget install Microsoft.DotNet.SDK.10`.
- **2026-10-27 — Clones y reparto.** Se estudiaron boring.notch, BetterLyrics, WindowsMediaController (MIT), Poricom y Clone-Wars (ver `07-RESEARCH…` §6). Se creó `../REPARTO-MODELOS.md` (qué modelo hace cada tarea). Pendiente: decidir si usar `Dubya.WindowsMediaController` o `Windows.Media.Control` directo. Siguiente paso: M0.
- **2026-10-27 — M0 terminado.** .NET SDK 10.0.403 instalado, VS Code + C# Dev Kit, telemetría de la CLI desactivada (`DOTNET_CLI_TELEMETRY_OPTOUT=1`). Los proyectos se movieron de OneDrive a `C:\Dev\NotchBar` y `C:\Dev\Marginalia` (OneDrive bloquea `bin/obj`). Repositorio git creado. Siguiente paso: M1.
- **2026-10-27 — M1 y decisión de música.** Esqueleto creado (4 proyectos, notch 220×34, bandeja, instancia única); compila sin avisos, 54 MB RAM. Falta la verificación visual de la persona usuaria para marcar M1. Música: opción A (YouTube Music + reproductor externo para carpetas locales, todo vía SMTC con `Windows.Media.Control`).
- **2026-10-27 — M2b y M3.** Estilos, modo invisible, Esc y franja reservada probados con UI Automation y capturas. M3 funciona con YouTube Music en Chrome (datos, carátula, botones). Errores encontrados en las pruebas y corregidos: carátula con el logo de Chrome, parpadeo de "Nada en reproducción" al cambiar de canción (Chrome cierra y reabre su sesión; ahora se esperan 1,5 s) y notch desplazado tras quitar la franja reservada (ahora se recoloca con `WM_SETTINGCHANGE`). Pendiente: probar música local con MusicBee. Siguiente: M3b.
